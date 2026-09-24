using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerceApi.Data;
using ECommerceApi.Dtos;
using ECommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ECommerceApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
    {
        // 1. Validar si el usuario ya existe en la BD
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new BadHttpRequestException("El correo electrónico ya está registrado.");
        }

        // 2. Hash de contraseña seguro usando BCrypt (Salt integrado dinámicamente)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Instanciar la entidad del modelo
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "Customer" // Rol predeterminado para nuevos registros
        };

        // 4. Generar Tokens
        var token = GenerateJwtToken(user, out var expiration);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "7")
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new AuthResponseDto(token, refreshToken, expiration, user.Role, user.Email);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        // 1. Buscar usuario por Email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            throw new BadHttpRequestException("Credenciales inválidas.");
        }

        // 2. Verificar hash de contraseña con BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new BadHttpRequestException("Credenciales inválidas.");
        }

        // 3. Generar nuevo Access Token y Refresh Token
        var token = GenerateJwtToken(user, out var expiration);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "7")
        );

        await _context.SaveChangesAsync();

        return new AuthResponseDto(token, refreshToken, expiration, user.Role, user.Email);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        // 1. Extraer los Claims del Access Token caducado
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new BadHttpRequestException("Token de acceso o Refresh Token inválido.");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new BadHttpRequestException("Token inválido.");
        }

        // 2. Obtener usuario de la BD y validar el Refresh Token
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new BadHttpRequestException("Refresh Token expirado o no válido.");
        }

        // 3. Generar par de tokens nuevo (Rotación de Refresh Tokens)
        var newAccessToken = GenerateJwtToken(user, out var expiration);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "7")
        );

        await _context.SaveChangesAsync();

        return new AuthResponseDto(newAccessToken, newRefreshToken, expiration, user.Role, user.Email);
    }

    #region Métodos Privados de Criptografía y JWT

    private string GenerateJwtToken(User user, out DateTime expiration)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("Falta JwtSettings:SecretKey en appsettings.json");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims: Información incrustada dentro del payload del JWT (Legible pero Inalterable por la Firma)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var minutes = double.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");
        expiration = DateTime.UtcNow.AddMinutes(minutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private static string GenerateRefreshToken()
    {
        // Cadena aleatoria criptográficamente segura (32 bytes = 256 bits)
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false // Ignorar expiración para poder leer los Claims del Token caducado
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Token no válido.");
        }

        return principal;
    }

    #endregion
}
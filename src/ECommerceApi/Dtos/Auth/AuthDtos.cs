namespace ECommerceApi.Dtos;

public record RegisterDto(
    string Email, 
    string Password
);

public record LoginDto(
    string Email, 
    string Password
);

public record AuthResponseDto(
    string Token, 
    string RefreshToken, 
    DateTime Expiration, 
    string Role, 
    string Email
);

public record RefreshTokenRequestDto(
    string AccessToken, 
    string RefreshToken
);
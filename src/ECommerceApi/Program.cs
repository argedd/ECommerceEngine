using System.Text;
using ECommerceApi.Data;
using ECommerceApi.Repositories;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


//middleware
app.UseGlobalExceptionMiddleware();

// 1. Inyección de Controladores de API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Configuración de Base de Datos Relacional (EF Core & Dapper)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// DapperContext se registra como Singleton ya que solo mantiene la cadena de conexión
builder.Services.AddSingleton<DapperContext>();

// 3. Configuración de Conexión a Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// 4. Inyección de Servicios de Aplicación (Scoped por Request HTTP)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();

//4.1 add servicio del carrito 
// Inyección de Servicios de Carrito
builder.Services.AddScoped<ICartService, CartService>();
//inyeccion del servicio de ordenes 
builder.Services.AddScoped<IOrderService, OrderService>();

// 5. Configuración de Autenticación JWT Bearer
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("Falta JwtSettings:SecretKey en appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Cambiar a true en producción (HTTPS obligado)
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Elimina la tolerancia por defecto de 5 minutos al evaluar la expiración
    };
});

builder.Services.AddAuthorization();

// 6. Configuración de Swagger UI con Soporte para Token Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerce Engine API",
        Version = "v1",
        Description = "Backend Enterprise de Comercio Electrónico de Alto Rendimiento (.NET 10)"
    });

    // Permite probar endpoints con cerrojo introduciendo el token en la interfaz de Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Encabezado de autorización JWT usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 6.5 Sembrar datos iniciales de prueba al arrancar (migraciones + categorías + productos)
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedDataAsync(scope.ServiceProvider);
}

// 7. Pipeline de Procesamiento HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// El middleware de autenticación debe ejecutarse estrictamente ANTES que el de autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Ejecutar el Seeder de datos iniciales
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DataSeeder.SeedDataAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones o sembrar los datos iniciales.");
    }
}

app.Run();
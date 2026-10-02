# ECommerce Engine API

Backend empresarial de comercio electrónico construido con **.NET 10** (ASP.NET Core Web API). Expone una API REST con autenticación JWT, catálogo paginado con Dapper, carrito de compras en Redis y checkout transaccional con EF Core.

---

## Inicialización rápida (en otro equipo)

> Prerrequisito: [.NET SDK 10](https://dotnet.microsoft.com/download) y [Docker](https://www.docker.com/products/docker-desktop/).

```bash
# 1. Clonar el repositorio
git clone <URL_DEL_REPOSITORIO> ECommerceEngine
cd ECommerceEngine

# 2. Levantar SQL Server (puerto 1433) y Redis (puerto 6379)
docker compose up -d

# 3. Restaurar dependencias y compilar
dotnet restore src/ECommerceApi
dotnet build src/ECommerceApi

# 4. Ejecutar la API
dotnet run --project src/ECommerceApi
```

Al arrancar, la aplicación **aplica las migraciones pendientes y siembra datos de prueba automáticamente** (3 categorías y 4 productos). Abrí la documentación interactiva en:

- **Swagger UI:** http://localhost:5249/swagger
- **Base URL HTTP:** http://localhost:5249
- **Base URL HTTPS:** https://localhost:7059

---

## Configuración

### `src/ECommerceApi/appsettings.json`

| Clave | Valor por defecto | Descripción |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True` | SQL Server (coincide con `docker-compose.yml`) |
| `ConnectionStrings:Redis` | `localhost:6379` | Redis para el carrito |
| `JwtSettings:SecretKey` | Clave de desarrollo (68 chars) | Firma HS256 de los tokens |
| `JwtSettings:Issuer` | `ECommerceApi` | Emisor del JWT |
| `JwtSettings:Audience` | `ECommerceClients` | Audiencia del JWT |
| `JwtSettings:ExpirationInMinutes` | `60` | Vigencia del access token |
| `JwtSettings:RefreshTokenExpirationInDays` | `7` | Vigencia del refresh token |

> ⚠️ **Importante:** la `SecretKey` está hardcodeada para desarrollo. En equipos compartidos o producción, mojala a *user-secrets* o variables de entorno:

```bash
dotnet user-secrets init --project src/ECommerceApi
dotnet user-secrets set "JwtSettings:SecretKey" "<tu_clave_secreta>" --project src/ECommerceApi
```

### Migraciones (EF Core)

| Acción | Comando |
|---|---|
| Aplicar migraciones manualmente | `dotnet ef database update --project src/ECommerceApi` |
| Crear una migración nueva | `dotnet ef migrations add <Nombre> --project src/ECommerceApi` |
| Ver migraciones aplicadas | `dotnet ef migrations list --project src/ECommerceApi` |

Las migraciones se aplican solas al arrancar (`DataSeeder`); el comando manual solo es necesario si desactivás ese comportamiento.

---

## Arquitectura

### Esquema de comunicación entre componentes

```
┌─────────────┐   HTTPS + JSON + Bearer JWT     ┌─────────────────────────────────┐
│   Cliente   │ ──────────────────────────────▶ │      ASP.NET Core (.NET 10)     │
│ (Swagger,   │ ◀────────────────────────────── │       Controllers (API REST)    │
│  app web)   │        JSON responses           │  Auth · Catalog · Cart · Orders │
└─────────────┘                                 └───────────────┬─────────────────┘
                                                               │ Middleware JWT
                                                               ▼
                                          ┌──────────────────────────────────────┐
                                          │   Servicios de aplicación (Scoped)   │
                                          │   AuthService   CartService          │
                                          │   OrderService                       │
                                          └───────┬──────────────────┬───────────┘
                                                  │                  │
                              ┌───────────────────▼───┐    ┌────────▼────────────┐
                              │   Repositorio (Dapper) │    │       Redis         │
                              │ ProductReadRepository │    │  Carritos en JSON   │
                              │  (lecturas catálogo)  │    │  clave cart:{userId}│
                              └───────────┬───────────┘    │  TTL: 30 días       │
                                          │                └─────────────────────┘
                                          │ SQL
                              ┌───────────▼───────────┐
                              │       SQL Server       │
                              │  EF Core (escrituras)  │
                              │  Dapper (lecturas)     │
                              │  Users · Categories    │
                              │  Products · Orders     │
                              └────────────────────────┘
```

### Cómo se comunican los componentes

| Flujo | Componentes | Detalle |
|---|---|---|
| **Registro / Login** | `AuthController` → `AuthService` → `ApplicationDbContext` (EF Core) → SQL Server | BCrypt hashea y verifica el password. Emite JWT (HS256, claims: `NameIdentifier`, `Email`, `Role`, `Jti`) + refresh token de 256 bits rotativo, persistido en el usuario. |
| **Catálogo** | `CatalogController` → `ProductReadRepository` (Dapper) → SQL Server | Dapper ejecuta SQL con multi-mapping `Product` + `Category`. La paginación usa `COUNT` + `OFFSET/FETCH` en una sola ida a la base. |
| **Carrito** | `CartController` → `CartService` → Redis + `ProductReadRepository` | El carrito vive en Redis como JSON (`cart:{userId}`, TTL 30 días). Antes de escribir, `CartService` valida existencia, precio oficial y stock consultando SQL vía Dapper. |
| **Checkout** | `OrdersController` → `OrderService` → EF Core + `CartService` | Transacción atómica: descuenta inventario, crea `Order` + `OrderDetails`, commitea, y limpia el carrito de Redis. Cualquier fallo revierte todo (`RollbackAsync`). |
| **Historial / detalle** | `OrdersController` → `OrderService` → EF Core | Carga órdenes del usuario con `Include(OrderDetails).ThenInclude(Product)` para resolver el nombre del producto. |
| **Seed inicial** | `DataSeeder` (al arrancar) → EF Core → SQL Server | Aplica migraciones pendientes y siembra categorías/productos solo si la tabla `Categories` está vacía (idempotente). |

### Estructura del proyecto

```
ECommerceEngine/
├── docker-compose.yml            # SQL Server (azure-sql-edge) + Redis
├── ECommerceEngine.slnx
└── src/ECommerceApi/
    ├── Controllers/
    │   ├── Auth/AuthController.cs        # register, login, refresh-token
    │   ├── Catalog/CatalogController.cs  # productos paginados, por ID, por categoría
    │   ├── Order/OrdersController.cs     # checkout, historial, detalle (Authorize)
    │   └── Products/CartController.cs    # carrito completo (Authorize)
    ├── Services/
    │   ├── Auth/                         # AuthService (BCrypt + JWT + refresh token)
    │   ├── Cart/                         # CartService (Redis + validación de stock)
    │   └── Order/                        # OrderService (transacción atómica)
    ├── Repositories/
    │   ├── IProductReadRepository.cs
    │   └── ProductReadRepository.cs      # Dapper, multi-mapping Product/Category
    ├── Data/
    │   ├── ApplicationDbContext.cs       # EF Core (5 DbSets)
    │   ├── DapperContext.cs              # SqlConnection por llamada
    │   └── DataSeeder.cs                 # migraciones + seed automático
    ├── Models/                           # User, Category, Product, Order, OrderDetail
    ├── Dtos/                             # Auth, Catalog (Products), Order
    ├── Migrations/                       # InitialCreate, AddOrdersAndOrderItems
    ├── Middleware/
    └── Program.cs                        # DI, JWT, Swagger, pipeline HTTP
```

### Stack y decisiones

| Capa | Tecnología | Rol |
|---|---|---|
| Framework | .NET 10 (ASP.NET Core Web API) | Host HTTP, DI, middleware |
| ORM | EF Core 10 + SQL Server | Escrituras y modelo relacional (Users, Orders) |
| Micro-ORM | Dapper 2.1.89 | Lecturas del catálogo (rendimiento) |
| Cache/estado | Redis 7 (StackExchange.Redis 3.3.1) | Carrito de compras con TTL |
| Auth | JWT Bearer (HS256) + BCrypt.Net-Next 4.2.0 | Autenticación y hash de passwords |
| Docs | Swashbuckle (Swagger) 10.2.3 | Documentación interactiva |
| Infra dev | Docker Compose (`azure-sql-edge`, `redis:alpine`) | SQL Server + Redis locales |

---

## Endpoints de la API

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| `POST` | `/api/Auth/register` | — | Registro de cliente (BCrypt + JWT) |
| `POST` | `/api/Auth/login` | — | Login y emisión de tokens |
| `POST` | `/api/Auth/refresh-token` | — | Renovación de tokens |
| `GET` | `/api/Catalog?pageNumber=1&pageSize=10&search=` | — | Productos activos paginados |
| `GET` | `/api/Catalog/{id}` | — | Producto por ID |
| `GET` | `/api/Catalog/category/{categoryId}` | — | Productos de una categoría |
| `GET` | `/api/Cart` | ✔ | Carrito del usuario autenticado |
| `POST` | `/api/Cart/items` | ✔ | Agregar producto al carrito |
| `PUT` | `/api/Cart/items/{productId}` | ✔ | Actualizar cantidad |
| `DELETE` | `/api/Cart/items/{productId}` | ✔ | Quitar producto del carrito |
| `DELETE` | `/api/Cart` | ✔ | Vaciar carrito |
| `POST` | `/api/Orders/checkout` | ✔ | Checkout atómico del carrito |
| `GET` | `/api/Orders` | ✔ | Historial de órdenes |
| `GET` | `/api/Orders/{id}` | ✔ | Detalle de una orden |

---

## Verificación (checklist)

- [ ] `docker compose up -d` → contenedores `ecommerce_sqlserver` y `ecommerce_redis` levantados
- [ ] `dotnet build src/ECommerceApi` → 0 errores / 0 warnings
- [ ] `dotnet run --project src/ECommerceApi` → app escuchando en `http://localhost:5249`
- [ ] `GET /api/Catalog` → **200** con 4 productos y `totalCount: 4`
- [ ] `POST /api/Auth/register` → **200** con `token` y `refreshToken`
- [ ] `POST /api/Cart/items` con el Bearer token → **200** con el carrito actualizado
- [ ] `POST /api/Orders/checkout` → **200** con la orden creada y carrito vacío
- [ ] Swagger cargando en `/swagger`

---

## Notas para producción

- Mover `JwtSettings:SecretKey` a variables de entorno / secret manager (nunca en el repo).
- Activar `RequireHttpsMetadata = true` en la configuración JWT (`Program.cs`).
- El seeder corre al arranque en cualquier entorno; desactivarlo en producción o condicionarlo a `IsDevelopment()`.
- Los datos sembrados (`DataSeeder`) son solo de prueba: categorías "Electrónica y Tecnología", "Ropa y Moda", "Hogar y Cocina" y 4 productos.
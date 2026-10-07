# DecoupledPayments – Backend (.NET 10, Onion Architecture)

Solución independiente: `backend.sln`.

## Capas
- `1.Domain` – `TransactionContext`, estados y reglas.
- `2.Application` – casos de uso (`PaymentApplicationService`), DTOs e interfaces.
- `3.Infrastructure` – EF Core (`PaymentsDbContext`) con **PostgreSQL o SQL Server**, anti-replay en base de datos, emisión JWT RS256.
- `4.Presentation/WebApi` – controlador, `SingleUseTokenMiddleware`, autenticación JWT, CORS, OpenAPI, health checks.

## Base de datos (obligatoria)
Elija el motor por configuración:
```bash
# PostgreSQL (por defecto)
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=decoupledpayments;Username=dp_user;Password=dp_pass_dev'

# SQL Server
export Database__Provider=SqlServer
export ConnectionStrings__Default='Server=localhost,1433;Database=decoupledpayments;User Id=sa;Password=Dev_Passw0rd_2026;TrustServerCertificate=True'
```
El esquema se crea al arrancar (`Database__Initialization=EnsureCreated`) o con los scripts de `../database`.

## Ejecutar
```bash
dotnet restore backend.sln
dotnet build backend.sln
dotnet run --project src/4.Presentation/DecoupledPayments.WebApi --launch-profile https
```
API: `https://localhost:7100` · Salud: `/healthz`, `/healthz/ready` · Contrato: `/openapi/v1.json`

Guía completa: `../docs/02-backend-paso-a-paso.md` y `../docs/07-capa-de-datos.md`.

## Endpoints (`/api/v1/decoupled-payments`)
| Método | Ruta | Notas |
|---|---|---|
| POST | `/initiate` | 201. Crea la transacción (OTP simulado `123456`). |
| POST | `/verify-otp` | 200. Devuelve `transaction_token` (JWT 60 s). |
| POST | `/execute` | Bearer `transaction_token` + `X-Idempotency-Key`. Reuso del token → 409 `TOKEN_ALREADY_USED`. |

## Notas de producción
- Defina `Jwt__PrivateKeyPem` (misma clave en todas las réplicas) o use KMS/HSM + JWKS.
- Reemplace el OTP simulado por TOTP/Push/SMS y no lo persista en claro.
- Use un usuario de BD de mínimos privilegios y conexión cifrada.

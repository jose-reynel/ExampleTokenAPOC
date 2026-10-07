# 2. Backend paso a paso (`backend/backend.sln`)

El backend necesita una base de datos **PostgreSQL o SQL Server** (ver `07-capa-de-datos.md`). Ya no guarda nada en memoria local.

## 2.1 Levantar una base de datos de desarrollo
Elija una:
```bash
# PostgreSQL
docker run -d --name dp-postgres -p 5432:5432 \
  -e POSTGRES_DB=decoupledpayments -e POSTGRES_USER=dp_user -e POSTGRES_PASSWORD=dp_pass_dev \
  postgres:16-alpine

# SQL Server (la contraseña debe cumplir la política de complejidad)
docker run -d --name dp-sqlserver -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD='Dev_Passw0rd_2026' \
  mcr.microsoft.com/mssql/server:2022-latest
```
Al arrancar, el backend crea el esquema solo (`Database:Initialization = EnsureCreated`).

## 2.2 Restaurar y compilar
```bash
cd backend
dotnet restore backend.sln
dotnet build backend.sln -c Release
```
Si el restore falla por versión de paquete, ajuste los números en los `.csproj`
(`Microsoft.AspNetCore.*` y `Microsoft.EntityFrameworkCore.SqlServer` = 10.0.x; `Npgsql.EntityFrameworkCore.PostgreSQL` = 10.0.x; `Microsoft.IdentityModel.JsonWebTokens` = 8.x).

## 2.3 Ejecutar
**Con PostgreSQL** (valores por defecto de `appsettings.json`):
```bash
dotnet run --project src/4.Presentation/DecoupledPayments.WebApi --launch-profile https
```
**Con SQL Server** (por variables de entorno):
```bash
export Database__Provider=SqlServer
export ConnectionStrings__Default='Server=localhost,1433;Database=decoupledpayments;User Id=sa;Password=Dev_Passw0rd_2026;TrustServerCertificate=True'
dotnet run --project src/4.Presentation/DecoupledPayments.WebApi --launch-profile https
```
- API: `https://localhost:7100`
- Liveness: `/healthz` · Readiness (incluye BD): `/healthz/ready`
- Contrato OpenAPI (solo en Development): `https://localhost:7100/openapi/v1.json`

## 2.4 Configuración
| Clave (variable de entorno) | Por defecto | Descripción |
|---|---|---|
| `Database:Provider` (`Database__Provider`) | `PostgreSql` | `PostgreSql` o `SqlServer` |
| `ConnectionStrings:Default` (`ConnectionStrings__Default`) | localhost PostgreSQL | Cadena de conexión del motor elegido |
| `Database:Initialization` | `EnsureCreated` | `EnsureCreated` crea el esquema; `None` lo deja al DBA (scripts en `/database`) |
| `Database:MaxRetries` / `RetryDelaySeconds` | `10` / `3` | Reintentos al inicializar si la BD aún no responde |
| `Jwt:Issuer` / `Jwt:Audience` | ver `appsettings.json` | Emisor y audiencia del token |
| `Jwt:TransactionTokenTtlSeconds` | `60` | Vida del `transaction_token` |
| `Jwt:KeyId` | `apoc-key-1` | `kid` de la clave de firma |
| `Jwt:PrivateKeyPem` (`Jwt__PrivateKeyPem`) | *(vacío)* | Clave RSA privada PEM compartida; vacío = clave efímera |
| `Cors:AllowedOrigins` | `https://localhost:7200`, `http://localhost:5200` | Orígenes del frontend permitidos |

Por variables de entorno se usa `__` como separador: `Cors__AllowedOrigins__0=http://localhost:5200`.

## 2.5 Probar el flujo completo con curl
> `-k` acepta el certificado de desarrollo. Con `jq` se extraen los valores automáticamente.

```bash
API=https://localhost:7100/api/v1/decoupled-payments

# 1) Iniciar transacción
INIT=$(curl -sk -X POST $API/initiate -H "Content-Type: application/json" -d '{
  "merchant_id":"MERCHANT_PSE_8892",
  "user_document_id":"1018234567",
  "amount":250000,
  "currency":"COP",
  "callback_url":"https://comercio.com/pago/callback"}')
echo $INIT
TX=$(echo $INIT | jq -r .transaction_id)
ST=$(echo $INIT | jq -r .session_token)

# 2) Verificar OTP (simulado: 123456)
OTP=$(curl -sk -X POST $API/verify-otp -H "Content-Type: application/json" \
  -d "{\"transaction_id\":\"$TX\",\"session_token\":\"$ST\",\"otp_code\":\"123456\"}")
echo $OTP
TOKEN=$(echo $OTP | jq -r .transaction_token)

# 3) Ejecutar el pago (primer uso: 200)
curl -sk -X POST $API/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Idempotency-Key: $(uuidgen)" \
  -H "Content-Type: application/json" \
  -d "{\"transaction_id\":\"$TX\",\"source_account_id\":\"ACC_SAVINGS_9812\"}"

# 4) Replay con el MISMO token (debe dar 409 TOKEN_ALREADY_USED)
curl -sk -i -X POST $API/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Idempotency-Key: $(uuidgen)" \
  -H "Content-Type: application/json" \
  -d "{\"transaction_id\":\"$TX\",\"source_account_id\":\"ACC_SAVINGS_9812\"}"
```

## 2.6 Verificar en la base de datos
```sql
SELECT transaction_id, status, amount, currency, created_at FROM payments.transactions ORDER BY created_at DESC;
SELECT jti, expires_at FROM payments.used_tokens;   -- un registro por token consumido
```
(PostgreSQL y SQL Server usan los mismos nombres de tabla y columna.)

## 2.7 Códigos de respuesta
| HTTP | `error` | Causa |
|---|---|---|
| 201 | – | Transacción creada |
| 400 | `INVALID_REQUEST` / `MISSING_IDEMPOTENCY_KEY` | Datos inválidos (OTP incorrecto, monto ≤ 0, campos fuera de rango, falta `X-Idempotency-Key`) |
| 401 | (sin cuerpo) / `MISSING_BEARER_TOKEN` / `EXPIRED_TOKEN` | Token ausente, inválido o expirado |
| 403 | `TOKEN_TRANSACTION_MISMATCH` | El token pertenece a otra transacción |
| 404 | `TRANSACTION_NOT_FOUND` | `transaction_id` inexistente |
| 409 | `TOKEN_ALREADY_USED` | Replay del `transaction_token` |
| 409 | `INVALID_TRANSACTION_STATE` | Transacción expirada o en estado no válido |
| 409 | `CONCURRENT_UPDATE` | Otra operación modificó la transacción a la vez; reintente |
| 503 | – | `/healthz/ready`: sin conexión a la base de datos |

## 2.8 Capas del código
- **Domain:** `TransactionContext` (estados, regla del OTP, expiración).
- **Application:** `PaymentApplicationService`, DTOs, interfaces.
- **Infrastructure:** `PaymentsDbContext` (EF Core), repositorio y anti-replay sobre la BD, `JwtTokenService` (RS256), `JwtKeyProvider`, purga de tokens vencidos.
- **WebApi:** controlador, `SingleUseTokenMiddleware` (consume el `jti` tras validar el JWT), manejo de errores, CORS, health checks.

> Con varias réplicas del backend todas deben compartir la misma clave de firma (`Jwt__PrivateKeyPem`). Ver `07-capa-de-datos.md` y `06-openshift.md`.

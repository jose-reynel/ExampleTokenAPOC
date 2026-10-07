# ExampleTokenAPOC – Pago desacoplado con token de un solo uso (.NET 10)

APOC de pago desacoplado: iniciación → verificación de OTP → ejecución con un `transaction_token` (JWT RS256, 60 s, un solo uso) y control anti-replay. Persistencia en **PostgreSQL o SQL Server** (EF Core), frontend Blazor WebAssembly, listo para Docker y OpenShift.

| Carpeta | Contenido |
|---|---|
| `backend/` | `backend.sln` – API ASP.NET Core 10 (arquitectura cebolla) |
| `frontend/` | `frontend.sln` – portal Blazor WebAssembly |
| `database/` | Scripts SQL de esquema (PostgreSQL y SQL Server) |
| `openshift/` | Manifiestos y scripts de build/despliegue |
| `docs/` | Guías paso a paso, manuales técnico y funcional (md y Word) |
| `docker-compose.yml`, `env/` | Ejecución local con Docker (perfiles `postgres` / `sqlserver`) |

## Inicio rápido (Docker)
```bash
docker compose --env-file env/postgres.env --profile postgres up -d --build
# Frontend: http://localhost:5200/payment-gateway   ·   Backend: http://localhost:5100
```
OTP simulado: `123456`. Más detalle en [`docs/README.md`](docs/README.md).

> Proyecto de prueba de concepto: las contraseñas de `env/` y del compose son **solo de desarrollo**.

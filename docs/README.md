# Documentación – Pago Desacoplado (APOC) · .NET 10

Guía paso a paso para usar, probar y publicar en Docker el backend y el frontend.

| # | Documento | Contenido |
|---|---|---|
| 1 | [Requisitos e instalación](01-requisitos-e-instalacion.md) | Herramientas, estructura del repositorio, puertos |
| 2 | [Backend paso a paso](02-backend-paso-a-paso.md) | Compilar, ejecutar, configurar y probar la API |
| 3 | [Frontend paso a paso](03-frontend-paso-a-paso.md) | Ejecutar el portal Blazor y recorrer el flujo de pago |
| 4 | [Publicación en Docker](04-docker-publicacion.md) | Imágenes, docker compose, registro y variables |
| 5 | [Solución de problemas](05-solucion-de-problemas.md) | Errores frecuentes y cómo resolverlos |
| 6 | [Despliegue en OpenShift](06-openshift.md) | Build, manifiestos, Routes TLS, ConfigMaps, Secrets y operación |
| 7 | [Capa de datos (ORM)](07-capa-de-datos.md) | EF Core con PostgreSQL o SQL Server, esquema, concurrencia, cambio de motor |
| – | [Manual funcional](manuales/manual-funcional.md) | Proceso, casos de uso, reglas de negocio, pruebas de aceptación |
| – | [Manual técnico](manuales/manual-tecnico.md) | Arquitectura, API, seguridad, configuración, decisiones y limitaciones |

> **Versiones en Word** de los manuales (con diagramas e índice): `manuales/Manual_Funcional_APOC_Pago_Desacoplado.docx` y `manuales/Manual_Tecnico_APOC_Pago_Desacoplado.docx`.

## Estructura del proyecto
```
.
├── docker-compose.yml
├── openshift/                 ← manifiestos y scripts (build.sh · deploy.sh)
├── database/                  ← scripts SQL (postgresql · sqlserver)
├── env/                       ← variables de ejemplo para Docker Compose
├── docs/                      ← esta documentación (+ manuales/)
├── backend/
│   ├── backend.sln
│   ├── Dockerfile
│   └── src/ (1.Domain · 2.Application · 3.Infrastructure · 4.Presentation/WebApi)
└── frontend/
    ├── frontend.sln
    ├── Dockerfile
    ├── docker/ (nginx.conf · 40-appsettings.sh)
    └── src/DecoupledPayments.Client
```
`backend.sln` y `frontend.sln` son independientes: el frontend no referencia código del backend, solo consume su API HTTP.

## Flujo de negocio (resumen)
1. `POST /initiate` crea la transacción y devuelve `transaction_id` + `session_token`.
2. `POST /verify-otp` valida el OTP y emite el `transaction_token` (JWT RS256, 60 s, un solo uso).
3. `POST /execute` ejecuta el débito con `Authorization: Bearer <transaction_token>` y `X-Idempotency-Key`.
4. Reusar el mismo token devuelve **409 `TOKEN_ALREADY_USED`** (control anti-replay).

Todo el estado (transacciones y tokens consumidos) se guarda en **PostgreSQL o SQL Server**, elegido por configuración (`Database__Provider`).

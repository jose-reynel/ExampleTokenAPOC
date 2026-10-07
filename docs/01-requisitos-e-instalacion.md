# 1. Requisitos e instalación

## Herramientas
| Herramienta | Versión | Para qué |
|---|---|---|
| .NET SDK | 10.0 (≥ 10.0.100) | Compilar y ejecutar sin Docker |
| Docker Desktop / Docker Engine + Compose v2 | reciente | Publicación en contenedores |
| PostgreSQL 13+ **o** SQL Server 2019+ | reciente | Base de datos (puede ser un contenedor de desarrollo) |
| Git | cualquiera | Control de versiones |
| `curl` (y opcionalmente `jq`) | cualquiera | Probar la API desde terminal |
| IDE (opcional) | Visual Studio 2026 / VS Code / Rider | Edición |

Verifique las versiones:
```bash
dotnet --version        # debe empezar por 10.
docker --version
docker compose version
```

## Descomprimir y ubicarse
```bash
unzip DecoupledPayments-APOC.zip
cd apoc
```

## Certificado HTTPS de desarrollo (solo ejecución sin Docker)
```bash
dotnet dev-certs https --trust
```

## Puertos usados
| Componente | Sin Docker | Con Docker Compose |
|---|---|---|
| Backend | https://localhost:7100 · http://localhost:5100 | http://localhost:5100 |
| PostgreSQL / SQL Server (desarrollo) | 5432 / 1433 | 5432 / 1433 (perfiles del compose) |
| Frontend | https://localhost:7200 · http://localhost:5200 | http://localhost:5200 |

> Si algún puerto está ocupado, cámbielo en `Properties/launchSettings.json` (sin Docker) o en `docker-compose.yml` (con Docker). Recuerde mantener alineado el CORS del backend y `ApiBaseUrl` del frontend.

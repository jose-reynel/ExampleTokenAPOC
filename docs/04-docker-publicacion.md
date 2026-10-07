# 4. Publicación en Docker

## 4.1 Qué se genera
| Imagen / servicio | Base | Contenido | Puerto interno |
|---|---|---|---|
| `decoupled-payments-backend` | `dotnet/aspnet:10.0` | API publicada (usuario no root) | 8080 |
| `decoupled-payments-frontend` | `nginx-unprivileged:1.27-alpine` | Archivos estáticos de Blazor WASM (sin root) | 8080 |
| `postgres` *(perfil `postgres`)* | `postgres:16-alpine` | Base de datos de desarrollo | 5432 |
| `sqlserver` *(perfil `sqlserver`)* | `mssql/server:2022-latest` | Base de datos de desarrollo | 1433 |

Los Dockerfiles son *multi-stage* y las imágenes de la aplicación corren **sin root y en puertos > 1024**, por lo que funcionan igual en Docker y en OpenShift (ver `06-openshift.md`).

## 4.2 Opción A – Docker Compose (recomendada)
Desde la raíz del proyecto. Elija el motor con un perfil y su archivo de entorno:
```bash
# PostgreSQL
docker compose --env-file env/postgres.env  --profile postgres  up -d --build

# SQL Server
docker compose --env-file env/sqlserver.env --profile sqlserver up -d --build
```
- Frontend: http://localhost:5200/payment-gateway
- Backend: http://localhost:5100 (salud: `/healthz`, `/healthz/ready`)

El backend reintenta la conexión hasta que la base de datos esté disponible (`Database:MaxRetries`), por lo que no es necesario esperar a que el motor arranque.

Ver registros, detener y limpiar:
```bash
docker compose logs -f backend
docker compose --env-file env/postgres.env --profile postgres down       # conserva los datos (volumen)
docker compose --env-file env/postgres.env --profile postgres down -v    # borra también los datos
```
> Los archivos `env/*.env` contienen contraseñas **solo de desarrollo**. No los use en entornos reales.

### Usar una base de datos externa (sin perfiles)
```bash
export DB_PROVIDER=SqlServer
export DB_CONNECTION='Server=sql.miempresa.com,1433;Database=decoupledpayments;User Id=dp_app;Password=***;Encrypt=True'
export DB_INITIALIZATION=None      # el DBA aplicó database/sqlserver/001_create_schema.sql
docker compose up -d --build backend frontend
```

## 4.3 Opción B – Imágenes por separado
```bash
# Backend (ejemplo con PostgreSQL ya disponible en db.local)
docker build -t decoupled-payments-backend:1.1.0 ./backend
docker run -d --name dp-backend -p 5100:8080 \
  -e Database__Provider=PostgreSql \
  -e "ConnectionStrings__Default=Host=db.local;Port=5432;Database=decoupledpayments;Username=dp_user;Password=***" \
  -e Cors__AllowedOrigins__0=http://localhost:5200 \
  decoupled-payments-backend:1.1.0

# Frontend
docker build -t decoupled-payments-frontend:1.0.0 ./frontend
docker run -d --name dp-frontend -p 5200:8080 \
  -e API_BASE_URL=http://localhost:5100/ \
  decoupled-payments-frontend:1.0.0
```

## 4.4 Variables de entorno
**Backend**
| Variable | Ejemplo | Descripción |
|---|---|---|
| `Database__Provider` | `PostgreSql` / `SqlServer` | Motor de base de datos |
| `ConnectionStrings__Default` | ver `env/*.env` | Cadena de conexión |
| `Database__Initialization` | `EnsureCreated` / `None` | Crear esquema al arrancar o dejarlo al DBA |
| `Cors__AllowedOrigins__0` (`__1`, …) | `http://localhost:5200` | Origen exacto del frontend (esquema + host + puerto) |
| `Jwt__Issuer` / `Jwt__Audience` | ver compose | Deben ser coherentes en emisión y validación |
| `Jwt__TransactionTokenTtlSeconds` | `60` | Vida del token |
| `Jwt__PrivateKeyPem` | PEM PKCS#8 | Clave RSA compartida (obligatoria con >1 réplica) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | En Production no se expone OpenAPI |

**Frontend**
| Variable | Ejemplo | Descripción |
|---|---|---|
| `API_BASE_URL` | `http://localhost:5100/` | URL del backend **vista desde el navegador**, con `/` final |

> `API_BASE_URL` no puede ser `http://backend:8080/`: esa dirección solo existe dentro de la red de Docker y el navegador no la resuelve.

## 4.5 Verificación rápida
```bash
curl -s http://localhost:5100/healthz/ready     # {"status":"ready"} => hay conexión a la BD
curl -s http://localhost:5200/appsettings.json  # muestra ApiBaseUrl
curl -i -X POST http://localhost:5100/api/v1/decoupled-payments/initiate \
  -H "Content-Type: application/json" \
  -d '{"merchant_id":"M1","user_document_id":"1","amount":1000,"currency":"COP","callback_url":"https://x.co/cb"}'
```
Luego repita el recorrido de `03-frontend-paso-a-paso.md` (sección 3.4).

## 4.6 Publicar en un registro de imágenes
```bash
REGISTRY=registry.miempresa.com/pagos     # ajuste a su registro
docker login registry.miempresa.com

docker tag decoupled-payments-backend:1.1.0  $REGISTRY/decoupled-payments-backend:1.1.0
docker tag decoupled-payments-frontend:1.0.0 $REGISTRY/decoupled-payments-frontend:1.0.0
docker push $REGISTRY/decoupled-payments-backend:1.1.0
docker push $REGISTRY/decoupled-payments-frontend:1.0.0
```
Para desplegar en otro servidor, cambie `image:` en el compose por las rutas del registro y use `docker compose pull && docker compose up -d`.

## 4.7 Consideraciones para un entorno real
1. **HTTPS:** ponga un proxy inverso (nginx, Traefik, Caddy o el balanceador de la nube) que termine TLS. Si el frontend se sirve por HTTPS, el backend también debe ser HTTPS (si no, el navegador bloquea la llamada por *mixed content*).
2. **Base de datos gestionada:** use un servicio con respaldo y alta disponibilidad; no use los contenedores de desarrollo. Cifre la conexión (`SSL Mode=Require` en Npgsql, `Encrypt=True` en SQL Server).
3. **Varias réplicas del backend:** el estado ya está en la BD; defina además la **misma** `Jwt__PrivateKeyPem` en todas (desde un secreto/KMS) y cree el esquema antes (`Initialization=None` + scripts) para evitar carreras al primer arranque.
4. **Secretos:** no guarde cadenas de conexión ni claves en la imagen ni en el repositorio; use variables de entorno, Docker/Kubernetes secrets o un gestor (p. ej. HashiCorp Vault).
5. **OTP real:** sustituya el OTP simulado `123456` por TOTP/Push/SMS antes de producción.
6. **Versionado:** etiquete las imágenes con versión y evite `latest` en producción.

## 4.8 Comandos de mantenimiento
```bash
docker compose build --no-cache backend      # reconstruir sin caché
docker compose restart backend               # reinicia (el estado persiste en la BD)
docker image ls | grep decoupled
```

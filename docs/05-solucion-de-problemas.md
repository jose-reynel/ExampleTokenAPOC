# 5. Solución de problemas

| Síntoma | Causa probable | Solución |
|---|---|---|
| El navegador muestra error de **CORS** | El origen del frontend no está permitido | Añada el origen exacto (esquema + host + puerto) en `Cors:AllowedOrigins` / `Cors__AllowedOrigins__0` y reinicie el backend |
| "Failed to fetch" desde el portal | `ApiBaseUrl` incorrecto o backend caído | Revise `appsettings.json` del frontend / `API_BASE_URL`; pruebe la URL con `curl` |
| Peticiones a `http://backend:8080` fallan en el navegador | Se usó el nombre interno de Docker | Use la URL pública (`http://localhost:5100/`) |
| Error de certificado en `https://localhost:7100` | Certificado de desarrollo no confiable | `dotnet dev-certs https --trust` |
| 401 al ejecutar el pago | Token expirado (60 s) o backend reiniciado | Repita el flujo desde el paso 1 |
| 409 `TOKEN_ALREADY_USED` en el primer intento | El token ya se usó (p. ej. doble clic) | Genere un token nuevo con `verify-otp` |
| 409 `INVALID_TRANSACTION_STATE` | Transacción expirada (5 min) o ya ejecutada | Inicie una transacción nueva |
| 400 `MISSING_IDEMPOTENCY_KEY` | Falta el header `X-Idempotency-Key` (UUID) | Envíe un UUID válido |
| 403 `TOKEN_TRANSACTION_MISMATCH` | Token de una transacción distinta | Use el token emitido para ese `transaction_id` |
| 404 al recargar `/payment-gateway` en nginx | Falta la reescritura SPA | Verifique `frontend/docker/nginx.conf` (`try_files ... /index.html`) |
| `dotnet restore` sin acceso a internet | Red restringida | Configure un feed NuGet interno o el proxy |
| `dotnet restore` falla por versión de paquete | Versión no disponible en su feed | Ajuste las versiones en los `.csproj` (ver doc 2, sección 2.1) |
| Backend no arranca: *Falta la cadena de conexión* | No se definió `ConnectionStrings__Default` | Defínala (ver doc 07, sección 7.3) |
| `/healthz/ready` devuelve 503 | El backend no alcanza la base de datos | Verifique host/puerto/credenciales, firewall y que el motor esté levantado |
| Logs: "Base de datos no disponible (intento n/10)" | El motor aún arranca o la cadena es incorrecta | Espere; si persiste, revise la cadena y `Database__Provider` |
| SQL Server: *The certificate chain was issued by an authority that is not trusted* | Certificado autofirmado del servidor | Agregue `TrustServerCertificate=True` (solo desarrollo) o instale un certificado válido |
| PostgreSQL: `no pg_hba.conf entry` / `password authentication failed` | Usuario, clave o red no permitidos | Revise usuario/clave y las reglas de acceso del servidor |
| Error al crear tablas con `Initialization=None` (`relation ... does not exist`) | No se aplicó el script del motor | Ejecute `database/<motor>/001_create_schema.sql` |
| 409 `CONCURRENT_UPDATE` | Dos operaciones simultáneas sobre la misma transacción | Reintente; es el control de concurrencia optimista |
| 401 intermitente con varias réplicas | Cada réplica generó su propia clave RSA | Defina la misma `Jwt__PrivateKeyPem` en todas |
| Puerto ocupado al levantar Docker | Otro proceso usa 5100/5200 | Cambie el mapeo `"5100:8080"` / `"5200:8080"` y alinee CORS y `API_BASE_URL` |
| `exec /docker-entrypoint.d/40-appsettings.sh: no such file` | Fin de línea Windows (CRLF) en el script | Convierta a LF: `sed -i 's/\r$//' frontend/docker/40-appsettings.sh` |

Para problemas específicos de OpenShift (SCC, Routes, ImagePullBackOff…) vea `06-openshift.md`, sección 6.10.

## Diagnóstico general
```bash
docker compose logs --tail=100 backend
docker compose exec frontend cat /usr/share/nginx/html/appsettings.json
dotnet build backend.sln -v minimal   # compilación local
```

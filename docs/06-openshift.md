# 6. Despliegue en OpenShift

Guía para publicar backend y frontend en Red Hat OpenShift (4.x) con los manifiestos de la carpeta `openshift/`.

## 6.1 Cómo está adaptado a OpenShift
| Requisito de OpenShift | Cómo se cumple |
|---|---|
| Contenedores sin root (SCC `restricted-v2`, UID aleatorio) | Backend: `USER 1654` numérico y sin escrituras en disco. Frontend: `nginxinc/nginx-unprivileged` con archivos de grupo 0 y permisos de grupo (`g+rwX`) |
| Puertos > 1024 | Ambos escuchan en **8080** |
| TLS gestionado por la plataforma | `Route` con `termination: edge` y redirección HTTP→HTTPS |
| Configuración fuera de la imagen | `ConfigMap` + `envFrom` (CORS, JWT, `API_BASE_URL`) |
| Probes de salud | Backend: `GET /healthz/ready` (verifica la BD) y `/healthz`; frontend: `GET /` |
| Credenciales fuera de la imagen | `Secret` `dp-db-secret` (motor + cadena de conexión) y opcional `dp-jwt-key` (clave RSA) |
| Endurecimiento | `runAsNonRoot`, `allowPrivilegeEscalation: false`, `capabilities: drop ALL`, `seccompProfile: RuntimeDefault` |

## 6.2 Requisitos
- Cliente `oc` instalado y acceso al clúster (`oc login --server=https://api.<clúster>:6443`).
- Permiso para crear un proyecto (o uno existente donde pueda crear Deployments, Services y Routes).
- El clúster debe poder descargar `mcr.microsoft.com/dotnet/*` y `nginxinc/nginx-unprivileged` durante el build. En clústeres desconectados, replique esas imágenes en su registro interno y cambie la línea `FROM` de los Dockerfiles.

## 6.3 Paso a paso (rápido, con scripts)
Desde la raíz del proyecto:
```bash
oc login --server=https://api.<clúster>:6443 --token=<token>

# 1) Construir las imágenes dentro de OpenShift (crea proyecto, BuildConfigs e ImageStreams)
./openshift/build.sh decoupled-payments

# 2) Desplegar. Por defecto crea un PostgreSQL de apoyo (pruebas) y el secret dp-db-secret
./openshift/deploy.sh decoupled-payments
```
`deploy.sh` consulta el dominio de aplicaciones del clúster, genera la contraseña de la BD (y la conserva en re-despliegues) y reemplaza los placeholders `__PROJECT__`, `__APPS_DOMAIN__` y `__DB_PASSWORD__`.

**Con una base de datos existente (PostgreSQL o SQL Server), modo `external`:**
```bash
oc create secret generic dp-db-secret -n decoupled-payments \
  --from-literal=Database__Provider=SqlServer \
  --from-literal='ConnectionStrings__Default=Server=sql.miempresa.com,1433;Database=decoupledpayments;User Id=dp_app;Password=***;Encrypt=True'

DB_MODE=external ./openshift/deploy.sh decoupled-payments
```
Para PostgreSQL externo use `Database__Provider=PostgreSql` y una cadena Npgsql (`Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require`). Si el DBA aplica el esquema con los scripts de `database/`, agregue `Database__Initialization: "None"` al ConfigMap `dp-backend-config`.

Al terminar muestra las URLs:
- Frontend: `https://dp.<dominio-apps>/payment-gateway`
- Backend (readiness): `https://dp-api.<dominio-apps>/healthz/ready`

## 6.4 Paso a paso (manual, comando por comando)
```bash
# Proyecto y dominio de apps
oc new-project decoupled-payments
APPS=$(oc get ingresses.config.openshift.io cluster -o jsonpath='{.spec.domain}')

# Build binario con el Dockerfile de cada componente
oc new-build --name dp-backend  --binary --strategy=docker
oc start-build dp-backend  --from-dir=backend  --follow
oc new-build --name dp-frontend --binary --strategy=docker
oc start-build dp-frontend --from-dir=frontend --follow

# Base de datos de apoyo (PostgreSQL) con contraseña generada
PASS=$(openssl rand -hex 16)
sed "s/__DB_PASSWORD__/$PASS/g" openshift/database-postgres.yaml | oc apply -f -
oc rollout status deployment/dp-postgres

# Backend y frontend
for f in backend frontend; do
  sed -e "s/__PROJECT__/decoupled-payments/g" -e "s/__APPS_DOMAIN__/$APPS/g" openshift/$f.yaml | oc apply -f -
done
oc rollout status deployment/dp-backend
oc rollout status deployment/dp-frontend
oc get routes
```

## 6.5 Alternativa: construir con Docker/Podman y subir a la registry de OpenShift
```bash
oc registry info                       # confirme el host de la registry (exponga la ruta si hace falta)
HOST=$(oc get route default-route -n openshift-image-registry -o jsonpath='{.spec.host}')
docker login -u $(oc whoami) -p $(oc whoami -t) $HOST

docker build -t $HOST/decoupled-payments/dp-backend:latest  ./backend
docker build -t $HOST/decoupled-payments/dp-frontend:latest ./frontend
docker push $HOST/decoupled-payments/dp-backend:latest
docker push $HOST/decoupled-payments/dp-frontend:latest
```
Con la registry interna expuesta, los manifiestos funcionan sin cambios (usan `image-registry.openshift-image-registry.svc:5000/<proyecto>/...`). Si usa un registro externo (Quay, Harbor…), cambie `image:` en los dos Deployments y cree un `pull secret` (`oc create secret docker-registry ...` + `oc secrets link default <secret> --for=pull`).

## 6.6 Configuración
| Recurso | Clave | Valor |
|---|---|---|
| ConfigMap `dp-backend-config` | `Cors__AllowedOrigins__0` | `https://dp.<dominio-apps>` (origen exacto del frontend) |
| ConfigMap `dp-backend-config` | `Jwt__Issuer`, `Jwt__Audience`, `Jwt__TransactionTokenTtlSeconds` | ver `openshift/backend.yaml` |
| ConfigMap `dp-backend-config` | `Database__Initialization` | `EnsureCreated` (por defecto) o `None` |
| Secret `dp-db-secret` | `Database__Provider` | `PostgreSql` o `SqlServer` |
| Secret `dp-db-secret` | `ConnectionStrings__Default` | Cadena de conexión del motor |
| Secret `dp-jwt-key` *(opcional)* | `Jwt__PrivateKeyPem` | Clave RSA privada PEM compartida entre réplicas |
| ConfigMap `dp-frontend-config` | `API_BASE_URL` | `https://dp-api.<dominio-apps>/` (con `/` final) |

**Cambiar de motor de base de datos** (p. ej. de PostgreSQL a SQL Server): actualice solo el secret y reinicie.
```bash
oc create secret generic dp-db-secret --dry-run=client -o yaml \
  --from-literal=Database__Provider=SqlServer \
  --from-literal='ConnectionStrings__Default=Server=...;Database=decoupledpayments;User Id=...;Password=...;TrustServerCertificate=True' \
  | oc apply -f -
oc rollout restart deployment/dp-backend
```
(La imagen es la misma; los datos NO se migran entre motores: ver `07-capa-de-datos.md`.)

**Clave RSA compartida** (necesaria para más de una réplica):
```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem
oc create secret generic dp-jwt-key --from-file=Jwt__PrivateKeyPem=jwt-private.pem
oc rollout restart deployment/dp-backend
```
Para cambiar un valor de un ConfigMap:
```bash
oc edit configmap dp-backend-config
oc rollout restart deployment/dp-backend
```
Si usa dominios propios en las Routes (`spec.host`), actualice ambos ConfigMaps con esos dominios.

## 6.7 Verificación
```bash
oc get pods                                        # backend, frontend y postgres en Running y Ready
oc logs deployment/dp-backend                      # "Base de datos lista (PostgreSql)."
curl -s https://dp-api.$APPS/healthz/ready         # {"status":"ready"}
curl -s https://dp.$APPS/appsettings.json          # muestra ApiBaseUrl
oc rsh deployment/dp-postgres psql -U dp_user -d decoupledpayments -c "select status, count(*) from payments.transactions group by status;"
```
Después abra `https://dp.<dominio-apps>/payment-gateway` y repita el recorrido de `03-frontend-paso-a-paso.md` (sección 3.4). Si el clúster usa certificados autofirmados, el navegador pedirá confiar en ellos.

## 6.8 Actualizar a una nueva versión
```bash
oc start-build dp-backend  --from-dir=backend  --follow
oc rollout restart deployment/dp-backend

oc start-build dp-frontend --from-dir=frontend --follow
oc rollout restart deployment/dp-frontend
```
(`imagePullPolicy: Always` con la etiqueta `latest` hace que el reinicio tome la imagen nueva.)

## 6.9 Escalado y estado
El estado (transacciones y tokens consumidos) está en la **base de datos**, por lo que el backend puede escalar siempre que:
1. todas las réplicas usen la **misma clave RSA** (`dp-jwt-key`, sección 6.6); con la clave efímera por defecto, un token emitido por una réplica no valida en otra;
2. el esquema exista antes de escalar (primer arranque con 1 réplica, o `Database__Initialization=None` con los scripts de `database/`).

```bash
oc scale deployment/dp-backend --replicas=2
oc scale deployment/dp-frontend --replicas=2     # estático: sin restricciones
```
El anti-replay es atómico entre réplicas porque se apoya en la clave primaria de `payments.used_tokens`.

El PostgreSQL de apoyo (`dp-postgres`) es de **una sola instancia** con un volumen; no es alta disponibilidad. Para producción use una base gestionada o un operador (CloudNativePG, Crunchy…) con modo `external`.

## 6.10 Problemas frecuentes en OpenShift
| Síntoma | Causa probable | Solución |
|---|---|---|
| Pod en `ImagePullBackOff` | El build no terminó o la imagen no existe en el ImageStream | `oc get is,bc,builds`; ejecute `./openshift/build.sh` antes de `deploy.sh` |
| `CrashLoopBackOff` con *permission denied* | Imagen que escribe en rutas sin permiso para el UID aleatorio | Use los Dockerfiles de este proyecto; revise `oc logs` y la SCC aplicada |
| `Error creating: unable to validate against any SCC` | Manifiesto fuerza `runAsUser` u otro campo no permitido | No fije `runAsUser`; deje que `restricted-v2` asigne el UID |
| Route responde **503 Application is not available** | Pods no Ready (p. ej. el readiness `/healthz/ready` falla por la BD) | `oc get pods,endpoints`; `oc logs deployment/dp-backend` |
| Backend `Running` pero no `Ready` | Sin conexión a la base de datos | Revise `dp-db-secret`, el Service `dp-postgres` y las credenciales |
| `Falta la cadena de conexión 'ConnectionStrings:Default'` | No existe `dp-db-secret` (modo `external`) | Cree el secret antes de `deploy.sh` |
| `CreateContainerConfigError` en el backend | Secret `dp-db-secret` inexistente | Créelo (modo `external`) o ejecute `deploy.sh` en modo interno |
| `password authentication failed` en PostgreSQL | La contraseña del secret no coincide con la del volumen inicializado | Use la contraseña original (`oc get secret dp-postgres-credentials -o jsonpath='{.data.POSTGRESQL_PASSWORD}' \| base64 -d`) o borre el PVC si no hay datos |
| 401 con 2 réplicas, a veces sí a veces no | Réplicas con claves RSA distintas | Configure `dp-jwt-key` y reinicie |
| Error de CORS en el navegador | `Cors__AllowedOrigins__0` no coincide con la Route del frontend | Corrija el ConfigMap (con `https://`, sin `/` final) y reinicie el backend |
| "Failed to fetch" / *mixed content* | `API_BASE_URL` con `http://` desde una página `https://` | Use `https://dp-api.<dominio>/` |
| El portal sigue llamando a la URL anterior | Se cambió el ConfigMap sin reiniciar o el navegador cacheó `appsettings.json` | `oc rollout restart deployment/dp-frontend` y recargue con Ctrl+F5 |
| El build falla al descargar imágenes base | Clúster sin salida a `mcr.microsoft.com` / Docker Hub / quay.io | Replique las imágenes base en su registro y cambie los `FROM` / `image:` |

## 6.11 Limpieza
```bash
oc delete project decoupled-payments        # elimina todo, incluida la base de datos de apoyo y su volumen
# o, conservando el proyecto y los datos:
oc delete deployment,svc,route,configmap -l app.kubernetes.io/part-of=decoupled-payments
```
> Los BuildConfigs/ImageStreams creados con `oc new-build` llevan la etiqueta `build=<nombre>`; para borrarlos: `oc delete bc,is dp-backend dp-frontend`. El PVC `dp-postgres-data` y los secrets se conservan con el segundo comando.


#!/usr/bin/env bash
# Aplica base de datos (opcional), ConfigMaps, Secrets, Deployments, Services y Routes en OpenShift.
#
# Uso:   ./openshift/deploy.sh [proyecto] [dominio-apps]
# Modo de base de datos (variable DB_MODE):
#   internal-postgres (por defecto) -> despliega PostgreSQL de apoyo + secret dp-db-secret
#   external                        -> usa una BD existente; cree antes el secret dp-db-secret
#       oc create secret generic dp-db-secret -n <proyecto> \
#         --from-literal=Database__Provider=SqlServer \
#         --from-literal='ConnectionStrings__Default=Server=host,1433;Database=decoupledpayments;User Id=...;Password=...;TrustServerCertificate=True'
set -euo pipefail

PROJECT="${1:-decoupled-payments}"
APPS_DOMAIN="${2:-$(oc get ingresses.config.openshift.io cluster -o jsonpath='{.spec.domain}')}"
DB_MODE="${DB_MODE:-internal-postgres}"
DIR="$(cd "$(dirname "$0")" && pwd)"

render() {   # render <archivo> <password>
  sed -e "s/__PROJECT__/${PROJECT}/g" -e "s/__APPS_DOMAIN__/${APPS_DOMAIN}/g" -e "s/__DB_PASSWORD__/${2:-unused}/g" "$DIR/$1"
}

echo "Proyecto: $PROJECT | Dominio de apps: $APPS_DOMAIN | Base de datos: $DB_MODE"
oc get project "$PROJECT" >/dev/null 2>&1 || oc new-project "$PROJECT"

if [ "$DB_MODE" = "internal-postgres" ]; then
  # Conserva la contraseña existente para no desalinearla con el volumen ya inicializado
  if oc get secret dp-postgres-credentials -n "$PROJECT" >/dev/null 2>&1; then
    DB_PASSWORD="$(oc get secret dp-postgres-credentials -n "$PROJECT" -o jsonpath='{.data.POSTGRESQL_PASSWORD}' | base64 -d)"
  else
    DB_PASSWORD="${DB_PASSWORD:-$(openssl rand -hex 16)}"
  fi
  render database-postgres.yaml "$DB_PASSWORD" | oc apply -n "$PROJECT" -f -
  oc rollout status deployment/dp-postgres -n "$PROJECT" --timeout=240s
elif [ "$DB_MODE" = "external" ]; then
  oc get secret dp-db-secret -n "$PROJECT" >/dev/null 2>&1 || {
    echo "ERROR: DB_MODE=external requiere el secret dp-db-secret (ver cabecera de este script)." >&2; exit 1; }
else
  echo "DB_MODE inválido: $DB_MODE (use internal-postgres o external)" >&2; exit 1
fi

for f in backend.yaml frontend.yaml; do
  render "$f" | oc apply -n "$PROJECT" -f -
done

oc rollout status deployment/dp-backend  -n "$PROJECT" --timeout=240s
oc rollout status deployment/dp-frontend -n "$PROJECT" --timeout=180s

echo
echo "Frontend: https://dp.${APPS_DOMAIN}/payment-gateway"
echo "Backend : https://dp-api.${APPS_DOMAIN}/healthz/ready"

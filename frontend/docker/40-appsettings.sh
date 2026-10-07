#!/bin/sh
# Genera wwwroot/appsettings.json con la URL del backend al iniciar el contenedor.
set -eu
CONF=/usr/share/nginx/html/appsettings.json
rm -f "$CONF.gz" "$CONF.br"
printf '{\n  "ApiBaseUrl": "%s"\n}\n' "${API_BASE_URL:-http://localhost:5100/}" > "$CONF"
echo "appsettings.json -> ApiBaseUrl=${API_BASE_URL:-http://localhost:5100/}"

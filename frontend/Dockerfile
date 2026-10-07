# Contexto de build: carpeta frontend/
# Compatible con OpenShift (SCC restricted): nginx sin privilegios en el puerto 8080,
# archivos con grupo 0 y permisos de grupo para soportar un UID aleatorio.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY frontend.sln global.json Directory.Build.props ./
COPY src/DecoupledPayments.Client/*.csproj src/DecoupledPayments.Client/
RUN dotnet restore frontend.sln

COPY src ./src
RUN dotnet publish src/DecoupledPayments.Client/DecoupledPayments.Client.csproj \
    -c Release -o /app/publish --no-restore

# El resultado es estático: se sirve con nginx sin privilegios
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
USER 0
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/40-appsettings.sh /docker-entrypoint.d/40-appsettings.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/40-appsettings.sh \
 && chgrp -R 0 /usr/share/nginx/html /etc/nginx/conf.d \
 && chmod -R g+rwX /usr/share/nginx/html /etc/nginx/conf.d
USER 101

# URL del backend vista DESDE EL NAVEGADOR (debe terminar en "/")
ENV API_BASE_URL=http://localhost:5100/
EXPOSE 8080

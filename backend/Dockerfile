# Contexto de build: carpeta backend/
# Compatible con OpenShift (SCC restricted): sin root, puerto >1024, sin escrituras en disco.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero (aprovecha la caché de capas)
COPY backend.sln global.json Directory.Build.props ./
COPY src/1.Domain/DecoupledPayments.Domain/*.csproj src/1.Domain/DecoupledPayments.Domain/
COPY src/2.Application/DecoupledPayments.Application/*.csproj src/2.Application/DecoupledPayments.Application/
COPY src/3.Infrastructure/DecoupledPayments.Infrastructure/*.csproj src/3.Infrastructure/DecoupledPayments.Infrastructure/
COPY src/4.Presentation/DecoupledPayments.WebApi/*.csproj src/4.Presentation/DecoupledPayments.WebApi/
RUN dotnet restore backend.sln

COPY src ./src
RUN dotnet publish src/4.Presentation/DecoupledPayments.WebApi/DecoupledPayments.WebApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
# UID numérico (OpenShift lo reemplaza por uno aleatorio del rango del proyecto)
USER 1654
EXPOSE 8080
ENTRYPOINT ["dotnet", "DecoupledPayments.WebApi.dll"]

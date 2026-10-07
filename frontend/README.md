# DecoupledPayments – Frontend (Blazor WebAssembly, .NET 10)

Solución independiente: `frontend.sln`. No referencia proyectos del backend; solo consume su API HTTP.

## Ejecutar
1. Levantar primero el backend (`backend.sln`) en `https://localhost:7100`.
2. Luego:
```bash
dotnet restore frontend.sln
dotnet run --project src/DecoupledPayments.Client --launch-profile https
```
Portal: `https://localhost:7200/payment-gateway`

## Configuración
`src/DecoupledPayments.Client/wwwroot/appsettings.json` → `ApiBaseUrl` (debe terminar en `/`).
El backend debe permitir el origen del frontend en CORS (`Cors:AllowedOrigins`).

## Prueba end-to-end
1. Paso 1: "Iniciar Transacción".
2. Paso 2: OTP `123456` → "Validar y Obtener Token".
3. Paso 3: "Ejecutar Débito Bancario" → referencia `CUS_xxxx`.
4. "Probar Ataque de Reinteracción": reenvía el mismo token y debe mostrar `409 TOKEN_ALREADY_USED`.

# 3. Frontend paso a paso (`frontend/frontend.sln`)

Portal Blazor WebAssembly que consume el backend por HTTP.

## 3.1 Prerrequisito
El backend debe estar en ejecución (por defecto `https://localhost:7100`) y permitir el origen del frontend en CORS.

## 3.2 Restaurar y ejecutar
```bash
cd frontend
dotnet restore frontend.sln
dotnet run --project src/DecoupledPayments.Client --launch-profile https
```
Abra `https://localhost:7200/payment-gateway`.

## 3.3 Configurar la URL del backend
Archivo `src/DecoupledPayments.Client/wwwroot/appsettings.json`:
```json
{ "ApiBaseUrl": "https://localhost:7100/" }
```
- Debe terminar en `/`.
- Es una configuración de **navegador**: use la URL que el navegador puede alcanzar.
- Con Docker, este archivo se regenera al arrancar el contenedor desde la variable `API_BASE_URL`.

## 3.4 Recorrido del flujo en pantalla
1. **Paso 1 – Iniciación:** revise documento y monto → *Iniciar Transacción*.
2. **Paso 2 – OTP:** escriba `123456` → *Validar y Obtener Token*. Un OTP incorrecto muestra "OTP incorrecto o expirado".
3. **Paso 3 – Débito:** *Ejecutar Débito Bancario*. Debe aparecer "¡Pago Exitoso!" con la referencia `CUS_AAAAMMDD_xxxxxx`.
4. **Prueba de replay:** pulse *Probar Ataque de Reinteracción*. Se reenvía el mismo token y debe mostrarse **HTTP 409 – `TOKEN_ALREADY_USED`**.

> El `transaction_token` dura 60 s. Si tarda más en pulsar *Ejecutar*, el backend responderá 401; reinicie el flujo.

## 3.5 Publicar los archivos estáticos (sin Docker)
```bash
dotnet publish src/DecoupledPayments.Client -c Release -o out
```
El resultado está en `out/wwwroot` y puede alojarse en cualquier servidor estático (nginx, IIS, Azure Static Web Apps, S3 + CDN). Configure la reescritura de rutas a `index.html` (SPA).

## 3.6 Estructura del cliente
- `Models/PaymentModels.cs` – modelos de petición/respuesta.
- `Services/PaymentApiClient.cs` – cliente HTTP (JSON en `snake_case`, cabeceras `Authorization` y `X-Idempotency-Key`).
- `Pages/PaymentGateway.razor` – asistente de 3 pasos.

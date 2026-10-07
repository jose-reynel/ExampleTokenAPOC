# Scripts de base de datos

| Motor | Script |
|---|---|
| PostgreSQL | `postgresql/001_create_schema.sql` |
| SQL Server | `sqlserver/001_create_schema.sql` |

Los scripts reflejan exactamente el modelo del ORM (`PaymentsDbContext`).
Úselos cuando `Database__Initialization=None` (entornos controlados por DBA).
Con `EnsureCreated` (por defecto) el backend crea el esquema solo al arrancar.

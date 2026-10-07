-- DecoupledPayments · SQL Server 2019+ · esquema de la APOC
-- Úselo con Database__Initialization=None (el ORM no crea tablas).
--   sqlcmd -S <host> -U <usuario> -d decoupledpayments -i 001_create_schema.sql
-- (cree antes la base: CREATE DATABASE decoupledpayments;)

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'payments')
    EXEC (N'CREATE SCHEMA payments');
GO

IF OBJECT_ID(N'payments.transactions', N'U') IS NULL
BEGIN
    CREATE TABLE payments.transactions (
        transaction_id    uniqueidentifier NOT NULL,
        merchant_id       nvarchar(64)     NOT NULL,
        user_document_id  nvarchar(32)     NOT NULL,
        amount            decimal(18,2)    NOT NULL,
        currency          nvarchar(3)      NOT NULL,
        status            nvarchar(32)     NOT NULL,   -- PendingOtpVerification | Authorized | Executed | Failed
        expected_otp      nvarchar(16)     NOT NULL,
        created_at        datetime2        NOT NULL,
        expires_at        datetime2        NOT NULL,
        CONSTRAINT pk_transactions PRIMARY KEY (transaction_id)
    );
    CREATE INDEX ix_transactions_status_expires
        ON payments.transactions (status, expires_at);
END
GO

IF OBJECT_ID(N'payments.used_tokens', N'U') IS NULL
BEGIN
    CREATE TABLE payments.used_tokens (
        jti         nvarchar(64) NOT NULL,
        expires_at  datetime2    NOT NULL,
        CONSTRAINT pk_used_tokens PRIMARY KEY (jti)
    );
    CREATE INDEX ix_used_tokens_expires_at
        ON payments.used_tokens (expires_at);
END
GO

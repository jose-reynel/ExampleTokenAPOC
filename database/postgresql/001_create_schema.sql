-- DecoupledPayments · PostgreSQL 13+ · esquema de la APOC
-- Úselo con Database__Initialization=None (el ORM no crea tablas).
--   psql -h <host> -U <usuario> -d decoupledpayments -f 001_create_schema.sql

CREATE SCHEMA IF NOT EXISTS payments;

CREATE TABLE IF NOT EXISTS payments.transactions (
    transaction_id    uuid           NOT NULL,
    merchant_id       varchar(64)    NOT NULL,
    user_document_id  varchar(32)    NOT NULL,
    amount            numeric(18,2)  NOT NULL,
    currency          varchar(3)     NOT NULL,
    status            varchar(32)    NOT NULL,   -- PendingOtpVerification | Authorized | Executed | Failed
    expected_otp      varchar(16)    NOT NULL,
    created_at        timestamptz    NOT NULL,
    expires_at        timestamptz    NOT NULL,
    CONSTRAINT pk_transactions PRIMARY KEY (transaction_id)
);

CREATE INDEX IF NOT EXISTS ix_transactions_status_expires
    ON payments.transactions (status, expires_at);

CREATE TABLE IF NOT EXISTS payments.used_tokens (
    jti         varchar(64)  NOT NULL,
    expires_at  timestamptz  NOT NULL,
    CONSTRAINT pk_used_tokens PRIMARY KEY (jti)
);

CREATE INDEX IF NOT EXISTS ix_used_tokens_expires_at
    ON payments.used_tokens (expires_at);

-- Buat database
CREATE DATABASE scanDBProject;
GO
--DROP DATABASE scanDBProject;
-- Gunakan database
USE scanDBProject;
GO
-- Tabel lini
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='lines' and xtype='U')
CREATE TABLE lines (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    nama VARCHAR(50) NOT NULL
);
select * from lines
INSERT INTO lines (nama)
VALUES 
    ('1'),
    ('2'),
    ('3');

-- Tabel robot
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='robots' and xtype='U')
CREATE TABLE robots (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    line_id BIGINT NOT NULL,
    nama VARCHAR(50) NOT NULL,
    FOREIGN KEY (line_id) REFERENCES lines(id)
);

-- Tabel pengguna
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' and xtype='U')
CREATE TABLE users (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    email VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    role VARCHAR(10) NOT NULL CHECK (role IN ('Admin', 'Checker', 'Poles')),
    line_id BIGINT,
    nama VARCHAR(100) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
	phonenumber NVARCHAR(20) NULL,
    isactive BIT NOT NULL DEFAULT 1,
    createdby NVARCHAR(50) NULL,
    FOREIGN KEY (line_id) REFERENCES lines(id)
);

-- Tambahkan user
IF NOT EXISTS (SELECT 1 FROM users WHERE email = 'Administrator')
BEGIN
INSERT INTO users (email, password, role, nama, line_id, phonenumber, createdby)
VALUES ('admin@example.com', '$2a$11$BHcjzUL38w0eVKAJ7MDPE.ZrbNqsQ5gGiIidwPQdv//LqcllTmk6u', 'Admin', 'Administrator', 1, '081234567890','system');
END
ELSE
BEGIN
    PRINT 'EXISTS';
END
go
--sudah pake bcrypt / hash di pass
--user : admin@example.com	pass : administrator/admin3 || $2a$11$BHcjzUL38w0eVKAJ7MDPE.ZrbNqsQ5gGiIidwPQdv//LqcllTmk6u
select * from [dbo].[Users]


-- Tabel inventaris
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='inventories' and xtype='U')
CREATE TABLE inventories (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    inv_id VARCHAR(50) UNIQUE NOT NULL,
    tipe VARCHAR(50) NOT NULL,
    warna VARCHAR(50) NOT NULL,
    created_at DATETIME DEFAULT GETDATE()
);

-- Tabel kategori NG
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ng_categories' and xtype='U')
CREATE TABLE ng_categories (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    category VARCHAR(50) NOT NULL,
    sub_category VARCHAR(50) NOT NULL,
    created_at DATETIME DEFAULT GETDATE()
);

-- Tabel transaksi
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='transactions' and xtype='U')
CREATE TABLE transactions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    inv_id BIGINT NOT NULL,
    line_id BIGINT NOT NULL,
    robot_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    role VARCHAR(10) NOT NULL CHECK (role IN ('checker', 'poles')),
    status VARCHAR(10) NOT NULL CHECK (status IN ('OK', 'POLESH', 'NG')),
    qty INT NOT NULL,
    shift VARCHAR(1) NOT NULL CHECK (shift IN ('1', '2')),
    opposite_shift BIT DEFAULT 0,
    checker_id BIGINT,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (inv_id) REFERENCES inventories(id),
    FOREIGN KEY (line_id) REFERENCES lines(id),
    FOREIGN KEY (robot_id) REFERENCES robots(id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (checker_id) REFERENCES transactions(id)
);

-- Tabel detail NG
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ng_details' and xtype='U')
CREATE TABLE ng_details (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    transaction_id BIGINT NOT NULL,
    ng_category_id BIGINT NOT NULL,
    qty INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (transaction_id) REFERENCES transactions(id),
    FOREIGN KEY (ng_category_id) REFERENCES ng_categories(id)
);


CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_inventories_inv_id ON inventories(inv_id);
CREATE INDEX idx_transactions_inv_id ON transactions(inv_id);
CREATE INDEX idx_transactions_checker_id ON transactions(checker_id);
CREATE INDEX idx_transactions_created_at ON transactions(created_at);
CREATE INDEX idx_ng_details_transaction_id ON ng_details(transaction_id);
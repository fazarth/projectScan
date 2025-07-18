-- Buat database
CREATE DATABASE MvcDemoDB;
GO

-- Gunakan database
USE MvcDemoDB;
GO

-- Buat tabel Users
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
	FullName NVARCHAR(100) NULL,
    Email NVARCHAR(100) NULL,
	Role NVARCHAR(50) NOT NULL DEFAULT 'User',
    PhoneNumber NVARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy NVARCHAR(50) NULL
);
GO

ALTER TABLE Users
ADD Role NVARCHAR(50) NOT NULL DEFAULT 'User';
go

-- Tambahkan user (sementara password belum di-hash)
INSERT INTO Users (Username, Password)
VALUES ('admin', '$2a$11$tmYCSyvP9qnscxq0qWZcb.e10ciRthnPtBdliFE/rZjKxcbincRMO');
go
--sudah pake bcrypt / hash di pass
--user : admin3	pass : admin3 || $2a$11$tmYCSyvP9qnscxq0qWZcb.e10ciRthnPtBdliFE/rZjKxcbincRMO
--muafa3, pass : muafa3
select * from Users
go
--DELETE FROM Users WHERE Id = 2;
--ALTER TABLE Users ADD
--    FullName NVARCHAR(100) NULL,
--    Email NVARCHAR(100) NULL,
--    PhoneNumber NVARCHAR(20) NULL,
--    IsActive BIT NOT NULL DEFAULT 1,
--    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
--    CreatedBy NVARCHAR(50) NULL;
--go
ALTER TABLE Users
ALTER COLUMN CreatedBy NVARCHAR(100) NULL;

UPDATE Users
SET
    FullName = 'Administrator',
    Email = 'admin@example.com',
    PhoneNumber = '081234567890',
    Role = 'Admin',
    CreatedBy = 'system'
WHERE Username = 'admin3'And Id = 3;
go

SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH, 
    IS_NULLABLE, 
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users';

sp_help Users;

SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users'
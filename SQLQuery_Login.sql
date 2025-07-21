-- Buat database
CREATE DATABASE MvcDemoDB;
GO

-- Gunakan database
USE MvcDemoDB;
GO

-- Buat tabel Users
IF NOT EXISTS ( SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users' AND TABLE_TYPE = 'BASE TABLE')
BEGIN
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
END
ELSE
BEGIN
    PRINT 'EXISTS';
END
GO

-- Tambahkan user
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin3')
BEGIN
INSERT INTO Users (Username, Password, FullName,Email, PhoneNumber, Role, CreatedBy)
VALUES ('Administrator', '$2a$11$tmYCSyvP9qnscxq0qWZcb.e10ciRthnPtBdliFE/rZjKxcbincRMO', 'Administrator','admin@example.com','081234567890','Admin','system');
END
ELSE
BEGIN
    PRINT 'EXISTS';
END
go
--sudah pake bcrypt / hash di pass
--user : Administrator	pass : administrator || $2a$11$BHcjzUL38w0eVKAJ7MDPE.ZrbNqsQ5gGiIidwPQdv//LqcllTmk6u
select * from [dbo].[Users]
go

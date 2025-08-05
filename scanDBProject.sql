-- Buat database
CREATE DATABASE scanDBProject;
GO
--DROP DATABASE scanDBProject;
-- Gunakan database
USE scanDBProject;
GO

select * from inventories
select line_id,* from users
select * from transactions order by created_at desc

select u.line_id, r.nama, * from users u
join robots r on u.line_id = r.line_id

CREATE TABLE qr_counter (
    Id INT PRIMARY KEY,
    LastNumber INT
);

INSERT INTO qr_counter (Id, LastNumber) VALUES (1, 0);

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

INSERT INTO [scanDBProject].[dbo].[robots] (
    [line_id],
    [nama]
) VALUES
(1, 'Mangga'),
(1, 'Pisang'),
(1, 'Nanas'),
(1, 'Manggis'),
(1, 'Jambu'),
(2, 'Duku'),
(2, 'Sirsak'),
(2, 'Jeruk');

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
ALTER TABLE users
ADD user_group NVARCHAR(50) NULL;

--UPDATE users
--SET user_group = 'A'
--WHERE user_group = '1';

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
	project VARCHAR(50),
    part_no VARCHAR(100),
    part_name VARCHAR(100),
    barcode VARCHAR(255),
    created_by VARCHAR(50),
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
select * from ng_categories
USE [scanDBProject]
GO
INSERT INTO [scanDBProject].[dbo].[ng_categories] ([category], [sub_category], [created_at]) VALUES
('BINTIK', 'BINTIK CAT', GETDATE()),
('BINTIK', 'KOTORAN LUAR', GETDATE()),
('KEBA', 'SERAT BENANG', GETDATE()),
('HAJIKI', 'OIL', GETDATE()),
('HAJIKI', 'AIR / BLISTER', GETDATE()),
('SCRACTH', 'GORES', GETDATE()),
('SCRACTH', 'EX-MIGAKI', GETDATE()),
('NG ROBOT', 'SAGING / LELEH', GETDATE()),
('NG ROBOT', 'ABSORB', GETDATE()),
('NG ROBOT', 'TIPIS', GETDATE()),
('NG ROBOT', 'ORANGE PEEL', GETDATE()),
('NG ROBOT', 'MOTLING / BELANG', GETDATE()),
('NG ROBOT', 'BEDA WARNA', GETDATE()),
('NG ROBOT', 'OVER MASKING', GETDATE()),
('NG ROBOT', 'GALER', GETDATE()),
('NG ROBOT', 'POPING', GETDATE()),
('NG ROBOT', 'LIPTING', GETDATE()),
('NG ROBOT', 'HANDLING', GETDATE()),
('NG INJECTION', 'OVER CUT', GETDATE()),
('NG INJECTION', 'FLOW MARK', GETDATE()),
('NG INJECTION', 'SILVER / NYEREP', GETDATE()),
('NG INJECTION', 'CRACK / RETAK', GETDATE()),
('NG INJECTION', 'PIN PATAH', GETDATE()),
('NG INJECTION', 'BURRY', GETDATE()),
('NG INJECTION', 'BUBBLE', GETDATE()),
('NG INJECTION', 'VACUM', GETDATE()),
('LAIN-LAIN', 'NYAMUK', GETDATE()),
('LAIN-LAIN', 'RAMBUT', GETDATE())
GO

SELECT *
FROM inventories

SELECT *
FROM transactions
order by created_at desc
WHERE created_at >= '2025-08-04 21:00:00'
  AND created_at <  '2025-08-05 08:00:00'

CREATE TABLE [dbo].[transactions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[inv_id] [bigint] NOT NULL,
	[barcode] [varchar](50) NOT NULL,
	[line_id] [bigint] NOT NULL,
	[robot_id] [bigint] NOT NULL,
	[user_id] [bigint] NOT NULL,
	[role] [varchar](10) NOT NULL,
	[status] [varchar](10) NOT NULL,
	[ng_detail_id] [bigint] NULL,
	[qty] [int] NOT NULL,
	[shift] [varchar](1) NULL,
	[opposite_shift] [bit] NULL,
	[created_at] [datetime] NULL,
 CONSTRAINT [PK_transact_3214EC077AFA8A74] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO [dbo].[inventories] (
    [project],
    [inv_id],
    [tipe],
    [warna],
    [part_no],
    [part_name],
    [barcode]
) VALUES
('D74', 'ADM-D74-CFB1-AAPT', 'D-CUSTOM', 'W09', '52101-BZE80-A0', 'FRONT BUMPER', 'uploads/1750665409_ADM-D74-CFB1-AAPT.png'),
('D74', 'ADM-D74-CFB1-BAPT', 'D-CUSTOM', 'S28', '52101-BZE80-B0', 'FRONT BUMPER', 'uploads/1750665541_ADM-D74-CFB1-BAPT.png'),
('D74', 'ADM-D74-CFB1-CAPT', 'D-CUSTOM', '1G3', '52101-BZE80-B1', 'FRONT BUMPER', 'uploads/1750665575_ADM-D74-CFB1-CAPT.png'),
('D74', 'ADM-D74-CFB1-DAPT', 'D-CUSTOM', 'X13', '52101-BZE80-C0', 'FRONT BUMPER', 'uploads/1750665618_ADM-D74-CFB1-DAPT.png'),
('D74', 'ADM-D74-CFB1-EAPT', 'D-CUSTOM', 'R75', '52101-BZE80-D0', 'FRONT BUMPER', 'uploads/1750665651_ADM-D74-CFB1-EAPT.png'),
('D74', 'ADM-D74-CFB1-FAPT', 'D-CUSTOM', 'R79', '52101-BZE80-D1', 'FRONT BUMPER', 'uploads/1750665693_ADM-D74-CFB1-FAPT.png'),
('D74', 'ADM-D74-CFB1-GAPT', 'D-CUSTOM', 'R80', '52101-BZE80-D2', 'FRONT BUMPER', 'uploads/1750665719_ADM-D74-CFB1-GAPT.png'),
('D74', 'ADM-D74-CFB1-HAPT', 'D-CUSTOM', 'Y13', '52101-BZE80-F0', 'FRONT BUMPER', 'uploads/1750665752_ADM-D74-CFB1-HAPT.png'),
('D74', 'ADM-D74-CRB1-AAPT', 'J-1', 'W09', '52159-BZQ20-A0', 'COVER, RR BUMPER', 'uploads/1750665787_ADM-D74-CRB1-AAPT.png'),
('D74', 'ADM-D74-CRB1-BAPT', 'J-1', 'S28', '52159-BZQ20-B0', 'COVER, RR BUMPER', 'uploads/1750665827_ADM-D74-CRB1-BAPT.png'),
('D74', 'ADM-D74-CRB1-CAPT', 'J-1', '1G3', '52159-BZQ20-B1', 'COVER, RR BUMPER', 'uploads/1750665861_ADM-D74-CRB1-CAPT.png'),
('D74', 'ADM-D74-CRB1-DAPT', 'J-1', 'X13', '52159-BZQ20-C0', 'COVER, RR BUMPER', 'uploads/1750665887_ADM-D74-CRB1-DAPT.png'),
('D74', 'ADM-D74-CRB1-EAPT', 'J-1', 'R75', '52159-BZQ20-D0', 'COVER, RR BUMPER', 'uploads/1750665916_ADM-D74-CRB1-EAPT.png'),
('D74', 'ADM-D74-CRB1-FAPT', 'J-1', 'R79', '52159-BZQ20-D1', 'COVER, RR BUMPER', 'uploads/1750665949_ADM-D74-CRB1-FAPT.png'),
('D74', 'ADM-D74-CRB1-GAPT', 'J-1', 'R80', '52159-BZQ20-D2', 'COVER, RR BUMPER', 'uploads/1750665985_ADM-D74-CRB1-GAPT.png'),
('D74', 'ADM-D74-CRB1-HAPT', 'J-1', 'Y13', '52159-BZQ20-F0', 'COVER, RR BUMPER', 'uploads/1750666079_ADM-D74-CRB1-HAPT.png'),
('D74', 'ADM-D74-CRB2-AAPT', 'J-2', 'W09', '52159-BZQ40-A0', 'COVER, RR BUMPER', 'uploads/1750666107_ADM-D74-CRB2-AAPT.png'),
('D74', 'ADM-D74-CRB2-BAPT', 'J-2', 'S28', '52159-BZQ40-B0', 'COVER, RR BUMPER', 'uploads/1750666135_ADM-D74-CRB2-BAPT.png'),
('D74', 'ADM-D74-CRB2-CAPT', 'J-2', '1G3', '52159-BZQ40-B1', 'COVER, RR BUMPER', 'uploads/1750666163_ADM-D74-CRB2-CAPT.png'),
('D74', 'ADM-D74-CRB2-DAPT', 'J-2', 'X13', '52159-BZQ40-C0', 'COVER, RR BUMPER', 'uploads/1750666193_ADM-D74-CRB2-DAPT.png'),
('D74', 'ADM-D74-CRB2-FAPT', 'J-2', 'R79', '52159-BZQ40-D0', 'COVER, RR BUMPER', 'uploads/1750666222_ADM-D74-CRB2-FAPT.png'),
('D74', 'ADM-D74-CRB2-GAPT', 'J-2', 'R80', '52159-BZQ40-D1', 'COVER, RR BUMPER', 'uploads/1750666252_ADM-D74-CRB2-GAPT.png'),
('D74', 'ADM-D74-CRB2-HAPT', 'J-2', 'Y13', '52159-BZQ40-F0', 'COVER, RR BUMPER', 'uploads/1750666280_ADM-D74-CRB2-HAPT.png'),
('D74', 'ADM-D74-CFB2-AAPT', 'T-CUSTOM', 'W09', '52119-BZW20-A0', 'FRONT BUMPER', 'uploads/1750666618_ADM-D74-CFB2-AAPT.png'),
('D74', 'ADM-D74-CFB2-BAPT', 'T-CUSTOM', 'S28', '52119-BZW20-B0', 'FRONT BUMPER', 'uploads/1750666647_ADM-D74-CFB2-BAPT.png'),
('D74', 'ADM-D74-CFB2-CAPT', 'T-CUSTOM', '1G3', '52119-BZW20-B1', 'FRONT BUMPER', 'uploads/1750666673_ADM-D74-CFB2-CAPT.png'),
('D74', 'ADM-D74-CFB2-DAPT', 'T-CUSTOM', 'X13', '52119-BZW20-C0', 'FRONT BUMPER', 'uploads/1750666703_ADM-D74-CFB2-DAPT.png'),
('D74', 'ADM-D74-CFB2-EAPT', 'T-CUSTOM', 'R79', '52119-BZW20-D0', 'FRONT BUMPER', 'uploads/1750666761_ADM-D74-CFB2-EAPT.png'),
('D74', 'ADM-D74-CFB2-FAPT', 'T-CUSTOM', 'R80', '52119-BZW20-D1', 'FRONT BUMPER', 'uploads/1750666790_ADM-D74-CFB2-FAPT.png'),
('D74', 'ADM-D74-CFB2-GAPT', 'T-CUSTOM', 'Y13', '52119-BZW20-F0', 'FRONT BUMPER', 'uploads/1750666816_ADM-D74-CFB2-GAPT.png'),
('D14', 'ADM-D14-CFB0-DAPT', 'D14', 'W09', '52119-BZM20-A0', 'FRONT BUMPER', 'uploads/1750666842_ADM-D14-CFB0-DAPT.png'),
('D14', 'ADM-D14-CFB0-EAPT', 'D14', '1E7', '52119-BZM20-B0', 'FRONT BUMPER', 'uploads/1750667018_ADM-D14-CFB0-EAPT.png'),
('D14', 'ADM-D14-CFB0-FAPT', 'D14', 'X12', '52119-BZM20-C0', 'FRONT BUMPER', 'uploads/1750667048_ADM-D14-CFB0-FAPT.png'),
('D14', 'ADM-D14-CFB0-GAPT', 'D14', 'R54', '52119-BZM20-D0', 'FRONT BUMPER', 'uploads/1750667070_ADM-D14-CFB0-GAPT.png'),
('D14', 'ADM-D14-CFB0-HAPT', 'D14', '3Q3', '52119-BZM20-D1', 'FRONT BUMPER', 'uploads/1750667093_ADM-D14-CFB0-HAPT.png'),
('D14', 'ADM-D14-CFB0-IAPT', 'D14', '4T3', '52119-BZM20-E0', 'FRONT BUMPER', 'uploads/1750667121_ADM-D14-CFB0-IAPT.png'),
('D14', 'ADM-D14-FRBR-GAPT', 'D14', 'R54', '52552-BZ020-D0', 'FILLER RH', 'uploads/1750667146_ADM-D14-FRBR-GAPT.png'),
('D14', 'ADM-D14-FRBR-IAPT', 'D14', '4T3', '52553-BZ020-D0', 'FILLER RH', 'uploads/1750667172_ADM-D14-FRBR-IAPT.png'),
('D14', 'ADM-D14-FRBL-GAPT', 'D14', 'R54', '52553-BZ020-D0', 'FILLER LH', 'uploads/1750667195_ADM-D14-FRBL-GAPT.png'),
('D14', 'ADM-D14-FRBL-IAPT', 'D14', '4T3', '52553-BZ020-E0', 'FILLER LH', 'uploads/1750667219_ADM-D14-FRBL-IAPT.png'),
('D55', 'ADM-D55-CFD0-AAPT', 'D-FACE', 'W09', '52119-BZR60-A0', 'FRONT BUMPER', 'uploads/1750667245_ADM-D55-CFD0-AAPT.png'),
('D55', 'ADM-D55-CFD0-BAPT', 'D-FACE', 'S28', '52119-BZR60-B0', 'FRONT BUMPER', 'uploads/1750667270_ADM-D55-CFD0-BAPT.png'),
('D55', 'ADM-D55-CFD0-CAPT', 'D-FACE', '1G3', '52119-BZR60-B1', 'FRONT BUMPER', 'uploads/1750667297_ADM-D55-CFD0-CAPT.png'),
('D55', 'ADM-D55-CFD0-DAPT', 'D-FACE', 'X13', '52119-BZR60-C0', 'FRONT BUMPER', 'uploads/1750667324_ADM-D55-CFD0-DAPT.png'),
('D55', 'ADM-D55-CFD0-EAPT', 'D-FACE', 'R75', '52119-BZR60-D0', 'FRONT BUMPER', 'uploads/1750667671_ADM-D55-CFD0-EAPT.png'),
('D55', 'ADM-D55-CFD0-FAPT', 'D-FACE', 'W25', '52119-BZR60-A1', 'FRONT BUMPER', 'uploads/1750667696_ADM-D55-CFD0-FAPT.png'),
('D55', 'ADM-D55-CFD0-GAPT', 'D-FACE', 'Y13', '52119-BZR60-F0', 'FRONT BUMPER', 'uploads/1750667720_ADM-D55-CFD0-GAPT.png'),
('D55', 'ADM-D55-CFT0-AAPT', 'T-FACE', 'W09', '52119-BZR50-A0', 'FRONT BUMPER', 'uploads/1750667824_ADM-D55-CFT0-AAPT.png'),
('D55', 'ADM-D55-CFT0-BAPT', 'T-FACE', 'S28', '52119-BZR50-B0', 'FRONT BUMPER', 'uploads/1750667850_ADM-D55-CFT0-BAPT.png'),
('D55', 'ADM-D55-CFT0-CAPT', 'T-FACE', '1G3', '52119-BZR50-B1', 'FRONT BUMPER', 'uploads/1750667874_ADM-D55-CFT0-CAPT.png'),
('D55', 'ADM-D55-CFT0-DAPT', 'T-FACE', 'X13', '52119-BZR50-C0', 'FRONT BUMPER', 'uploads/1750667899_ADM-D55-CFT0-DAPT.png'),
('D55', 'ADM-D55-CFT0-EAPT', 'T-FACE', 'R40', '52119-BZR50-D0', 'FRONT BUMPER', 'uploads/1750667920_ADM-D55-CFT0-EAPT.png'),
('D55', 'ADM-D55-CFT0-FAPT', 'T-FACE', 'W25', '52119-BZR50-A1', 'FRONT BUMPER', 'uploads/1750667960_ADM-D55-CFT0-FAPT.png'),
('D55', 'ADM-D55-CFT0-GAPT', 'T-FACE', 'Y13', '52119-BZR50-F0', 'FRONT BUMPER', 'uploads/1750667984_ADM-D55-CFT0-GAPT.png'),
('D55', 'ADM-D55-CFT0-HAPT', 'T-FACE', 'B86', '52119-BZR50-J0', 'FRONT BUMPER', 'uploads/1750668007_ADM-D55-CFT0-HAPT.png'),
('D02', 'ADM-D02-CFB0-AAPT', 'D02', 'W09', '52119-BZY20-A0', 'FRONT BUMPER', 'uploads/1750668033_ADM-D02-CFB0-AAPT.png'),
('D02', 'ADM-D02-CFB0-BAPT', 'D02', 'S28', '52119-BZY20-B0', 'FRONT BUMPER', 'uploads/1750668055_ADM-D02-CFB0-BAPT.png'),
('D02', 'ADM-D02-CFB0-CAPT', 'D02', 'X12', '52119-BZY20-C0', 'FRONT BUMPER', 'uploads/1750668083_ADM-D02-CFB0-CAPT.png'),
('D02', 'ADM-D02-CFB0-DAPT', 'D02', '3Q3', '52119-BZY20-D0', 'FRONT BUMPER', 'uploads/1750668107_ADM-D02-CFB0-DAPT.png'),
('D02', 'ADM-D02-CFB0-FAPT', 'D02', '4T3', '52119-BZY20-E0', 'FRONT BUMPER', 'uploads/1750668135_ADM-D02-CFB0-FAPT.png'),
('D02', 'ADM-D02-CFB0-GAPT', 'D02', 'G64', '52119-BZY20-G', 'FRONT BUMPER', 'uploads/1750668158_ADM-D02-CFB0-GAPT.png'),
('MBI', 'MZI-ACE-GF00-BAPT', 'MBI', 'DARK GREY', 'A 400 884 03 85-A1', 'GRILLE FRONT MB', 'uploads/1750668178_MZI-ACE-GF00-BAPT.png'),
('MBI', 'MZI-ACE-GF00-DAPT', 'MBI', 'ARTIC WHITE', 'A4008840385-A2', 'GRILLE FRONT MB', 'uploads/1750668204_MZI-ACE-GF00-DAPT.png'),
('D26', 'ADM-D26-GRFR-AAPT', 'D26', 'AS72', '75555-BZ020', 'GARNISH ROOF, RH', 'uploads/1750668227_ADM-D26-GRFR-AAPT.png'),
('D26', 'ADM-D26-GRFL-AAPT', 'D26', 'AS72', '75556-BZ020', 'GARNISH ROOF, LH', 'uploads/1750668251_ADM-D26-GRFL-AAPT.png');

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
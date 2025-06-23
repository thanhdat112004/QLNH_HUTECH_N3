# RestaurantDB Schema

Các script T-SQL khởi tạo toàn bộ schema cho database **RestaurantDB**, phù hợp cho…Database-First / MVC scaffold.

---

## File: `scripts/schema.sql`

```sql
CREATE DATABASE RestaurantDB;
GO

USE RestaurantDB;
GO

-- 1. Roles: danh sách quyền
CREATE TABLE Roles (
    RoleId   INT           IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(50)  NOT NULL UNIQUE
);
GO

-- 2. Users: tài khoản người dùng
CREATE TABLE Users (
    UserId         INT           IDENTITY(1,1) PRIMARY KEY,
    Username       NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash   NVARCHAR(256) NOT NULL,
    FullName       NVARCHAR(100) NULL,
    Email          NVARCHAR(256) NOT NULL UNIQUE,
    EmailConfirmed BIT           NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- 3. UserRoles: quan hệ many-to-many giữa Users và Roles
CREATE TABLE UserRoles (
    UserId     INT        NOT NULL,
    RoleId     INT        NOT NULL,
    AssignedAt DATETIME2  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UserRoles       PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User  FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_UserRoles_Role  FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- 4. RestaurantTables: quản lý bàn ăn
CREATE TABLE RestaurantTables (
    TableId     INT          IDENTITY(1,1) PRIMARY KEY,
    TableNumber NVARCHAR(10) NOT NULL UNIQUE,
    Seats       INT          NOT NULL,
    Status      NVARCHAR(20) NOT NULL DEFAULT 'Available'
);
GO

-- 5. Categories: danh mục món ăn (burger, pizza, ...)
CREATE TABLE Categories (
    CategoryId INT           IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL UNIQUE,
    Slug       NVARCHAR(100) NOT NULL UNIQUE
);
GO

-- Seed một số danh mục mẫu
INSERT INTO Categories (Name, Slug)
VALUES
  ('Burger', 'burger'),
  ('Pizza',  'pizza'),
  ('Pasta',  'pasta'),
  ('Fries',  'fries');
GO

-- 6. MenuItems: danh mục món, kèm flag featured
CREATE TABLE MenuItems (
    MenuItemId    INT           IDENTITY(1,1) PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL,
    Description   NVARCHAR(500) NULL,
    Price         DECIMAL(10,2) NOT NULL,
    Stock         INT           NOT NULL DEFAULT 0,
    ImageUrl      NVARCHAR(255) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETDATE(),
    IsFeatured    BIT           NOT NULL DEFAULT 0,
    FeaturedOrder INT           NOT NULL DEFAULT 0,
    CategoryId    INT           NOT NULL
);
GO

ALTER TABLE MenuItems
ADD CONSTRAINT FK_MenuItems_Categories
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId);
GO

-- 7. Orders: DineIn vs Takeaway
CREATE TABLE Orders (
    OrderId       INT           IDENTITY(1,1) PRIMARY KEY,
    UserId        INT           NOT NULL,
    OrderType     NVARCHAR(20)  NOT NULL DEFAULT 'DineIn',    -- 'DineIn' hoặc 'Takeaway'
    TableId       INT           NULL,                         -- chỉ DineIn
    ContactInfo   NVARCHAR(200) NULL,                         -- chỉ Takeaway
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETDATE(),
    UpdatedAt     DATETIME2     NOT NULL DEFAULT GETDATE(),
    Paid          BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Orders_User       FOREIGN KEY (UserId)  REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Table      FOREIGN KEY (TableId) REFERENCES RestaurantTables(TableId),
    CONSTRAINT CHK_Orders_TypeTable CHECK (
        (OrderType = 'DineIn'    AND TableId IS NOT NULL) OR
        (OrderType = 'Takeaway' AND TableId IS NULL)
    )
);
GO

-- 8. OrderItems: chi tiết món trong đơn
CREATE TABLE OrderItems (
    OrderItemId INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId     INT           NOT NULL,
    MenuItemId  INT           NOT NULL,
    Quantity    INT           NOT NULL,
    UnitPrice   DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Order    FOREIGN KEY (OrderId)    REFERENCES Orders(OrderId),
    CONSTRAINT FK_OrderItems_MenuItem FOREIGN KEY (MenuItemId) REFERENCES MenuItems(MenuItemId)
);
GO

-- 9. Payments: ghi nhận thanh toán
CREATE TABLE Payments (
    PaymentId     INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId       INT           NOT NULL,
    Amount        DECIMAL(10,2) NOT NULL,
    PaymentMethod NVARCHAR(50)  NOT NULL,
    PaidAt        DATETIME2     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payments_Order FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);
GO

-- 10. EmailVerifications: token xác thực email / reset mật khẩu
CREATE TABLE EmailVerifications (
    VerificationId INT              IDENTITY(1,1) PRIMARY KEY,
    UserId         INT              NOT NULL,
    Token          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Type           NVARCHAR(20)     NOT NULL,  -- 'Registration' | 'PasswordReset'
    Expiration     DATETIME2        NOT NULL,
    IsUsed         BIT              NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_EmailVerifications_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- 11. Reservations: đặt bàn
CREATE TABLE Reservations (
    ReservationId   INT           IDENTITY(1,1) PRIMARY KEY,
    UserId          INT           NULL,                         -- NULL nếu guest
    GuestName       NVARCHAR(100) NULL,
    GuestContact    NVARCHAR(100) NULL,
    TableId         INT           NOT NULL,
    ReservationDate DATE          NOT NULL,
    ReservationTime TIME(0)       NOT NULL,
    NumberOfGuests  INT           NOT NULL DEFAULT 1,
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    SpecialRequests NVARCHAR(500) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reservations_User  FOREIGN KEY (UserId)   REFERENCES Users(UserId),
    CONSTRAINT FK_Reservations_Table FOREIGN KEY (TableId)  REFERENCES RestaurantTables(TableId),
    CONSTRAINT CHK_Reservations_Hours CHECK (ReservationTime BETWEEN '10:00' AND '22:00')
);
GO

CREATE UNIQUE INDEX UX_Reservations_Slot
    ON Reservations (TableId, ReservationDate, ReservationTime);
GO

-- 12. Invoices: lưu thông tin hoá đơn
CREATE TABLE Invoices (
    InvoiceId        INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId          INT           NOT NULL,
    UserId           INT           NULL,
    CustomerName     NVARCHAR(100) NOT NULL,
    CustomerContact  NVARCHAR(200) NOT NULL,
    OrderType        NVARCHAR(20)  NOT NULL,
    TableId          INT           NULL,
    InvoiceDate      DATETIME2     NOT NULL DEFAULT GETDATE(),
    TotalAmount      DECIMAL(10,2) NOT NULL,
    PaymentMethod    NVARCHAR(50)  NOT NULL,
    CONSTRAINT FK_Invoices_Order FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    CONSTRAINT FK_Invoices_User  FOREIGN KEY (UserId)  REFERENCES Users(UserId)
);
GO

-- 13. InvoiceDetails: chi tiết hoá đơn
CREATE TABLE InvoiceDetails (
    InvoiceDetailId  INT           IDENTITY(1,1) PRIMARY KEY,
    InvoiceId        INT           NOT NULL,
    MenuItemId       INT           NOT NULL,
    Quantity         INT           NOT NULL,
    UnitPrice        DECIMAL(10,2) NOT NULL,
    LineTotal        AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_InvoiceDetails_Invoice FOREIGN KEY (InvoiceId)   REFERENCES Invoices(InvoiceId),
    CONSTRAINT FK_InvoiceDetails_MenuItem FOREIGN KEY (MenuItemId) REFERENCES MenuItems(MenuItemId)
);
GO


## 2. Cài package tools EF , EF Core cho SQL Server , # Scaffold toàn bộ database để sinh models
# Cài package tools EF nếu chưa có
dotnet tool install --global dotnet-ef

# Cài các package EF Core cho SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package BCrypt.Net-Next

# Scaffold toàn bộ database

dotnet ef dbcontext scaffold "Server=.;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context AppDbContext --data-annotations --use-database-names










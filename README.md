# RestaurantDB Schema

Các script T-SQL và hướng dẫn EF Core CLI để khởi tạo và scaffold toàn bộ database **RestaurantDB**, đã bổ sung thêm bảng **Reservations**.

---

## 1. SQL Script khởi tạo Database

**File:** `scripts/init-restaurantdb.sql`

```sql
-- 1. Tạo Database và chọn context
CREATE DATABASE RestaurantDB;
GO

USE RestaurantDB;
GO

-- 2. Bảng Roles: danh sách các quyền (User, Employee, Admin)
CREATE TABLE Roles (
    RoleId    INT           IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(50)  NOT NULL UNIQUE
);
GO

-- 3. Bảng Users: thông tin tài khoản, bao gồm email và trạng thái xác thực
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

-- 4. Bảng UserRoles: ánh xạ nhiều-nhiều giữa Users và Roles
CREATE TABLE UserRoles (
    UserId     INT        NOT NULL,
    RoleId     INT        NOT NULL,
    AssignedAt DATETIME2  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UserRoles       PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User  FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_UserRoles_Role  FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- 5. Bảng RestaurantTables: quản lý bàn ăn
CREATE TABLE RestaurantTables (
    TableId     INT          IDENTITY(1,1) PRIMARY KEY,
    TableNumber NVARCHAR(10) NOT NULL UNIQUE,
    Seats       INT          NOT NULL,
    Status      NVARCHAR(20) NOT NULL DEFAULT 'Available'
);
GO

-- 6. Bảng MenuItems: danh sách món ăn cho trang Menu
CREATE TABLE MenuItems (
    MenuItemId  INT           IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Price       DECIMAL(10,2) NOT NULL,
    Stock       INT           NOT NULL DEFAULT 0,
    ImageUrl    NVARCHAR(255) NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- 7. Bảng FeaturedDishes: chọn món đặc trưng hiển thị trên trang chính
CREATE TABLE FeaturedDishes (
    FeaturedDishId INT          IDENTITY(1,1) PRIMARY KEY,
    MenuItemId     INT          NOT NULL,
    DisplayOrder   INT          NOT NULL DEFAULT 0,
    IsActive       BIT          NOT NULL DEFAULT 1,
    CreatedAt      DATETIME2    NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_FeaturedDishes_MenuItem
        FOREIGN KEY (MenuItemId) REFERENCES MenuItems(MenuItemId)
);
GO

-- 8. Bảng Orders: quản lý đơn hàng (tại bàn hoặc takeaway)
CREATE TABLE Orders (
    OrderId    INT           IDENTITY(1,1) PRIMARY KEY,
    UserId     INT           NOT NULL,
    TableId    INT           NULL,  -- NULL nếu takeaway
    Status     NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    CreatedAt  DATETIME2     NOT NULL DEFAULT GETDATE(),
    UpdatedAt  DATETIME2     NOT NULL DEFAULT GETDATE(),
    Paid       BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Orders_User  FOREIGN KEY (UserId)  REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Table FOREIGN KEY (TableId) REFERENCES RestaurantTables(TableId)
);
GO

-- 9. Bảng OrderItems: chi tiết món trong đơn hàng
CREATE TABLE OrderItems (
    OrderItemId INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId     INT           NOT NULL,
    MenuItemId  INT           NOT NULL,
    Quantity    INT           NOT NULL,
    UnitPrice   DECIMAL(10,2) NOT NULL,  -- giá tại thời điểm đặt
    CONSTRAINT FK_OrderItems_Order    FOREIGN KEY (OrderId)    REFERENCES Orders(OrderId),
    CONSTRAINT FK_OrderItems_MenuItem FOREIGN KEY (MenuItemId) REFERENCES MenuItems(MenuItemId)
);
GO

-- 10. Bảng Payments: ghi nhận thanh toán cho đơn hàng
CREATE TABLE Payments (
    PaymentId     INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId       INT           NOT NULL,
    Amount        DECIMAL(10,2) NOT NULL,
    PaymentMethod NVARCHAR(50)  NOT NULL,  -- ví dụ 'Stripe', 'Cash'
    PaidAt        DATETIME2     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payments_Order FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);
GO

-- 11. Bảng EmailVerifications: quản lý token xác thực email và reset mật khẩu
CREATE TABLE EmailVerifications (
    VerificationId INT              IDENTITY(1,1) PRIMARY KEY,
    UserId         INT              NOT NULL,
    Token          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Type           NVARCHAR(20)     NOT NULL,  -- 'Registration' | 'PasswordReset'
    Expiration     DATETIME2        NOT NULL,
    IsUsed         BIT              NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_EmailVerifications_User
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- 12. Bảng Reservations: quản lý đặt bàn
CREATE TABLE Reservations (
    ReservationId   INT           IDENTITY(1,1) PRIMARY KEY,
    UserId          INT           NULL,                         -- NULL nếu khách không có tài khoản
    GuestName       NVARCHAR(100) NULL,                         -- chỉ dùng khi UserId IS NULL
    GuestContact    NVARCHAR(100) NULL,                         -- email hoặc điện thoại của khách vãng lai
    TableId         INT           NOT NULL,                     -- bàn được đặt
    ReservationDate DATE          NOT NULL,                     -- ngày đặt
    ReservationTime TIME(0)       NOT NULL,                     -- giờ đặt
    NumberOfGuests  INT           NOT NULL DEFAULT 1,           -- số lượng khách
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',   -- Pending, Confirmed, Cancelled
    SpecialRequests NVARCHAR(500) NULL,                         -- yêu cầu đặc biệt
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reservations_User  FOREIGN KEY (UserId)   REFERENCES Users(UserId),
    CONSTRAINT FK_Reservations_Table FOREIGN KEY (TableId)  REFERENCES RestaurantTables(TableId),
    CONSTRAINT CHK_Reservations_Hours CHECK (ReservationTime BETWEEN '10:00' AND '22:00')
);
GO

-- Ngăn trùng giờ đặt cho cùng bàn
CREATE UNIQUE INDEX UX_Reservations_Slot
    ON Reservations (TableId, ReservationDate, ReservationTime);
GO

-- 13. Seed dữ liệu Roles
INSERT INTO Roles (Name) VALUES ('User'), ('Employee'), ('Admin');
GO

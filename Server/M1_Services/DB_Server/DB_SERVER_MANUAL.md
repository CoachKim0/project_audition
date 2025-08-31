# DB_Server 매뉴얼

> 데이터베이스 및 인증 서비스 - MySQL + Entity Framework Core

## 📋 목차
- [서비스 개요](#서비스-개요)
- [기술 스택](#기술-스택)
- [데이터베이스 스키마](#데이터베이스-스키마)
- [API 엔드포인트](#api-엔드포인트)
- [설정 및 실행](#설정-및-실행)
- [개발 가이드](#개발-가이드)
- [트러블슈팅](#트러블슈팅)

---

## 🎯 서비스 개요

### 역할
- **사용자 인증 및 권한 관리**
- **사용자 정보 CRUD 작업**  
- **게임방 데이터 관리**
- **세션 관리**
- **보안 이벤트 처리**

### 서비스 포트
- **gRPC**: 5553 (서비스 간 통신)

### 의존성
- MySQL Server 8.0+
- Entity Framework Core 8.0

---

## 🛠️ 기술 스택

### 백엔드 프레임워크
```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.62.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

### ORM 및 데이터베이스
- **Entity Framework Core 8.0**
- **MySQL 8.0** (Pomelo MySQL Provider)
- **Code First Migration**
- **LINQ to Entities**

---

## 🗄️ 데이터베이스 스키마

### 1. Users 테이블
```sql
CREATE TABLE `Users` (
    `UserId` varchar(36) PRIMARY KEY,
    `Username` varchar(50) UNIQUE NOT NULL,
    `PasswordHash` varchar(255) NOT NULL,
    `Email` varchar(100) UNIQUE NOT NULL,
    `Nickname` varchar(50),
    `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
    `LastLoginAt` datetime NULL,
    `IsActive` boolean DEFAULT 1,
    
    INDEX `IX_Users_Username` (`Username`),
    INDEX `IX_Users_Email` (`Email`)
);
```

### 2. UserSessions 테이블
```sql
CREATE TABLE `UserSessions` (
    `SessionId` varchar(128) PRIMARY KEY,
    `UserId` varchar(36) NOT NULL,
    `Token` varchar(512) NOT NULL,
    `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
    `ExpiresAt` datetime NOT NULL,
    `IsActive` boolean DEFAULT 1,
    `IpAddress` varchar(45),
    `UserAgent` varchar(512),
    
    INDEX `IX_UserSessions_Token` (`Token`),
    INDEX `IX_UserSessions_UserId_IsActive` (`UserId`, `IsActive`),
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE
);
```

### 3. GameRooms 테이블
```sql
CREATE TABLE `GameRooms` (
    `RoomId` varchar(36) PRIMARY KEY,
    `RoomName` varchar(100) NOT NULL,
    `CreatedBy` varchar(36),
    `MaxPlayers` int DEFAULT 8,
    `CurrentPlayers` int DEFAULT 0,
    `Status` varchar(20) DEFAULT 'Waiting',
    `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
    `StartedAt` datetime NULL,
    `FinishedAt` datetime NULL,
    `GameSettings` json,
    
    INDEX `IX_GameRooms_Status` (`Status`),
    INDEX `IX_GameRooms_CreatedBy` (`CreatedBy`),
    FOREIGN KEY (`CreatedBy`) REFERENCES `Users`(`UserId`) ON DELETE SET NULL
);
```

### 4. GameRoomPlayers 테이블
```sql
CREATE TABLE `GameRoomPlayers` (
    `Id` varchar(36) PRIMARY KEY,
    `RoomId` varchar(36) NOT NULL,
    `UserId` varchar(36) NOT NULL,
    `JoinedAt` datetime DEFAULT CURRENT_TIMESTAMP,
    `LeftAt` datetime NULL,
    `IsActive` boolean DEFAULT 1,
    `PlayerSlot` int DEFAULT 0,
    `PlayerData` json,
    
    UNIQUE KEY `IX_GameRoomPlayers_RoomId_UserId` (`RoomId`, `UserId`),
    INDEX `IX_GameRoomPlayers_RoomId_PlayerSlot` (`RoomId`, `PlayerSlot`),
    FOREIGN KEY (`RoomId`) REFERENCES `GameRooms`(`RoomId`) ON DELETE CASCADE,
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE
);
```

---

## 🔌 API 엔드포인트

### 인증 서비스 (AuthService)

#### 1. 로그인
```protobuf
rpc Login(LoginRequest) returns (LoginResponse);

message LoginRequest {
  string username = 1;
  string password = 2;
}

message LoginResponse {
  bool success = 1;
  string token = 2;
  string user_id = 3;
  string message = 4;
}
```

**사용 예시:**
```csharp
var response = await authClient.LoginAsync(new LoginRequest 
{
    Username = "testuser",
    Password = "password123"
});

if (response.Success) 
{
    Console.WriteLine($"로그인 성공! 토큰: {response.Token}");
}
```

#### 2. 회원가입
```protobuf
rpc Register(RegisterRequest) returns (RegisterResponse);

message RegisterRequest {
  string username = 1;
  string password = 2;
  string email = 3;
  string nickname = 4;
}
```

#### 3. 토큰 검증
```protobuf
rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);

message ValidateTokenRequest {
  string token = 1;
}

message ValidateTokenResponse {
  bool valid = 1;
  string user_id = 2;
}
```

### 사용자 서비스 (UserService)

#### 1. 사용자 정보 조회
```protobuf
rpc GetUser(GetUserRequest) returns (GetUserResponse);

message GetUserRequest {
  string user_id = 1;
}

message GetUserResponse {
  string user_id = 1;
  string username = 2;
  string nickname = 3;
  string email = 4;
  int64 created_at = 5;
  int64 last_login = 6;
}
```

#### 2. 사용자 정보 수정
```protobuf
rpc UpdateUser(UpdateUserRequest) returns (UpdateUserResponse);

message UpdateUserRequest {
  string user_id = 1;
  string nickname = 2;
  string email = 3;
}
```

#### 3. 사용자 목록 조회
```protobuf
rpc GetUserList(GetUserListRequest) returns (GetUserListResponse);

message GetUserListRequest {
  int32 page = 1;
  int32 page_size = 2;
}
```

---

## ⚙️ 설정 및 실행

### 1. MySQL 서버 준비

#### MySQL 설치 (Windows)
```bash
# MySQL 8.0 설치 후
mysql -u root -p
CREATE DATABASE GameDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

#### MySQL 설치 (Docker)
```bash
docker run --name mysql-game \
  -e MYSQL_ROOT_PASSWORD=1234 \
  -e MYSQL_DATABASE=GameDatabase \
  -p 3306:3306 \
  -d mysql:8.0
```

### 2. 연결 문자열 설정

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GameDatabase;Uid=root;Pwd=1234;CharSet=utf8mb4;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### 개발 환경 (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GameDatabase_Dev;Uid=root;Pwd=1234;CharSet=utf8mb4;"
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 3. 서비스 실행

#### 개발 환경 실행
```bash
cd DB_Server
dotnet run
```

#### 운영 환경 실행
```bash
cd DB_Server
dotnet run --environment Production
```

#### 실행 로그 확인사항
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://[::]:5553
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: DB_Server.Program[0]
      데이터베이스 마이그레이션 시작...
info: DB_Server.Program[0]
      데이터베이스 초기화 완료
DB Server 시작됨 (gRPC 포트: 5553)
```

---

## 💻 개발 가이드

### 1. 새로운 엔티티 추가

#### Step 1: 엔티티 클래스 생성
```csharp
// Data/Entities/NewEntity.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServer.Data.Entities;

[Table("NewEntities")]
public class NewEntity
{
    [Key]
    [StringLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### Step 2: DbContext에 추가
```csharp
// Data/Context/GameDbContext.cs
public DbSet<NewEntity> NewEntities { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... 기존 설정

    modelBuilder.Entity<NewEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
}
```

#### Step 3: 마이그레이션 생성 및 적용
```bash
# 마이그레이션 생성
dotnet ef migrations add AddNewEntity

# 데이터베이스 업데이트
dotnet ef database update
```

### 2. 새로운 서비스 메소드 추가

#### Step 1: 인터페이스 확장
```csharp
// Services/IUserService.cs
public interface IUserService
{
    // 기존 메소드들...
    Task<bool> NewMethodAsync(string parameter);
}
```

#### Step 2: 구현체 추가
```csharp
// Services/UserService.cs
public async Task<bool> NewMethodAsync(string parameter)
{
    try
    {
        // 비즈니스 로직 구현
        var entity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == parameter);
        // ... 로직
        await _context.SaveChangesAsync();
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "NewMethod 실행 중 오류 발생");
        return false;
    }
}
```

#### Step 3: gRPC 서비스 확장 (필요시)
```protobuf
// Protos/db_service.proto
service UserService {
  // 기존 메소드들...
  rpc NewMethod(NewMethodRequest) returns (NewMethodResponse);
}

message NewMethodRequest {
  string parameter = 1;
}

message NewMethodResponse {
  bool success = 1;
  string message = 2;
}
```

### 3. 데이터베이스 쿼리 최적화

#### 인덱스 추가
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        // 복합 인덱스 추가
        entity.HasIndex(e => new { e.Username, e.IsActive });
        
        // 고유 인덱스 추가
        entity.HasIndex(e => e.Email).IsUnique();
    });
}
```

#### 효율적인 쿼리 작성
```csharp
// 좋은 예: 필요한 필드만 선택
var users = await _context.Users
    .Where(u => u.IsActive)
    .Select(u => new { u.UserId, u.Username, u.Nickname })
    .ToListAsync();

// 나쁜 예: 모든 데이터 로드
var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
```

---

## 🔍 트러블슈팅

### 1. 연결 오류

#### MySQL 연결 실패
```
MySqlException: Unable to connect to any of the specified MySQL hosts.
```

**해결 방법:**
```bash
# MySQL 서버 상태 확인
systemctl status mysql  # Linux
net start mysql         # Windows

# 방화벽 확인
netstat -an | findstr 3306

# 연결 문자열 확인
mysql -h localhost -u root -p
```

#### 권한 오류
```
MySqlException: Access denied for user 'root'@'localhost'
```

**해결 방법:**
```sql
-- MySQL 콘솔에서 실행
ALTER USER 'root'@'localhost' IDENTIFIED BY '1234';
FLUSH PRIVILEGES;

-- 또는 새 사용자 생성
CREATE USER 'gameuser'@'localhost' IDENTIFIED BY '1234';
GRANT ALL PRIVILEGES ON GameDatabase.* TO 'gameuser'@'localhost';
```

### 2. 마이그레이션 오류

#### 테이블 이미 존재
```
MySqlException: Table 'Users' already exists
```

**해결 방법:**
```bash
# 마이그레이션 히스토리 확인
dotnet ef migrations list

# 특정 마이그레이션으로 되돌리기
dotnet ef database update PreviousMigration

# 마이그레이션 제거
dotnet ef migrations remove
```

### 3. 성능 문제

#### 느린 쿼리 진단
```csharp
// DbContext에서 로그 활성화
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging() // 개발환경에서만
        .EnableDetailedErrors();
}
```

#### 연결 풀 최적화
```csharp
// Program.cs
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(3);
        mySqlOptions.CommandTimeout(30);
    }));
```

### 4. gRPC 통신 오류

#### 서비스 찾을 수 없음
```
RpcException: Status(StatusCode="Unimplemented", Detail="Service is unimplemented.")
```

**해결 방법:**
```csharp
// Program.cs에서 서비스 등록 확인
app.MapGrpcService<AuthGrpcService>();
app.MapGrpcService<UserGrpcService>();

// proto 파일 컴파일 확인
dotnet build
```

---

## 📊 모니터링 및 성능

### 1. 헬스 체크 설정
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GameDbContext>();

app.MapHealthChecks("/health");
```

### 2. 메트릭 수집
```csharp
// 쿼리 실행 시간 측정
using var activity = Activity.StartActivity("Database.GetUser");
var user = await _context.Users.FindAsync(userId);
activity?.SetTag("userId", userId);
```

### 3. 로그 레벨 설정
```json
{
  "Logging": {
    "LogLevel": {
      "DbServer.Services": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## 🔒 보안 가이드

### 1. 비밀번호 보안
```csharp
// 더 강력한 해싱 알고리즘 사용 (BCrypt 권장)
public static string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
}

public static bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### 2. SQL 인젝션 방지
```csharp
// 좋은 예: 파라미터화된 쿼리
var users = await _context.Users
    .Where(u => u.Username == username)  // EF가 자동으로 파라미터화
    .ToListAsync();

// 나쁜 예: 문자열 연결 (사용 금지)
var sql = $"SELECT * FROM Users WHERE Username = '{username}'";
```

### 3. 토큰 보안
```csharp
// JWT 토큰 사용 권장 (추후 구현)
public class JwtTokenService
{
    public string GenerateToken(string userId, TimeSpan expiry)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("userId", userId) }),
            Expires = DateTime.UtcNow.Add(expiry),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

*이 매뉴얼은 DB_Server의 완전한 가이드입니다. 추가 질문이나 개선 제안이 있으시면 개발팀에 문의해주세요.*
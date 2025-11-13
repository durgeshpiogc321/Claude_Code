# User Story: Login Activity Module
## User Login Activity Tracking & Management

**Project:** UserManagement - LoginandRegisterMVC Enhancement  
**Module:** Login Activity  
**Created:** November 12, 2025  
**Version:** 1.0  
**Purpose:** Track, monitor, and manage user login activities for security and auditing  
**Total Stories:** 12  
**Estimated Effort:** 38 hours

---

## Executive Summary

This module focuses on comprehensive login activity tracking including login history, failed login attempts, session management, device tracking, and security monitoring.

### Current State:
- ❌ No login history tracking
- ❌ No failed login attempt logging
- ❌ No session management
- ❌ No device/browser tracking
- ❌ No login statistics
- ❌ No suspicious activity detection

### Target State:
- ✅ Complete login history database
- ✅ Success and failure login tracking
- ✅ Device and browser information capture
- ✅ IP address and location tracking
- ✅ Login history view page
- ✅ Login statistics dashboard
- ✅ Failed login attempt monitoring
- ✅ Session management
- ✅ Suspicious login detection
- ✅ Login activity export

---

## EPIC: User Login Activity Tracking

### Story LA.1: Create Login History Database Table
**As a** developer  
**I want to** store login history  
**So that** we can track all login attempts

**Acceptance Criteria:**
- [ ] Create LoginHistory model
- [ ] Fields: LoginId, UserId, LoginTime, IPAddress, UserAgent, Browser, OperatingSystem, DeviceType, Location, Success, FailureReason
- [ ] Create database migration
- [ ] Add foreign key to Users table
- [ ] Add indexes for performance

**Estimated Effort:** 3 hours

---

### Story LA.2: Log Successful Login Attempts
**As a** developer  
**I want to** log successful logins  
**So that** we have a record of user access

**Acceptance Criteria:**
- [ ] Add logging code to Login action (successful path)
- [ ] Capture IP address from HttpContext
- [ ] Parse UserAgent to extract browser and OS
- [ ] Determine device type (Desktop, Mobile, Tablet)
- [ ] Save to LoginHistory table with Success = true
- [ ] Update Users.LastLoginAt field

**Estimated Effort:** 4 hours

---

### Story LA.3: Log Failed Login Attempts
**As a** developer  
**I want to** log failed login attempts  
**So that** we can detect suspicious activity

**Acceptance Criteria:**
- [ ] Add logging code to Login action (failure path)
- [ ] Capture IP address
- [ ] Parse UserAgent information
- [ ] Save to LoginHistory table with Success = false
- [ ] Record failure reason (Invalid credentials, User not found, Account locked)
- [ ] Track failed attempts per IP address

**Estimated Effort:** 3 hours

---

### Story LA.4: Create Login History View Page
**As an** admin  
**I want to** view login history  
**So that** I can audit user access

**Acceptance Criteria:**
- [ ] Create LoginHistory/Index.cshtml view
- [ ] Create LoginHistoryController
- [ ] Display login history in table format
- [ ] Show columns: Username, Login Time, IP Address, Browser, Device, Location, Status
- [ ] Add pagination (20 records per page)
- [ ] Style success/failure with badges (green/red)

**Estimated Effort:** 5 hours

---

### Story LA.5: Add Login History Filters
**As an** admin  
**I want to** filter login history  
**So that** I can find specific records

**Acceptance Criteria:**
- [ ] Add "Filter by User" dropdown
- [ ] Add "Filter by Status" (All, Success, Failed)
- [ ] Add "Date Range" picker (From Date, To Date)
- [ ] Add "Filter by IP Address" text box
- [ ] Add "Filter by Device Type" dropdown
- [ ] Implement filter logic in controller
- [ ] Show filtered results count

**Estimated Effort:** 4 hours

---

### Story LA.6: Create User's Own Login History Page
**As a** user  
**I want to** view my login history  
**So that** I can monitor my account security

**Acceptance Criteria:**
- [ ] Create LoginHistory/MyLogins.cshtml view
- [ ] Create MyLogins action (filtered by current user)
- [ ] Display last 50 login attempts
- [ ] Show login time, IP address, device, location, status
- [ ] Add "Report Suspicious Activity" button
- [ ] Add link from user profile page

**Estimated Effort:** 3 hours

---

### Story LA.7: Implement Failed Login Attempt Lockout
**As a** developer  
**I want to** lock accounts after multiple failed attempts  
**So that** we prevent brute force attacks

**Acceptance Criteria:**
- [ ] Add FailedLoginAttempts field to User model
- [ ] Add AccountLockedUntil field to User model
- [ ] Count failed attempts per user/IP
- [ ] Lock account after 5 failed attempts
- [ ] Lock account for 30 minutes
- [ ] Show lockout message to user
- [ ] Reset counter on successful login

**Estimated Effort:** 5 hours

---

### Story LA.8: Add Login Statistics Widget
**As an** admin  
**I want to** see login statistics  
**So that** I can monitor system access

**Acceptance Criteria:**
- [ ] Create statistics widget on dashboard
- [ ] Show "Total Logins Today" count
- [ ] Show "Successful Logins" count
- [ ] Show "Failed Logins" count
- [ ] Show "Unique Users Logged In" count
- [ ] Display with icons and colors
- [ ] Add link to full login history

**Estimated Effort:** 3 hours

---

### Story LA.9: Create Login Activity Chart
**As an** admin  
**I want to** see login trends  
**So that** I can visualize access patterns

**Acceptance Criteria:**
- [ ] Create line chart showing last 30 days
- [ ] X-axis: Dates, Y-axis: Login count
- [ ] Show two lines: Successful logins (green), Failed logins (red)
- [ ] Use Chart.js library
- [ ] Make chart interactive with tooltips
- [ ] Add chart to dashboard or reports page

**Estimated Effort:** 4 hours

---

### Story LA.10: Implement Suspicious Login Detection
**As a** developer  
**I want to** detect suspicious login patterns  
**So that** we can alert administrators

**Acceptance Criteria:**
- [ ] Detect login from new location
- [ ] Detect login from new device
- [ ] Detect multiple failed attempts from same IP
- [ ] Detect login outside normal hours (optional)
- [ ] Flag suspicious logins in database
- [ ] Show warning badge in login history
- [ ] Send email notification to admin (future)

**Estimated Effort:** 4 hours

---

### Story LA.11: Add Login History Export
**As an** admin  
**I want to** export login history  
**So that** I can analyze data offline

**Acceptance Criteria:**
- [ ] Add "Export to CSV" button on login history page
- [ ] Create ExportToCSV action
- [ ] Include columns: Username, Login Time, IP Address, Browser, Device, Location, Status
- [ ] Export respects current filters
- [ ] Download file with timestamp in name
- [ ] Add "Export to PDF" option (optional)

**Estimated Effort:** 3 hours

---

### Story LA.12: Display Active Sessions
**As a** user  
**I want to** see my active sessions  
**So that** I can manage account security

**Acceptance Criteria:**
- [ ] Create Sessions/Index.cshtml view
- [ ] Display active sessions in table
- [ ] Show device, browser, location, last activity time
- [ ] Highlight current session
- [ ] Add "End Session" button for each session
- [ ] Add "End All Other Sessions" button
- [ ] Show session expiration time

**Estimated Effort:** 4 hours

---

## Technical Architecture

### Database Schema

```sql
-- Login History table
CREATE TABLE LoginHistory (
    LoginId INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(128) FOREIGN KEY REFERENCES Users(UserId),
    LoginTime DATETIME2 NOT NULL DEFAULT GETDATE(),
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    Browser NVARCHAR(100),
    OperatingSystem NVARCHAR(100),
    DeviceType NVARCHAR(50), -- Desktop, Mobile, Tablet
    Location NVARCHAR(200), -- City, Country (optional, requires IP geolocation service)
    Success BIT NOT NULL,
    FailureReason NVARCHAR(200), -- Invalid credentials, User not found, Account locked
    IsSuspicious BIT DEFAULT 0,
    SessionToken NVARCHAR(500), -- For session tracking
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Indexes for performance
CREATE INDEX IX_LoginHistory_UserId ON LoginHistory(UserId);
CREATE INDEX IX_LoginHistory_LoginTime ON LoginHistory(LoginTime);
CREATE INDEX IX_LoginHistory_IpAddress ON LoginHistory(IpAddress);
CREATE INDEX IX_LoginHistory_Success ON LoginHistory(Success);
CREATE INDEX IX_LoginHistory_IsSuspicious ON LoginHistory(IsSuspicious);

-- Update Users table for lockout
ALTER TABLE Users ADD FailedLoginAttempts INT DEFAULT 0;
ALTER TABLE Users ADD AccountLockedUntil DATETIME2 NULL;
ALTER TABLE Users ADD LastLoginAt DATETIME2 NULL;

-- User Sessions table
CREATE TABLE UserSessions (
    SessionId INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(128) FOREIGN KEY REFERENCES Users(UserId),
    SessionToken NVARCHAR(500) NOT NULL UNIQUE,
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    Browser NVARCHAR(100),
    OperatingSystem NVARCHAR(100),
    DeviceType NVARCHAR(50),
    Location NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    LastActivity DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
CREATE INDEX IX_UserSessions_SessionToken ON UserSessions(SessionToken);
CREATE INDEX IX_UserSessions_IsActive ON UserSessions(IsActive);
```

### LoginHistory Model

```csharp
public class LoginHistory
{
    public int LoginId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }
    public string? Location { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public bool IsSuspicious { get; set; } = false;
    public string? SessionToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation property
    public User? User { get; set; }
}
```

### UserAgent Parsing Service

```csharp
public interface IUserAgentParser
{
    string GetBrowser(string userAgent);
    string GetOperatingSystem(string userAgent);
    string GetDeviceType(string userAgent);
}

public class UserAgentParser : IUserAgentParser
{
    public string GetBrowser(string userAgent)
    {
        // Parse browser from UserAgent string
        // Use library like UAParser.NET or custom parsing
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        return "Unknown";
    }
    
    public string GetOperatingSystem(string userAgent)
    {
        // Parse OS from UserAgent string
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac")) return "macOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iOS")) return "iOS";
        return "Unknown";
    }
    
    public string GetDeviceType(string userAgent)
    {
        if (userAgent.Contains("Mobile")) return "Mobile";
        if (userAgent.Contains("Tablet")) return "Tablet";
        return "Desktop";
    }
}
```

### Login Logging Service

```csharp
public interface ILoginHistoryService
{
    Task LogLoginAsync(string userId, bool success, string? failureReason = null);
    Task<List<LoginHistory>> GetUserLoginHistoryAsync(string userId, int count = 50);
    Task<List<LoginHistory>> GetAllLoginHistoryAsync(LoginHistoryFilter filter);
    Task<int> GetFailedLoginAttemptsAsync(string userId, string? ipAddress = null);
    Task LockAccountAsync(string userId, int minutes = 30);
    Task<bool> IsAccountLockedAsync(string userId);
}

public class LoginHistoryService : ILoginHistoryService
{
    private readonly UserContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAgentParser _userAgentParser;
    
    public async Task LogLoginAsync(string userId, bool success, string? failureReason = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        
        var loginHistory = new LoginHistory
        {
            UserId = userId,
            LoginTime = DateTime.Now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Browser = _userAgentParser.GetBrowser(userAgent ?? ""),
            OperatingSystem = _userAgentParser.GetOperatingSystem(userAgent ?? ""),
            DeviceType = _userAgentParser.GetDeviceType(userAgent ?? ""),
            Success = success,
            FailureReason = failureReason
        };
        
        _context.LoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
    }
    
    // Additional methods...
}
```

### Required NuGet Packages

```xml
<!-- User Agent Parsing (Optional) -->
<PackageReference Include="UAParser" Version="3.1.47" />

<!-- IP Geolocation (Optional, for location tracking) -->
<PackageReference Include="MaxMind.GeoIP2" Version="4.0.0" />
```

---

## Summary

**Module:** Login Activity  
**Total Stories:** 12  
**Total Estimated Effort:** 38 hours  
**Dependencies:** User Management Module, Dashboard Module  
**Priority:** High (Security & Compliance)

### Key Deliverables:
- ✅ Complete login history tracking
- ✅ Success and failure login logging
- ✅ Device and browser tracking
- ✅ Login history view pages (Admin & User)
- ✅ Login statistics widgets
- ✅ Failed login attempt lockout
- ✅ Suspicious login detection
- ✅ Active session management
- ✅ Login activity export

---

## Security Considerations

1. **Data Privacy:** Ensure login history complies with privacy regulations
2. **IP Address Storage:** Consider GDPR implications for IP address storage
3. **Session Security:** Use secure session tokens
4. **Rate Limiting:** Implement rate limiting for login attempts
5. **Encryption:** Consider encrypting sensitive login data

---

**Related Modules:**
- Requires `User_Story_User_Management_Module.md` to be completed first
- See `User_Story_Dashboard_Module.md` for login statistics widgets
- See `User_Story_Advanced_Features_Module.md` for general activity logging

---

**End of Module**


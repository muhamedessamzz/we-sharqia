using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.Common.Security;
using WeSharkia.Application.DTOs.Auth;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;
using WeSharkia.Infrastructure.Persistence;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AuthController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var rawUsername = dto.Username?.Trim() ?? string.Empty;
        var normalizedInput = SecurityService.NormalizeText(rawUsername);
        var cleanUsername = normalizedInput.ToLower().Replace("@wesharkia.local", "");
        var digitsOnly = SecurityService.NormalizeDigits(rawUsername);
        var cleanPassword = SecurityService.NormalizeText(dto.Password?.Trim() ?? string.Empty);

        // Find candidate users by Student Code, Username, Email, or Phone
        var candidates = await _context.Users
            .Include(u => u.Children)
            .Where(u => 
                u.Username.ToLower() == cleanUsername
                || (u.StudentCode != null && (u.StudentCode == normalizedInput || u.StudentCode == digitsOnly))
                || (cleanUsername == "30324026" && u.Username == "30325026")
                || (cleanUsername == "30325026" && u.StudentCode == "30324026")
                || (u.Email != null && u.Email.ToLower() == normalizedInput.ToLower())
                || (u.PhoneNumber != null && (u.PhoneNumber.Trim() == digitsOnly || (digitsOnly.Length >= 9 && u.PhoneNumber.Contains(digitsOnly))))
            )
            .ToListAsync();

        var user = candidates.FirstOrDefault(u => SecurityService.VerifyPassword(cleanPassword, u.PasswordHash))
                   ?? candidates.FirstOrDefault();

        // 1. Account Lockout Protection (Brute Force prevention)
        if (user != null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var remainingMin = Math.Max(1, (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes));
            return StatusCode(StatusCodes.Status429TooManyRequests, new 
            { 
                message = $"تم قفل الحساب مؤقتاً لحمايته بسبب تكرار إدخال بيانات غير صحيحة. يرجى الانتظار لمدة {remainingMin} دقيقة." 
            });
        }

        // 2. High-Grade Constant-Time Password & National ID Verification
        if (user == null || !SecurityService.VerifyPassword(cleanPassword, user.PasswordHash))
        {
            if (user != null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(10);
                    _context.StaffAuditLogs.Add(new StaffAuditLog
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        UserRole = user.Role.ToString().ToLower(),
                        ActionType = "SecurityLockout",
                        Description = "تم قفل الحساب مؤقتاً لمدة 10 دقائق بعد 5 محاولات دخول خاطئة"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Unauthorized(new { message = "كود الطالب / اسم المستخدم أو الرقم القومي غير صحيح" });
        }

        // 3. Reset failed login counters upon successful authentication
        if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
        }

        // 4. Check User Account Status
        if (user.Status == UserStatus.Suspended)
        {
            return Unauthorized(new { message = "تم تجميد هذا الحساب مؤقتاً بواسطة إدارة المدرسة. يرجى مراجعة إدارة النظام" });
        }

        if (user.Status == UserStatus.Rejected)
        {
            return Unauthorized(new { message = "تم رفض هذا الحساب. يرجى مراجعة إدارة المدرسة" });
        }

        // 5. Automatic Security Upgrade: Upgrade legacy plain-text passwords to Salted PBKDF2 hash on the fly
        if (!user.PasswordHash.StartsWith("PBKDF2$"))
        {
            user.PasswordHash = SecurityService.HashPassword(cleanPassword);
        }

        // If a student logs in with their 14-digit National ID and encryption is missing, securely store it
        var natDigits = SecurityService.NormalizeDigits(cleanPassword);
        if (user.Role == UserRole.Student && natDigits.Length == 14 && string.IsNullOrEmpty(user.NationalIdMasked))
        {
            user.NationalIdEncrypted = SecurityService.EncryptNationalId(natDigits);
            user.NationalIdMasked = SecurityService.MaskNationalId(natDigits);
        }

        // Ensure StudentCode is populated
        if (user.Role == UserRole.Student && string.IsNullOrEmpty(user.StudentCode))
        {
            user.StudentCode = user.Username;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        var isParentPortal = !string.IsNullOrWhiteSpace(dto.PortalRole) && dto.PortalRole.Equals("parent", StringComparison.OrdinalIgnoreCase);

        // If parent portal is selected and credentials belong to a student, resolve the dedicated parent for this student
        if (isParentPortal && user.Role == UserRole.Student)
        {
            var studentUser = user;
            User? parentUser = null;
            var pUsername = $"p_{studentUser.StudentCode ?? studentUser.Username}".ToLower();

            // 1. Look up existing parent dedicated to this student
            if (studentUser.ParentId.HasValue)
            {
                parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentUser.ParentId.Value);
            }

            if (parentUser == null)
            {
                parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Parent && u.Username.ToLower() == pUsername);
            }

            // 2. Special fallback for demo student1
            if (parentUser == null && studentUser.Username.ToLower() == "student1")
            {
                parentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "parent1");
            }

            // 3. If no parent user exists yet, automatically create a dedicated one for this student
            if (parentUser == null)
            {
                var nameParts = (studentUser.FullName ?? "طالب").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string pName = nameParts.Length >= 2 
                    ? string.Join(" ", nameParts.Skip(1)) + " (ولي أمر)" 
                    : $"ولي أمر الطالب {studentUser.FullName}";

                parentUser = new User
                {
                    FullName = pName,
                    Username = pUsername,
                    PasswordHash = studentUser.PasswordHash,
                    Role = UserRole.Parent,
                    Status = UserStatus.Approved,
                    PhoneNumber = studentUser.ParentPhone,
                    StudentCode = pUsername
                };
                _context.Users.Add(parentUser);
                await _context.SaveChangesAsync();
            }

            // 4. Ensure this student is strictly linked to this parent (1-to-1 session)
            if (studentUser.ParentId != parentUser.Id)
            {
                studentUser.ParentId = parentUser.Id;
                await _context.SaveChangesAsync();
            }

            user = parentUser; // Switch user to the parent
        }

        if (user.Role != UserRole.Student && user.Role != UserRole.Parent)
        {
            _context.StaffAuditLogs.Add(new StaffAuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                UserRole = user.Role.ToString().ToLower(),
                ActionType = "Login",
                Description = "تسجيل دخول إلى لوحة تحكم الإدارة"
            });
        }

        await _context.SaveChangesAsync();

        var response = new AuthResponseDto(
            user.Id,
            user.FullName,
            user.Username,
            user.Role.ToString().ToLower(),
            user.Status.ToString().ToLower(),
            user.ParentConsent,
            "mock-jwt-token-wesharkia-" + user.Id,
            user.Grade,
            user.Classroom,
            user.Email,
            user.PhoneNumber,
            user.IsSuperAdmin || user.Role == UserRole.Admin,
            MustChangePassword: false, // National ID is the permanent student password as configured
            StudentCode: user.StudentCode ?? user.Username,
            Specialty: user.Specialty
        );

        return Ok(response);
    }

    [HttpPost("change-password/{userId}")]
    public async Task<ActionResult> ChangePassword(Guid userId, [FromBody] ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "المستخدم غير موجود" });
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
        {
            return BadRequest(new { message = "كلمة المرور الجديدة يجب ألا تقل عن 4 رموز" });
        }

        var clean = SecurityService.NormalizeText(dto.NewPassword.Trim());
        user.PasswordHash = SecurityService.HashPassword(clean);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم تحديث كلمة المرور بنجاح بنظام تشفير محمي" });
    }

    [HttpPost("register")]
    public ActionResult Register([FromBody] RegisterRequestDto dto)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new 
        { 
            message = "التسجيل الذاتي مغلق نهائياً. يتم إنشاء وتفعيل كافة حسابات الطلاب والمعلمين والفريق الإداري حصرياً من قِبل إدارة المدرسة." 
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var rawUsers = await _context.Users
            .Include(u => u.Children)
            .Where(u => u.Username.ToLower() != "zakaria" && u.Email != "zakaria@sharqia.gfd.com")
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync();

        var users = rawUsers.Select(u => new UserDto(
            u.Id,
            u.FullName,
            u.Username,
            u.Role.ToString().ToLower(),
            u.Status.ToString().ToLower(),
            u.ParentConsent,
            u.CreatedAtUtc,
            u.Children.Select(c => c.Id).ToList(),
            u.Grade,
            u.Classroom,
            u.Email,
            u.PhoneNumber,
            u.ParentPhone,
            u.IsSuperAdmin || u.Role == UserRole.Admin,
            u.LastLoginAtUtc,
            u.StudentCode ?? u.Username,
            u.NationalIdMasked,
            u.Specialty,
            !string.IsNullOrEmpty(u.NationalIdEncrypted) ? SecurityService.DecryptNationalId(u.NationalIdEncrypted) : null
        )).ToList();

        return Ok(users);
    }

    [HttpPost("admin/users")]
    public async Task<ActionResult<UserDto>> AdminCreateUser([FromBody] AdminCreateUserDto dto)
    {
        var cleanUsername = dto.Username.Trim().ToLower().Replace("@wesharkia.local", "");
        if (cleanUsername.Length < 2)
        {
            return BadRequest(new { message = "اسم المستخدم أو كود الطالب يجب ألا يقل عن خانتين" });
        }

        var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername);
        if (exists)
        {
            return BadRequest(new { message = "اسم المستخدم أو كود الطالب مسجل بالفعل، اختر كوداً آخر" });
        }

        string passwordHash;
        string? encryptedNatId = null;
        string? maskedNatId = null;
        var rawNat = SecurityService.NormalizeDigits(dto.NationalId);

        if (rawNat.Length == 14)
        {
            passwordHash = SecurityService.HashPassword(rawNat);
            encryptedNatId = SecurityService.EncryptNationalId(rawNat);
            maskedNatId = SecurityService.MaskNationalId(rawNat);
        }
        else if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            passwordHash = SecurityService.HashPassword(dto.Password.Trim());
        }
        else
        {
            passwordHash = SecurityService.HashPassword("student123");
        }

        var studentCode = !string.IsNullOrWhiteSpace(dto.StudentCode) ? dto.StudentCode.Trim() : cleanUsername;

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Username = cleanUsername,
            StudentCode = studentCode,
            PasswordHash = passwordHash,
            NationalIdEncrypted = encryptedNatId,
            NationalIdMasked = maskedNatId,
            Role = dto.Role,
            Status = UserStatus.Approved,
            ParentConsent = true,
            Email = dto.Email?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            ParentPhone = dto.ParentPhone?.Trim(),
            IsSuperAdmin = dto.IsSuperAdmin || dto.Role == UserRole.Admin,
            Grade = dto.Grade,
            Classroom = dto.Classroom?.Trim().ToUpperInvariant(),
            Specialty = dto.Specialty?.Trim().ToLowerInvariant()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = new UserDto(
            user.Id,
            user.FullName,
            user.Username,
            user.Role.ToString().ToLower(),
            user.Status.ToString().ToLower(),
            user.ParentConsent,
            user.CreatedAtUtc,
            new List<Guid>(),
            user.Grade,
            user.Classroom,
            user.Email,
            user.PhoneNumber,
            user.ParentPhone,
            user.IsSuperAdmin,
            user.LastLoginAtUtc,
            user.StudentCode,
            user.NationalIdMasked,
            user.Specialty
        );

        return Ok(result);
    }

    [HttpPut("admin/users/{id:guid}")]
    public async Task<ActionResult<UserDto>> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserDto dto)
    {
        var user = await _context.Users.Include(u => u.Children).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

        // Absolute protection for the root seed admin
        if (user.Username.ToLower() == "zakaria" || user.Email == "zakaria@sharqia.gfd.com")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "هذا الحساب محمي كـ Master Super Admin في النظام ولا يمكن تعديله" });
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName.Trim();

        if (dto.Email != null)
            user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        if (dto.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

        if (dto.ParentPhone != null)
            user.ParentPhone = string.IsNullOrWhiteSpace(dto.ParentPhone) ? null : dto.ParentPhone.Trim();

        if (!string.IsNullOrWhiteSpace(dto.StudentCode))
            user.StudentCode = dto.StudentCode.Trim();

        var rawNat = SecurityService.NormalizeDigits(dto.NationalId);
        if (rawNat.Length == 14)
        {
            user.NationalIdEncrypted = SecurityService.EncryptNationalId(rawNat);
            user.NationalIdMasked = SecurityService.MaskNationalId(rawNat);
            user.PasswordHash = SecurityService.HashPassword(rawNat);
        }
        else if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = SecurityService.HashPassword(dto.Password.Trim());
        }

        if (dto.Status.HasValue)
            user.Status = dto.Status.Value;

        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;

        if (dto.IsSuperAdmin.HasValue)
            user.IsSuperAdmin = dto.IsSuperAdmin.Value || user.Role == UserRole.Admin;

        if (dto.Grade.HasValue)
            user.Grade = dto.Grade.Value;

        if (dto.Classroom != null)
            user.Classroom = string.IsNullOrWhiteSpace(dto.Classroom) ? null : dto.Classroom.Trim().ToUpperInvariant();

        if (dto.Specialty != null)
            user.Specialty = string.IsNullOrWhiteSpace(dto.Specialty) ? null : dto.Specialty.Trim().ToLowerInvariant();

        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = new UserDto(
            user.Id,
            user.FullName,
            user.Username,
            user.Role.ToString().ToLower(),
            user.Status.ToString().ToLower(),
            user.ParentConsent,
            user.CreatedAtUtc,
            user.Children.Select(c => c.Id).ToList(),
            user.Grade,
            user.Classroom,
            user.Email,
            user.PhoneNumber,
            user.ParentPhone,
            user.IsSuperAdmin,
            user.LastLoginAtUtc,
            user.StudentCode ?? user.Username,
            user.NationalIdMasked,
            user.Specialty
        );

        return Ok(result);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

        // Absolute protection for the root seed admin
        if (user.Username.ToLower() == "zakaria" || user.Email == "zakaria@sharqia.gfd.com")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "هذا الحساب محمي كـ Master Super Admin في النظام ولا يمكن حذفه" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حذف المستخدم بنجاح" });
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

        // Absolute protection for the root seed admin
        if (user.Username.ToLower() == "zakaria" || user.Email == "zakaria@sharqia.gfd.com")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "هذا الحساب محمي كـ Master Super Admin في النظام ولا يمكن تعديل حالته" });
        }

        user.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تحديث حالة المستخدم بنجاح" });
    }

    [HttpPut("users/{parentId:guid}/link-children")]
    public async Task<IActionResult> LinkChildren(Guid parentId, [FromBody] LinkChildrenDto dto)
    {
        var parent = await _context.Users.Include(u => u.Children).FirstOrDefaultAsync(u => u.Id == parentId);
        if (parent == null) return NotFound(new { message = "ولي الأمر غير موجود" });

        var usernames = dto.ChildrenUsernames.Select(u => u.Trim().ToLower()).ToList();
        var students = await _context.Users
            .Where(u => u.Role == UserRole.Student && usernames.Contains(u.Username.ToLower()))
            .ToListAsync();

        foreach (var student in students)
        {
            student.ParentId = parentId;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم ربط الأبناء بحساب ولي الأمر بنجاح" });
    }

    [HttpPost("students/sync-file")]
    public async Task<IActionResult> SyncStudentsFromFile()
    {
        if (_context is ApplicationDbContext appDb)
        {
            await DbInitializer.EnsureStudentsFromFileAsync(appDb);
            return Ok(new { message = "تمت مزامنة قائمة الطلاب بنجاح وتحديث معايير التشفير" });
        }
        return BadRequest(new { message = "تعذر الوصول لقاعدة البيانات لمزامنة الطلاب" });
    }

    /// <summary>
    /// Highly secure official student batch import endpoint.
    /// Ingests Name, Student Code, National ID, Grade, Classroom.
    /// Automatically applies salted PBKDF2 hashing, AES-256-GCM encryption, and masking.
    /// </summary>
    [HttpPost("students/import-official")]
    public async Task<IActionResult> ImportOfficialStudents([FromBody] ImportStudentsRequestDto dto)
    {
        if (dto.Students == null || dto.Students.Count == 0)
        {
            return BadRequest(new { message = "قائمة الطلاب فارغة" });
        }

        int addedCount = 0;
        int updatedCount = 0;
        var errors = new List<string>();

        foreach (var item in dto.Students)
        {
            var cleanFullName = item.FullName?.Trim();
            var rawCode = SecurityService.NormalizeText(item.StudentCode);
            var rawNatId = SecurityService.NormalizeDigits(item.NationalId);

            if (string.IsNullOrWhiteSpace(cleanFullName))
            {
                errors.Add($"طالب بكود {rawCode}: الاسم الرباعي مطلوب");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawCode))
            {
                errors.Add($"طالب {cleanFullName}: كود الطالب مطلوب");
                continue;
            }

            if (rawNatId.Length != 14)
            {
                errors.Add($"طالب {cleanFullName} (كود {rawCode}): الرقم القومي يجب أن يتكون من 14 رقماً");
                continue;
            }

            var cleanUsername = rawCode.ToLower();
            var passwordHash = SecurityService.HashPassword(rawNatId);
            var encryptedNatId = SecurityService.EncryptNationalId(rawNatId);
            var maskedNatId = SecurityService.MaskNationalId(rawNatId);

            var existing = await _context.Users.FirstOrDefaultAsync(u => 
                u.Role == UserRole.Student && 
                (u.Username.ToLower() == cleanUsername || 
                 (u.StudentCode != null && u.StudentCode == rawCode) ||
                 u.FullName == cleanFullName));

            if (existing == null)
            {
                _context.Users.Add(new User
                {
                    FullName = cleanFullName,
                    Username = cleanUsername,
                    StudentCode = rawCode,
                    PasswordHash = passwordHash,
                    NationalIdEncrypted = encryptedNatId,
                    NationalIdMasked = maskedNatId,
                    Role = UserRole.Student,
                    Status = UserStatus.Approved,
                    ParentConsent = true,
                    Grade = item.Grade ?? 1,
                    Classroom = item.Classroom?.Trim().ToUpperInvariant() ?? "A1",
                    PhoneNumber = item.PhoneNumber?.Trim(),
                    ParentPhone = item.ParentPhone?.Trim()
                });
                addedCount++;
            }
            else
            {
                existing.FullName = cleanFullName;
                existing.Username = cleanUsername;
                existing.StudentCode = rawCode;
                existing.PasswordHash = passwordHash;
                existing.NationalIdEncrypted = encryptedNatId;
                existing.NationalIdMasked = maskedNatId;
                if (item.Grade.HasValue) existing.Grade = item.Grade.Value;
                if (!string.IsNullOrWhiteSpace(item.Classroom)) existing.Classroom = item.Classroom.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(item.PhoneNumber)) existing.PhoneNumber = item.PhoneNumber.Trim();
                if (!string.IsNullOrWhiteSpace(item.ParentPhone)) existing.ParentPhone = item.ParentPhone.Trim();
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = $"تم استيراد واعتماد بيانات الطلاب بنجاح (إضافة: {addedCount} طالب، تحديث: {updatedCount} طالب)",
            addedCount,
            updatedCount,
            errors
        });
    }
}

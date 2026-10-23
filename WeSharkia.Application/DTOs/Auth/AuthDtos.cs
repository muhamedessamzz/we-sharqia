using WeSharkia.Domain.Enums;

namespace WeSharkia.Application.DTOs.Auth;

public record LoginRequestDto(string Username, string Password, string? PortalRole = null);

public record RegisterRequestDto(
    string FullName,
    string Username,
    string Password,
    UserRole Role,
    bool ParentConsent = false,
    int? Grade = null,
    string? Classroom = null
);

public record AuthResponseDto(
    Guid Id,
    string FullName,
    string Username,
    string Role,
    string Status,
    bool ParentConsent,
    string Token,
    int? Grade = null,
    string? Classroom = null,
    string? Email = null,
    string? PhoneNumber = null,
    bool IsSuperAdmin = false,
    bool MustChangePassword = false,
    string? StudentCode = null,
    string? Specialty = null
);

public record ChangePasswordDto(string NewPassword);

public record UserDto(
    Guid Id,
    string FullName,
    string Username,
    string Role,
    string Status,
    bool ParentConsent,
    DateTime CreatedAtUtc,
    List<Guid> ChildrenIds,
    int? Grade = null,
    string? Classroom = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? ParentPhone = null,
    bool IsSuperAdmin = false,
    DateTime? LastLoginAtUtc = null,
    string? StudentCode = null,
    string? NationalIdMasked = null,
    string? Specialty = null,
    string? NationalId = null
);

public record AdminCreateUserDto(
    string FullName,
    string Username,
    string Password,
    UserRole Role,
    string? Email = null,
    string? PhoneNumber = null,
    string? ParentPhone = null,
    bool IsSuperAdmin = false,
    int? Grade = null,
    string? Classroom = null,
    string? StudentCode = null,
    string? NationalId = null,
    string? Specialty = null
);

public record UpdateUserStatusDto(UserStatus Status);
public record LinkChildrenDto(List<string> ChildrenUsernames);

public record AdminUpdateUserDto(
    string? FullName = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? ParentPhone = null,
    string? Password = null,
    UserStatus? Status = null,
    UserRole? Role = null,
    bool? IsSuperAdmin = null,
    int? Grade = null,
    string? Classroom = null,
    string? StudentCode = null,
    string? NationalId = null,
    string? Specialty = null
);

public record ImportStudentItemDto(
    string FullName,
    string StudentCode,
    string NationalId,
    int? Grade = null,
    string? Classroom = null,
    string? PhoneNumber = null,
    string? ParentPhone = null
);

public record ImportStudentsRequestDto(
    List<ImportStudentItemDto> Students
);


using WeSharkia.Domain.Common;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public bool ParentConsent { get; set; }

    // Contact & Admin privileges
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ParentPhone { get; set; }
    public bool IsSuperAdmin { get; set; } = false;
    public DateTime? LastLoginAtUtc { get; set; }

    // Student Grade & Classroom (Grade: 1=الأول, 2=الثاني, 3=الثالث | Classroom: A1-A4, B1-B3, C1-C3)
    public int? Grade { get; set; }
    public string? Classroom { get; set; }

    // Engineer/Staff Specialty (Strictly 4: programming, networking, communication, ai)
    public string? Specialty { get; set; }

    // Student Credentials & High-Security PII Protection
    public string? StudentCode { get; set; }
    public string? NationalIdEncrypted { get; set; }
    public string? NationalIdMasked { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }

    // Relationship: A Parent can have multiple Students (Children)

    public Guid? ParentId { get; set; }
    public User? Parent { get; set; }
    public ICollection<User> Children { get; set; } = new List<User>();

    // Navigation
    public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
    public ICollection<VideoView> VideoViews { get; set; } = new List<VideoView>();
    public ICollection<SurveyResponse> Surveys { get; set; } = new List<SurveyResponse>();
}

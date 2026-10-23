using WeSharkia.Domain.Common;

namespace WeSharkia.Domain.Entities;

public class StaffAttendance : BaseEntity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public DateTime Date { get; set; } // UTC date
    public DateTime CheckInTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOutTimeUtc { get; set; }
    public string Status { get; set; } = "Present"; // Present, Late, Absent, Excused
    public string? Notes { get; set; }
}

public class StudentAttendance : BaseEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty; // e.g. "A1"
    public int Grade { get; set; } = 1;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Present"; // Present, Absent, Late, Excused
    public Guid RecordedByTeacherId { get; set; }
    public string RecordedByTeacherName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ClassScheduleItem : BaseEntity
{
    public int Grade { get; set; } = 1; // 1, 2, 3
    public string Classroom { get; set; } = "A1"; // A1..A4, B1..B3, C1..C3
    public int DayOfWeek { get; set; } // 0=الأحد, 1=الإثنين, 2=الثلاثاء, 3=الأربعاء, 4=الخميس
    public int PeriodNumber { get; set; } = 1; // 1 to 6
    public string StartTime { get; set; } = "08:00";
    public string EndTime { get; set; } = "08:45";
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string RoomOrLab { get; set; } = "قاعة دراسية"; // "معمل الشبكات 1", "معمل الإلكترونيات", "قاعة A1"
    public bool IsLabSession { get; set; } = false;
}

public class ParentRequest : BaseEntity
{
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty;

    // Request Types: "استئذان مبكر", "عذر غياب طبي", "طلب مقابلة الأخصائي", "استفسار إداري"
    public string RequestType { get; set; } = "استئذان مبكر";
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public string? AdminResponse { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public class StudentDisciplineNotice : BaseEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty;
    public int Grade { get; set; } = 1;
    public Guid? ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;

    // Categories: "uniform" (مخالفة زي), "haircut" (مخالفة مظهر وشعر), "absence" (إنذار غياب), "disruption" (ملاحظة سلوكية ومشاغبة), "summon" (استدعاء ولي أمر), "positive" (سلوك متميز وتكريم)
    public string Category { get; set; } = "disruption";
    public string Title { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning"; // "info", "warning", "urgent"
    public string Status { get; set; } = "pending"; // "pending", "acknowledged", "resolved"
    public string IssuedBy { get; set; } = "أ. منى (الأخصائي الاجتماعي)";
    public DateTime IncidentDate { get; set; } = DateTime.UtcNow;

    // Guardian Acknowledgment & Feedback
    public bool IsAcknowledgedByParent { get; set; } = false;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? ParentNotes { get; set; }
}

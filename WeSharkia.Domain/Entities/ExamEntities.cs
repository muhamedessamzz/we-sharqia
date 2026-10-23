using WeSharkia.Domain.Common;

namespace WeSharkia.Domain.Entities;

public class SchoolExam : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    // "تكويني نظري", "تكويني عملي", "تحسيني"
    public string ExamType { get; set; } = "تكويني نظري";
    public string Subject { get; set; } = string.Empty;
    public string Classroom { get; set; } = "all"; // A1..C2, or "all"
    public int Grade { get; set; } = 1; // 1, 2, 3
    public double MaxScore { get; set; } = 100.0;
    public double PassingScore { get; set; } = 50.0;
    public DateTime ExamDate { get; set; } = DateTime.UtcNow;
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation
    public ICollection<ExamStudentScore> Scores { get; set; } = new List<ExamStudentScore>();
}

public class ExamStudentScore : BaseEntity
{
    public Guid ExamId { get; set; }
    public SchoolExam? Exam { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty;
    public int Grade { get; set; } = 1;

    public double Score { get; set; } = 0.0;
    public double MaxScore { get; set; } = 100.0;

    // Remedial / Improvement tracking
    public bool IsRemedial { get; set; } = false;
    // "مجتاز", "يحتاج تحسين", "قيد التحسيني", "لم يجتز بعد - فرصة استثنائية"
    public string RemedialStatus { get; set; } = "مجتاز";
    public double? PreviousScore { get; set; }

    public string? TeacherNotes { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

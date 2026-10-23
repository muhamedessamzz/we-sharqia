using WeSharkia.Domain.Common;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Domain.Entities;

public class StudentProgress : BaseEntity
{
    public Guid StudentId { get; set; }
    public User? Student { get; set; }

    public Guid AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public ProgressStatus Status { get; set; } = ProgressStatus.Working;
    public int CurrentStageIndex { get; set; }
    public int Stars { get; set; }
    public int AutoScore { get; set; }
    public int? ManualGrade { get; set; }
    public bool IsGraded { get; set; }
    public string? TeacherFeedback { get; set; }
    public string? TeacherEncouragementMsg { get; set; }
    public int ExtraMinutesGranted { get; set; }

    public string? HelpTypeRequested { get; set; }
    public bool HasRequestedHelp { get; set; }
    public bool IsLate { get; set; }

    public string? AnswersJson { get; set; } // Key-value map of stage_question -> answer

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public DateTime LastActiveAtUtc { get; set; } = DateTime.UtcNow;
}

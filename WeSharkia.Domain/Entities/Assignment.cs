using WeSharkia.Domain.Common;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Domain.Entities;

public class Assignment : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string? ResourceLinksJson { get; set; } // JSON array of string links
    public DateTime? DueDate { get; set; }
    public int SuggestedMinutes { get; set; } = 15;
    public AssignmentMode Mode { get; set; } = AssignmentMode.Sequential;
    public bool IsPublished { get; set; } = true;
    public bool AssignToAll { get; set; } = true;
    public string? TargetClassroom { get; set; } // e.g. "A1", "A2", "all"
    public string AssignmentType { get; set; } = "assignment"; // "assignment", "quiz", "task"
    public MeasurementPhase Phase { get; set; } = MeasurementPhase.Baseline;

    public Guid TeacherId { get; set; }
    public User? Teacher { get; set; }

    public ICollection<AssignmentStage> Stages { get; set; } = new List<AssignmentStage>();
    public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
}

public class AssignmentStage : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int PassingScorePercent { get; set; } = 60;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question : BaseEntity
{
    public Guid StageId { get; set; }
    public AssignmentStage? Stage { get; set; }

    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public string Explanation { get; set; } = string.Empty;

    // For MCQ, TF, Order
    public string? OptionsJson { get; set; } // JSON array of string choices
    public int CorrectIndex { get; set; } // for MCQ & TF

    // For Text
    public string? ExpectedAnswer { get; set; }
}

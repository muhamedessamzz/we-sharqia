using WeSharkia.Domain.Common;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Domain.Entities;

public class VideoTutorial : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "digital"; // digital, time, solve, team
    public string Url { get; set; } = string.Empty;
    public string? WrittenExplanation { get; set; }
    public string? VisualStepsJson { get; set; } // JSON array of step strings
    public string? AudioSpeechText { get; set; }

    public ICollection<VideoView> Views { get; set; } = new List<VideoView>();
}

public class VideoView : BaseEntity
{
    public Guid StudentId { get; set; }
    public User? Student { get; set; }

    public Guid VideoId { get; set; }
    public VideoTutorial? Video { get; set; }

    public int ViewCount { get; set; } = 1;
    public bool WatchedFully { get; set; }
    public bool? UnderstoodRating { get; set; } // true = understood, false = didn't understand, null = no rating
    public MeasurementPhase Phase { get; set; } = MeasurementPhase.Baseline;
    public DateTime LastViewedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DigitalSkill : BaseEntity
{
    public Guid StudentId { get; set; }
    public User? Student { get; set; }

    public string SkillKey { get; set; } = string.Empty; // open, reply, forms, search, time
    public bool ClaimedByStudent { get; set; }
    public bool ConfirmedByTeacher { get; set; }
}

public class SurveyResponse : BaseEntity
{
    public Guid StudentId { get; set; }
    public User? Student { get; set; }

    public MeasurementPhase Phase { get; set; } = MeasurementPhase.Baseline;
    public bool CanOpenFile { get; set; }
    public bool CanReplyAssignment { get; set; }
    public bool CanUseForms { get; set; }
    public int MotivationRating { get; set; } = 3; // 1 to 5
    public string? StudentFeedback { get; set; }
}

public class ClassroomObservation : BaseEntity
{
    public MeasurementPhase Phase { get; set; } = MeasurementPhase.Baseline;
    public string ObservationType { get; set; } = "card"; // card, daily, interview, fourth
    public int StudentsStuckCount { get; set; }
    public int TotalObservedCount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ObservedBy { get; set; } = string.Empty;
}

public class StudentSupportCase : BaseEntity
{
    public Guid StudentId { get; set; }
    public User? Student { get; set; }

    public string Status { get; set; } = "open"; // open, inSupport, improved
    public string NotesJson { get; set; } = "[]"; // JSON array of notes
}

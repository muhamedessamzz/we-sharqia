using WeSharkia.Domain.Common;

namespace WeSharkia.Domain.Entities;

public class SportsAssessment : BaseEntity
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Classroom { get; set; } = "A1";
    public int Grade { get; set; } = 1;

    // Morning Queue & Assembly
    public string MorningAssemblyStatus { get; set; } = "حاضر ومنضبط"; // "حاضر ومنضبط", "تأخر عن التمارين", "غائب عن الطابور"

    // Physical fitness tests (0 - 100)
    public double FitnessScore { get; set; } = 90.0;
    public int SpeedScore { get; set; } = 9; // سرعة (1 - 10)
    public int EnduranceScore { get; set; } = 9; // تحمل وقوة بدنية (1 - 10)
    public int FlexibilityScore { get; set; } = 8; // مرونة وتوافق عضلي عصبي (1 - 10)

    public string PreferredSport { get; set; } = "كرة قدم"; // كرة قدم، تنس طاولة، كرة سلة، ألعاب قوى، شطرنج
    public string? HonorBadge { get; set; } // "بطل اللياقة البدنية", "كابتن الفصل", "هداف الدوري", "انضباط بدني متميز"
    public string? CoachNotes { get; set; }

    public Guid RecordedByCoachId { get; set; }
    public string RecordedByCoachName { get; set; } = "كابتن حسام حسن";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SportsTournament : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string TournamentType { get; set; } = "دوري كرة القدم";
    public string TargetStages { get; set; } = "كافة المراحل (A, B, C)";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "جاري التنظيم والتصفيات"; // "قادم", "جاري التنظيم والتصفيات", "مكتمل وتتويج الفائزين"
    public string? WinningClassroom { get; set; } // e.g. "فصل A1"
    public string? BestPlayerOrScorer { get; set; }
    public string? RulesAndNotes { get; set; }
}

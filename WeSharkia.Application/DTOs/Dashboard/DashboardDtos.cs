namespace WeSharkia.Application.DTOs.Dashboard;

public record DashboardStatsDto(
    int TotalStudents,
    int TotalAssignments,
    int CompletedSubmissions,
    int HelpRequests,
    int ActiveNow,
    double SubmissionRatePercent,
    List<HonorRollItemDto> HonorRoll
)
{
    public double CompletionRate => SubmissionRatePercent;
    public int CompletedProgresses => CompletedSubmissions;
    public int HelpRequestsCount => HelpRequests;
}

public record HonorRollItemDto(
    Guid StudentId,
    string StudentName,
    int TotalStars,
    int CompletedAssignments
);

public record IndicatorRowDto(
    string Name,
    double? BaselineValue,
    double? DuringValue,
    double? PostValue,
    double TargetValue,
    bool IsHigherBetter,
    bool IsAchieved
);

public record CreateSocialCaseDto(
    Guid StudentId,
    string? CaseType,
    string? Priority,
    string Note,
    string? ActionPlan,
    string? CounselorName
);

public record AddCaseNoteDto(
    string Note,
    string? Recommendation,
    string? CounselorName,
    string? NewStatus
);

public record UpdateCaseStatusDto(string Status);


using WeSharkia.Domain.Enums;

namespace WeSharkia.Application.DTOs.Progress;

public record StudentProgressDto(
    Guid Id,
    Guid StudentId,
    string? StudentName,
    int? Grade,
    string? Classroom,
    Guid AssignmentId,
    string? AssignmentTitle,
    ProgressStatus Status,
    int CurrentStageIndex,
    int TotalStages,
    int Stars,
    int AutoScore,
    int? ManualGrade,
    bool IsGraded,
    string? TeacherFeedback,
    string? TeacherEncouragementMsg,
    int ExtraMinutesGranted,
    string? HelpTypeRequested,
    bool HasRequestedHelp,
    bool IsLate,
    string? AnswersJson,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    DateTime LastActiveAtUtc
);

public record UpdateProgressStatusDto(
    ProgressStatus? Status,
    string? HelpType,
    int? StageIndex,
    int? StarsEarned,
    int? ScoreIncrement,
    string? AnswersJson
);

public record SendEncouragementMsgDto(string Message);
public record GrantExtraMinutesDto(int Minutes);
public record GradeSubmissionDto(int? Grade, string? Feedback, bool ReturnForRevision);

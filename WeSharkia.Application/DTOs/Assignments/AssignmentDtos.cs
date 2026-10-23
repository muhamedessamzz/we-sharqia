using WeSharkia.Domain.Enums;

namespace WeSharkia.Application.DTOs.Assignments;

public record QuestionDto(
    Guid? Id,
    string Text,
    QuestionType Type,
    int Points,
    string Explanation,
    List<string>? Options,
    int CorrectIndex,
    string? ExpectedAnswer
);

public record AssignmentStageDto(
    Guid? Id,
    string Name,
    int OrderIndex,
    int PassingScorePercent,
    List<QuestionDto> Questions
);

public record AssignmentDto(
    Guid Id,
    string Title,
    string Instructions,
    List<string> ResourceLinks,
    DateTime? DueDate,
    int SuggestedMinutes,
    AssignmentMode Mode,
    bool IsPublished,
    bool AssignToAll,
    MeasurementPhase Phase,
    Guid TeacherId,
    string? TeacherName,
    DateTime CreatedAtUtc,
    List<AssignmentStageDto> Stages,
    string? TargetClassroom = null,
    string AssignmentType = "assignment"
);

public record CreateOrUpdateAssignmentDto(
    string Title,
    string Instructions,
    List<string> ResourceLinks,
    DateTime? DueDate,
    int SuggestedMinutes,
    AssignmentMode Mode,
    bool IsPublished,
    bool AssignToAll,
    MeasurementPhase Phase,
    List<AssignmentStageDto> Stages,
    string? TargetClassroom = null,
    string AssignmentType = "assignment"
);


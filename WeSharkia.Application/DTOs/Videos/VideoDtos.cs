using WeSharkia.Domain.Enums;

namespace WeSharkia.Application.DTOs.Videos;

public record VideoTutorialDto(
    Guid Id,
    string Title,
    string Category,
    string Url,
    string? WrittenExplanation,
    List<string> VisualSteps,
    string? AudioSpeechText,
    DateTime CreatedAtUtc,
    int TotalViews,
    bool? CurrentStudentWatched,
    bool? CurrentStudentUnderstood
);

public record CreateVideoTutorialDto(
    string Title,
    string Category,
    string Url,
    string? WrittenExplanation,
    List<string> VisualSteps,
    string? AudioSpeechText
);

public record RecordVideoViewDto(
    bool? WatchedFully,
    bool? UnderstoodRating
);

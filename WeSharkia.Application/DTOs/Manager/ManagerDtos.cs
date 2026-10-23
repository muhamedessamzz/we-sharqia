namespace WeSharkia.Application.DTOs.Manager;

public record StaffOverviewItemDto(
    Guid Id,
    string FullName,
    string Username,
    string Role,
    string? Email,
    string? PhoneNumber,
    DateTime? LastLoginAtUtc,
    int AssignmentsCount,
    int GradedCount,
    double CorrectionRate,
    int SystemProductivityScore,
    double? LatestEvaluationScore,
    string? RecognitionBadge
);

public record ManagerOverviewDto(
    int TotalStaffCount,
    int ActiveTodayStaffCount,
    int TotalPublishedAssignments,
    int TotalGradedSubmissions,
    int PendingSocialCasesCount,
    int UnreadStaffNotificationsCount,
    double AverageStaffRating,
    List<StaffOverviewItemDto> Staff
);

public record AuditLogDto(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid UserId,
    string UserName,
    string UserRole,
    string ActionType,
    string Description,
    string? DetailsJson
);

public record CreateAuditLogDto(
    Guid UserId,
    string UserName,
    string UserRole,
    string ActionType,
    string Description,
    string? DetailsJson = null
);

public record StaffEvaluationDto(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid StaffUserId,
    string StaffUserName,
    string StaffRole,
    Guid EvaluatorId,
    string EvaluatorName,
    string Period,
    int PunctualityRating,
    int PedagogyRating,
    int SystemEngagementRating,
    int CooperationRating,
    double OverallScore,
    string? Notes,
    string? RecognitionBadge
);

public record SaveStaffEvaluationDto(
    Guid StaffUserId,
    string StaffUserName,
    string StaffRole,
    Guid EvaluatorId,
    string EvaluatorName,
    string Period,
    int PunctualityRating,
    int PedagogyRating,
    int SystemEngagementRating,
    int CooperationRating,
    string? Notes,
    string? RecognitionBadge
);

public record StaffNotificationReceiptDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserRole,
    DateTime ReadAtUtc,
    DateTime? AcknowledgedAtUtc
);

public record StaffNotificationDto(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid SenderId,
    string SenderName,
    string Title,
    string Content,
    string TargetRole,
    Guid? TargetUserId,
    string? TargetUserName,
    string Priority,
    bool RequiresAcknowledgment,
    int TotalTargeted,
    int ReadCount,
    int AcknowledgedCount,
    List<StaffNotificationReceiptDto> Receipts
);

public record CreateStaffNotificationDto(
    Guid SenderId,
    string SenderName,
    string Title,
    string Content,
    string TargetRole = "all",
    Guid? TargetUserId = null,
    string? TargetUserName = null,
    string Priority = "normal",
    bool RequiresAcknowledgment = false
);

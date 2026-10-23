using WeSharkia.Domain.Common;

namespace WeSharkia.Domain.Entities;

public class StaffNotification : BaseEntity
{
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // Target: "all", "Teacher", "SocialWorker", "StudentAffairs", "AssessmentCoordinator", "QualityCoordinator", "TechnicalEngineer", "IT", or "single_user"
    public string TargetRole { get; set; } = "all";
    public Guid? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }

    // "normal", "urgent", "administrative"
    public string Priority { get; set; } = "normal";

    // Requires staff to explicitly click "تم الاطلاع والعلم"
    public bool RequiresAcknowledgment { get; set; } = false;

    // Navigation
    public ICollection<StaffNotificationReceipt> Receipts { get; set; } = new List<StaffNotificationReceipt>();
}

public class StaffNotificationReceipt : BaseEntity
{
    public Guid NotificationId { get; set; }
    public StaffNotification? Notification { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;

    public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
}

public class StaffAuditLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;

    // Action types: "Login", "CreateAssignment", "GradeAssignment", "UpdateSocialCase", "BulkImport", "SendNotification", "Evaluation", "SystemSetting"
    public string ActionType { get; set; } = "Action";
    public string Description { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
}

public class StaffEvaluation : BaseEntity
{
    public Guid StaffUserId { get; set; }
    public string StaffUserName { get; set; } = string.Empty;
    public string StaffRole { get; set; } = string.Empty;

    public Guid EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;

    public string Period { get; set; } = "الفصل الحالي"; // e.g. "أكتوبر 2026"

    // Ratings 1 to 5
    public int PunctualityRating { get; set; } = 5;      // الالتزام والانضباط
    public int PedagogyRating { get; set; } = 5;         // جودة التدريس والمهام
    public int SystemEngagementRating { get; set; } = 5; // التفاعل مع المنصة
    public int CooperationRating { get; set; } = 5;      // التعاون وروح الفريق

    public double OverallScore { get; set; } = 5.0;      // متوسط التقييم من 5
    public string? Notes { get; set; }                   // ملاحظات المدير وتوجيهاته
    public string? RecognitionBadge { get; set; }        // وسام تشجيعي مثل "معلم متميز"
}

public class StaffLeaveRequest : BaseEntity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string LeaveType { get; set; } = "عارضة"; // "اعتيادية", "عارضة", "مرضية", "إذن خروج"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // "pending", "approved", "rejected"
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class StaffInvestigation : BaseEntity
{
    public string InvestigationNumber { get; set; } = string.Empty; // e.g. "تحقيق-2026/08"
    public string Subject { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffUserName { get; set; } = string.Empty;
    public string StaffRole { get; set; } = string.Empty;
    public Guid InvestigatorId { get; set; }
    public string InvestigatorName { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; } = DateTime.UtcNow;
    public string SummaryOfAllegations { get; set; } = string.Empty;
    public string? StaffStatement { get; set; } // إفادة العضو المحال للتحقيق
    public string? Findings { get; set; } // نتائج الفحص والتحقيق
    public string Decision { get; set; } = "قيد الفحص"; // "حفظ التحقيق لعدم المخالفة", "لفت نظر وتنبيه شفوي", "إنذار إداري كتابي", "خصم يوم من الراتب", "إحالة للشؤون القانونية المركزية"
    public string Status { get; set; } = "open"; // "open", "statements_taken", "closed_decision_rendered"
    public string? ManagerEndorsement { get; set; } // اعتماد مدير المدرسة
    public DateTime? ClosedAtUtc { get; set; }
}

public class StaffMemo : BaseEntity
{
    public string MemoNumber { get; set; } = string.Empty; // e.g. "مذكرة-2026/015"
    public string MemoType { get; set; } = "مذكرة شكوى"; // "مذكرة شكوى", "مذكرة تحقيق ومساءلة", "مذكرة لفت نظر / مخالفة إدارية", "مذكرة التماس / عرض", "مذكرة إشادة وتقدير"
    public string Title { get; set; } = string.Empty;
    public Guid? TargetStaffId { get; set; }
    public string? TargetStaffName { get; set; }
    public string? TargetStaffRole { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Recommendations { get; set; } // توصيات ومقترح مسؤول الموارد البشرية
    public string Priority { get; set; } = "عادية"; // "عادية", "عاجلة", "سرية للغاية"
    public string Status { get; set; } = "مسودة"; // "مسودة", "قيد الفحص", "محالة للمدير", "معتمدة ومحفوظة"
    public string? ManagerRemarks { get; set; } // تأشيرة وتوجيه مدير المدرسة
}


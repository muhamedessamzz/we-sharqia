namespace WeSharkia.Domain.Enums;

public enum UserRole
{
    Student = 1,
    Teacher = 2,
    Parent = 3,
    Admin = 4,
    SocialWorker = 5,
    QualityCoordinator = 6,
    IT = 7,
    TechnicalEngineer = 8,
    StudentAffairs = 9,
    AssessmentCoordinator = 10,
    SchoolManager = 11,
    SportsCoach = 12,
    HR = 13
}

public enum UserStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3
}

public enum AssignmentMode
{
    Sequential = 1, // تدرجي 3 مراحل: سهل -> متوسط -> صعب
    Free = 2        // حر: أي عدد من الأقسام بأي ترتيب
}

public enum QuestionType
{
    MultipleChoice = 1, // mcq
    TrueFalse = 2,      // tf
    Text = 3,           // text
    FileOrLink = 4,     // link
    Order = 5           // order
}

public enum ProgressStatus
{
    Working = 1, // يعمل
    Help = 2,    // طلب مساعدة
    Idle = 3,    // خامل / قد يحتاج مساعدة
    Done = 4     // سلّم
}

public enum MeasurementPhase
{
    Baseline = 1, // قبلي
    During = 2,   // أثناء التنفيذ
    Post = 3      // بعدي
}

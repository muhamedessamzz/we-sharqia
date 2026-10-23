namespace WeSharkia.Application.DTOs.Operations;

public class CheckInStaffDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? UserRole { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public record StaffAttendanceDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserRole,
    DateTime Date,
    DateTime CheckInTimeUtc,
    DateTime? CheckOutTimeUtc,
    string Status,
    string? Notes
);

public record RecordStudentAttendanceItemDto(
    Guid StudentId,
    string StudentName,
    string Status, // Present, Absent, Late, Excused
    string? Notes = null
);

public record BatchStudentAttendanceDto(
    string Classroom,
    int Grade,
    DateTime Date,
    Guid TeacherId,
    string TeacherName,
    List<RecordStudentAttendanceItemDto> Items
);

public record StudentAttendanceDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Classroom,
    int Grade,
    DateTime Date,
    string Status,
    string? Notes
);

public record StudentAttendanceStatsDto(
    int TotalDays,
    int PresentDays,
    int AbsentDays,
    int LateDays,
    int ExcusedDays,
    double AttendanceRate
);

public record ClassScheduleItemDto(
    Guid Id,
    int Grade,
    string Classroom,
    int DayOfWeek,
    int PeriodNumber,
    string StartTime,
    string EndTime,
    string SubjectName,
    string TeacherName,
    string RoomOrLab,
    bool IsLabSession
);

public record CreateParentRequestDto(
    Guid ParentId,
    string ParentName,
    string ParentPhone,
    Guid StudentId,
    string StudentName,
    string Classroom,
    string RequestType,
    string Details
);

public record ReviewParentRequestDto(
    string Status, // approved, rejected
    string AdminResponse,
    string ReviewedBy
);

public record ParentRequestDto(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid ParentId,
    string ParentName,
    string ParentPhone,
    Guid StudentId,
    string StudentName,
    string Classroom,
    string RequestType,
    string Details,
    string Status,
    string? AdminResponse,
    string? ReviewedBy,
    DateTime? ReviewedAtUtc
);

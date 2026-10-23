namespace WeSharkia.Application.DTOs.Announcements;

public record AnnouncementDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    string Classroom,
    int? Grade,
    string Title,
    string Content,
    string Importance,
    DateTime CreatedAtUtc
);

public record CreateAnnouncementDto(
    string Classroom,
    int? Grade,
    string Title,
    string Content,
    string Importance = "normal"
);

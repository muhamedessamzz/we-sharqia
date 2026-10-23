using WeSharkia.Domain.Common;

namespace WeSharkia.Domain.Entities;

public class ClassroomAnnouncement : BaseEntity
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string Classroom { get; set; } = "all"; // e.g. "A1", "all"
    public int? Grade { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Importance { get; set; } = "normal"; // normal, urgent, assignment
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Announcements;
using WeSharkia.Domain.Entities;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AnnouncementsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnnouncementDto>>> GetAnnouncements([FromQuery] string? classroom)
    {
        var query = _context.ClassroomAnnouncements.AsQueryable();

        if (!string.IsNullOrWhiteSpace(classroom) && classroom.ToLower() != "all")
        {
            var cleanClass = classroom.Trim().ToUpperInvariant();
            query = query.Where(a => a.Classroom == "all" || a.Classroom.ToUpper() == cleanClass);
        }

        var list = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(50)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.TeacherId,
                a.TeacherName,
                a.Classroom,
                a.Grade,
                a.Title,
                a.Content,
                a.Importance,
                a.CreatedAtUtc
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement([FromBody] CreateAnnouncementDto dto, [FromQuery] Guid? teacherId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { message = "عنوان ونص الرسالة مطلوبان" });
        }

        var teacher = teacherId.HasValue 
            ? await _context.Users.FindAsync(teacherId.Value) 
            : await _context.Users.FirstOrDefaultAsync(u => u.Role == Domain.Enums.UserRole.Teacher);

        var announcement = new ClassroomAnnouncement
        {
            TeacherId = teacher?.Id ?? Guid.Empty,
            TeacherName = teacher?.FullName ?? "معلم الفصل",
            Classroom = string.IsNullOrWhiteSpace(dto.Classroom) ? "all" : dto.Classroom.Trim().ToUpperInvariant(),
            Grade = dto.Grade,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Importance = string.IsNullOrWhiteSpace(dto.Importance) ? "normal" : dto.Importance.Trim().ToLowerInvariant()
        };

        _context.ClassroomAnnouncements.Add(announcement);
        await _context.SaveChangesAsync();

        var result = new AnnouncementDto(
            announcement.Id,
            announcement.TeacherId,
            announcement.TeacherName,
            announcement.Classroom,
            announcement.Grade,
            announcement.Title,
            announcement.Content,
            announcement.Importance,
            announcement.CreatedAtUtc
        );

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id)
    {
        var item = await _context.ClassroomAnnouncements.FindAsync(id);
        if (item == null) return NotFound(new { message = "الرسالة غير موجودة" });

        _context.ClassroomAnnouncements.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حذف الرسالة بنجاح" });
    }
}

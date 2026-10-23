using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Progress;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public ProgressController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<StudentProgressDto>>> GetStudentProgress(Guid studentId)
    {
        var list = await _context.StudentProgresses
            .Include(p => p.Student)
            .Include(p => p.Assignment)
                .ThenInclude(a => a!.Stages)
            .Where(p => p.StudentId == studentId)
            .ToListAsync();

        return Ok(list.Select(MapToDto).ToList());
    }

    [HttpGet("live")]
    public async Task<ActionResult<List<StudentProgressDto>>> GetLiveBoard()
    {
        var list = await _context.StudentProgresses
            .Include(p => p.Student)
            .Include(p => p.Assignment)
                .ThenInclude(a => a!.Stages)
            .OrderByDescending(p => p.LastActiveAtUtc)
            .ToListAsync();

        return Ok(list.Select(MapToDto).ToList());
    }

    [HttpPost("start/{assignmentId:guid}")]
    public async Task<ActionResult<StudentProgressDto>> StartOrGetProgress(Guid assignmentId, [FromQuery] Guid studentId)
    {
        var existing = await _context.StudentProgresses
            .Include(p => p.Student)
            .Include(p => p.Assignment)
                .ThenInclude(a => a!.Stages)
            .FirstOrDefaultAsync(p => p.AssignmentId == assignmentId && p.StudentId == studentId);

        if (existing != null)
        {
            existing.LastActiveAtUtc = DateTime.UtcNow;
            if (existing.Status == ProgressStatus.Idle) existing.Status = ProgressStatus.Working;
            await _context.SaveChangesAsync();
            return Ok(MapToDto(existing));
        }

        var assignment = await _context.Assignments.Include(a => a.Stages).FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null) return NotFound(new { message = "التكليف غير موجود" });

        var progress = new StudentProgress
        {
            StudentId = studentId,
            AssignmentId = assignmentId,
            Status = ProgressStatus.Working,
            CurrentStageIndex = 0,
            Stars = 0,
            AutoScore = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastActiveAtUtc = DateTime.UtcNow
        };

        _context.StudentProgresses.Add(progress);
        await _context.SaveChangesAsync();

        // reload
        var created = await _context.StudentProgresses
            .Include(p => p.Student)
            .Include(p => p.Assignment)
                .ThenInclude(a => a!.Stages)
            .FirstAsync(p => p.Id == progress.Id);

        return Ok(MapToDto(created));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressStatusDto dto)
    {
        var p = await _context.StudentProgresses.Include(x => x.Assignment).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(new { message = "سجل التقدم غير موجود" });

        p.LastActiveAtUtc = DateTime.UtcNow;

        if (dto.Status.HasValue) p.Status = dto.Status.Value;
        if (dto.HelpType != null)
        {
            p.HelpTypeRequested = dto.HelpType;
            p.HasRequestedHelp = true;
        }
        if (dto.StageIndex.HasValue) p.CurrentStageIndex = dto.StageIndex.Value;
        if (dto.StarsEarned.HasValue) p.Stars += dto.StarsEarned.Value;
        if (dto.ScoreIncrement.HasValue) p.AutoScore += dto.ScoreIncrement.Value;
        if (dto.AnswersJson != null) p.AnswersJson = dto.AnswersJson;

        if (dto.Status == ProgressStatus.Done && p.FinishedAtUtc == null)
        {
            p.FinishedAtUtc = DateTime.UtcNow;
            if (p.Assignment?.DueDate != null && DateTime.UtcNow > p.Assignment.DueDate)
            {
                p.IsLate = true;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم التحديث بنجاح" });
    }

    [HttpPost("{id:guid}/message")]
    public async Task<IActionResult> SendEncouragement(Guid id, [FromBody] SendEncouragementMsgDto dto)
    {
        var p = await _context.StudentProgresses.FindAsync(id);
        if (p == null) return NotFound(new { message = "سجل التقدم غير موجود" });

        p.TeacherEncouragementMsg = dto.Message;
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم إرسال رسالة التشجيع للطالب" });
    }

    [HttpPost("{id:guid}/extra-minutes")]
    public async Task<IActionResult> GrantExtraMinutes(Guid id, [FromBody] GrantExtraMinutesDto dto)
    {
        var p = await _context.StudentProgresses.FindAsync(id);
        if (p == null) return NotFound(new { message = "سجل التقدم غير موجود" });

        p.ExtraMinutesGranted = dto.Minutes;
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم منح الوقت الإضافي" });
    }

    [HttpPost("{id:guid}/grade")]
    public async Task<IActionResult> GradeSubmission(Guid id, [FromBody] GradeSubmissionDto dto)
    {
        var p = await _context.StudentProgresses.FindAsync(id);
        if (p == null) return NotFound(new { message = "سجل التقدم غير موجود" });

        p.TeacherFeedback = dto.Feedback;
        if (dto.ReturnForRevision)
        {
            p.Status = ProgressStatus.Working;
            p.IsGraded = false;
            p.TeacherEncouragementMsg = dto.Feedback ?? "يرجى مراجعة إجابتك وتعديلها 💜";
        }
        else
        {
            p.ManualGrade = dto.Grade;
            p.IsGraded = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حفظ التقييم بنجاح" });
    }

    private static StudentProgressDto MapToDto(StudentProgress p)
    {
        var totalStages = p.Assignment?.Stages.Count ?? 3;
        return new StudentProgressDto(
            p.Id,
            p.StudentId,
            p.Student?.FullName,
            p.Student?.Grade,
            p.Student?.Classroom,
            p.AssignmentId,
            p.Assignment?.Title,
            p.Status,
            p.CurrentStageIndex,
            totalStages,
            p.Stars,
            p.AutoScore,
            p.ManualGrade,
            p.IsGraded,
            p.TeacherFeedback,
            p.TeacherEncouragementMsg,
            p.ExtraMinutesGranted,
            p.HelpTypeRequested,
            p.HasRequestedHelp,
            p.IsLate,
            p.AnswersJson,
            p.StartedAtUtc,
            p.FinishedAtUtc,
            p.LastActiveAtUtc
        );
    }
}

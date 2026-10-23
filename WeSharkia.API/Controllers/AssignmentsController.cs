using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Assignments;
using WeSharkia.Domain.Entities;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AssignmentsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssignmentDto>>> GetAssignments([FromQuery] bool? onlyPublished, [FromQuery] string? classroom, [FromQuery] string? type)
    {
        var query = _context.Assignments
            .Include(a => a.Teacher)
            .Include(a => a.Stages)
                .ThenInclude(s => s.Questions)
            .AsQueryable();

        if (onlyPublished == true)
        {
            query = query.Where(a => a.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(classroom) && classroom.ToLower() != "all")
        {
            var cleanClass = classroom.Trim().ToUpperInvariant();
            query = query.Where(a => a.AssignToAll || a.TargetClassroom == null || a.TargetClassroom == "" || a.TargetClassroom == "all" || a.TargetClassroom.ToUpper().Contains(cleanClass));
        }

        if (!string.IsNullOrWhiteSpace(type) && type.ToLower() != "all")
        {
            var cleanType = type.Trim().ToLowerInvariant();
            query = query.Where(a => a.AssignmentType.ToLower() == cleanType);
        }

        var list = await query.OrderByDescending(a => a.CreatedAtUtc).ToListAsync();

        var result = list.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssignmentDto>> GetAssignmentById(Guid id)
    {
        var a = await _context.Assignments
            .Include(a => a.Teacher)
            .Include(a => a.Stages)
                .ThenInclude(s => s.Questions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return NotFound(new { message = "التكليف غير موجود" });

        return Ok(MapToDto(a));
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> CreateAssignment([FromBody] CreateOrUpdateAssignmentDto dto, [FromQuery] Guid? teacherId)
    {
        var tId = teacherId ?? (await _context.Users.FirstOrDefaultAsync(u => u.Role == Domain.Enums.UserRole.Teacher))?.Id;
        if (tId == null) return BadRequest(new { message = "يجب تحديد المعلم" });

        var assignment = new Assignment
        {
            Title = dto.Title,
            Instructions = dto.Instructions,
            ResourceLinksJson = JsonSerializer.Serialize(dto.ResourceLinks),
            DueDate = dto.DueDate,
            SuggestedMinutes = dto.SuggestedMinutes,
            Mode = dto.Mode,
            IsPublished = dto.IsPublished,
            AssignToAll = dto.AssignToAll || string.IsNullOrWhiteSpace(dto.TargetClassroom) || dto.TargetClassroom.ToLower() == "all",
            TargetClassroom = !string.IsNullOrWhiteSpace(dto.TargetClassroom) && dto.TargetClassroom.ToLower() != "all" ? dto.TargetClassroom.Trim().ToUpperInvariant() : null,
            AssignmentType = string.IsNullOrWhiteSpace(dto.AssignmentType) ? "assignment" : dto.AssignmentType.Trim().ToLowerInvariant(),
            Phase = dto.Phase,
            TeacherId = tId.Value
        };

        int stageIdx = 0;
        foreach (var stageDto in dto.Stages)
        {
            var stage = new AssignmentStage
            {
                Name = stageDto.Name,
                OrderIndex = stageIdx++,
                PassingScorePercent = stageDto.PassingScorePercent
            };

            foreach (var qDto in stageDto.Questions)
            {
                stage.Questions.Add(new Question
                {
                    Text = qDto.Text,
                    Type = qDto.Type,
                    Points = qDto.Points,
                    Explanation = qDto.Explanation,
                    OptionsJson = qDto.Options != null ? JsonSerializer.Serialize(qDto.Options) : null,
                    CorrectIndex = qDto.CorrectIndex,
                    ExpectedAnswer = qDto.ExpectedAnswer
                });
            }

            assignment.Stages.Add(stage);
        }

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssignmentById), new { id = assignment.Id }, MapToDto(assignment));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAssignment(Guid id)
    {
        var a = await _context.Assignments.FindAsync(id);
        if (a == null) return NotFound(new { message = "التكليف غير موجود" });

        _context.Assignments.Remove(a);
        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حذف التكليف بنجاح" });
    }

    private static AssignmentDto MapToDto(Assignment a)
    {
        var links = !string.IsNullOrEmpty(a.ResourceLinksJson)
            ? JsonSerializer.Deserialize<List<string>>(a.ResourceLinksJson) ?? new List<string>()
            : new List<string>();

        var stages = a.Stages.OrderBy(s => s.OrderIndex).Select(s => new AssignmentStageDto(
            s.Id,
            s.Name,
            s.OrderIndex,
            s.PassingScorePercent,
            s.Questions.Select(q => new QuestionDto(
                q.Id,
                q.Text,
                q.Type,
                q.Points,
                q.Explanation,
                !string.IsNullOrEmpty(q.OptionsJson) ? JsonSerializer.Deserialize<List<string>>(q.OptionsJson) : null,
                q.CorrectIndex,
                q.ExpectedAnswer
            )).ToList()
        )).ToList();

        return new AssignmentDto(
            a.Id,
            a.Title,
            a.Instructions,
            links,
            a.DueDate,
            a.SuggestedMinutes,
            a.Mode,
            a.IsPublished,
            a.AssignToAll,
            a.Phase,
            a.TeacherId,
            a.Teacher?.FullName,
            a.CreatedAtUtc,
            stages,
            a.TargetClassroom,
            a.AssignmentType
        );
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Domain.Entities;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SkillsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<DigitalSkill>>> GetSkills(Guid studentId)
    {
        var skills = await _context.DigitalSkills.Where(s => s.StudentId == studentId).ToListAsync();
        return Ok(skills);
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAllSkills()
    {
        var skills = await _context.DigitalSkills
            .Include(s => s.Student)
            .OrderBy(s => s.Student!.FullName)
            .ToListAsync();
        return Ok(skills);
    }

    [HttpPost("claim")]
    public async Task<IActionResult> ClaimSkill([FromQuery] Guid studentId, [FromQuery] string skillKey, [FromQuery] bool claimed)
    {
        var sk = await _context.DigitalSkills.FirstOrDefaultAsync(s => s.StudentId == studentId && s.SkillKey == skillKey);
        if (sk == null)
        {
            sk = new DigitalSkill { StudentId = studentId, SkillKey = skillKey, ClaimedByStudent = claimed };
            _context.DigitalSkills.Add(sk);
        }
        else
        {
            sk.ClaimedByStudent = claimed;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تحديث المهارة بنجاح" });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmSkill([FromQuery] Guid studentId, [FromQuery] string skillKey, [FromQuery] bool confirmed)
    {
        var sk = await _context.DigitalSkills.FirstOrDefaultAsync(s => s.StudentId == studentId && s.SkillKey == skillKey);
        if (sk == null)
        {
            sk = new DigitalSkill { StudentId = studentId, SkillKey = skillKey, ConfirmedByTeacher = confirmed };
            _context.DigitalSkills.Add(sk);
        }
        else
        {
            sk.ConfirmedByTeacher = confirmed;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تأكيد المهارة من المعلم" });
    }
}

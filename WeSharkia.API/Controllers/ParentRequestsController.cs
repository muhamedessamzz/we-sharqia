using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Operations;
using WeSharkia.Domain.Entities;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/parent-requests")]
public class ParentRequestsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public ParentRequestsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ParentRequestDto>> CreateRequest([FromBody] CreateParentRequestDto dto)
    {
        var req = new ParentRequest
        {
            ParentId = dto.ParentId,
            ParentName = dto.ParentName.Trim(),
            ParentPhone = dto.ParentPhone.Trim(),
            StudentId = dto.StudentId,
            StudentName = dto.StudentName.Trim(),
            Classroom = dto.Classroom.Trim().ToUpperInvariant(),
            RequestType = dto.RequestType,
            Details = dto.Details.Trim(),
            Status = "pending"
        };

        _context.ParentRequests.Add(req);
        await _context.SaveChangesAsync();

        return Ok(new ParentRequestDto(
            req.Id,
            req.CreatedAtUtc,
            req.ParentId,
            req.ParentName,
            req.ParentPhone,
            req.StudentId,
            req.StudentName,
            req.Classroom,
            req.RequestType,
            req.Details,
            req.Status,
            req.AdminResponse,
            req.ReviewedBy,
            req.ReviewedAtUtc
        ));
    }

    [HttpGet("parent/{parentId:guid}")]
    public async Task<ActionResult<List<ParentRequestDto>>> GetParentRequests(Guid parentId)
    {
        var list = await _context.ParentRequests
            .Where(r => r.ParentId == parentId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ParentRequestDto(
                r.Id,
                r.CreatedAtUtc,
                r.ParentId,
                r.ParentName,
                r.ParentPhone,
                r.StudentId,
                r.StudentName,
                r.Classroom,
                r.RequestType,
                r.Details,
                r.Status,
                r.AdminResponse,
                r.ReviewedBy,
                r.ReviewedAtUtc
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet]
    public async Task<ActionResult<List<ParentRequestDto>>> GetAllRequests([FromQuery] string? status = null)
    {
        var query = _context.ParentRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            query = query.Where(r => r.Status.ToLower() == status.ToLower());
        }

        var list = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ParentRequestDto(
                r.Id,
                r.CreatedAtUtc,
                r.ParentId,
                r.ParentName,
                r.ParentPhone,
                r.StudentId,
                r.StudentName,
                r.Classroom,
                r.RequestType,
                r.Details,
                r.Status,
                r.AdminResponse,
                r.ReviewedBy,
                r.ReviewedAtUtc
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> ReviewRequest(Guid id, [FromBody] ReviewParentRequestDto dto)
    {
        var req = await _context.ParentRequests.FindAsync(id);
        if (req == null) return NotFound(new { message = "الطلب غير موجود" });

        req.Status = dto.Status.ToLower();
        req.AdminResponse = dto.AdminResponse.Trim();
        req.ReviewedBy = dto.ReviewedBy.Trim();
        req.ReviewedAtUtc = DateTime.UtcNow;

        // Record Audit log
        _context.StaffAuditLogs.Add(new StaffAuditLog
        {
            UserId = Guid.Empty,
            UserName = dto.ReviewedBy,
            UserRole = "studentaffairs",
            ActionType = "ReviewParentRequest",
            Description = $"البت في طلب ولي أمر الطالب {req.StudentName} ({req.RequestType}) بحالة: {dto.Status}"
        });

        await _context.SaveChangesAsync();

        return Ok(new { message = "تم حفظ قرار مراجعة الطلب بنجاح" });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HrController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HrController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.UtcNow.Date;

        var staffRoles = new[]
        {
            UserRole.Teacher,
            UserRole.TechnicalEngineer,
            UserRole.StudentAffairs,
            UserRole.SocialWorker,
            UserRole.AssessmentCoordinator,
            UserRole.QualityCoordinator,
            UserRole.SportsCoach,
            UserRole.HR,
            UserRole.IT,
            UserRole.SchoolManager
        };

        var allStaff = await _context.Users
            .Where(u => staffRoles.Contains(u.Role) && u.Status == UserStatus.Approved)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Username,
                u.Email,
                u.PhoneNumber,
                Role = u.Role.ToString().ToLower(),
                u.LastLoginAtUtc
            })
            .ToListAsync();

        var todayAttendances = await _context.StaffAttendances
            .Where(a => a.Date == today)
            .ToListAsync();

        var pendingLeaves = await _context.StaffLeaveRequests
            .Where(l => l.Status == "pending")
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        var allLeaves = await _context.StaffLeaveRequests
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(30)
            .ToListAsync();

        var investigations = await _context.StaffInvestigations
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();

        var memos = await _context.StaffMemos
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync();

        return Ok(new
        {
            totalStaffCount = allStaff.Count,
            todayPresentCount = todayAttendances.Count(a => a.Status == "Present"),
            todayLateCount = todayAttendances.Count(a => a.Status == "Late"),
            todayAbsentCount = todayAttendances.Count(a => a.Status == "Absent"),
            pendingLeavesCount = pendingLeaves.Count,
            activeInvestigationsCount = investigations.Count(i => i.Status == "open" || i.Status == "statements_taken"),
            totalMemosCount = memos.Count,
            staff = allStaff,
            todayAttendances,
            leaves = allLeaves,
            investigations,
            memos
        });
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] DateTime? date, [FromQuery] string? role)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var query = _context.StaffAttendances.Where(a => a.Date == targetDate);

        if (!string.IsNullOrEmpty(role) && role.ToLower() != "all")
        {
            query = query.Where(a => a.UserRole.ToLower() == role.ToLower());
        }

        var list = await query.OrderBy(a => a.UserName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("attendance/record")]
    public async Task<IActionResult> RecordAttendance([FromBody] RecordStaffAttendanceDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });

        var date = (dto.Date ?? DateTime.UtcNow).Date;
        var existing = await _context.StaffAttendances
            .FirstOrDefaultAsync(a => a.UserId == dto.UserId && a.Date == date);

        if (existing != null)
        {
            existing.Status = dto.Status;
            existing.Notes = dto.Notes;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _context.StaffAttendances.Add(new StaffAttendance
            {
                UserId = user.Id,
                UserName = user.FullName,
                UserRole = user.Role.ToString(),
                Date = date,
                CheckInTimeUtc = DateTime.UtcNow,
                Status = dto.Status,
                Notes = dto.Notes
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تسجيل وتحديث حضور الموظف بنجاح" });
    }

    [HttpGet("leaves")]
    public async Task<IActionResult> GetLeaves()
    {
        var leaves = await _context.StaffLeaveRequests
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();
        return Ok(leaves);
    }

    [HttpPost("leaves")]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });

        var leave = new StaffLeaveRequest
        {
            UserId = user.Id,
            UserName = user.FullName,
            UserRole = user.Role.ToString(),
            LeaveType = dto.LeaveType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DaysCount = Math.Max(1, dto.DaysCount),
            Reason = dto.Reason,
            Status = "pending",
            Notes = dto.Notes
        };

        _context.StaffLeaveRequests.Add(leave);
        await _context.SaveChangesAsync();

        return Ok(leave);
    }

    [HttpPut("leaves/{id}/status")]
    public async Task<IActionResult> UpdateLeaveStatus(Guid id, [FromBody] UpdateLeaveStatusDto dto)
    {
        var leave = await _context.StaffLeaveRequests.FindAsync(id);
        if (leave == null) return NotFound(new { message = "طلب الإجازة غير موجود" });

        leave.Status = dto.Status;
        leave.ApprovedBy = dto.ApprovedBy ?? "الموارد البشرية";
        leave.Notes = dto.Notes ?? leave.Notes;
        leave.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(leave);
    }

    [HttpGet("investigations")]
    public async Task<IActionResult> GetInvestigations()
    {
        var list = await _context.StaffInvestigations
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("investigations")]
    public async Task<IActionResult> CreateInvestigation([FromBody] CreateInvestigationDto dto)
    {
        var count = await _context.StaffInvestigations.CountAsync();
        var num = $"تحقيق-{DateTime.UtcNow.Year}/{(count + 1):D2}";

        var inv = new StaffInvestigation
        {
            InvestigationNumber = num,
            Subject = dto.Subject,
            StaffUserId = dto.StaffUserId,
            StaffUserName = dto.StaffUserName,
            StaffRole = dto.StaffRole,
            InvestigatorId = dto.InvestigatorId,
            InvestigatorName = dto.InvestigatorName,
            IncidentDate = dto.IncidentDate,
            SummaryOfAllegations = dto.SummaryOfAllegations,
            StaffStatement = dto.StaffStatement,
            Findings = dto.Findings,
            Decision = dto.Decision ?? "قيد الفحص والتحقيق",
            Status = dto.Status ?? "open"
        };

        _context.StaffInvestigations.Add(inv);
        await _context.SaveChangesAsync();
        return Ok(inv);
    }

    [HttpPut("investigations/{id}")]
    public async Task<IActionResult> UpdateInvestigation(Guid id, [FromBody] UpdateInvestigationDto dto)
    {
        var inv = await _context.StaffInvestigations.FindAsync(id);
        if (inv == null) return NotFound(new { message = "ملف التحقيق غير موجود" });

        if (!string.IsNullOrEmpty(dto.StaffStatement)) inv.StaffStatement = dto.StaffStatement;
        if (!string.IsNullOrEmpty(dto.Findings)) inv.Findings = dto.Findings;
        if (!string.IsNullOrEmpty(dto.Decision)) inv.Decision = dto.Decision;
        if (!string.IsNullOrEmpty(dto.Status)) inv.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.ManagerEndorsement)) inv.ManagerEndorsement = dto.ManagerEndorsement;

        if (dto.Status == "closed_decision_rendered" && inv.ClosedAtUtc == null)
        {
            inv.ClosedAtUtc = DateTime.UtcNow;
        }

        inv.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(inv);
    }

    [HttpGet("memos")]
    public async Task<IActionResult> GetMemos()
    {
        var memos = await _context.StaffMemos
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync();
        return Ok(memos);
    }

    [HttpPost("memos")]
    public async Task<IActionResult> CreateMemo([FromBody] CreateMemoDto dto)
    {
        var count = await _context.StaffMemos.CountAsync();
        var memoNum = $"مذكرة-{DateTime.UtcNow.Year}/{(count + 1):D3}";

        var memo = new StaffMemo
        {
            MemoNumber = memoNum,
            MemoType = dto.MemoType,
            Title = dto.Title,
            TargetStaffId = dto.TargetStaffId,
            TargetStaffName = dto.TargetStaffName,
            TargetStaffRole = dto.TargetStaffRole,
            CreatedById = dto.CreatedById,
            CreatedByName = dto.CreatedByName,
            Content = dto.Content,
            Recommendations = dto.Recommendations,
            Priority = dto.Priority ?? "عادية",
            Status = dto.Status ?? "مسودة",
            ManagerRemarks = dto.ManagerRemarks
        };

        _context.StaffMemos.Add(memo);
        await _context.SaveChangesAsync();
        return Ok(memo);
    }

    [HttpPut("memos/{id}")]
    public async Task<IActionResult> UpdateMemo(Guid id, [FromBody] UpdateMemoDto dto)
    {
        var memo = await _context.StaffMemos.FindAsync(id);
        if (memo == null) return NotFound(new { message = "المذكرة غير موجودة" });

        if (!string.IsNullOrEmpty(dto.Content)) memo.Content = dto.Content;
        if (!string.IsNullOrEmpty(dto.Recommendations)) memo.Recommendations = dto.Recommendations;
        if (!string.IsNullOrEmpty(dto.Status)) memo.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.ManagerRemarks)) memo.ManagerRemarks = dto.ManagerRemarks;

        memo.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(memo);
    }
}

public class RecordStaffAttendanceDto
{
    public Guid UserId { get; set; }
    public DateTime? Date { get; set; }
    public string Status { get; set; } = "Present";
    public string? Notes { get; set; }
}

public class CreateLeaveDto
{
    public Guid UserId { get; set; }
    public string LeaveType { get; set; } = "عارضة";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateLeaveStatusDto
{
    public string Status { get; set; } = "approved";
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class CreateInvestigationDto
{
    public string Subject { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffUserName { get; set; } = string.Empty;
    public string StaffRole { get; set; } = string.Empty;
    public Guid InvestigatorId { get; set; }
    public string InvestigatorName { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; } = DateTime.UtcNow;
    public string SummaryOfAllegations { get; set; } = string.Empty;
    public string? StaffStatement { get; set; }
    public string? Findings { get; set; }
    public string? Decision { get; set; }
    public string? Status { get; set; }
}

public class UpdateInvestigationDto
{
    public string? StaffStatement { get; set; }
    public string? Findings { get; set; }
    public string? Decision { get; set; }
    public string? Status { get; set; }
    public string? ManagerEndorsement { get; set; }
}

public class CreateMemoDto
{
    public string MemoType { get; set; } = "مذكرة شكوى";
    public string Title { get; set; } = string.Empty;
    public Guid? TargetStaffId { get; set; }
    public string? TargetStaffName { get; set; }
    public string? TargetStaffRole { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Recommendations { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? ManagerRemarks { get; set; }
}

public class UpdateMemoDto
{
    public string? Content { get; set; }
    public string? Recommendations { get; set; }
    public string? Status { get; set; }
    public string? ManagerRemarks { get; set; }
}

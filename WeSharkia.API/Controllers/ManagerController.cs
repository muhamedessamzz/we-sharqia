using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Manager;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManagerController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public ManagerController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ManagerOverviewDto>> GetOverview()
    {
        var staffRoles = new[]
        {
            UserRole.Teacher,
            UserRole.TechnicalEngineer,
            UserRole.StudentAffairs,
            UserRole.SocialWorker,
            UserRole.AssessmentCoordinator,
            UserRole.QualityCoordinator,
            UserRole.IT,
            UserRole.Admin,
            UserRole.SchoolManager,
            UserRole.SportsCoach
        };

        var staffUsers = await _context.Users
            .Where(u => staffRoles.Contains(u.Role) && u.Username.ToLower() != "zakaria" && u.Email != "zakaria@sharqia.gfd.com")
            .ToListAsync();

        var todayUtc = DateTime.UtcNow.Date;
        var activeTodayCount = staffUsers.Count(u => u.LastLoginAtUtc.HasValue && u.LastLoginAtUtc.Value.Date == todayUtc);

        var totalAssignments = await _context.Assignments.CountAsync(a => a.IsPublished);
        var totalGradedSubmissions = await _context.StudentProgresses.CountAsync(p => p.Status == ProgressStatus.Done);
        var pendingSocialCases = await _context.StudentSupportCases.CountAsync(c => c.Status == "open");
        var activeNotificationsCount = await _context.StaffNotifications.CountAsync();

        var allEvaluations = await _context.StaffEvaluations.ToListAsync();
        var avgRating = allEvaluations.Any() ? Math.Round(allEvaluations.Average(e => e.OverallScore), 1) : 4.8;

        var allAssignments = await _context.Assignments.ToListAsync();
        var allProgresses = await _context.StudentProgresses.ToListAsync();

        var staffItems = new List<StaffOverviewItemDto>();

        foreach (var staff in staffUsers.OrderBy(u => u.Role).ThenBy(u => u.FullName))
        {
            var roleStr = staff.Role.ToString().ToLower();
            var assignments = allAssignments.Where(a => a.TeacherId == staff.Id).ToList();
            var assignmentsCount = assignments.Count;

            var assignmentIds = assignments.Select(a => a.Id).ToHashSet();
            var relatedProgress = allProgresses.Where(p => assignmentIds.Contains(p.AssignmentId)).ToList();
            var gradedCount = relatedProgress.Count(p => p.Status == ProgressStatus.Done);
            var totalSubmissions = relatedProgress.Count;
            var correctionRate = totalSubmissions > 0 ? Math.Round((double)gradedCount / totalSubmissions * 100, 1) : 100.0;

            // System Productivity Score calculation (0 to 100)
            int score = 60;
            if (staff.LastLoginAtUtc.HasValue)
            {
                var daysAgo = (DateTime.UtcNow - staff.LastLoginAtUtc.Value).TotalDays;
                if (daysAgo <= 1) score += 20;
                else if (daysAgo <= 3) score += 10;
            }
            if (assignmentsCount > 0) score += 10;
            if (correctionRate >= 90) score += 10;
            if (score > 100) score = 100;

            var latestEval = allEvaluations
                .Where(e => e.StaffUserId == staff.Id)
                .OrderByDescending(e => e.CreatedAtUtc)
                .FirstOrDefault();

            staffItems.Add(new StaffOverviewItemDto(
                staff.Id,
                staff.FullName,
                staff.Username,
                roleStr,
                staff.Email,
                staff.PhoneNumber,
                staff.LastLoginAtUtc,
                assignmentsCount,
                gradedCount,
                correctionRate,
                score,
                latestEval?.OverallScore,
                latestEval?.RecognitionBadge
            ));
        }

        return Ok(new ManagerOverviewDto(
            staffUsers.Count,
            activeTodayCount,
            totalAssignments,
            totalGradedSubmissions,
            pendingSocialCases,
            activeNotificationsCount,
            avgRating,
            staffItems
        ));
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<List<AuditLogDto>>> GetAuditLogs([FromQuery] string? role = null, [FromQuery] Guid? userId = null, [FromQuery] int limit = 50)
    {
        var query = _context.StaffAuditLogs.AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role) && role.ToLower() != "all")
        {
            query = query.Where(l => l.UserRole.ToLower() == role.ToLower());
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(Math.Min(100, Math.Max(10, limit)))
            .Select(l => new AuditLogDto(
                l.Id,
                l.CreatedAtUtc,
                l.UserId,
                l.UserName,
                l.UserRole,
                l.ActionType,
                l.Description,
                l.DetailsJson
            ))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost("audit-logs")]
    public async Task<IActionResult> CreateAuditLog([FromBody] CreateAuditLogDto dto)
    {
        var log = new StaffAuditLog
        {
            UserId = dto.UserId,
            UserName = dto.UserName,
            UserRole = dto.UserRole.ToLower(),
            ActionType = dto.ActionType,
            Description = dto.Description,
            DetailsJson = dto.DetailsJson
        };

        _context.StaffAuditLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم تسجيل الإجراء بنجاح", logId = log.Id });
    }

    [HttpGet("evaluations")]
    public async Task<ActionResult<List<StaffEvaluationDto>>> GetEvaluations([FromQuery] Guid? staffUserId = null)
    {
        var query = _context.StaffEvaluations.AsQueryable();

        if (staffUserId.HasValue)
        {
            query = query.Where(e => e.StaffUserId == staffUserId.Value);
        }

        var evals = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new StaffEvaluationDto(
                e.Id,
                e.CreatedAtUtc,
                e.StaffUserId,
                e.StaffUserName,
                e.StaffRole,
                e.EvaluatorId,
                e.EvaluatorName,
                e.Period,
                e.PunctualityRating,
                e.PedagogyRating,
                e.SystemEngagementRating,
                e.CooperationRating,
                e.OverallScore,
                e.Notes,
                e.RecognitionBadge
            ))
            .ToListAsync();

        return Ok(evals);
    }

    [HttpPost("evaluations")]
    public async Task<ActionResult<StaffEvaluationDto>> SaveEvaluation([FromBody] SaveStaffEvaluationDto dto)
    {
        var average = Math.Round((dto.PunctualityRating + dto.PedagogyRating + dto.SystemEngagementRating + dto.CooperationRating) / 4.0, 1);

        var eval = new StaffEvaluation
        {
            StaffUserId = dto.StaffUserId,
            StaffUserName = dto.StaffUserName,
            StaffRole = dto.StaffRole.ToLower(),
            EvaluatorId = dto.EvaluatorId,
            EvaluatorName = dto.EvaluatorName,
            Period = dto.Period,
            PunctualityRating = dto.PunctualityRating,
            PedagogyRating = dto.PedagogyRating,
            SystemEngagementRating = dto.SystemEngagementRating,
            CooperationRating = dto.CooperationRating,
            OverallScore = average,
            Notes = dto.Notes,
            RecognitionBadge = dto.RecognitionBadge
        };

        _context.StaffEvaluations.Add(eval);

        // Record an audit log for the evaluation
        _context.StaffAuditLogs.Add(new StaffAuditLog
        {
            UserId = dto.EvaluatorId,
            UserName = dto.EvaluatorName,
            UserRole = "schoolmanager",
            ActionType = "Evaluation",
            Description = $"تسجيل تقييم دوري بدرجة {average} من 5 للموظف: {dto.StaffUserName}"
        });

        await _context.SaveChangesAsync();

        var result = new StaffEvaluationDto(
            eval.Id,
            eval.CreatedAtUtc,
            eval.StaffUserId,
            eval.StaffUserName,
            eval.StaffRole,
            eval.EvaluatorId,
            eval.EvaluatorName,
            eval.Period,
            eval.PunctualityRating,
            eval.PedagogyRating,
            eval.SystemEngagementRating,
            eval.CooperationRating,
            eval.OverallScore,
            eval.Notes,
            eval.RecognitionBadge
        );

        return Ok(result);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<StaffNotificationDto>>> GetNotifications()
    {
        var staffCount = await _context.Users.CountAsync(u => u.Role != UserRole.Student && u.Role != UserRole.Parent);

        var notifications = await _context.StaffNotifications
            .Include(n => n.Receipts)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();

        var result = notifications.Select(n =>
        {
            int targeted = staffCount;
            if (n.TargetRole.ToLower() == "single_user") targeted = 1;

            return new StaffNotificationDto(
                n.Id,
                n.CreatedAtUtc,
                n.SenderId,
                n.SenderName,
                n.Title,
                n.Content,
                n.TargetRole,
                n.TargetUserId,
                n.TargetUserName,
                n.Priority,
                n.RequiresAcknowledgment,
                targeted,
                n.Receipts.Count,
                n.Receipts.Count(r => r.AcknowledgedAtUtc.HasValue),
                n.Receipts.Select(r => new StaffNotificationReceiptDto(
                    r.Id,
                    r.UserId,
                    r.UserName,
                    r.UserRole,
                    r.ReadAtUtc,
                    r.AcknowledgedAtUtc
                )).ToList()
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPost("notifications")]
    public async Task<ActionResult<StaffNotificationDto>> CreateNotification([FromBody] CreateStaffNotificationDto dto)
    {
        var notification = new StaffNotification
        {
            SenderId = dto.SenderId,
            SenderName = dto.SenderName,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            TargetRole = dto.TargetRole,
            TargetUserId = dto.TargetUserId,
            TargetUserName = dto.TargetUserName,
            Priority = dto.Priority,
            RequiresAcknowledgment = dto.RequiresAcknowledgment
        };

        _context.StaffNotifications.Add(notification);

        // Record audit log
        _context.StaffAuditLogs.Add(new StaffAuditLog
        {
            UserId = dto.SenderId,
            UserName = dto.SenderName,
            UserRole = "schoolmanager",
            ActionType = "SendNotification",
            Description = $"إرسال تعميم إداري: \"{dto.Title}\" ({dto.Priority})"
        });

        await _context.SaveChangesAsync();

        var staffCount = await _context.Users.CountAsync(u => u.Role != UserRole.Student && u.Role != UserRole.Parent);

        return Ok(new StaffNotificationDto(
            notification.Id,
            notification.CreatedAtUtc,
            notification.SenderId,
            notification.SenderName,
            notification.Title,
            notification.Content,
            notification.TargetRole,
            notification.TargetUserId,
            notification.TargetUserName,
            notification.Priority,
            notification.RequiresAcknowledgment,
            staffCount,
            0,
            0,
            new List<StaffNotificationReceiptDto>()
        ));
    }

    [HttpDelete("notifications/{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var notification = await _context.StaffNotifications.FindAsync(id);
        if (notification == null) return NotFound(new { message = "الإشعار غير موجود" });

        _context.StaffNotifications.Remove(notification);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم حذف الإشعار بنجاح" });
    }

    [HttpGet("notifications/my")]
    public async Task<ActionResult<object>> GetMyNotifications([FromQuery] Guid userId, [FromQuery] string role)
    {
        var cleanRole = role?.Trim().ToLower() ?? "";

        var notifications = await _context.StaffNotifications
            .Include(n => n.Receipts)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();

        var filtered = notifications.Where(n =>
        {
            var target = n.TargetRole?.Trim().ToLower() ?? "";

            // 1. Internal Staff Circulars (Teachers, Engineers, Specialists, Leadership, Admins)
            // "all", "staff", "all_staff" represent school staff/employees only.
            // Under NO circumstance should internal staff directives reach students or parents!
            if (target == "all" || target == "staff" || target == "all_staff")
            {
                return cleanRole != "student" && cleanRole != "parent";
            }

            // 2. School-wide public announcements (to everyone including parents and students)
            if (target == "all_school" || target == "everyone")
            {
                return true;
            }

            // 3. Parent-targeted notifications
            if ((target == "parent" || target == "parents") && cleanRole == "parent") return true;

            // 4. Student-targeted notifications
            if ((target == "student" || target == "students") && cleanRole == "student") return true;

            // 5. Direct single user notification
            if (target == "single_user" && n.TargetUserId.HasValue && n.TargetUserId.Value == userId) return true;

            // 6. Direct role match for staff roles (e.g. "teacher", "socialworker", etc.)
            if (target == cleanRole) return true;

            return false;
        }).Select(n =>
        {
            var myReceipt = n.Receipts.FirstOrDefault(r => r.UserId == userId);
            return new
            {
                n.Id,
                n.CreatedAtUtc,
                n.SenderName,
                n.Title,
                n.Content,
                n.Priority,
                n.RequiresAcknowledgment,
                IsRead = myReceipt != null,
                ReadAtUtc = myReceipt?.ReadAtUtc,
                IsAcknowledged = myReceipt?.AcknowledgedAtUtc != null,
                AcknowledgedAtUtc = myReceipt?.AcknowledgedAtUtc
            };
        }).ToList();

        var unreadCount = filtered.Count(n => !n.IsRead);

        return Ok(new
        {
            UnreadCount = unreadCount,
            Notifications = filtered
        });
    }

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, [FromQuery] Guid userId, [FromQuery] string userName, [FromQuery] string userRole)
    {
        var notification = await _context.StaffNotifications.Include(n => n.Receipts).FirstOrDefaultAsync(n => n.Id == id);
        if (notification == null) return NotFound(new { message = "الإشعار غير موجود" });

        var existing = notification.Receipts.FirstOrDefault(r => r.UserId == userId);
        if (existing == null)
        {
            var receipt = new StaffNotificationReceipt
            {
                NotificationId = id,
                UserId = userId,
                UserName = userName,
                UserRole = userRole.ToLower(),
                ReadAtUtc = DateTime.UtcNow
            };
            _context.StaffNotificationReceipts.Add(receipt);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "تم تسجيل القراءة" });
    }

    [HttpPost("notifications/{id:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeNotification(Guid id, [FromQuery] Guid userId, [FromQuery] string userName, [FromQuery] string userRole)
    {
        var notification = await _context.StaffNotifications.Include(n => n.Receipts).FirstOrDefaultAsync(n => n.Id == id);
        if (notification == null) return NotFound(new { message = "الإشعار غير موجود" });

        var receipt = notification.Receipts.FirstOrDefault(r => r.UserId == userId);
        if (receipt == null)
        {
            receipt = new StaffNotificationReceipt
            {
                NotificationId = id,
                UserId = userId,
                UserName = userName,
                UserRole = userRole.ToLower(),
                ReadAtUtc = DateTime.UtcNow,
                AcknowledgedAtUtc = DateTime.UtcNow
            };
            _context.StaffNotificationReceipts.Add(receipt);
        }
        else
        {
            receipt.AcknowledgedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تأكيد العلم والاستلام بنجاح" });
    }
}

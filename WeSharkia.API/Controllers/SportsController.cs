using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SportsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var assessments = await _context.SportsAssessments.ToListAsync();
        var tournaments = await _context.SportsTournaments.OrderByDescending(t => t.StartDate).ToListAsync();

        var totalAssessed = assessments.Count;
        var excellentFitnessCount = assessments.Count(a => a.FitnessScore >= 85);
        var activeTournamentsCount = tournaments.Count(t => t.Status.Contains("جاري") || t.Status == "قادم");
        var badgesCount = assessments.Count(a => !string.IsNullOrEmpty(a.HonorBadge));

        return Ok(new
        {
            totalAssessed,
            excellentFitnessCount,
            activeTournamentsCount,
            badgesCount,
            tournaments,
            recentAssessments = assessments.OrderByDescending(a => a.RecordedAtUtc).Take(15).ToList()
        });
    }

    [HttpGet("assessments")]
    public async Task<IActionResult> GetAssessments([FromQuery] string? classroom, [FromQuery] int? grade)
    {
        var query = _context.SportsAssessments.AsQueryable();

        if (!string.IsNullOrEmpty(classroom) && classroom.ToLower() != "all")
        {
            query = query.Where(a => a.Classroom == classroom);
        }

        if (grade.HasValue && grade.Value > 0)
        {
            query = query.Where(a => a.Grade == grade.Value);
        }

        var list = await query.OrderBy(a => a.StudentName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("assessments")]
    public async Task<IActionResult> SaveAssessment([FromBody] SaveSportsAssessmentDto dto)
    {
        var student = await _context.Users.FindAsync(dto.StudentId);
        if (student == null) return NotFound(new { message = "الطالب غير موجود" });

        var existing = await _context.SportsAssessments
            .FirstOrDefaultAsync(a => a.StudentId == dto.StudentId);

        if (existing != null)
        {
            existing.MorningAssemblyStatus = dto.MorningAssemblyStatus ?? existing.MorningAssemblyStatus;
            existing.FitnessScore = dto.FitnessScore;
            existing.SpeedScore = dto.SpeedScore;
            existing.EnduranceScore = dto.EnduranceScore;
            existing.FlexibilityScore = dto.FlexibilityScore;
            existing.PreferredSport = dto.PreferredSport ?? existing.PreferredSport;
            existing.HonorBadge = dto.HonorBadge;
            existing.CoachNotes = dto.CoachNotes;
            existing.RecordedByCoachName = dto.CoachName ?? existing.RecordedByCoachName;
            existing.RecordedAtUtc = DateTime.UtcNow;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _context.SportsAssessments.Add(new SportsAssessment
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                StudentCode = student.StudentCode ?? student.Username,
                Classroom = student.Classroom ?? "A1",
                Grade = student.Grade ?? 1,
                MorningAssemblyStatus = dto.MorningAssemblyStatus ?? "حاضر ومنضبط",
                FitnessScore = dto.FitnessScore,
                SpeedScore = dto.SpeedScore,
                EnduranceScore = dto.EnduranceScore,
                FlexibilityScore = dto.FlexibilityScore,
                PreferredSport = dto.PreferredSport ?? "كرة قدم",
                HonorBadge = dto.HonorBadge,
                CoachNotes = dto.CoachNotes,
                RecordedByCoachId = dto.CoachId,
                RecordedByCoachName = dto.CoachName ?? "كابتن حسام حسن",
                RecordedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تسجيل التقييم البدني والرياضي بنجاح" });
    }

    [HttpGet("tournaments")]
    public async Task<IActionResult> GetTournaments()
    {
        var list = await _context.SportsTournaments
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("tournaments")]
    public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentDto dto)
    {
        var tournament = new SportsTournament
        {
            Title = dto.Title,
            TournamentType = dto.TournamentType ?? "دوري كرة القدم",
            TargetStages = dto.TargetStages ?? "كافة المراحل",
            StartDate = dto.StartDate ?? DateTime.UtcNow,
            Status = dto.Status ?? "جاري التنظيم والتصفيات",
            WinningClassroom = dto.WinningClassroom,
            BestPlayerOrScorer = dto.BestPlayerOrScorer,
            RulesAndNotes = dto.RulesAndNotes
        };

        _context.SportsTournaments.Add(tournament);
        await _context.SaveChangesAsync();
        return Ok(tournament);
    }

    [HttpPut("tournaments/{id}")]
    public async Task<IActionResult> UpdateTournament(Guid id, [FromBody] UpdateTournamentDto dto)
    {
        var t = await _context.SportsTournaments.FindAsync(id);
        if (t == null) return NotFound(new { message = "البطولة غير موجودة" });

        if (!string.IsNullOrEmpty(dto.Status)) t.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.WinningClassroom)) t.WinningClassroom = dto.WinningClassroom;
        if (!string.IsNullOrEmpty(dto.BestPlayerOrScorer)) t.BestPlayerOrScorer = dto.BestPlayerOrScorer;
        if (!string.IsNullOrEmpty(dto.RulesAndNotes)) t.RulesAndNotes = dto.RulesAndNotes;

        t.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(t);
    }
}

public class SaveSportsAssessmentDto
{
    public Guid StudentId { get; set; }
    public string? MorningAssemblyStatus { get; set; }
    public double FitnessScore { get; set; } = 85;
    public int SpeedScore { get; set; } = 8;
    public int EnduranceScore { get; set; } = 8;
    public int FlexibilityScore { get; set; } = 8;
    public string? PreferredSport { get; set; }
    public string? HonorBadge { get; set; }
    public string? CoachNotes { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
}

public class CreateTournamentDto
{
    public string Title { get; set; } = string.Empty;
    public string? TournamentType { get; set; }
    public string? TargetStages { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Status { get; set; }
    public string? WinningClassroom { get; set; }
    public string? BestPlayerOrScorer { get; set; }
    public string? RulesAndNotes { get; set; }
}

public class UpdateTournamentDto
{
    public string? Status { get; set; }
    public string? WinningClassroom { get; set; }
    public string? BestPlayerOrScorer { get; set; }
    public string? RulesAndNotes { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssessmentController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AssessmentController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var exams = await _context.SchoolExams
            .Include(e => e.Scores)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        var totalExams = exams.Count;
        var formativeTheoryCount = exams.Count(e => e.ExamType == "تكويني نظري");
        var formativePracticalCount = exams.Count(e => e.ExamType == "تكويني عملي");
        var remedialCount = exams.Count(e => e.ExamType == "تحسيني");

        var allScores = await _context.ExamStudentScores.ToListAsync();
        var totalGraded = allScores.Count;
        var remedialStudentsCount = allScores.Count(s => s.IsRemedial && s.RemedialStatus != "اجتاز التحسين بنجاح");
        var passedRemedialCount = allScores.Count(s => s.IsRemedial && s.RemedialStatus == "اجتاز التحسين بنجاح");

        return Ok(new
        {
            totalExams,
            formativeTheoryCount,
            formativePracticalCount,
            remedialCount,
            totalGraded,
            remedialStudentsCount,
            passedRemedialCount,
            exams
        });
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams([FromQuery] string? type, [FromQuery] string? classroom)
    {
        var query = _context.SchoolExams.Include(e => e.Scores).AsQueryable();

        if (!string.IsNullOrEmpty(type) && type.ToLower() != "all")
        {
            query = query.Where(e => e.ExamType == type);
        }

        if (!string.IsNullOrEmpty(classroom) && classroom.ToLower() != "all")
        {
            query = query.Where(e => e.Classroom == classroom || e.Classroom == "all");
        }

        var list = await query.OrderByDescending(e => e.ExamDate).ToListAsync();
        return Ok(list);
    }

    [HttpPost("exams")]
    public async Task<IActionResult> CreateExam([FromBody] CreateSchoolExamDto dto)
    {
        var exam = new SchoolExam
        {
            Title = dto.Title,
            ExamType = dto.ExamType,
            Subject = dto.Subject,
            Classroom = dto.Classroom ?? "all",
            Grade = dto.Grade > 0 ? dto.Grade : 1,
            MaxScore = dto.MaxScore > 0 ? dto.MaxScore : 100,
            PassingScore = dto.PassingScore > 0 ? dto.PassingScore : 50,
            ExamDate = dto.ExamDate ?? DateTime.UtcNow,
            CreatedById = dto.CreatedById,
            CreatedByName = dto.CreatedByName ?? "مسؤول التقييم والامتحانات",
            Notes = dto.Notes,
            IsPublished = true
        };

        _context.SchoolExams.Add(exam);
        await _context.SaveChangesAsync();

        // Optionally pre-populate students from target classroom
        var studentsQuery = _context.Users.Where(u => u.Role == UserRole.Student && u.Status == UserStatus.Approved);
        if (exam.Classroom != "all")
        {
            studentsQuery = studentsQuery.Where(u => u.Classroom == exam.Classroom);
        }
        else if (exam.Grade > 0)
        {
            studentsQuery = studentsQuery.Where(u => u.Grade == exam.Grade);
        }

        var students = await studentsQuery.OrderBy(u => u.FullName).Take(40).ToListAsync();
        if (students.Any())
        {
            var initialScores = students.Select(s => new ExamStudentScore
            {
                ExamId = exam.Id,
                StudentId = s.Id,
                StudentName = s.FullName,
                StudentCode = s.StudentCode ?? s.Username,
                Classroom = s.Classroom ?? "A1",
                Grade = s.Grade ?? 1,
                Score = 0,
                MaxScore = exam.MaxScore,
                IsRemedial = exam.ExamType == "تحسيني",
                RemedialStatus = exam.ExamType == "تحسيني" ? "يحتاج تحسين" : "مجتاز"
            }).ToList();

            _context.ExamStudentScores.AddRange(initialScores);
            await _context.SaveChangesAsync();
        }

        return Ok(exam);
    }

    [HttpGet("exams/{id}/scores")]
    public async Task<IActionResult> GetExamScores(Guid id)
    {
        var scores = await _context.ExamStudentScores
            .Where(s => s.ExamId == id)
            .OrderBy(s => s.StudentName)
            .ToListAsync();

        return Ok(scores);
    }

    [HttpPost("exams/{id}/scores")]
    public async Task<IActionResult> SaveExamScore(Guid id, [FromBody] SaveScoreDto dto)
    {
        var exam = await _context.SchoolExams.FindAsync(id);
        if (exam == null) return NotFound(new { message = "الامتحان غير موجود" });

        var existing = await _context.ExamStudentScores
            .FirstOrDefaultAsync(s => s.ExamId == id && s.StudentId == dto.StudentId);

        if (existing != null)
        {
            if (dto.IsRemedial && existing.Score > 0 && existing.PreviousScore == null)
            {
                existing.PreviousScore = existing.Score;
            }

            existing.Score = dto.Score;
            existing.MaxScore = exam.MaxScore;
            existing.TeacherNotes = dto.TeacherNotes ?? existing.TeacherNotes;
            existing.IsRemedial = dto.IsRemedial;
            
            // Auto determine remedial status if not manually overridden
            if (dto.IsRemedial)
            {
                existing.RemedialStatus = !string.IsNullOrEmpty(dto.RemedialStatus)
                    ? dto.RemedialStatus
                    : (dto.Score >= exam.PassingScore ? "اجتاز التحسين بنجاح" : "لم يجتز بعد - فرصة استثنائية");
            }
            else
            {
                existing.RemedialStatus = dto.Score < exam.PassingScore ? "يحتاج تحسين" : "مجتاز";
            }

            existing.RecordedAtUtc = DateTime.UtcNow;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var student = await _context.Users.FindAsync(dto.StudentId);
            if (student == null) return NotFound(new { message = "الطالب غير موجود" });

            _context.ExamStudentScores.Add(new ExamStudentScore
            {
                ExamId = id,
                StudentId = student.Id,
                StudentName = student.FullName,
                StudentCode = student.StudentCode ?? student.Username,
                Classroom = student.Classroom ?? "A1",
                Grade = student.Grade ?? 1,
                Score = dto.Score,
                MaxScore = exam.MaxScore,
                IsRemedial = dto.IsRemedial,
                RemedialStatus = dto.IsRemedial
                    ? (!string.IsNullOrEmpty(dto.RemedialStatus) ? dto.RemedialStatus : (dto.Score >= exam.PassingScore ? "اجتاز التحسين بنجاح" : "لم يجتز بعد - فرصة استثنائية"))
                    : (dto.Score < exam.PassingScore ? "يحتاج تحسين" : "مجتاز"),
                TeacherNotes = dto.TeacherNotes,
                RecordedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم رصد وحفظ درجة الطالب بنجاح" });
    }

    [HttpGet("remedial-students")]
    public async Task<IActionResult> GetRemedialStudents([FromQuery] string? classroom)
    {
        var query = _context.ExamStudentScores
            .Include(s => s.Exam)
            .Where(s => s.IsRemedial || s.RemedialStatus == "يحتاج تحسين" || s.RemedialStatus == "قيد الاختبار التحسيني" || s.RemedialStatus == "لم يجتز بعد - فرصة استثنائية");

        if (!string.IsNullOrEmpty(classroom) && classroom.ToLower() != "all")
        {
            query = query.Where(s => s.Classroom == classroom);
        }

        var list = await query.OrderBy(s => s.StudentName).ToListAsync();
        return Ok(list);
    }

    [HttpPut("scores/{scoreId}/remedial-status")]
    public async Task<IActionResult> UpdateRemedialStatus(Guid scoreId, [FromBody] UpdateRemedialStatusDto dto)
    {
        var score = await _context.ExamStudentScores.FindAsync(scoreId);
        if (score == null) return NotFound(new { message = "سجل الدرجة غير موجود" });

        score.IsRemedial = true;
        score.RemedialStatus = dto.RemedialStatus;
        if (dto.NewScore.HasValue)
        {
            if (score.PreviousScore == null) score.PreviousScore = score.Score;
            score.Score = dto.NewScore.Value;
        }
        if (!string.IsNullOrEmpty(dto.TeacherNotes)) score.TeacherNotes = dto.TeacherNotes;

        score.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(score);
    }
}

public class CreateSchoolExamDto
{
    public string Title { get; set; } = string.Empty;
    public string ExamType { get; set; } = "تكويني نظري";
    public string Subject { get; set; } = string.Empty;
    public string? Classroom { get; set; }
    public int Grade { get; set; } = 1;
    public double MaxScore { get; set; } = 100;
    public double PassingScore { get; set; } = 50;
    public DateTime? ExamDate { get; set; }
    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public string? Notes { get; set; }
}

public class SaveScoreDto
{
    public Guid StudentId { get; set; }
    public double Score { get; set; }
    public bool IsRemedial { get; set; } = false;
    public string? RemedialStatus { get; set; }
    public string? TeacherNotes { get; set; }
}

public class UpdateRemedialStatusDto
{
    public string RemedialStatus { get; set; } = "اجتاز التحسين بنجاح";
    public double? NewScore { get; set; }
    public string? TeacherNotes { get; set; }
}

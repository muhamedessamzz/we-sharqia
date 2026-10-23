using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Operations;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private static readonly object _lock = new();
    private static SchoolScheduleData? _cachedSchedule;
    private static readonly string ScheduleFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "school_schedule.json");
    private static readonly string AltScheduleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "school_schedule.json");
    private static readonly string LinksFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "staff_links.json");

    public ScheduleController(IApplicationDbContext context)
    {
        _context = context;
        EnsureScheduleLoaded();
    }

    private static void EnsureScheduleLoaded()
    {
        if (_cachedSchedule != null) return;

        lock (_lock)
        {
            if (_cachedSchedule != null) return;

            string targetPath = System.IO.File.Exists(ScheduleFilePath) ? ScheduleFilePath : AltScheduleFilePath;
            if (!System.IO.File.Exists(targetPath))
            {
                // Look relative to solution directory
                var devPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "WeSharkia.API", "Data", "school_schedule.json"));
                if (System.IO.File.Exists(devPath)) targetPath = devPath;
            }

            if (System.IO.File.Exists(targetPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(targetPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _cachedSchedule = JsonSerializer.Deserialize<SchoolScheduleData>(json, options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScheduleController] Error reading schedule JSON: {ex.Message}");
                }
            }

            // Load any manually saved staff links
            LoadStaffLinks();
        }
    }

    private static void LoadStaffLinks()
    {
        if (_cachedSchedule?.Staff == null) return;

        try
        {
            if (System.IO.File.Exists(LinksFilePath))
            {
                var linksJson = System.IO.File.ReadAllText(LinksFilePath);
                var links = JsonSerializer.Deserialize<Dictionary<string, StaffLinkRecord>>(linksJson);
                if (links != null)
                {
                    foreach (var staff in _cachedSchedule.Staff)
                    {
                        if (links.TryGetValue(staff.Id, out var link) || links.TryGetValue(staff.CanonicalName, out link))
                        {
                            staff.UserAccountId = link.UserId;
                            staff.MatchedUserEmail = link.UserEmail;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleController] Error reading staff links: {ex.Message}");
        }
    }

    private static void SaveStaffLinks()
    {
        if (_cachedSchedule?.Staff == null) return;

        try
        {
            var dict = new Dictionary<string, StaffLinkRecord>();
            foreach (var s in _cachedSchedule.Staff)
            {
                if (!string.IsNullOrEmpty(s.UserAccountId) || !string.IsNullOrEmpty(s.MatchedUserEmail))
                {
                    dict[s.Id] = new StaffLinkRecord(s.Id, s.CanonicalName, s.UserAccountId, s.MatchedUserEmail);
                }
            }

            var dir = Path.GetDirectoryName(LinksFilePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            System.IO.File.WriteAllText(LinksFilePath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleController] Error writing staff links: {ex.Message}");
        }
    }

    public static string NormalizeArabic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var t = text.Trim();
        t = Regex.Replace(t, @"^([مأا]\/|\bمهندس\b|\bأستاذ\b|\bاستاذ\b|\bد\b|\bدكتور\b)\s*", "", RegexOptions.IgnoreCase);
        t = t.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ة', 'ه').Replace('ى', 'ي');
        // remove diacritics / tashkeel
        t = Regex.Replace(t, @"[\u064B-\u065F\u0670]", "");
        t = Regex.Replace(t, @"\s+", " ").Trim().ToLowerInvariant();
        return t;
    }

    [HttpGet]
    public ActionResult<List<object>> GetClassSchedule([FromQuery] string? classroom)
    {
        EnsureScheduleLoaded();

        var cleanClass = classroom?.Trim().ToUpperInvariant() ?? "A1";

        if (_cachedSchedule?.Classes != null && _cachedSchedule.Classes.Count > 0)
        {
            var list = _cachedSchedule.Classes
                .Where(s => string.Equals(s.Classroom, cleanClass, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodNumber)
                .Select(s => new
                {
                    id = s.Id,
                    grade = s.Grade,
                    classroom = s.Classroom,
                    dayOfWeek = s.DayName, // Arabic day name e.g. "الأحد"
                    dayIndex = s.DayOfWeek, // 0..4
                    periodNumber = s.PeriodNumber,
                    periodName = s.PeriodName,
                    startTime = s.StartTime,
                    endTime = s.EndTime,
                    subject = s.SubjectName,
                    subjectName = s.SubjectName,
                    teacherName = s.TeacherName,
                    cleanTeacherName = s.CleanTeacherName,
                    roomName = s.RoomOrLab,
                    roomOrLab = s.RoomOrLab,
                    isLab = s.IsLabSession,
                    isLabSession = s.IsLabSession,
                    note = s.Note
                })
                .ToList();

            return Ok(list);
        }

        return Ok(new List<object>());
    }

    [HttpGet("teacher")]
    public ActionResult<object> GetTeacherSchedule([FromQuery] string? teacherName, [FromQuery] string? userId)
    {
        EnsureScheduleLoaded();

        if (_cachedSchedule == null)
        {
            return Ok(new { matched = false, classes = new List<object>(), duties = new List<object>(), allStaff = new List<object>() });
        }

        StaffScheduleRecord? matchedStaff = null;
        var cleanInput = teacherName?.Trim() ?? string.Empty;
        var normInput = NormalizeArabic(cleanInput);

        // 1. Try matching by UserId if passed
        if (!string.IsNullOrEmpty(userId) && _cachedSchedule.Staff != null)
        {
            matchedStaff = _cachedSchedule.Staff.FirstOrDefault(s => string.Equals(s.UserAccountId, userId, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Try exact canonical name or clean name match
        if (matchedStaff == null && !string.IsNullOrEmpty(cleanInput) && _cachedSchedule.Staff != null)
        {
            matchedStaff = _cachedSchedule.Staff.FirstOrDefault(s =>
                string.Equals(s.CanonicalName, cleanInput, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.CleanName, cleanInput, StringComparison.OrdinalIgnoreCase));
        }

        // 3. Try normalized Arabic match
        if (matchedStaff == null && !string.IsNullOrEmpty(normInput) && _cachedSchedule.Staff != null)
        {
            matchedStaff = _cachedSchedule.Staff.FirstOrDefault(s =>
                string.Equals(s.NormalizedName, normInput, StringComparison.OrdinalIgnoreCase));
        }

        // 4. Try fuzzy substring / token match
        if (matchedStaff == null && !string.IsNullOrEmpty(normInput) && _cachedSchedule.Staff != null)
        {
            matchedStaff = _cachedSchedule.Staff.FirstOrDefault(s =>
                s.NormalizedName.Contains(normInput) || normInput.Contains(s.NormalizedName));
        }

        // If still not matched, default to the first staff member (e.g. م/ إبراهيم رجب)
        var fallbackStaff = matchedStaff ?? _cachedSchedule.Staff?.FirstOrDefault();

        var staffNameForFilter = fallbackStaff != null ? fallbackStaff.CanonicalName : cleanInput;
        var normStaffName = fallbackStaff != null ? fallbackStaff.NormalizedName : normInput;

        var teacherClasses = _cachedSchedule.Classes
            .Where(c =>
                string.Equals(c.TeacherName, staffNameForFilter, StringComparison.OrdinalIgnoreCase) ||
                (fallbackStaff != null && string.Equals(c.CleanTeacherName, fallbackStaff.CleanName, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(normStaffName) && NormalizeArabic(c.TeacherName).Contains(normStaffName)))
            .OrderBy(c => c.DayOfWeek)
            .ThenBy(c => c.PeriodNumber)
            .Select(s => new
            {
                id = s.Id,
                grade = s.Grade,
                classroom = s.Classroom,
                dayOfWeek = s.DayName,
                dayIndex = s.DayOfWeek,
                periodNumber = s.PeriodNumber,
                periodName = s.PeriodName,
                startTime = s.StartTime,
                endTime = s.EndTime,
                subject = s.SubjectName,
                subjectName = s.SubjectName,
                teacherName = s.TeacherName,
                cleanTeacherName = s.CleanTeacherName,
                roomName = s.RoomOrLab,
                roomOrLab = s.RoomOrLab,
                isLab = s.IsLabSession,
                isLabSession = s.IsLabSession,
                note = s.Note
            })
            .ToList();

        var teacherDuties = _cachedSchedule.Duties
            .Where(d =>
                string.Equals(d.TeacherName, staffNameForFilter, StringComparison.OrdinalIgnoreCase) ||
                (fallbackStaff != null && string.Equals(d.CleanTeacherName, fallbackStaff.CleanName, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(normStaffName) && NormalizeArabic(d.TeacherName).Contains(normStaffName)))
            .OrderBy(d => d.DayOfWeek)
            .Select(d => new
            {
                id = d.Id,
                teacherName = d.TeacherName,
                cleanTeacherName = d.CleanTeacherName,
                dutyType = d.DutyType,
                dayOfWeek = d.DayName,
                dayIndex = d.DayOfWeek,
                periodsText = d.PeriodsText,
                startTime = d.StartTime,
                endTime = d.EndTime,
                notes = d.Notes,
                isConflictResolved = d.IsConflictResolved
            })
            .ToList();

        return Ok(new
        {
            matched = matchedStaff != null,
            staff = fallbackStaff,
            classes = teacherClasses,
            duties = teacherDuties,
            allStaff = _cachedSchedule.Staff
        });
    }

    [HttpGet("master")]
    public ActionResult<object> GetMasterSchedule(
        [FromQuery] string? day,
        [FromQuery] string? classroom,
        [FromQuery] string? teacher)
    {
        EnsureScheduleLoaded();

        if (_cachedSchedule == null)
        {
            return Ok(new { classes = new List<object>(), duties = new List<object>() });
        }

        var classes = _cachedSchedule.Classes.AsEnumerable();
        var duties = _cachedSchedule.Duties.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(day) && day != "all")
        {
            classes = classes.Where(c => c.DayName == day || c.DayOfWeek.ToString() == day);
            duties = duties.Where(d => d.DayName == day || d.DayOfWeek.ToString() == day);
        }

        if (!string.IsNullOrWhiteSpace(classroom) && classroom != "all")
        {
            classes = classes.Where(c => string.Equals(c.Classroom, classroom, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(teacher) && teacher != "all")
        {
            var normT = NormalizeArabic(teacher);
            classes = classes.Where(c => NormalizeArabic(c.TeacherName).Contains(normT));
            duties = duties.Where(d => NormalizeArabic(d.TeacherName).Contains(normT));
        }

        return Ok(new
        {
            academicYear = _cachedSchedule.AcademicYear,
            days = _cachedSchedule.Days,
            periodTimes = _cachedSchedule.PeriodTimes,
            classrooms = _cachedSchedule.Classrooms,
            staff = _cachedSchedule.Staff,
            classes = classes.OrderBy(c => c.DayOfWeek).ThenBy(c => c.PeriodNumber).ToList(),
            duties = duties.OrderBy(d => d.DayOfWeek).ToList()
        });
    }

    [HttpGet("staff")]
    public ActionResult<List<StaffScheduleRecord>> GetStaffList()
    {
        EnsureScheduleLoaded();
        return Ok(_cachedSchedule?.Staff ?? new List<StaffScheduleRecord>());
    }

    [HttpPost("staff/link")]
    public ActionResult<object> LinkStaffAccount([FromBody] LinkStaffAccountRequest request)
    {
        EnsureScheduleLoaded();

        if (_cachedSchedule?.Staff == null)
        {
            return BadRequest(new { message = "الجدول غير محمل" });
        }

        var targetStaff = _cachedSchedule.Staff.FirstOrDefault(s =>
            s.Id == request.StaffId ||
            s.CanonicalName == request.StaffName ||
            s.CleanName == request.StaffName);

        if (targetStaff == null)
        {
            return NotFound(new { message = "لم يتم العثور على عضو هيئة التدريس في الجدول" });
        }

        targetStaff.UserAccountId = request.UserId;
        targetStaff.MatchedUserEmail = request.UserEmail;

        SaveStaffLinks();

        return Ok(new
        {
            success = true,
            message = $"تم ربط جدول {targetStaff.CanonicalName} بالحساب بنجاح",
            staff = targetStaff
        });
    }

    [HttpGet("all")]
    public ActionResult<List<object>> GetAllSchedules()
    {
        EnsureScheduleLoaded();

        if (_cachedSchedule?.Classes != null)
        {
            var list = _cachedSchedule.Classes
                .OrderBy(s => s.Classroom)
                .ThenBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodNumber)
                .Select(s => new
                {
                    id = s.Id,
                    grade = s.Grade,
                    classroom = s.Classroom,
                    dayOfWeek = s.DayName,
                    dayIndex = s.DayOfWeek,
                    periodNumber = s.PeriodNumber,
                    periodName = s.PeriodName,
                    startTime = s.StartTime,
                    endTime = s.EndTime,
                    subject = s.SubjectName,
                    subjectName = s.SubjectName,
                    teacherName = s.TeacherName,
                    cleanTeacherName = s.CleanTeacherName,
                    roomName = s.RoomOrLab,
                    roomOrLab = s.RoomOrLab,
                    isLab = s.IsLabSession,
                    isLabSession = s.IsLabSession,
                    note = s.Note
                })
                .ToList();

            return Ok(list);
        }

        return Ok(new List<object>());
    }
}

// Data models for parsing JSON
public class SchoolScheduleData
{
    public string AcademicYear { get; set; } = "2026/2027";
    public string GeneratedAt { get; set; } = string.Empty;
    public List<SchedulePeriodTime> PeriodTimes { get; set; } = new();
    public List<ScheduleDayItem> Days { get; set; } = new();
    public List<ScheduleClassroomItem> Classrooms { get; set; } = new();
    public List<StaffScheduleRecord> Staff { get; set; } = new();
    public List<ClassSessionRecord> Classes { get; set; } = new();
    public List<DutyScheduleRecord> Duties { get; set; } = new();
}

public class SchedulePeriodTime
{
    public object Period { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsBreak { get; set; }
    public int? DurationMinutes { get; set; }
}

public class ScheduleDayItem
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ScheduleClassroomItem
{
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class StaffScheduleRecord
{
    public string Id { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string CleanName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int WeeklyClassesCount { get; set; }
    public int WeeklyDutiesCount { get; set; }
    public List<string> Subjects { get; set; } = new();
    public List<string> Classrooms { get; set; } = new();
    public string? UserAccountId { get; set; }
    public string? MatchedUserEmail { get; set; }
}

public class ClassSessionRecord
{
    public string Id { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Classroom { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int PeriodNumber { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string CleanTeacherName { get; set; } = string.Empty;
    public string RoomOrLab { get; set; } = string.Empty;
    public bool IsLabSession { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class DutyScheduleRecord
{
    public string Id { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string CleanTeacherName { get; set; } = string.Empty;
    public string DutyType { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string PeriodsText { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsConflictResolved { get; set; }
}

public record StaffLinkRecord(string StaffId, string StaffName, string? UserId, string? UserEmail);

public class LinkStaffAccountRequest
{
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
}

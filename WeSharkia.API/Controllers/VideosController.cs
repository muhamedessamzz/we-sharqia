using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Application.DTOs.Videos;
using WeSharkia.Domain.Entities;

namespace WeSharkia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public VideosController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<VideoTutorialDto>>> GetVideos([FromQuery] Guid? studentId)
    {
        var list = await _context.VideoTutorials
            .Include(v => v.Views)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync();

        var result = list.Select(v =>
        {
            var studentView = studentId.HasValue ? v.Views.FirstOrDefault(x => x.StudentId == studentId.Value) : null;
            var steps = !string.IsNullOrEmpty(v.VisualStepsJson)
                ? JsonSerializer.Deserialize<List<string>>(v.VisualStepsJson) ?? new List<string>()
                : new List<string>();

            return new VideoTutorialDto(
                v.Id,
                v.Title,
                v.Category,
                v.Url,
                v.WrittenExplanation,
                steps,
                v.AudioSpeechText,
                v.CreatedAtUtc,
                v.Views.Sum(x => x.ViewCount),
                studentView?.WatchedFully,
                studentView?.UnderstoodRating
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VideoTutorialDto>> CreateVideo([FromBody] CreateVideoTutorialDto dto)
    {
        var v = new VideoTutorial
        {
            Title = dto.Title,
            Category = dto.Category,
            Url = dto.Url,
            WrittenExplanation = dto.WrittenExplanation,
            VisualStepsJson = JsonSerializer.Serialize(dto.VisualSteps),
            AudioSpeechText = dto.AudioSpeechText
        };

        _context.VideoTutorials.Add(v);
        await _context.SaveChangesAsync();

        return Ok(new VideoTutorialDto(
            v.Id,
            v.Title,
            v.Category,
            v.Url,
            v.WrittenExplanation,
            dto.VisualSteps,
            v.AudioSpeechText,
            v.CreatedAtUtc,
            0,
            null,
            null
        ));
    }

    [HttpPost("{id:guid}/view")]
    public async Task<IActionResult> RecordView(Guid id, [FromQuery] Guid studentId, [FromBody] RecordVideoViewDto dto)
    {
        var view = await _context.VideoViews.FirstOrDefaultAsync(v => v.VideoId == id && v.StudentId == studentId);
        if (view == null)
        {
            view = new VideoView
            {
                VideoId = id,
                StudentId = studentId,
                ViewCount = 1,
                WatchedFully = dto.WatchedFully ?? false,
                UnderstoodRating = dto.UnderstoodRating,
                LastViewedAtUtc = DateTime.UtcNow
            };
            _context.VideoViews.Add(view);
        }
        else
        {
            view.ViewCount++;
            if (dto.WatchedFully.HasValue) view.WatchedFully = dto.WatchedFully.Value;
            if (dto.UnderstoodRating.HasValue) view.UnderstoodRating = dto.UnderstoodRating.Value;
            view.LastViewedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تسجيل المشاهدة بنجاح" });
    }
}

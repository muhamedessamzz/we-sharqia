using Microsoft.EntityFrameworkCore;
using WeSharkia.Application.Common.Interfaces;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentStage> AssignmentStages => Set<AssignmentStage>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<StudentProgress> StudentProgresses => Set<StudentProgress>();
    public DbSet<VideoTutorial> VideoTutorials => Set<VideoTutorial>();
    public DbSet<VideoView> VideoViews => Set<VideoView>();
    public DbSet<DigitalSkill> DigitalSkills => Set<DigitalSkill>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<ClassroomObservation> ClassroomObservations => Set<ClassroomObservation>();
    public DbSet<StudentSupportCase> StudentSupportCases => Set<StudentSupportCase>();
    public DbSet<ClassroomAnnouncement> ClassroomAnnouncements => Set<ClassroomAnnouncement>();
    public DbSet<StaffNotification> StaffNotifications => Set<StaffNotification>();
    public DbSet<StaffNotificationReceipt> StaffNotificationReceipts => Set<StaffNotificationReceipt>();
    public DbSet<StaffAuditLog> StaffAuditLogs => Set<StaffAuditLog>();
    public DbSet<StaffEvaluation> StaffEvaluations => Set<StaffEvaluation>();
    public DbSet<StaffAttendance> StaffAttendances => Set<StaffAttendance>();
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<ClassScheduleItem> ClassScheduleItems => Set<ClassScheduleItem>();
    public DbSet<ParentRequest> ParentRequests => Set<ParentRequest>();
    public DbSet<StaffLeaveRequest> StaffLeaveRequests => Set<StaffLeaveRequest>();
    public DbSet<StaffInvestigation> StaffInvestigations => Set<StaffInvestigation>();
    public DbSet<StaffMemo> StaffMemos => Set<StaffMemo>();
    public DbSet<SchoolExam> SchoolExams => Set<SchoolExam>();
    public DbSet<ExamStudentScore> ExamStudentScores => Set<ExamStudentScore>();
    public DbSet<SportsAssessment> SportsAssessments => Set<SportsAssessment>();
    public DbSet<SportsTournament> SportsTournaments => Set<SportsTournament>();
    public DbSet<StudentDisciplineNotice> StudentDisciplineNotices => Set<StudentDisciplineNotice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Self-referencing (Parent -> Children)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Parent)
            .WithMany(u => u.Children)
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Assignment -> Teacher
        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Teacher)
            .WithMany()
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignment -> Stages
        modelBuilder.Entity<AssignmentStage>()
            .HasOne(s => s.Assignment)
            .WithMany(a => a.Stages)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stage -> Questions
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Stage)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        // StudentProgress
        modelBuilder.Entity<StudentProgress>()
            .HasOne(p => p.Student)
            .WithMany(u => u.ProgressRecords)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentProgress>()
            .HasOne(p => p.Assignment)
            .WithMany(a => a.ProgressRecords)
            .HasForeignKey(p => p.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // VideoView
        modelBuilder.Entity<VideoView>()
            .HasOne(v => v.Student)
            .WithMany(u => u.VideoViews)
            .HasForeignKey(v => v.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VideoView>()
            .HasOne(v => v.Video)
            .WithMany(t => t.Views)
            .HasForeignKey(v => v.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        // StaffNotification -> Receipts
        modelBuilder.Entity<StaffNotificationReceipt>()
            .HasOne(r => r.Notification)
            .WithMany(n => n.Receipts)
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

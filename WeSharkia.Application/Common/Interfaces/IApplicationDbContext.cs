using Microsoft.EntityFrameworkCore;
using WeSharkia.Domain.Entities;

namespace WeSharkia.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<AssignmentStage> AssignmentStages { get; }
    DbSet<Question> Questions { get; }
    DbSet<StudentProgress> StudentProgresses { get; }
    DbSet<VideoTutorial> VideoTutorials { get; }
    DbSet<VideoView> VideoViews { get; }
    DbSet<DigitalSkill> DigitalSkills { get; }
    DbSet<SurveyResponse> SurveyResponses { get; }
    DbSet<ClassroomObservation> ClassroomObservations { get; }
    DbSet<StudentSupportCase> StudentSupportCases { get; }
    DbSet<ClassroomAnnouncement> ClassroomAnnouncements { get; }
    DbSet<StaffNotification> StaffNotifications { get; }
    DbSet<StaffNotificationReceipt> StaffNotificationReceipts { get; }
    DbSet<StaffAuditLog> StaffAuditLogs { get; }
    DbSet<StaffEvaluation> StaffEvaluations { get; }
    DbSet<StaffAttendance> StaffAttendances { get; }
    DbSet<StudentAttendance> StudentAttendances { get; }
    DbSet<ClassScheduleItem> ClassScheduleItems { get; }
    DbSet<ParentRequest> ParentRequests { get; }
    DbSet<StaffLeaveRequest> StaffLeaveRequests { get; }
    DbSet<StaffInvestigation> StaffInvestigations { get; }
    DbSet<StaffMemo> StaffMemos { get; }
    DbSet<SchoolExam> SchoolExams { get; }
    DbSet<ExamStudentScore> ExamStudentScores { get; }
    DbSet<SportsAssessment> SportsAssessments { get; }
    DbSet<SportsTournament> SportsTournaments { get; }
    DbSet<StudentDisciplineNotice> StudentDisciplineNotices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeSharkia.Application.Common.Security;
using WeSharkia.Domain.Entities;
using WeSharkia.Domain.Enums;

namespace WeSharkia.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureSchemaExtensionsAsync(context);

        if (await context.Users.AnyAsync())
        {
            await EnsureStaffMembersAsync(context);
            await EnsureStudentsFromFileAsync(context);
            await EnsureStaffManagerDataAsync(context);
            await EnsureSchoolOperationsDataAsync(context);
            return; // Already seeded
        }

        // 1. Seed Users for each role
        var admin = new User
        {
            FullName = "مدير النظام (الأدمن)",
            Username = "admin",
            PasswordHash = "admin123", // In production, use BCrypt or PBKDF2
            Role = UserRole.Admin,
            Status = UserStatus.Approved
        };

        var teacher = new User
        {
            FullName = "أستاذ أحمد الشافعي",
            Username = "teacher",
            PasswordHash = "teacher123",
            Role = UserRole.Teacher,
            Status = UserStatus.Approved
        };

        var student1 = new User
        {
            FullName = "علي محمد الشرقاوي",
            Username = "student1",
            PasswordHash = "student123",
            Role = UserRole.Student,
            Status = UserStatus.Approved,
            ParentConsent = true,
            Grade = 1,
            Classroom = "A1"
        };

        var student2 = new User
        {
            FullName = "سارة إبراهيم",
            Username = "student2",
            PasswordHash = "student123",
            Role = UserRole.Student,
            Status = UserStatus.Approved,
            ParentConsent = true,
            Grade = 2,
            Classroom = "B1"
        };


        var parent = new User
        {
            FullName = "محمد الشرقاوي (ولي أمر)",
            Username = "parent1",
            PasswordHash = "parent123",
            Role = UserRole.Parent,
            Status = UserStatus.Approved
        };

        var social = new User
        {
            FullName = "أ. منى (الأخصائي الاجتماعي)",
            Username = "social",
            PasswordHash = "social123",
            Role = UserRole.SocialWorker,
            Status = UserStatus.Approved
        };

        var quality = new User
        {
            FullName = "د. كمال (منسق الجودة)",
            Username = "quality",
            PasswordHash = "quality123",
            Role = UserRole.QualityCoordinator,
            Status = UserStatus.Approved
        };

        var it = new User
        {
            FullName = "م. تامر (مسؤول IT)",
            Username = "it",
            PasswordHash = "it123",
            Role = UserRole.IT,
            Status = UserStatus.Approved
        };

        await context.Users.AddRangeAsync(admin, teacher, student1, student2, parent, social, quality, it);
        await context.SaveChangesAsync();

        // Link parent to student1
        student1.ParentId = parent.Id;
        await context.SaveChangesAsync();

        // 2. Seed Sample Videos (جاهز: الفيديوهات)
        var v1 = new VideoTutorial
        {
            Title = "كيف تبدأ تكليفك وترسل الحل خطوة بخطوة",
            Category = "solve",
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            WrittenExplanation = "في هذا الفيديو نتعلم كيفية قراءة الأسئلة، واستخدام ميزة طلب المساعدة في حال التعثر، وتأكيد التسليم في الموعد.",
            VisualStepsJson = JsonSerializer.Serialize(new[]
            {
                "افتح قائمة تكليفاتي من اللوحة الرئيسية",
                "ابدأ بالمرحلة الأولى السهلة وركّز في قراءة المطلوب",
                "إذا واجهتك صعوبة اضغط زر (أحتاج مساعدة 🙋)",
                "بعد إنهاء المراحل اضغط تسليم واستلم شهادتك"
            }),
            AudioSpeechText = "أهلاً بك في منصة وي الشرقية. في هذا الفيديو سنتعلم كيفية إنهاء التكليف بنجاح."
        };

        var v2 = new VideoTutorial
        {
            Title = "تنظيم الوقت والتعلم الذاتي",
            Category = "time",
            Url = "https://www.youtube.com/watch?v=kJQP7kiw5Fk",
            WrittenExplanation = "استراتيجية الـ 15 دقيقة الأولى: ابدأ فوراً بدون تأجيل وتغلب على المماطلة لتفادي التأخير.",
            VisualStepsJson = JsonSerializer.Serialize(new[]
            {
                "قسّم وقتك إلى فترات مركزة 15 دقيقة",
                "أغلق المشتتات والصفحات غير المطلوبة",
                "راجع إجابتك بدقة قبل الانتقال للمرحلة التالية"
            }),
            AudioSpeechText = "تعلم كيف تنظم وقتك وتسلم تكليفاتك في الموعد المحدد."
        };

        await context.VideoTutorials.AddRangeAsync(v1, v2);

        // 3. Seed Sample Assignment (تدرجي 3 مراحل: سهل - متوسط - صعب)
        var assignment = new Assignment
        {
            TeacherId = teacher.Id,
            Title = "المهمة الأولى: أساسيات الشبكات والمهارات الرقمية",
            Instructions = "اقرأ الأسئلة بعناية وتدرج من المرحلة الأولى وحتى المرحلة الثالثة. لا تتردد في طلب المساعدة عند الحاجة.",
            SuggestedMinutes = 15,
            DueDate = DateTime.UtcNow.AddDays(3),
            Mode = AssignmentMode.Sequential,
            IsPublished = true,
            AssignToAll = true,
            Phase = MeasurementPhase.Baseline,
            ResourceLinksJson = JsonSerializer.Serialize(new[] { "https://wesharkia.local/docs/intro" })
        };

        // Stage 1 (سهل)
        var s1 = new AssignmentStage
        {
            Assignment = assignment,
            Name = "المرحلة الأولى: مفاهيم أساسية (سهل)",
            OrderIndex = 0,
            PassingScorePercent = 60
        };
        s1.Questions.Add(new Question
        {
            Stage = s1,
            Text = "ما هو البروتوكول المستخدم لتصفح مواقع الويب الآمنة؟",
            Type = QuestionType.MultipleChoice,
            Points = 5,
            Explanation = "HTTPS هو البروتوكول الآمن لتصفح الويب المشفر.",
            OptionsJson = JsonSerializer.Serialize(new[] { "FTP", "HTTP", "HTTPS", "SMTP" }),
            CorrectIndex = 2
        });
        s1.Questions.Add(new Question
        {
            Stage = s1,
            Text = "يمكن لطالب مدرسة WE طلب المساعدة من المعلم أو الزميل في أي وقت عند التعثر.",
            Type = QuestionType.TrueFalse,
            Points = 5,
            Explanation = "طلب المساعدة دليل وعي ورغبة في التعلم وليس عجزاً.",
            OptionsJson = JsonSerializer.Serialize(new[] { "صح", "خطأ" }),
            CorrectIndex = 0
        });

        // Stage 2 (متوسط)
        var s2 = new AssignmentStage
        {
            Assignment = assignment,
            Name = "المرحلة الثانية: التطبيق العملي (متوسط)",
            OrderIndex = 1,
            PassingScorePercent = 60
        };
        s2.Questions.Add(new Question
        {
            Stage = s2,
            Text = "رتب خطوات حل المشكلة البرمجية بالشكل السليم:",
            Type = QuestionType.Order,
            Points = 10,
            Explanation = "فهم المشكلة أولاً ثم التخطيط ثم كتابة الكود ثم الاختبار.",
            OptionsJson = JsonSerializer.Serialize(new[]
            {
                "فهم المشكلة وقراءة المدخلات",
                "التخطيط وكتابة الخوارزمية",
                "كتابة الكود البرمجي",
                "اختبار الحل وتصحيح الأخطاء"
            }),
            CorrectIndex = 0
        });

        // Stage 3 (صعب)
        var s3 = new AssignmentStage
        {
            Assignment = assignment,
            Name = "المرحلة الثالثة: التحدي والتحليل (صعب)",
            OrderIndex = 2,
            PassingScorePercent = 60
        };
        s3.Questions.Add(new Question
        {
            Stage = s3,
            Text = "ما هي الفائدة الرئيسية من استخدام بنية Clean Architecture في بناء الأنظمة الكبيرة؟",
            Type = QuestionType.MultipleChoice,
            Points = 10,
            Explanation = "فصل المسؤوليات وتسهيل الصيانة والاختبار دون الاعتماد على مكتبات أو قواعد بيانات بعينها.",
            OptionsJson = JsonSerializer.Serialize(new[]
            {
                "زيادة حجم الكود بدون فائدة",
                "فصل منطق العمل (Business Logic) وتسهيل الصيانة والاختبار",
                "جعل النظام يعمل على منصة واحدة فقط",
                "منع استخدام قواعد البيانات"
            }),
            CorrectIndex = 1
        });

        assignment.Stages.Add(s1);
        assignment.Stages.Add(s2);
        assignment.Stages.Add(s3);

        await context.Assignments.AddAsync(assignment);
        await context.SaveChangesAsync();

        // 4. Sample Progress for student1
        var progress = new StudentProgress
        {
            StudentId = student1.Id,
            AssignmentId = assignment.Id,
            Status = ProgressStatus.Working,
            CurrentStageIndex = 0,
            Stars = 2,
            AutoScore = 10,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            LastActiveAtUtc = DateTime.UtcNow
        };
        await context.StudentProgresses.AddAsync(progress);

        // 5. Sample Skills for student1
        await context.DigitalSkills.AddRangeAsync(
            new DigitalSkill { StudentId = student1.Id, SkillKey = "open", ClaimedByStudent = true, ConfirmedByTeacher = true },
            new DigitalSkill { StudentId = student1.Id, SkillKey = "reply", ClaimedByStudent = true, ConfirmedByTeacher = true },
            new DigitalSkill { StudentId = student1.Id, SkillKey = "forms", ClaimedByStudent = true, ConfirmedByTeacher = false },
            new DigitalSkill { StudentId = student1.Id, SkillKey = "search", ClaimedByStudent = true, ConfirmedByTeacher = false },
            new DigitalSkill { StudentId = student1.Id, SkillKey = "time", ClaimedByStudent = false, ConfirmedByTeacher = false }
        );

        if (!await context.StudentSupportCases.AnyAsync())
        {
            var notesStudent1 = new List<object>
            {
                new {
                    date = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd HH:mm"),
                    type = "إرشاد أكاديمي وسلوكي",
                    priority = "متوسطة",
                    note = "متابعة الالتزام بحل التكليفات في المعمل وتطوير الثقة بالنفس عند استخدام موجهات Cisco.",
                    counselor = "أ. منى (الأخصائي الاجتماعي)",
                    actionPlan = "خطة تحفيز وتشجيع وربط مع زميل متميز (Peer-tutoring)."
                }
            };
            await context.StudentSupportCases.AddAsync(new StudentSupportCase
            {
                StudentId = student1.Id,
                Status = "inSupport",
                NotesJson = System.Text.Json.JsonSerializer.Serialize(notesStudent1)
            });
        }

        await context.SaveChangesAsync();

        await EnsureStaffMembersAsync(context);
        await EnsureStudentsFromFileAsync(context);
        await EnsureStaffManagerDataAsync(context);
        await EnsureSchoolOperationsDataAsync(context);
    }

    private static async Task EnsureStaffMembersAsync(ApplicationDbContext context)
    {
        var hasChanged = false;

        // Ensure 4 specialized technical engineers for the 4 official tracks: programming, networking, communication, ai
        var eng1 = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "engineer");
        if (eng1 == null)
        {
            context.Users.Add(new User
            {
                FullName = "م. هاني كمال (مهندس برمجيات)",
                Username = "engineer",
                PasswordHash = "engineer123",
                Role = UserRole.TechnicalEngineer,
                Specialty = "programming",
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }
        else if (string.IsNullOrEmpty(eng1.Specialty))
        {
            eng1.Specialty = "programming";
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "engineer_net"))
        {
            context.Users.Add(new User
            {
                FullName = "م. رامي يوسف (مهندس شبكات)",
                Username = "engineer_net",
                PasswordHash = "engineer123",
                Role = UserRole.TechnicalEngineer,
                Specialty = "networking",
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "engineer_comm"))
        {
            context.Users.Add(new User
            {
                FullName = "م. دينا فاروق (مهندسة اتصالات)",
                Username = "engineer_comm",
                PasswordHash = "engineer123",
                Role = UserRole.TechnicalEngineer,
                Specialty = "communication",
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "engineer_ai"))
        {
            context.Users.Add(new User
            {
                FullName = "م. أحمد حسام (مهندس ذكاء اصطناعي)",
                Username = "engineer_ai",
                PasswordHash = "engineer123",
                Role = UserRole.TechnicalEngineer,
                Specialty = "ai",
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "affairs"))
        {
            context.Users.Add(new User
            {
                FullName = "أ. إيمان فتحي (شؤون طلاب)",
                Username = "affairs",
                PasswordHash = "affairs123",
                Role = UserRole.StudentAffairs,
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "assessment"))
        {
            context.Users.Add(new User
            {
                FullName = "د. سحر عبد الرحمن (مسؤول تقييم وامتحانات)",
                Username = "assessment",
                PasswordHash = "assessment123",
                Role = UserRole.AssessmentCoordinator,
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");
        if (adminUser != null)
        {
            if (!adminUser.IsSuperAdmin || string.IsNullOrEmpty(adminUser.Email))
            {
                adminUser.IsSuperAdmin = true;
                adminUser.Email = "admin@wesharkia.local";
                adminUser.PhoneNumber = "01000000000";
                hasChanged = true;
            }
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "manager"))
        {
            context.Users.Add(new User
            {
                FullName = "د. خالد السعدني (مدير المدرسة)",
                Username = "manager",
                PasswordHash = "manager123",
                Role = UserRole.SchoolManager,
                Status = UserStatus.Approved,
                Email = "manager@wesharkia.local",
                PhoneNumber = "01011112222"
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "coach"))
        {
            context.Users.Add(new User
            {
                FullName = "كابتن حسام حسن (التربية الرياضية والبدنية)",
                Username = "coach",
                PasswordHash = "coach123",
                Role = UserRole.SportsCoach,
                Status = UserStatus.Approved,
                Email = "coach@wesharkia.local",
                PhoneNumber = "01055566677",
                LastLoginAtUtc = DateTime.UtcNow.AddMinutes(-25)
            });
            hasChanged = true;
        }

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == "hr"))
        {
            context.Users.Add(new User
            {
                FullName = "أ. مها الشناوي (الموارد البشرية وشؤون العاملين)",
                Username = "hr",
                PasswordHash = "hr123",
                Role = UserRole.HR,
                Status = UserStatus.Approved,
                Email = "hr@wesharkia.local",
                PhoneNumber = "01033344455",
                LastLoginAtUtc = DateTime.UtcNow.AddMinutes(-10)
            });
            hasChanged = true;
        }

        // Master Protected Super Admin (Hidden from Dashboard)
        var rootAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "zakaria" || u.Email == "zakaria@sharqia.gfd.com");
        if (rootAdmin == null)
        {
            context.Users.Add(new User
            {
                FullName = "مدير المنصة",
                Username = "zakaria",
                Email = "zakaria@sharqia.gfd.com",
                PasswordHash = "Zakaria@2004",
                PhoneNumber = "01000000000",
                Role = UserRole.Admin,
                IsSuperAdmin = true,
                Status = UserStatus.Approved
            });
            hasChanged = true;
        }
        else
        {
            if (rootAdmin.Email != "zakaria@sharqia.gfd.com" || rootAdmin.PasswordHash != "Zakaria@2004" || !rootAdmin.IsSuperAdmin)
            {
                rootAdmin.Email = "zakaria@sharqia.gfd.com";
                rootAdmin.PasswordHash = "Zakaria@2004";
                rootAdmin.IsSuperAdmin = true;
                rootAdmin.Status = UserStatus.Approved;
                rootAdmin.Role = UserRole.Admin;
                hasChanged = true;
            }
        }

        if (hasChanged)
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task EnsureStudentsFromFileAsync(ApplicationDbContext context)
    {
        try
        {
            // Find students.json in candidate locations (prioritize source files)
            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "students.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "students.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "students.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "server", "WeSharkia.API", "students.json"),
                Path.Combine(AppContext.BaseDirectory, "students.json")
            };

            string jsonPath = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(jsonPath))
            {
                Console.WriteLine("[DbInitializer] students.json not found in candidate paths.");
                return;
            }

            Console.WriteLine($"[DbInitializer] Loading students from: {jsonPath}");
            var jsonText = await File.ReadAllTextAsync(jsonPath);
            var studentsData = JsonSerializer.Deserialize<List<StudentSeedItem>>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (studentsData == null || studentsData.Count == 0) return;

            bool hasChanged = false;
            foreach (var item in studentsData)
            {
                var rawCode = !string.IsNullOrWhiteSpace(item.StudentCode) ? item.StudentCode : item.Code;
                var rawUsername = !string.IsNullOrWhiteSpace(item.Username) ? item.Username : rawCode;
                if (string.IsNullOrWhiteSpace(rawUsername)) continue;

                var cleanUsername = rawUsername.Trim().ToLower();
                var cleanCode = !string.IsNullOrWhiteSpace(rawCode) ? rawCode.Trim() : cleanUsername;
                var cleanFullName = item.FullName?.Trim();
                var rawNatId = SecurityService.NormalizeDigits(item.NationalId);
                var hasNatId = rawNatId.Length == 14;

                string passwordHash;
                if (hasNatId)
                {
                    passwordHash = SecurityService.HashPassword(rawNatId);
                }
                else if (!string.IsNullOrWhiteSpace(item.Password))
                {
                    passwordHash = item.Password.StartsWith("PBKDF2$") ? item.Password : SecurityService.HashPassword(item.Password);
                }
                else
                {
                    passwordHash = SecurityService.HashPassword("student123");
                }

                string? encryptedNatId = hasNatId ? SecurityService.EncryptNationalId(rawNatId) : null;
                string? maskedNatId = hasNatId ? SecurityService.MaskNationalId(rawNatId) : null;

                // Match either by clean username or student code or exact full name for students
                var existing = await context.Users.FirstOrDefaultAsync(u => 
                    u.Role == UserRole.Student && 
                    ((cleanFullName != null && u.FullName == cleanFullName) ||
                     (u.StudentCode != null && u.StudentCode == cleanCode) ||
                     u.Username.ToLower() == cleanUsername));

                var rawParentPhone = !string.IsNullOrWhiteSpace(item.ParentPhone) ? item.ParentPhone : item.ParentPhoneNumber;

                if (existing == null)
                {
                    var isUsernameTaken = await context.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername);
                    var effectiveUsername = isUsernameTaken ? $"{cleanUsername}_{item.Classroom?.ToLower() ?? "std"}" : cleanUsername;

                    context.Users.Add(new User
                    {
                        FullName = cleanFullName ?? "طالب",
                        Username = effectiveUsername,
                        StudentCode = cleanCode,
                        PasswordHash = passwordHash,
                        NationalIdEncrypted = encryptedNatId,
                        NationalIdMasked = maskedNatId,
                        Role = UserRole.Student,
                        Status = UserStatus.Approved,
                        ParentConsent = item.ParentConsent ?? true,
                        Grade = item.Grade ?? 2,
                        Classroom = item.Classroom?.Trim().ToUpperInvariant() ?? "B1",
                        PhoneNumber = item.PhoneNumber?.Trim(),
                        ParentPhone = rawParentPhone?.Trim()
                    });
                    hasChanged = true;
                }
                else
                {
                    if (existing.StudentCode != cleanCode && !string.IsNullOrWhiteSpace(cleanCode))
                    {
                        existing.StudentCode = cleanCode;
                        hasChanged = true;
                    }
                    if (hasNatId && (existing.NationalIdMasked != maskedNatId || !SecurityService.VerifyPassword(rawNatId, existing.PasswordHash)))
                    {
                        existing.NationalIdEncrypted = encryptedNatId;
                        existing.NationalIdMasked = maskedNatId;
                        existing.PasswordHash = passwordHash;
                        hasChanged = true;
                    }
                    var isUsernameTaken = await context.Users.AnyAsync(u => u.Id != existing.Id && u.Username.ToLower() == cleanUsername);
                    var effectiveUsername = isUsernameTaken ? $"{cleanUsername}_{item.Classroom?.ToLower() ?? "std"}" : cleanUsername;

                    if (existing.Username.ToLower() != effectiveUsername)
                    {
                        existing.Username = effectiveUsername;
                        hasChanged = true;
                    }
                    if (!string.IsNullOrWhiteSpace(cleanFullName) && existing.FullName != cleanFullName)
                    {
                        existing.FullName = cleanFullName;
                        hasChanged = true;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Classroom) && existing.Classroom != item.Classroom.Trim().ToUpperInvariant())
                    {
                        existing.Classroom = item.Classroom.Trim().ToUpperInvariant();
                        hasChanged = true;
                    }
                    if (item.Grade.HasValue && existing.Grade != item.Grade.Value)
                    {
                        existing.Grade = item.Grade.Value;
                        hasChanged = true;
                    }
                    if (!string.IsNullOrWhiteSpace(item.PhoneNumber) && existing.PhoneNumber != item.PhoneNumber.Trim())
                    {
                        existing.PhoneNumber = item.PhoneNumber.Trim();
                        hasChanged = true;
                    }
                    if (!string.IsNullOrWhiteSpace(rawParentPhone) && existing.ParentPhone != rawParentPhone.Trim())
                    {
                        existing.ParentPhone = rawParentPhone.Trim();
                        hasChanged = true;
                    }
                }
            }

            if (hasChanged)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[DbInitializer] Successfully synced {studentsData.Count} students from {jsonPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Failed to seed students from file: {ex.Message}");
        }
    }

    public static async Task EnsureSchemaExtensionsAsync(ApplicationDbContext context)
    {
        try
        {
            var alterStatements = new[]
            {
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Assignments') AND name = 'TargetClassroom') ALTER TABLE Assignments ADD TargetClassroom NVARCHAR(100) NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Assignments') AND name = 'AssignmentType') ALTER TABLE Assignments ADD AssignmentType NVARCHAR(50) NOT NULL DEFAULT 'assignment';",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'LastLoginAtUtc') ALTER TABLE Users ADD LastLoginAtUtc DATETIME2 NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'StudentCode') ALTER TABLE Users ADD StudentCode NVARCHAR(100) NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'NationalIdEncrypted') ALTER TABLE Users ADD NationalIdEncrypted NVARCHAR(MAX) NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'NationalIdMasked') ALTER TABLE Users ADD NationalIdMasked NVARCHAR(50) NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'FailedLoginAttempts') ALTER TABLE Users ADD FailedLoginAttempts INT NOT NULL DEFAULT 0;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'LockoutEndUtc') ALTER TABLE Users ADD LockoutEndUtc DATETIME2 NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Specialty') ALTER TABLE Users ADD Specialty NVARCHAR(100) NULL;",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ParentPhone') ALTER TABLE Users ADD ParentPhone NVARCHAR(50) NULL;"
            };

            foreach (var sql in alterStatements)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbInitializer] Alter column warning: {ex.Message}");
                }
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_StudentCode' AND object_id = OBJECT_ID('Users')) CREATE NONCLUSTERED INDEX IX_Users_StudentCode ON Users (StudentCode) WHERE StudentCode IS NOT NULL;");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] Index creation notice: {ex.Message}");
            }


            // 2. Ensure ClassroomAnnouncements table exists
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClassroomAnnouncements' AND xtype='U')
                CREATE TABLE ClassroomAnnouncements (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    TeacherId UNIQUEIDENTIFIER NOT NULL,
                    TeacherName NVARCHAR(MAX) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    Importance NVARCHAR(50) NOT NULL DEFAULT 'normal'
                );
            ");

            // 3. Ensure Staff Manager tables exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffNotifications' AND xtype='U')
                CREATE TABLE StaffNotifications (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    SenderId UNIQUEIDENTIFIER NOT NULL,
                    SenderName NVARCHAR(MAX) NOT NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    TargetRole NVARCHAR(100) NOT NULL,
                    TargetUserId UNIQUEIDENTIFIER NULL,
                    TargetUserName NVARCHAR(MAX) NULL,
                    Priority NVARCHAR(50) NOT NULL DEFAULT 'normal',
                    RequiresAcknowledgment BIT NOT NULL DEFAULT 0
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffNotificationReceipts' AND xtype='U')
                CREATE TABLE StaffNotificationReceipts (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    NotificationId UNIQUEIDENTIFIER NOT NULL,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    UserName NVARCHAR(MAX) NOT NULL,
                    UserRole NVARCHAR(100) NOT NULL,
                    ReadAtUtc DATETIME2 NOT NULL,
                    AcknowledgedAtUtc DATETIME2 NULL,
                    CONSTRAINT FK_Receipts_Notifications FOREIGN KEY (NotificationId) REFERENCES StaffNotifications(Id) ON DELETE CASCADE
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffAuditLogs' AND xtype='U')
                CREATE TABLE StaffAuditLogs (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    UserName NVARCHAR(MAX) NOT NULL,
                    UserRole NVARCHAR(100) NOT NULL,
                    ActionType NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(MAX) NOT NULL,
                    DetailsJson NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffEvaluations' AND xtype='U')
                CREATE TABLE StaffEvaluations (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    StaffUserId UNIQUEIDENTIFIER NOT NULL,
                    StaffUserName NVARCHAR(MAX) NOT NULL,
                    StaffRole NVARCHAR(100) NOT NULL,
                    EvaluatorId UNIQUEIDENTIFIER NOT NULL,
                    EvaluatorName NVARCHAR(MAX) NOT NULL,
                    Period NVARCHAR(100) NOT NULL,
                    PunctualityRating INT NOT NULL DEFAULT 5,
                    PedagogyRating INT NOT NULL DEFAULT 5,
                    SystemEngagementRating INT NOT NULL DEFAULT 5,
                    CooperationRating INT NOT NULL DEFAULT 5,
                    OverallScore FLOAT NOT NULL DEFAULT 5.0,
                    Notes NVARCHAR(MAX) NULL,
                    RecognitionBadge NVARCHAR(100) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffAttendances' AND xtype='U')
                CREATE TABLE StaffAttendances (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    UserName NVARCHAR(MAX) NOT NULL,
                    UserRole NVARCHAR(100) NOT NULL,
                    Date DATETIME2 NOT NULL,
                    CheckInTimeUtc DATETIME2 NOT NULL,
                    CheckOutTimeUtc DATETIME2 NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Present',
                    Notes NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentAttendances' AND xtype='U')
                CREATE TABLE StudentAttendances (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    StudentId UNIQUEIDENTIFIER NOT NULL,
                    StudentName NVARCHAR(MAX) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NOT NULL DEFAULT 1,
                    Date DATETIME2 NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Present',
                    RecordedByTeacherId UNIQUEIDENTIFIER NOT NULL,
                    RecordedByTeacherName NVARCHAR(MAX) NOT NULL,
                    Notes NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ClassScheduleItems' AND xtype='U')
                CREATE TABLE ClassScheduleItems (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    Grade INT NOT NULL DEFAULT 1,
                    Classroom NVARCHAR(50) NOT NULL,
                    DayOfWeek INT NOT NULL,
                    PeriodNumber INT NOT NULL,
                    StartTime NVARCHAR(20) NOT NULL,
                    EndTime NVARCHAR(20) NOT NULL,
                    SubjectName NVARCHAR(MAX) NOT NULL,
                    TeacherName NVARCHAR(MAX) NOT NULL,
                    RoomOrLab NVARCHAR(MAX) NOT NULL,
                    IsLabSession BIT NOT NULL DEFAULT 0
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ParentRequests' AND xtype='U')
                CREATE TABLE ParentRequests (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    ParentId UNIQUEIDENTIFIER NOT NULL,
                    ParentName NVARCHAR(MAX) NOT NULL,
                    ParentPhone NVARCHAR(50) NOT NULL,
                    StudentId UNIQUEIDENTIFIER NOT NULL,
                    StudentName NVARCHAR(MAX) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    RequestType NVARCHAR(100) NOT NULL,
                    Details NVARCHAR(MAX) NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'pending',
                    AdminResponse NVARCHAR(MAX) NULL,
                    ReviewedBy NVARCHAR(MAX) NULL,
                    ReviewedAtUtc DATETIME2 NULL
                );

                IF EXISTS (SELECT * FROM sysobjects WHERE name='StaffLeaveRequests' AND xtype='U') AND NOT EXISTS (SELECT * FROM syscolumns WHERE id = OBJECT_ID('StaffLeaveRequests') AND name = 'UserId')
                BEGIN
                    DROP TABLE StaffLeaveRequests;
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffLeaveRequests' AND xtype='U')
                CREATE TABLE StaffLeaveRequests (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    UserName NVARCHAR(MAX) NOT NULL,
                    UserRole NVARCHAR(100) NOT NULL,
                    LeaveType NVARCHAR(100) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NOT NULL,
                    DaysCount INT NOT NULL,
                    Reason NVARCHAR(MAX) NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'pending',
                    ApprovedBy NVARCHAR(MAX) NULL,
                    Notes NVARCHAR(MAX) NULL
                );

                IF EXISTS (SELECT * FROM sysobjects WHERE name='StaffInvestigations' AND xtype='U') AND NOT EXISTS (SELECT * FROM syscolumns WHERE id = OBJECT_ID('StaffInvestigations') AND name = 'InvestigationNumber')
                BEGIN
                    DROP TABLE StaffInvestigations;
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffInvestigations' AND xtype='U')
                CREATE TABLE StaffInvestigations (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    InvestigationNumber NVARCHAR(100) NOT NULL,
                    Subject NVARCHAR(MAX) NOT NULL,
                    StaffUserId UNIQUEIDENTIFIER NOT NULL,
                    StaffUserName NVARCHAR(MAX) NOT NULL,
                    StaffRole NVARCHAR(100) NOT NULL,
                    InvestigatorId UNIQUEIDENTIFIER NOT NULL,
                    InvestigatorName NVARCHAR(MAX) NOT NULL,
                    IncidentDate DATETIME2 NOT NULL,
                    SummaryOfAllegations NVARCHAR(MAX) NOT NULL,
                    StaffStatement NVARCHAR(MAX) NULL,
                    Findings NVARCHAR(MAX) NULL,
                    Decision NVARCHAR(100) NOT NULL DEFAULT 'قيد الفحص',
                    Status NVARCHAR(50) NOT NULL DEFAULT 'open',
                    ManagerEndorsement NVARCHAR(MAX) NULL,
                    ClosedAtUtc DATETIME2 NULL
                );

                IF EXISTS (SELECT * FROM sysobjects WHERE name='StaffMemos' AND xtype='U') AND NOT EXISTS (SELECT * FROM syscolumns WHERE id = OBJECT_ID('StaffMemos') AND name = 'Title')
                BEGIN
                    DROP TABLE StaffMemos;
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffMemos' AND xtype='U')
                CREATE TABLE StaffMemos (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    MemoNumber NVARCHAR(100) NOT NULL,
                    MemoType NVARCHAR(100) NOT NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    TargetStaffId UNIQUEIDENTIFIER NULL,
                    TargetStaffName NVARCHAR(MAX) NULL,
                    TargetStaffRole NVARCHAR(100) NULL,
                    CreatedById UNIQUEIDENTIFIER NOT NULL,
                    CreatedByName NVARCHAR(MAX) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    Recommendations NVARCHAR(MAX) NULL,
                    Priority NVARCHAR(50) NOT NULL DEFAULT 'عادية',
                    Status NVARCHAR(50) NOT NULL DEFAULT 'مسودة',
                    ManagerRemarks NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SchoolExams' AND xtype='U')
                CREATE TABLE SchoolExams (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    ExamType NVARCHAR(100) NOT NULL,
                    Subject NVARCHAR(MAX) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NOT NULL,
                    MaxScore FLOAT NOT NULL DEFAULT 100,
                    PassingScore FLOAT NOT NULL DEFAULT 50,
                    ExamDate DATETIME2 NOT NULL,
                    CreatedById UNIQUEIDENTIFIER NOT NULL,
                    CreatedByName NVARCHAR(MAX) NOT NULL,
                    Notes NVARCHAR(MAX) NULL,
                    IsPublished BIT NOT NULL DEFAULT 1
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ExamStudentScores' AND xtype='U')
                CREATE TABLE ExamStudentScores (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    ExamId UNIQUEIDENTIFIER NOT NULL,
                    StudentId UNIQUEIDENTIFIER NOT NULL,
                    StudentName NVARCHAR(MAX) NOT NULL,
                    StudentCode NVARCHAR(100) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NOT NULL,
                    Score FLOAT NOT NULL DEFAULT 0,
                    MaxScore FLOAT NOT NULL DEFAULT 100,
                    IsRemedial BIT NOT NULL DEFAULT 0,
                    RemedialStatus NVARCHAR(100) NOT NULL DEFAULT 'مجتاز',
                    PreviousScore FLOAT NULL,
                    TeacherNotes NVARCHAR(MAX) NULL,
                    RecordedAtUtc DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SportsAssessments' AND xtype='U')
                CREATE TABLE SportsAssessments (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    StudentId UNIQUEIDENTIFIER NOT NULL,
                    StudentName NVARCHAR(MAX) NOT NULL,
                    StudentCode NVARCHAR(100) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NOT NULL,
                    MorningAssemblyStatus NVARCHAR(100) NOT NULL,
                    FitnessScore FLOAT NOT NULL DEFAULT 90,
                    SpeedScore INT NOT NULL DEFAULT 9,
                    EnduranceScore INT NOT NULL DEFAULT 9,
                    FlexibilityScore INT NOT NULL DEFAULT 8,
                    PreferredSport NVARCHAR(100) NOT NULL,
                    HonorBadge NVARCHAR(MAX) NULL,
                    CoachNotes NVARCHAR(MAX) NULL,
                    RecordedByCoachId UNIQUEIDENTIFIER NOT NULL,
                    RecordedByCoachName NVARCHAR(MAX) NOT NULL,
                    RecordedAtUtc DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SportsTournaments' AND xtype='U')
                CREATE TABLE SportsTournaments (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    TournamentType NVARCHAR(100) NOT NULL,
                    TargetStages NVARCHAR(MAX) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    Status NVARCHAR(100) NOT NULL,
                    WinningClassroom NVARCHAR(MAX) NULL,
                    BestPlayerOrScorer NVARCHAR(MAX) NULL,
                    RulesAndNotes NVARCHAR(MAX) NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentDisciplineNotices' AND xtype='U')
                CREATE TABLE StudentDisciplineNotices (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL,
                    StudentId UNIQUEIDENTIFIER NOT NULL,
                    StudentName NVARCHAR(MAX) NOT NULL,
                    Classroom NVARCHAR(50) NOT NULL,
                    Grade INT NOT NULL,
                    ParentId UNIQUEIDENTIFIER NULL,
                    ParentName NVARCHAR(MAX) NOT NULL,
                    ParentPhone NVARCHAR(50) NOT NULL,
                    Category NVARCHAR(100) NOT NULL,
                    Title NVARCHAR(MAX) NOT NULL,
                    Details NVARCHAR(MAX) NOT NULL,
                    ActionTaken NVARCHAR(MAX) NOT NULL,
                    Severity NVARCHAR(50) NOT NULL DEFAULT 'warning',
                    Status NVARCHAR(50) NOT NULL DEFAULT 'pending',
                    IssuedBy NVARCHAR(MAX) NOT NULL,
                    IncidentDate DATETIME2 NOT NULL,
                    IsAcknowledgedByParent BIT NOT NULL DEFAULT 0,
                    AcknowledgedAtUtc DATETIME2 NULL,
                    ParentNotes NVARCHAR(MAX) NULL
                );
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Schema extensions check: {ex.Message}");
        }
    }

    public static async Task EnsureStaffManagerDataAsync(ApplicationDbContext context)
    {
        try
        {
            var manager = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SchoolManager);
            var teachers = await context.Users.Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.TechnicalEngineer).ToListAsync();
            var affairs = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.StudentAffairs);
            var social = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SocialWorker);

            // 1. Seed Initial Manager Notifications if none exist
            if (!await context.StaffNotifications.AnyAsync())
            {
                var managerId = manager?.Id ?? Guid.NewGuid();
                var managerName = manager?.FullName ?? "د. خالد السعدني (مدير المدرسة)";

                var circular1 = new StaffNotification
                {
                    SenderId = managerId,
                    SenderName = managerName,
                    Title = "توجيه إداري ملزم: استكمال رصد درجات الاختبارات وتوثيق الأنشطة",
                    Content = "الزملاء الأعزاء في هيئة التدريس والإشراف، نرجو الانتهاء من رصد وتصحيح التكليفات والواجبات المتبقية لجميع الصفوف قبل نهاية يوم الخميس القادم لاعتماد كشوف التميز.",
                    TargetRole = "staff",
                    Priority = "administrative",
                    RequiresAcknowledgment = true
                };

                var circular2 = new StaffNotification
                {
                    SenderId = managerId,
                    SenderName = managerName,
                    Title = "شكر وتقدير لمسؤولي المعامل وشؤون الطلاب",
                    Content = "تتقدم إدارة المدرسة بخالص الشكر والتقدير لجهودكم المتميزة في استقرار العملية التعليمية وسلامة الأجهزة وسرعة حل مشكلات الطلاب.",
                    TargetRole = "staff",
                    Priority = "normal",
                    RequiresAcknowledgment = false
                };

                var circularParent = new StaffNotification
                {
                    SenderId = managerId,
                    SenderName = managerName,
                    Title = "إشعار لأولياء الأمور: موعد انعقاد الملتقى الدوري ومتابعة أداء الطلاب",
                    Content = "السادة أولياء الأمور الكرام، يسعد إدارة مدرسة WE للتكنولوجيا التطبيقية دعوتكم لحضور الملتقى الفصلي لمتابعة مستوى أبنائكم والتواصل مع مسؤولي التخصصات، يوم السبت القادم بمقر المدرسة.",
                    TargetRole = "parent",
                    Priority = "normal",
                    RequiresAcknowledgment = false
                };

                context.StaffNotifications.AddRange(circular1, circular2, circularParent);
                await context.SaveChangesAsync();

                // Add sample receipts showing who read circular1
                if (teachers.Any())
                {
                    foreach (var t in teachers.Take(2))
                    {
                        context.StaffNotificationReceipts.Add(new StaffNotificationReceipt
                        {
                            NotificationId = circular1.Id,
                            UserId = t.Id,
                            UserName = t.FullName,
                            UserRole = t.Role.ToString().ToLower(),
                            ReadAtUtc = DateTime.UtcNow.AddHours(-3),
                            AcknowledgedAtUtc = DateTime.UtcNow.AddHours(-2)
                        });
                    }
                }
                if (affairs != null)
                {
                    context.StaffNotificationReceipts.Add(new StaffNotificationReceipt
                    {
                        NotificationId = circular1.Id,
                        UserId = affairs.Id,
                        UserName = affairs.FullName,
                        UserRole = "studentaffairs",
                        ReadAtUtc = DateTime.UtcNow.AddHours(-1),
                        AcknowledgedAtUtc = DateTime.UtcNow.AddMinutes(-30)
                    });
                }
                await context.SaveChangesAsync();
            }

            // 2. Seed Initial Staff Audit Logs if none exist
            if (!await context.StaffAuditLogs.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var logs = new List<StaffAuditLog>();

                if (teachers.Any())
                {
                    var t1 = teachers.First();
                    logs.Add(new StaffAuditLog
                    {
                        UserId = t1.Id,
                        UserName = t1.FullName,
                        UserRole = t1.Role.ToString().ToLower(),
                        ActionType = "Login",
                        Description = "تسجيل الدخول إلى النظام من الجهاز المكتبي",
                        CreatedAtUtc = now.AddHours(-4)
                    });
                    logs.Add(new StaffAuditLog
                    {
                        UserId = t1.Id,
                        UserName = t1.FullName,
                        UserRole = t1.Role.ToString().ToLower(),
                        ActionType = "CreateAssignment",
                        Description = "نشر نشاط عملي جديد لمادة أساسيات الشبكات لفصل 1-A",
                        CreatedAtUtc = now.AddHours(-3)
                    });
                    logs.Add(new StaffAuditLog
                    {
                        UserId = t1.Id,
                        UserName = t1.FullName,
                        UserRole = t1.Role.ToString().ToLower(),
                        ActionType = "GradeAssignment",
                        Description = "تصحيح واعتماد درجات 18 طالباً في واجب الأسبوع الثالث",
                        CreatedAtUtc = now.AddHours(-2)
                    });
                }

                if (affairs != null)
                {
                    logs.Add(new StaffAuditLog
                    {
                        UserId = affairs.Id,
                        UserName = affairs.FullName,
                        UserRole = "studentaffairs",
                        ActionType = "Login",
                        Description = "تسجيل دخول إلى قسم شؤون الطلاب وقيد الدفعة",
                        CreatedAtUtc = now.AddHours(-5)
                    });
                    logs.Add(new StaffAuditLog
                    {
                        UserId = affairs.Id,
                        UserName = affairs.FullName,
                        UserRole = "studentaffairs",
                        ActionType = "BulkImport",
                        Description = "مراجعة وتحديث سجلات الغياب والأعذار الميدانية لطلاب الصف الأول",
                        CreatedAtUtc = now.AddHours(-3).AddMinutes(20)
                    });
                }

                if (social != null)
                {
                    logs.Add(new StaffAuditLog
                    {
                        UserId = social.Id,
                        UserName = social.FullName,
                        UserRole = "socialworker",
                        ActionType = "UpdateSocialCase",
                        Description = "إضافة جلسة إرشاد ومتابعة سلوكية جديدة لملف دعم طلابي",
                        CreatedAtUtc = now.AddHours(-6)
                    });
                }

                var coachUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SportsCoach);
                if (coachUser != null)
                {
                    logs.Add(new StaffAuditLog
                    {
                        UserId = coachUser.Id,
                        UserName = coachUser.FullName,
                        UserRole = "sportscoach",
                        ActionType = "SportsActivity",
                        Description = "رصد تقييمات اللياقة البدنية وتنظيم دوري الأنشطة الرياضية بين الفصول",
                        CreatedAtUtc = now.AddHours(-1).AddMinutes(15)
                    });
                }

                context.StaffAuditLogs.AddRange(logs);
                await context.SaveChangesAsync();
            }

            // 3. Seed Initial Evaluations if none exist
            if (!await context.StaffEvaluations.AnyAsync() && manager != null)
            {
                var evaluations = new List<StaffEvaluation>();
                if (teachers.Any())
                {
                    var t1 = teachers.First();
                    evaluations.Add(new StaffEvaluation
                    {
                        StaffUserId = t1.Id,
                        StaffUserName = t1.FullName,
                        StaffRole = t1.Role.ToString().ToLower(),
                        EvaluatorId = manager.Id,
                        EvaluatorName = manager.FullName,
                        Period = "الفصل الدراسي الحالي",
                        PunctualityRating = 5,
                        PedagogyRating = 5,
                        SystemEngagementRating = 5,
                        CooperationRating = 5,
                        OverallScore = 5.0,
                        Notes = "أداء تدريسي نموذجي وسرعة فائقة في تقييم وتسليم درجات الطلاب.",
                        RecognitionBadge = "معلم متميز"
                    });
                }

                if (affairs != null)
                {
                    evaluations.Add(new StaffEvaluation
                    {
                        StaffUserId = affairs.Id,
                        StaffUserName = affairs.FullName,
                        StaffRole = "studentaffairs",
                        EvaluatorId = manager.Id,
                        EvaluatorName = manager.FullName,
                        Period = "الفصل الدراسي الحالي",
                        PunctualityRating = 5,
                        PedagogyRating = 4,
                        SystemEngagementRating = 4,
                        CooperationRating = 5,
                        OverallScore = 4.5,
                        Notes = "انضباط عالي في مراجعة استمارات الطلاب وحفظ السجلات.",
                        RecognitionBadge = "انضباط إداري"
                    });
                }

                context.StaffEvaluations.AddRange(evaluations);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Failed to seed staff manager data: {ex.Message}");
        }
    }

    public static async Task EnsureSchoolOperationsDataAsync(ApplicationDbContext context)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            // 1. Seed Weekly Timetable if empty
            if (!await context.ClassScheduleItems.AnyAsync())
            {
                var schedule = new List<ClassScheduleItem>();
                var days = new[] { 0, 1, 2, 3, 4 }; // الأحد إلى الخميس
                var classes = new[] { "A1", "A2", "B1", "C1" };

                var subjectsTemplate = new[]
                {
                    new { Name = "أساسيات تكنولوجيا المعلومات والاتصالات", Teacher = "أستاذ أحمد الشافعي", Room = "معمل تكنولوجيا 1", IsLab = true, Start = "08:00", End = "08:45" },
                    new { Name = "شبكات الحاسب والألياف الضوئية", Teacher = "أ. محمود عبد الرحمن", Room = "معمل الشبكات التخصصي", IsLab = true, Start = "08:50", End = "09:35" },
                    new { Name = "التدريب العملي والورش الميدانية", Teacher = "م. هاني كمال (مهندس متخصص)", Room = "ورشة الإلكترونيات والاتصالات", IsLab = true, Start = "09:40", End = "10:25" },
                    new { Name = "اللغة الإنجليزية التخصصية (Technical English)", Teacher = "د. سحر عبد الرحمن", Room = "قاعة دراسية ذكية", IsLab = false, Start = "10:45", End = "11:30" },
                    new { Name = "الرياضيات التطبيقية والفيزياء", Teacher = "أ. محمود عبد الرحمن", Room = "قاعة دراسية ذكية", IsLab = false, Start = "11:35", End = "12:20" },
                    new { Name = "أساسيات البرمجة وقواعد البيانات", Teacher = "م. تامر (مسؤول IT)", Room = "معمل البرمجيات", IsLab = true, Start = "12:25", End = "01:10" }
                };

                foreach (var cls in classes)
                {
                    int grade = cls.StartsWith("A") ? 1 : cls.StartsWith("B") ? 2 : 3;
                    foreach (var day in days)
                    {
                        for (int p = 1; p <= 6; p++)
                        {
                            var template = subjectsTemplate[(p - 1 + day) % subjectsTemplate.Length];
                            schedule.Add(new ClassScheduleItem
                            {
                                Grade = grade,
                                Classroom = cls,
                                DayOfWeek = day,
                                PeriodNumber = p,
                                StartTime = template.Start,
                                EndTime = template.End,
                                SubjectName = template.Name,
                                TeacherName = template.Teacher,
                                RoomOrLab = template.Room,
                                IsLabSession = template.IsLab
                            });
                        }
                    }
                }

                context.ClassScheduleItems.AddRange(schedule);
                await context.SaveChangesAsync();
            }

            // 2. Seed Staff Attendance if empty
            if (!await context.StaffAttendances.AnyAsync(a => a.Date == today))
            {
                var teachers = await context.Users.Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.TechnicalEngineer || u.Role == UserRole.StudentAffairs).ToListAsync();
                var list = new List<StaffAttendance>();

                foreach (var t in teachers.Take(5))
                {
                    list.Add(new StaffAttendance
                    {
                        UserId = t.Id,
                        UserName = t.FullName,
                        UserRole = t.Role.ToString().ToLower(),
                        Date = today,
                        CheckInTimeUtc = today.AddHours(7).AddMinutes(45),
                        Status = "Present",
                        Notes = "حضور مبكر - طابور الصباح"
                    });
                }

                context.StaffAttendances.AddRange(list);
                await context.SaveChangesAsync();
            }

            // 3. Seed Student Attendance for Classroom A1
            if (!await context.StudentAttendances.AnyAsync(a => a.Date == today))
            {
                var students = await context.Users.Where(u => u.Role == UserRole.Student && u.Classroom == "A1").Take(15).ToListAsync();
                var teacher = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Teacher);

                var teacherId = teacher?.Id ?? Guid.NewGuid();
                var teacherName = teacher?.FullName ?? "أستاذ أحمد الشافعي";

                var attendances = new List<StudentAttendance>();
                int index = 0;
                foreach (var s in students)
                {
                    string status = index == 0 ? "Absent" : index == 1 ? "Late" : index == 2 ? "Excused" : "Present";
                    attendances.Add(new StudentAttendance
                    {
                        StudentId = s.Id,
                        StudentName = s.FullName,
                        Classroom = "A1",
                        Grade = s.Grade ?? 1,
                        Date = today,
                        Status = status,
                        RecordedByTeacherId = teacherId,
                        RecordedByTeacherName = teacherName,
                        Notes = status == "Absent" ? "غياب بدون إذن مسبق" : status == "Excused" ? "عذر مرضي معتمد" : null
                    });
                    index++;
                }

                context.StudentAttendances.AddRange(attendances);
                await context.SaveChangesAsync();
            }

            // 4. Seed Parent Requests if empty
            if (!await context.ParentRequests.AnyAsync())
            {
                var parent = await context.Users.Include(u => u.Children).FirstOrDefaultAsync(u => u.Role == UserRole.Parent && u.Children.Any());
                var student = parent?.Children.FirstOrDefault() ?? await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Student);

                if (student != null)
                {
                    var parentId = parent?.Id ?? Guid.NewGuid();
                    var parentName = parent?.FullName ?? "ولي أمر الطالب " + student.FullName;
                    var parentPhone = parent?.PhoneNumber ?? "01099887766";

                    context.ParentRequests.AddRange(
                        new ParentRequest
                        {
                            ParentId = parentId,
                            ParentName = parentName,
                            ParentPhone = parentPhone,
                            StudentId = student.Id,
                            StudentName = student.FullName,
                            Classroom = student.Classroom ?? "A1",
                            RequestType = "عذر غياب طبي",
                            Details = "نحيط علم إدارة المدرسة بغياب الطالب يوم أمس نتيجة وعكة صحية مفاجئة، ومرفق التقرير الطبي لرفع الغياب.",
                            Status = "approved",
                            AdminResponse = "تم قبول العذر الطبي واعتماد غياب الطالب بعذر رسمي دون التأثير على درجات المواظبة.",
                            ReviewedBy = "أ. إيمان فتحي (شؤون طلاب)",
                            ReviewedAtUtc = DateTime.UtcNow.AddHours(-2)
                        },
                        new ParentRequest
                        {
                            ParentId = parentId,
                            ParentName = parentName,
                            ParentPhone = parentPhone,
                            StudentId = student.Id,
                            StudentName = student.FullName,
                            Classroom = student.Classroom ?? "A1",
                            RequestType = "استئذان مبكر",
                            Details = "أرجو التكرم بالموافقة على خروج الطالب الساعة 01:00 ظهراً يوم الأربعاء القادم لظرف أسري طارئ.",
                            Status = "pending"
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }

            // 5. Seed HR Data (Leaves, Investigations, Memos)
            if (!await context.StaffLeaveRequests.AnyAsync())
            {
                var staffTeacher = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Teacher);
                var staffEng = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.TechnicalEngineer);

                context.StaffLeaveRequests.AddRange(
                    new StaffLeaveRequest
                    {
                        UserId = staffTeacher?.Id ?? Guid.NewGuid(),
                        UserName = staffTeacher?.FullName ?? "أ. أحمد الشافعي (معلم)",
                        UserRole = "Teacher",
                        LeaveType = "عارضة",
                        StartDate = today.AddDays(-1),
                        EndDate = today.AddDays(-1),
                        DaysCount = 1,
                        Reason = "ظرف أسري طارئ ومفاجئ",
                        Status = "approved",
                        ApprovedBy = "أ. مها الشناوي (الموارد البشرية)",
                        Notes = "تم اعتماد الإجازة العارضة وخصمها من الرصيد السنوي."
                    },
                    new StaffLeaveRequest
                    {
                        UserId = staffEng?.Id ?? Guid.NewGuid(),
                        UserName = staffEng?.FullName ?? "م. سارة المهندس (معامل)",
                        UserRole = "TechnicalEngineer",
                        LeaveType = "اعتيادية",
                        StartDate = today.AddDays(3),
                        EndDate = today.AddDays(5),
                        DaysCount = 3,
                        Reason = "إجازة اعتيادية سنوية لظروف خاصة",
                        Status = "pending",
                        Notes = "في انتظار موافقة الإدارة وتأمين بديل المعمل."
                    }
                );

                context.StaffInvestigations.Add(new StaffInvestigation
                {
                    InvestigationNumber = "تحقيق-2026/01",
                    Subject = "مذكرة بشأن تأخر فتح معمل الحاسب وتأخير الحصة الأولى",
                    StaffUserId = staffEng?.Id ?? Guid.NewGuid(),
                    StaffUserName = staffEng?.FullName ?? "م. سارة المهندس",
                    StaffRole = "TechnicalEngineer",
                    InvestigatorId = Guid.NewGuid(),
                    InvestigatorName = "أ. مها الشناوي (مسؤول الموارد البشرية)",
                    IncidentDate = today.AddDays(-3),
                    SummaryOfAllegations = "التأخر عن فتح معمل شبكات الحاسب لمدة 20 دقيقة مما أدى إلى تأخير التدريب العملي.",
                    StaffStatement = "أفادت الزميلة بوجود عطل فني في مفتاح المعمل الذكي وتم الاستعانة بفريق الصيانة فوراً.",
                    Findings = "ثبت صحة الإفادة وأن التأخير ناتج عن خلل في قفل الباب الإلكتروني للمعمل وليس إهمالاً شخصياً.",
                    Decision = "حفظ التحقيق لعدم وجود مخالفة شخصية مع التوجيه بمتابعة كفاءة الأقفال دورياً",
                    Status = "closed_decision_rendered",
                    ManagerEndorsement = "معتمد - د. خالد السعدني (مدير المدرسة)",
                    ClosedAtUtc = DateTime.UtcNow.AddDays(-2)
                });

                context.StaffMemos.AddRange(
                    new StaffMemo
                    {
                        MemoNumber = "مذكرة-2026/011",
                        MemoType = "مذكرة شكوى",
                        Title = "شكوى بشأن نقص كابلات شبكات الفايبر في معمل الاتصالات 2",
                        TargetStaffName = "مسؤول التوريدات والصيانة",
                        TargetStaffRole = "IT",
                        CreatedByName = "أ. مها الشناوي (الموارد البشرية)",
                        Content = "بناءً على طلب قسم الاتصالات، يوجد عجز في وصلات الألياف الضوئية مما يعيق تنفيذ التدريب العملي للجدارات.",
                        Recommendations = "التوجيه بسرعة صرف المستلزمات المعملية من مخازن الشركة المصرية للاتصالات.",
                        Priority = "عاجلة",
                        Status = "محالة للمدير",
                        ManagerRemarks = "يُصرف فوراً من العهدة الفرعية بالتنسيق مع الإدارة المالية."
                    },
                    new StaffMemo
                    {
                        MemoNumber = "مذكرة-2026/012",
                        MemoType = "مذكرة لفت نظر / مخالفة إدارية",
                        Title = "لفت نظر بعدم اصطحاب مشروبات ساخنة داخل المعامل التقنية",
                        TargetStaffName = staffTeacher?.FullName ?? "أ. أحمد الشافعي",
                        TargetStaffRole = "Teacher",
                        CreatedByName = "أ. مها الشناوي (الموارد البشرية)",
                        Content = "تم رصد دخول بعض السوائل إلى قاعة الأجهزة الحساسة وهو ما يخالف قواعد الأمن والسلامة المهنية بالمعامل.",
                        Recommendations = "التنبيه بالالتزام الصارم بلائحة السلامة داخل المعامل حفاظاً على الأجهزة.",
                        Priority = "عادية",
                        Status = "معتمدة ومحفوظة",
                        ManagerRemarks = "تعتمد وتُحفظ بملف المتابعة الدورية."
                    },
                    new StaffMemo
                    {
                        MemoNumber = "مذكرة-2026/013",
                        MemoType = "مذكرة إشادة وتقدير",
                        Title = "مذكرة إشادة بجهود طاقم التدريس والأنشطة الرياضية",
                        TargetStaffName = "كافة طاقم المدرسة والكابتن الرياضي",
                        TargetStaffRole = "all",
                        CreatedByName = "أ. مها الشناوي (الموارد البشرية)",
                        Content = "تقديراً للانضباط العالي في طابور الصباح وتنظيم الفعاليات الرياضية وتفوق الطلاب في اختبارات الجدارات.",
                        Recommendations = "منح شهادات شكر وتقدير رسمية في طابور الصباح وتكريم المتميزين.",
                        Priority = "عادية",
                        Status = "معتمدة ومحفوظة",
                        ManagerRemarks = "خالص الشكر والتقدير لجهودكم المخلصة في رفعة المدرسة."
                    }
                );

                await context.SaveChangesAsync();
            }

            // 6. Seed School Exams & Remedial Students Tracking
            if (!await context.SchoolExams.AnyAsync())
            {
                var examCoordinator = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.AssessmentCoordinator);
                var coordId = examCoordinator?.Id ?? Guid.NewGuid();
                var coordName = examCoordinator?.FullName ?? "أ. محمد عاطف المنشاوي (الامتحانات)";

                var examFormativeTheory = new SchoolExam
                {
                    Title = "الامتحان التكويني الأول - تكنولوجيا الشبكات والاتصالات",
                    ExamType = "تكويني نظري",
                    Subject = "تكنولوجيا الشبكات والاتصالات (ICT)",
                    Classroom = "A1",
                    Grade = 1,
                    MaxScore = 100,
                    PassingScore = 50,
                    ExamDate = today.AddDays(-5),
                    CreatedById = coordId,
                    CreatedByName = coordName,
                    Notes = "امتحان تكويني لقياس المكتسبات المعرفية للمرحلة الأولى.",
                    IsPublished = true
                };

                var examFormativePractical = new SchoolExam
                {
                    Title = "الامتحان التكويني المعملي - برمجة بايثون والخوارزميات",
                    ExamType = "تكويني عملي",
                    Subject = "برمجة الحاسب والخوارزميات (Python)",
                    Classroom = "A1",
                    Grade = 1,
                    MaxScore = 100,
                    PassingScore = 60,
                    ExamDate = today.AddDays(-2),
                    CreatedById = coordId,
                    CreatedByName = coordName,
                    Notes = "اختبار عملي فردي في معمل الحاسب لقياس كتابة وتصحيح الأكواد.",
                    IsPublished = true
                };

                var examRemedial = new SchoolExam
                {
                    Title = "الامتحان التحسيني - أساسيات شبكات الحاسب (الفرصة التعويضية)",
                    ExamType = "تحسيني",
                    Subject = "أساسيات وتراكيب شبكات الحاسب",
                    Classroom = "A1",
                    Grade = 1,
                    MaxScore = 100,
                    PassingScore = 50,
                    ExamDate = today.AddDays(2),
                    CreatedById = coordId,
                    CreatedByName = coordName,
                    Notes = "فرصة تحسينية معتمدة للطلاب الراغبين في رفع التقدير أو تعويض درجات الاختبار الأول.",
                    IsPublished = true
                };

                context.SchoolExams.AddRange(examFormativeTheory, examFormativePractical, examRemedial);
                await context.SaveChangesAsync();

                // Seed Exam Scores for Classroom A1 students
                var stdsA1 = await context.Users.Where(u => u.Role == UserRole.Student && u.Classroom == "A1").Take(12).ToListAsync();
                if (stdsA1.Any())
                {
                    var scores = new List<ExamStudentScore>();
                    int idx = 0;
                    foreach (var s in stdsA1)
                    {
                        // Formative theory
                        double score1 = 65 + (idx * 3) % 35;
                        scores.Add(new ExamStudentScore
                        {
                            ExamId = examFormativeTheory.Id,
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            StudentCode = s.StudentCode ?? s.Username,
                            Classroom = "A1",
                            Grade = 1,
                            Score = score1,
                            MaxScore = 100,
                            IsRemedial = false,
                            RemedialStatus = "مجتاز",
                            TeacherNotes = "مستوى جيد جداً في الجانب النظري."
                        });

                        // Formative practical
                        double score2 = 60 + (idx * 4) % 40;
                        scores.Add(new ExamStudentScore
                        {
                            ExamId = examFormativePractical.Id,
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            StudentCode = s.StudentCode ?? s.Username,
                            Classroom = "A1",
                            Grade = 1,
                            Score = score2,
                            MaxScore = 100,
                            IsRemedial = false,
                            RemedialStatus = "مجتاز",
                            TeacherNotes = "إتقان ممتاز في كتابة الدوال البرمجية."
                        });

                        // Remedial exam assignments with distinct realistic statuses
                        if (idx == 0)
                        {
                            scores.Add(new ExamStudentScore
                            {
                                ExamId = examRemedial.Id,
                                StudentId = s.Id,
                                StudentName = s.FullName,
                                StudentCode = s.StudentCode ?? s.Username,
                                Classroom = "A1",
                                Grade = 1,
                                Score = 82,
                                MaxScore = 100,
                                PreviousScore = 44,
                                IsRemedial = true,
                                RemedialStatus = "اجتاز التحسين بنجاح",
                                TeacherNotes = "اجتاز الطالب الامتحان التحسيني بنجاح كبير ورفع درجته من 44 إلى 82."
                            });
                        }
                        else if (idx == 1)
                        {
                            scores.Add(new ExamStudentScore
                            {
                                ExamId = examRemedial.Id,
                                StudentId = s.Id,
                                StudentName = s.FullName,
                                StudentCode = s.StudentCode ?? s.Username,
                                Classroom = "A1",
                                Grade = 1,
                                Score = 0,
                                MaxScore = 100,
                                PreviousScore = 38,
                                IsRemedial = true,
                                RemedialStatus = "قيد الاختبار التحسيني",
                                TeacherNotes = "تم تسجيل الطالب وموعد جلسته التحسينية غداً بالمعمل."
                            });
                        }
                        else if (idx == 2)
                        {
                            scores.Add(new ExamStudentScore
                            {
                                ExamId = examRemedial.Id,
                                StudentId = s.Id,
                                StudentName = s.FullName,
                                StudentCode = s.StudentCode ?? s.Username,
                                Classroom = "A1",
                                Grade = 1,
                                Score = 45,
                                MaxScore = 100,
                                PreviousScore = 32,
                                IsRemedial = true,
                                RemedialStatus = "لم يجتز بعد - فرصة استثنائية",
                                TeacherNotes = "حصل على 45 درجة وما زال يحتاج جلسة إرشاد ودعم إضافي."
                            });
                        }
                        else if (idx == 3)
                        {
                            scores.Add(new ExamStudentScore
                            {
                                ExamId = examRemedial.Id,
                                StudentId = s.Id,
                                StudentName = s.FullName,
                                StudentCode = s.StudentCode ?? s.Username,
                                Classroom = "A1",
                                Grade = 1,
                                Score = 0,
                                MaxScore = 100,
                                PreviousScore = 40,
                                IsRemedial = true,
                                RemedialStatus = "يحتاج تحسين",
                                TeacherNotes = "مرشح لدخول الاختبار التحسيني لرفع الدرجة."
                            });
                        }

                        idx++;
                    }

                    context.ExamStudentScores.AddRange(scores);
                    await context.SaveChangesAsync();
                }
            }

            // 7. Seed Sports Coach & Physical Education Data
            if (!await context.SportsAssessments.AnyAsync())
            {
                var coachUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SportsCoach);
                var coachId = coachUser?.Id ?? Guid.NewGuid();
                var coachName = coachUser?.FullName ?? "كابتن حسام حسن";

                var tournament1 = new SportsTournament
                {
                    Title = "دوري مدارس WE لكرة القدم المصغرة (خماسي)",
                    TournamentType = "دوري كرة القدم",
                    TargetStages = "الصف الأول والثاني (A & B)",
                    StartDate = today.AddDays(-7),
                    Status = "جاري التنظيم والتصفيات",
                    WinningClassroom = "التصفيات نصف النهائية بين A1 و B2",
                    BestPlayerOrScorer = "الطالب أحمد محمد مصطفى (فصل A1)",
                    RulesAndNotes = "المباريات تُقام في حصص التربية الرياضية مع تطبيق قواعد الروح الرياضية والالتزام بالزي الرياضي."
                };

                var tournament2 = new SportsTournament
                {
                    Title = "بطولة التنس والشطرنج للياقة الذهنية والتركيز",
                    TournamentType = "دوري الشطرنج وتنس الطاولة",
                    TargetStages = "كافة المراحل (A, B, C)",
                    StartDate = today.AddDays(7),
                    Status = "قادم",
                    RulesAndNotes = "فتح باب التسجيل لجميع فصول المدرسة."
                };

                context.SportsTournaments.AddRange(tournament1, tournament2);

                var stdsForSports = await context.Users.Where(u => u.Role == UserRole.Student && u.Classroom == "A1").Take(8).ToListAsync();
                if (stdsForSports.Any())
                {
                    var sportsList = new List<SportsAssessment>();
                    int spIdx = 0;
                    foreach (var s in stdsForSports)
                    {
                        string badge = spIdx == 0 ? "بطل اللياقة البدنية 🥇" : spIdx == 1 ? "كابتن فريق الفصل ⚽" : spIdx == 2 ? "انضباط بدني متميز ⭐" : null;
                        sportsList.Add(new SportsAssessment
                        {
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            StudentCode = s.StudentCode ?? s.Username,
                            Classroom = "A1",
                            Grade = 1,
                            MorningAssemblyStatus = spIdx == 0 ? "حاضر ومنضبط" : spIdx == 1 ? "حاضر ومنضبط" : spIdx == 2 ? "تأخر عن التمارين" : "حاضر ومنضبط",
                            FitnessScore = 85 + (spIdx * 2) % 15,
                            SpeedScore = 8 + (spIdx % 3),
                            EnduranceScore = 9,
                            FlexibilityScore = 8,
                            PreferredSport = spIdx % 2 == 0 ? "كرة قدم" : "تنس طاولة",
                            HonorBadge = badge,
                            CoachNotes = "لياقة بدنية ممتازة وتفاعل نشط في التمارين الصباحية وطابور المدرسة.",
                            RecordedByCoachId = coachId,
                            RecordedByCoachName = coachName,
                            RecordedAtUtc = DateTime.UtcNow
                        });
                        spIdx++;
                    }

                    context.SportsAssessments.AddRange(sportsList);
                }

                // Ensure parent1 has student1 and student2 linked
                var parentUser = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "parent1");
                var st1 = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "student1");
                var st2 = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == "student2");

                if (st1 == null)
                {
                    st1 = new User
                    {
                        FullName = "علي محمد الشرقاوي",
                        Username = "student1",
                        PasswordHash = "student123",
                        Role = UserRole.Student,
                        Status = UserStatus.Approved,
                        ParentConsent = true,
                        Grade = 1,
                        Classroom = "A1",
                        StudentCode = "STU-2026-001",
                        ParentId = parentUser?.Id,
                        Specialty = "programming"
                    };
                    await context.Users.AddAsync(st1);
                    await context.SaveChangesAsync();
                }

                if (parentUser != null)
                {
                    if (st1 != null && st1.ParentId != parentUser.Id) st1.ParentId = parentUser.Id;
                    if (st2 != null && st2.ParentId != parentUser.Id) st2.ParentId = parentUser.Id;
                    await context.SaveChangesAsync();
                }

                // Ensure st1 has sample submissions/progress records with teacher feedback
                if (st1 != null && !await context.StudentProgresses.AnyAsync(p => p.StudentId == st1.Id))
                {
                    var sampleAssignments = await context.Assignments.Take(3).ToListAsync();
                    if (sampleAssignments.Any())
                    {
                        var p1 = new StudentProgress
                        {
                            StudentId = st1.Id,
                            AssignmentId = sampleAssignments[0].Id,
                            Status = ProgressStatus.Done,
                            Stars = 3,
                            AutoScore = 95,
                            ManualGrade = 95,
                            IsGraded = true,
                            TeacherFeedback = "إجابة ممتازة واحترافية في محاكاة بروتوكولات الشبكات وتوصيلات الراوتر.",
                            TeacherEncouragementMsg = "أحسنت يا بطل! استمر بهذا المستوى المتميز 💜",
                            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
                            FinishedAtUtc = DateTime.UtcNow.AddDays(-1),
                            LastActiveAtUtc = DateTime.UtcNow.AddDays(-1)
                        };
                        context.StudentProgresses.Add(p1);

                        if (sampleAssignments.Count > 1)
                        {
                            var p2 = new StudentProgress
                            {
                                StudentId = st1.Id,
                                AssignmentId = sampleAssignments[1].Id,
                                Status = ProgressStatus.Done,
                                Stars = 3,
                                AutoScore = 90,
                                ManualGrade = 90,
                                IsGraded = true,
                                TeacherFeedback = "كود برمجي نظيف ومنظم بلغة C# مع الالتزام بالمعايير العالمية.",
                                StartedAtUtc = DateTime.UtcNow.AddDays(-4),
                                FinishedAtUtc = DateTime.UtcNow.AddDays(-3),
                                LastActiveAtUtc = DateTime.UtcNow.AddDays(-3)
                            };
                            context.StudentProgresses.Add(p2);
                        }

                        if (sampleAssignments.Count > 2)
                        {
                            var p3 = new StudentProgress
                            {
                                StudentId = st1.Id,
                                AssignmentId = sampleAssignments[2].Id,
                                Status = ProgressStatus.Working,
                                Stars = 1,
                                StartedAtUtc = DateTime.UtcNow.AddHours(-3),
                                LastActiveAtUtc = DateTime.UtcNow.AddHours(-1)
                            };
                            context.StudentProgresses.Add(p3);
                        }

                        await context.SaveChangesAsync();
                    }
                }

                // Ensure st1 has Exam Scores in Control
                if (st1 != null && !await context.ExamStudentScores.AnyAsync(e => e.StudentId == st1.Id))
                {
                    var examsList = await context.SchoolExams.Take(2).ToListAsync();
                    if (examsList.Any())
                    {
                        context.ExamStudentScores.Add(new ExamStudentScore
                        {
                            ExamId = examsList[0].Id,
                            StudentId = st1.Id,
                            StudentName = st1.FullName,
                            StudentCode = st1.StudentCode ?? "STU-2026-001",
                            Classroom = "A1",
                            Grade = 1,
                            Score = 94,
                            MaxScore = 100,
                            IsRemedial = false,
                            RemedialStatus = "مجتاز",
                            TeacherNotes = "أداء ممتاز في الامتحان التكويني النظري.",
                            RecordedAtUtc = DateTime.UtcNow.AddDays(-5)
                        });

                        if (examsList.Count > 1)
                        {
                            context.ExamStudentScores.Add(new ExamStudentScore
                            {
                                ExamId = examsList[1].Id,
                                StudentId = st1.Id,
                                StudentName = st1.FullName,
                                StudentCode = st1.StudentCode ?? "STU-2026-001",
                                Classroom = "A1",
                                Grade = 1,
                                Score = 98,
                                MaxScore = 100,
                                IsRemedial = false,
                                RemedialStatus = "مجتاز",
                                TeacherNotes = "تطبيق عملي ممتاز ومتقن في المعمل التخصصي.",
                                RecordedAtUtc = DateTime.UtcNow.AddDays(-3)
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }

                // Ensure st1 has Sports Fitness assessment
                if (st1 != null && !await context.SportsAssessments.AnyAsync(s => s.StudentId == st1.Id))
                {
                    var headCoachUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SportsCoach);
                    context.SportsAssessments.Add(new SportsAssessment
                    {
                        StudentId = st1.Id,
                        StudentName = st1.FullName,
                        StudentCode = st1.StudentCode ?? "STU-2026-001",
                        Classroom = "A1",
                        Grade = 1,
                        MorningAssemblyStatus = "حاضر ومنضبط",
                        FitnessScore = 95.0,
                        SpeedScore = 9,
                        EnduranceScore = 9,
                        FlexibilityScore = 8,
                        PreferredSport = "كرة قدم",
                        HonorBadge = "بطل اللياقة البدنية",
                        CoachNotes = "طالب متميز رياضياً وذو بنية بدنية قوية وانضباط عالي في طابور الصباح.",
                        RecordedByCoachId = headCoachUser?.Id ?? Guid.NewGuid(),
                        RecordedByCoachName = headCoachUser?.FullName ?? "كابتن حسام حسن",
                        RecordedAtUtc = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }

                // Seed initial Student Disciplinary Notices & Warnings & Commendations
                if (!await context.StudentDisciplineNotices.AnyAsync() && st1 != null)
                {
                    var notices = new List<StudentDisciplineNotice>
                    {
                        new StudentDisciplineNotice
                        {
                            StudentId = st1.Id,
                            StudentName = st1.FullName,
                            Classroom = st1.Classroom ?? "A1",
                            Grade = st1.Grade ?? 1,
                            ParentId = parentUser?.Id,
                            ParentName = parentUser?.FullName ?? "محمد الشرقاوي (ولي أمر)",
                            ParentPhone = "01012345678",
                            Category = "absence",
                            Title = "إشعار متابعة غياب (يومان متتاليان)",
                            Details = "تغيب الطالب عن حضور الحصص العملية يومي الأحد والإثنين بدون تقديم إذن مسبق لإدارة المدرسة.",
                            ActionTaken = "إخطار ولي الأمر عبر البوابة وطلب تقديم إفادة أو عذر طبي معتمد.",
                            Severity = "warning",
                            Status = "pending",
                            IssuedBy = "أ. منى (الأخصائي الاجتماعي)",
                            IncidentDate = DateTime.UtcNow.AddDays(-2)
                        },
                        new StudentDisciplineNotice
                        {
                            StudentId = st1.Id,
                            StudentName = st1.FullName,
                            Classroom = st1.Classroom ?? "A1",
                            Grade = st1.Grade ?? 1,
                            ParentId = parentUser?.Id,
                            ParentName = parentUser?.FullName ?? "محمد الشرقاوي (ولي أمر)",
                            ParentPhone = "01012345678",
                            Category = "uniform",
                            Title = "ملاحظة التزام بالزي المدرسي الموحد والمعامل",
                            Details = "لوحظ عدم ارتداء البالطو المعتمد للسلامة المهنية أثناء التواجد في معمل شبكات الحاسب.",
                            ActionTaken = "تنبيه شفوي وتوجيه الطالب للالتزام باشتراطات السلامة المهنية المعتمدة لمدارس WE.",
                            Severity = "info",
                            Status = "acknowledged",
                            IssuedBy = "أ. منى (الأخصائي الاجتماعي)",
                            IncidentDate = DateTime.UtcNow.AddDays(-5),
                            IsAcknowledgedByParent = true,
                            AcknowledgedAtUtc = DateTime.UtcNow.AddDays(-4),
                            ParentNotes = "تم التنبيه على الطالب والالتزام بالبالطو كاملاً بدءاً من اليوم التالي."
                        },
                        new StudentDisciplineNotice
                        {
                            StudentId = st1.Id,
                            StudentName = st1.FullName,
                            Classroom = st1.Classroom ?? "A1",
                            Grade = st1.Grade ?? 1,
                            ParentId = parentUser?.Id,
                            ParentName = parentUser?.FullName ?? "محمد الشرقاوي (ولي أمر)",
                            ParentPhone = "01012345678",
                            Category = "disruption",
                            Title = "ملاحظة سلوكية داخل الورشة التدريبية",
                            Details = "انشغال الطالب بالهاتف واستخدامه بدون إذن أثناء الشرح العملي لتوصيلات الألياف الضوئية.",
                            ActionTaken = "تسليم الهاتف لمشرف الدور واستلامه بنهاية اليوم المدرسي مع التعهد بعدم التكرار.",
                            Severity = "warning",
                            Status = "pending",
                            IssuedBy = "أ. منى (الأخصائي الاجتماعي)",
                            IncidentDate = DateTime.UtcNow.AddDays(-1)
                        },
                        new StudentDisciplineNotice
                        {
                            StudentId = st1.Id,
                            StudentName = st1.FullName,
                            Classroom = st1.Classroom ?? "A1",
                            Grade = st1.Grade ?? 1,
                            ParentId = parentUser?.Id,
                            ParentName = parentUser?.FullName ?? "محمد الشرقاوي (ولي أمر)",
                            ParentPhone = "01012345678",
                            Category = "positive",
                            Title = "شهادة تميز وانضباط سلوكي وأكاديمي ⭐",
                            Details = "إشادة بجهود الطالب ومساعدته لزملائه في معمل البرمجة والحفاظ على نظافة وانضباط المعمل.",
                            ActionTaken = "منح الطالب 5 نجوم تميز وتكريمه في الإذاعة الصباحية للمدرسة.",
                            Severity = "info",
                            Status = "acknowledged",
                            IssuedBy = "إدارة شؤون الطلاب والأخصائي الاجتماعي",
                            IncidentDate = DateTime.UtcNow.AddDays(-7),
                            IsAcknowledgedByParent = true,
                            AcknowledgedAtUtc = DateTime.UtcNow.AddDays(-6),
                            ParentNotes = "سعداء جداً بتميز ابننا وشكراً جزيلاً لجهود المدرسة والمعلمين."
                        }
                    };

                    await context.StudentDisciplineNotices.AddRangeAsync(notices);
                }

                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Failed to seed school operations data: {ex.Message}");
        }
    }

    public class StudentSeedItem
    {
        public string FullName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? StudentCode { get; set; }
        public string? Code { get; set; }
        public string? NationalId { get; set; }
        public int? Grade { get; set; }
        public string Classroom { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentPhoneNumber { get; set; }
        public bool? ParentConsent { get; set; }
        public string? Status { get; set; }
    }
}

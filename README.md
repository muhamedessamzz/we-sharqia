# مدارس WE للتكنولوجيا التطبيقية - الشرقية | خادم النظام وقواعد البيانات (Backend API)
## WE Sharkia Applied Technology School - Core Backend API & Architecture

الواجهة البرمجية الخلفية (Backend Web API) الرسمية لنظام مدارس WE للتكنولوجيا التطبيقية بالشرقية، مبنية وفق أحدث معايير **Clean Architecture** باستخدام **ASP.NET Core (.NET 10)** و **Entity Framework Core** مع قاعدة بيانات **Microsoft SQL Server**.

---

### 🏛️ البنية الهندسية (Clean Architecture):

```text
server/
├── WeSharkia.Domain/          # الكيانات (Entities)، التعدادات (Enums)، والقواعد الأساسية
├── WeSharkia.Application/     # الواجهات (Interfaces)، كائنات نقل البيانات (DTOs)، ونماذج المنطق
├── WeSharkia.Infrastructure/  # EF Core DbContext، التشفير الآمن (AES-256 + PBKDF2)، والتهيئة التلقائية
└── WeSharkia.API/             # Controllers، إعدادات CORS، توثيق OpenAPI، والـ Startup
```

---

### 🛡️ الأمان وحماية البيانات (Security & Privacy):
- **تشفير الرقم القومي للطلاب والكوادر:** تشفير عسكري متقدم باستخدام خوارزمية **AES-256-GCM** مع مفاتيح ديناميكية وقناع عرض (`NationalIdMasked`).
- **تجزئة كلمات المرور:** خوارزمية **PBKDF2** مع Salt عشوائي لكل مستخدم لمنع هجمات Rainbow Tables.
- **CORS Policy مرنة:** مهيأة للسماح للواجهات الأمامية المحلية والسحابية (Vercel) بالاتصال السلس والآمن.

---

### 📊 قاعدة البيانات (Database & Migrations):
- متوافقة بالكامل مع **Microsoft SQL Server**.
- تهيئة تلقائية بالكامل عند أول تشغيل عبر `DbInitializer.SeedAsync`:
  - إنشاء **28 جدولاً رسمياً**.
  - إدخال الحسابات الافتراضية للكادر الإداري والمعلمين والمهندسين المتخصصين.
  - استيراد وتشفير بيانات الطلاب وقوائم الفصول.
  - تغذية جدول الحصص الأسبوعي الشامل للمدرسة (366 حصة ومعمل وملعب).

---

### ⚙️ المتطلبات والتشغيل محلياً (Getting Started):

#### المتطلبات:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server (LocalDB أو Express أو Remote Server)

#### التشغيل:
```bash
# الانتقال لمجلد المشروع
cd WeSharkia.API

# تشغيل السيرفر
dotnet run
```
سيبدأ الخادم بالاستماع افتراضياً على: `http://localhost:5142`.

---

### 🚀 النشر على MonsterASP / IIS:
يمكن نشر المشروع بسهولة عبر الأمر:
```bash
dotnet publish WeSharkia.API/WeSharkia.API.csproj -c Release -o publish
```
ثم رفع محتويات مجلد `publish` إلى الخادم السحابي.

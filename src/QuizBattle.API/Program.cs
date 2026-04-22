using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizBattle.Infrastructure;
using QuizBattle.Application.Interfaces;
using QuizBattle.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DbContext, Identity, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddSingleton<IMatchmakingStore, RedisMatchmakingStore>();
}

// Add Controllers
builder.Services.AddControllers();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "QuizBattleSecretKey123456789012345678901234";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuizBattle";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuizBattle API",
        Version = "v1",
        Description = "API برای بازی کوییز باتل"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database schema exists, then seed default data
await EnsureDatabaseCreatedAsync(app.Services);
await SeedDefaultDataAsync(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var disableHttpsRedirection = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");
if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Ensure database tables are created if missing
static async Task EnsureDatabaseCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<QuizBattle.Infrastructure.Data.ApplicationDbContext>();

    try
    {
        await context.Database.EnsureCreatedAsync();
    }
    catch
    {
        if (await context.Database.CanConnectAsync())
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.EnsureCreatedAsync();
    }
}

// Seed method
static async Task SeedDefaultDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<QuizBattle.Application.Interfaces.IUnitOfWork>();
    
    // Seed Categories
    var categoryRepo = unitOfWork.Repository<QuizBattle.Domain.Entities.Category>();
    var existingCategories = await categoryRepo.GetAllAsync();
    
    if (!existingCategories.Any())
    {
        var categories = new List<QuizBattle.Domain.Entities.Category>
        {
            new() { Name = "تاریخ", IconUrl = "📜", Description = "سوالات تاریخی ایران و جهان" },
            new() { Name = "علوم", IconUrl = "🔬", Description = "فیزیک، شیمی، زیست و..." },
            new() { Name = "جغرافیا", IconUrl = "🌍", Description = "کشورها، پایتخت‌ها و..." },
            new() { Name = "ورزش", IconUrl = "⚽", Description = "فوتبال، والیبال و..." },
            new() { Name = "سینما", IconUrl = "🎬", Description = "فیلم‌ها و بازیگران" },
            new() { Name = "موسیقی", IconUrl = "🎵", Description = "خوانندگان و آهنگ‌ها" },
            new() { Name = "اطلاعات عمومی", IconUrl = "💡", Description = "دانستنی‌های عمومی" },
            new() { Name = "کامپیوتر", IconUrl = "💻", Description = "فناوری اطلاعات" }
        };
        
        foreach (var cat in categories)
        {
            await categoryRepo.AddAsync(cat);
        }
        await unitOfWork.SaveChangesAsync();
        existingCategories = await categoryRepo.GetAllAsync();
    }
    
    // Seed Questions
    var questionRepo = unitOfWork.Repository<QuizBattle.Domain.Entities.Question>();
    var existingQuestions = await questionRepo.GetAllAsync();
    
    if (!existingQuestions.Any())
    {
        var catDict = existingCategories.ToDictionary(c => c.Name, c => c.Id);
        
        var questions = new List<QuizBattle.Domain.Entities.Question>
        {
            // تاریخ
            new() { Text = "کدام پادشاه هخامنشی امپراتوری ایران را تأسیس کرد؟", CategoryId = catDict["تاریخ"], Option1 = "کوروش بزرگ", Option2 = "داریوش", Option3 = "خشایارشا", Option4 = "اردشیر", CorrectAnswer = "کوروش بزرگ" },
            new() { Text = "جنگ چالدران بین ایران و کدام کشور بود؟", CategoryId = catDict["تاریخ"], Option1 = "عثمانی", Option2 = "روسیه", Option3 = "انگلیس", Option4 = "پرتغال", CorrectAnswer = "عثمانی" },
            new() { Text = "انقلاب مشروطه ایران در چه سالی رخ داد؟", CategoryId = catDict["تاریخ"], Option1 = "۱۲۸۵", Option2 = "۱۲۹۰", Option3 = "۱۳۰۰", Option4 = "۱۳۱۰", CorrectAnswer = "۱۲۸۵" },
            
            // علوم
            new() { Text = "فرمول شیمیایی آب کدام است؟", CategoryId = catDict["علوم"], Option1 = "H2O", Option2 = "CO2", Option3 = "NaCl", Option4 = "O2", CorrectAnswer = "H2O" },
            new() { Text = "کدام سیاره به خورشید نزدیک‌تر است؟", CategoryId = catDict["علوم"], Option1 = "عطارد", Option2 = "زهره", Option3 = "مریخ", Option4 = "زمین", CorrectAnswer = "عطارد" },
            new() { Text = "واحد اندازه‌گیری نیرو چیست؟", CategoryId = catDict["علوم"], Option1 = "نیوتن", Option2 = "ژول", Option3 = "وات", Option4 = "آمپر", CorrectAnswer = "نیوتن" },
            
            // جغرافیا
            new() { Text = "پایتخت ژاپن کدام شهر است؟", CategoryId = catDict["جغرافیا"], Option1 = "توکیو", Option2 = "کیوتو", Option3 = "اوزاکا", Option4 = "هیروشیما", CorrectAnswer = "توکیو" },
            new() { Text = "بلندترین قله جهان کدام است؟", CategoryId = catDict["جغرافیا"], Option1 = "اورست", Option2 = "K2", Option3 = "کانچنجونگا", Option4 = "لوتسه", CorrectAnswer = "اورست" },
            new() { Text = "بزرگ‌ترین اقیانوس جهان کدام است؟", CategoryId = catDict["جغرافیا"], Option1 = "آرام", Option2 = "اطلس", Option3 = "هند", Option4 = "منجمد شمالی", CorrectAnswer = "آرام" },
            
            // ورزش
            new() { Text = "کدام تیم بیشترین قهرمانی جام جهانی فوتبال را دارد؟", CategoryId = catDict["ورزش"], Option1 = "برزیل", Option2 = "آلمان", Option3 = "ایتالیا", Option4 = "آرژانتین", CorrectAnswer = "برزیل" },
            new() { Text = "المپیک ۲۰۲۰ در کدام شهر برگزار شد؟", CategoryId = catDict["ورزش"], Option1 = "توکیو", Option2 = "پاریس", Option3 = "لندن", Option4 = "ریو", CorrectAnswer = "توکیو" },
            new() { Text = "کدام ورزشکار ایرانی در کشتی فرنگی قهرمان جهان شد؟", CategoryId = catDict["ورزش"], Option1 = "حسن یزدانی", Option2 = "رضا یزدانی", Option3 = "کمیل قاسمی", Option4 = "حمید سوریان", CorrectAnswer = "حمید سوریان" },
            
            // سینما
            new() { Text = "فیلم تایتانیک در چه سالی ساخته شد؟", CategoryId = catDict["سینما"], Option1 = "۱۹۹۷", Option2 = "۱۹۹۵", Option3 = "۲۰۰۰", Option4 = "۱۹۹۲", CorrectAnswer = "۱۹۹۷" },
            new() { Text = "کارگردان فیلم «جدایی نادر از سیمین» کیست؟", CategoryId = catDict["سینما"], Option1 = "اصغر فرهادی", Option2 = "مجید مجیدی", Option3 = "عباس کیارستمی", Option4 = "جعفر پناهی", CorrectAnswer = "اصغر فرهادی" },
            new() { Text = "کدام فیلم برنده اسکار بهترین فیلم ۲۰۲۰ شد؟", CategoryId = catDict["سینما"], Option1 = "انگل", Option2 = "۱۹۱۷", Option3 = "جوکر", Option4 = "ایرلندی", CorrectAnswer = "انگل" },
            
            // موسیقی
            new() { Text = "محمدرضا شجریان در کدام سبک موسیقی فعالیت می‌کرد؟", CategoryId = catDict["موسیقی"], Option1 = "سنتی ایرانی", Option2 = "پاپ", Option3 = "راک", Option4 = "رپ", CorrectAnswer = "سنتی ایرانی" },
            new() { Text = "کدام خواننده آهنگ «سلطان قلب‌ها» را خواند؟", CategoryId = catDict["موسیقی"], Option1 = "معین", Option2 = "داریوش", Option3 = "ابی", Option4 = "گوگوش", CorrectAnswer = "معین" },
            
            // اطلاعات عمومی
            new() { Text = "پرجمعیت‌ترین کشور جهان کدام است؟", CategoryId = catDict["اطلاعات عمومی"], Option1 = "چین", Option2 = "هند", Option3 = "آمریکا", Option4 = "اندونزی", CorrectAnswer = "هند" },
            new() { Text = "کدام عنصر بیشترین فراوانی در پوسته زمین دارد؟", CategoryId = catDict["اطلاعات عمومی"], Option1 = "اکسیژن", Option2 = "سیلیسیم", Option3 = "آلومینیوم", Option4 = "آهن", CorrectAnswer = "اکسیژن" },
            new() { Text = "بزرگ‌ترین حیوان روی زمین کدام است؟", CategoryId = catDict["اطلاعات عمومی"], Option1 = "نهنگ آبی", Option2 = "فیل آفریقایی", Option3 = "زرافه", Option4 = "کوسه نهنگ", CorrectAnswer = "نهنگ آبی" },
            
            // کامپیوتر
            new() { Text = "زبان برنامه‌نویسی Python توسط چه کسی ساخته شد؟", CategoryId = catDict["کامپیوتر"], Option1 = "گیدو فن روسوم", Option2 = "جیمز گاسلینگ", Option3 = "بیارنه استراستروپ", Option4 = "دنیس ریچی", CorrectAnswer = "گیدو فن روسوم" },
            new() { Text = "HTML مخفف چیست؟", CategoryId = catDict["کامپیوتر"], Option1 = "HyperText Markup Language", Option2 = "High Tech Modern Language", Option3 = "Hyper Transfer Mail Link", Option4 = "Home Tool Markup Language", CorrectAnswer = "HyperText Markup Language" },
            new() { Text = "کدام شرکت سیستم عامل Windows را توسعه می‌دهد؟", CategoryId = catDict["کامپیوتر"], Option1 = "مایکروسافت", Option2 = "اپل", Option3 = "گوگل", Option4 = "آی‌بی‌ام", CorrectAnswer = "مایکروسافت" }
        };
        
        foreach (var q in questions)
        {
            await questionRepo.AddAsync(q);
        }
        await unitOfWork.SaveChangesAsync();
    }
    
    // Ensure answers exist for all questions with option fields
    var answerRepo = unitOfWork.Repository<QuizBattle.Domain.Entities.Answer>();
    var allQuestionsWithOptions = await questionRepo.Query().Include(q => q.Answers).ToListAsync();
    var answersToAdd = new List<QuizBattle.Domain.Entities.Answer>();

    foreach (var question in allQuestionsWithOptions)
    {
        if (!question.Answers.Any())
        {
            var options = new[] { question.Option1, question.Option2, question.Option3, question.Option4 };
            for (int i = 0; i < options.Length; i++)
            {
                answersToAdd.Add(new QuizBattle.Domain.Entities.Answer
                {
                    Text = options[i],
                    OrderIndex = i + 1,
                    IsCorrect = options[i] == question.CorrectAnswer,
                    QuestionId = question.Id
                });
            }
        }
    }

    if (answersToAdd.Any())
    {
        await answerRepo.AddRangeAsync(answersToAdd);
        await unitOfWork.SaveChangesAsync();
    }
    
    // Seed Store Items (Coin Packages)
    var storeRepo = unitOfWork.Repository<QuizBattle.Domain.Entities.StoreItem>();
    var existingStoreItems = await storeRepo.GetAllAsync();
    
    if (!existingStoreItems.Any())
    {
        var storeItems = new List<QuizBattle.Domain.Entities.StoreItem>
        {
            new() { Name = "بسته برنزی", Description = "مناسب برای شروع", CoinAmount = 100, PriceInToman = 10000, OrderIndex = 1 },
            new() { Name = "بسته نقره‌ای", Description = "محبوب‌ترین بسته", CoinAmount = 300, PriceInToman = 25000, IsPopular = true, OrderIndex = 2 },
            new() { Name = "بسته طلایی", Description = "پرفروش‌ترین", CoinAmount = 700, PriceInToman = 50000, DiscountPercent = 10, OrderIndex = 3 },
            new() { Name = "بسته الماس", Description = "بهترین ارزش", CoinAmount = 1500, PriceInToman = 90000, DiscountPercent = 15, OrderIndex = 4 },
            new() { Name = "بسته افسانه‌ای", Description = "برای حرفه‌ای‌ها", CoinAmount = 5000, PriceInToman = 250000, DiscountPercent = 20, OrderIndex = 5 }
        };
        
        foreach (var item in storeItems)
        {
            await storeRepo.AddAsync(item);
        }
        await unitOfWork.SaveChangesAsync();
    }
}

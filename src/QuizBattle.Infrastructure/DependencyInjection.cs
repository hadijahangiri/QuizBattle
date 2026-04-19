using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Interfaces;
using QuizBattle.Application.Services;
using QuizBattle.Infrastructure.Data;
using QuizBattle.Infrastructure.Repositories;
using QuizBattle.Infrastructure.Services;

namespace QuizBattle.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - use SQL Server via configured connection string
        var connectionString = configuration.GetConnectionString("GameDatabase")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured. Set GameDatabase or DefaultConnection in configuration.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure()));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IGameSettingsService, GameSettingService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IDailyChallengeService, DailyChallengeService>();
        services.AddScoped<IDailyRewardService, DailyRewardService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupBattleService, GroupBattleService>();
        services.AddScoped<IStoreService, StoreService>();

        return services;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // User & Auth
    public DbSet<User> GameUsers { get; set; } = null!;
    public DbSet<Avatar> Avatars { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    // Questions
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;
    public DbSet<QuestionReactionEntity> QuestionReactions { get; set; } = null!;

    // Game
    public DbSet<Game> Games { get; set; } = null!;
    public DbSet<GameRound> GameRounds { get; set; } = null!;
    public DbSet<RoundQuestion> RoundQuestions { get; set; } = null!;
    public DbSet<GameHelperUsage> GameHelperUsages { get; set; } = null!;

    // Daily Challenge
    public DbSet<DailyChallenge> DailyChallenges { get; set; } = null!;
    public DbSet<DailyChallengeQuestion> DailyChallengeQuestions { get; set; } = null!;
    public DbSet<DailyChallengeResult> DailyChallengeResults { get; set; } = null!;
    public DbSet<DailyChallengeAnswer> DailyChallengeAnswers { get; set; } = null!;

    // Groups
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public DbSet<GroupMembershipRequest> GroupMembershipRequests { get; set; } = null!;
    public DbSet<GroupChat> GroupChats { get; set; } = null!;

    // Group Battles
    public DbSet<GroupBattle> GroupBattles { get; set; } = null!;
    public DbSet<GroupBattlePlayer> GroupBattlePlayers { get; set; } = null!;
    public DbSet<GroupBattleMatch> GroupBattleMatches { get; set; } = null!;
    public DbSet<GroupBattleMatchRound> GroupBattleMatchRounds { get; set; } = null!;
    public DbSet<GroupBattleMatchQuestion> GroupBattleMatchQuestions { get; set; } = null!;

    // Rewards & Store
    public DbSet<DailyReward> DailyRewards { get; set; } = null!;
    public DbSet<UserDailyReward> UserDailyRewards { get; set; } = null!;
    public DbSet<StoreItem> StoreItems { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<CoinGiftRequest> CoinGiftRequests { get; set; } = null!;

    // Game configuration
    public DbSet<GameSettings> GameSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
        });

        // Game relationships
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasOne(g => g.Player1)
                .WithMany(u => u.GamesAsPlayer1)
                .HasForeignKey(g => g.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Player2)
                .WithMany(u => u.GamesAsPlayer2)
                .HasForeignKey(g => g.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Winner)
                .WithMany()
                .HasForeignKey(g => g.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Group relationships
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(e => e.UniqueCode).IsUnique();

            entity.HasOne(g => g.Owner)
                .WithMany(u => u.OwnedGroups)
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
        });

        // Group Battle relationships
        modelBuilder.Entity<GroupBattle>(entity =>
        {
            entity.HasOne(gb => gb.Group1)
                .WithMany(g => g.BattlesAsGroup1)
                .HasForeignKey(gb => gb.Group1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(gb => gb.Group2)
                .WithMany(g => g.BattlesAsGroup2)
                .HasForeignKey(gb => gb.Group2Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupBattleMatch>(entity =>
        {
            entity.HasOne(m => m.Group1Player)
                .WithMany()
                .HasForeignKey(m => m.Group1PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Group2Player)
                .WithMany()
                .HasForeignKey(m => m.Group2PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<QuestionReactionEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.QuestionId }).IsUnique();
            entity.HasOne(qr => qr.Question)
                .WithMany(q => q.Reactions)
                .HasForeignKey(qr => qr.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RoundQuestion>(entity =>
        {
            entity.HasOne(rq => rq.GameRound)
                .WithMany(gr => gr.Questions)
                .HasForeignKey(rq => rq.GameRoundId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(rq => rq.Question)
                .WithMany()
                .HasForeignKey(rq => rq.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DailyChallengeQuestion>(entity =>
        {
            entity.HasOne(dq => dq.DailyChallenge)
                .WithMany(dc => dc.Questions)
                .HasForeignKey(dq => dq.DailyChallengeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(dq => dq.Question)
                .WithMany()
                .HasForeignKey(dq => dq.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DailyChallengeAnswer>(entity =>
        {
            entity.HasOne(ans => ans.Result)
                .WithMany(r => r.Answers)
                .HasForeignKey(ans => ans.DailyChallengeResultId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(ans => ans.Question)
                .WithMany()
                .HasForeignKey(ans => ans.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<GroupBattleMatchQuestion>(entity =>
        {
            entity.HasOne(q => q.Round)
                .WithMany(r => r.Questions)
                .HasForeignKey(q => q.GroupBattleMatchRoundId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(q => q.Question)
                .WithMany()
                .HasForeignKey(q => q.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Transactions
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(t => t.RelatedUser)
                .WithMany()
                .HasForeignKey(t => t.RelatedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Coin gift requests
        modelBuilder.Entity<CoinGiftRequest>(entity =>
        {
            entity.HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>
/// کاربر Identity برای پنل مدیریت
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

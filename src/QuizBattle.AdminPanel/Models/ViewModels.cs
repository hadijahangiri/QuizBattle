using System.ComponentModel.DataAnnotations;
using QuizBattle.Domain.Enums;

namespace QuizBattle.AdminPanel.Models;

// Dashboard View Models
public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalGames { get; set; }
    public int TotalCategories { get; set; }
    public int ActiveGames { get; set; }
    public int ReportedQuestions { get; set; }
    public int TodaysGames { get; set; }
    public int NewUsersThisWeek { get; set; }
    public List<RecentGameViewModel> RecentGames { get; set; } = new();
    public List<TopUserViewModel> TopUsers { get; set; } = new();
    public Dictionary<string, int> GamesPerDay { get; set; } = new();
}

public class RecentGameViewModel
{
    public Guid Id { get; set; }
    public string Player1Name { get; set; } = "";
    public string Player2Name { get; set; } = "";
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameStatus Status { get; set; }
}

public class TopUserViewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }
    public int Coins { get; set; }
}

// Question View Models
public class QuestionListViewModel
{
    public List<QuestionViewModel> Questions { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public Guid? CategoryFilter { get; set; }
    public string? SearchTerm { get; set; }
}

public class QuestionViewModel
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = "";
    public Guid CategoryId { get; set; }
    public string CorrectAnswer { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int TimesUsed { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int ReportCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateQuestionViewModel
{
    [Required(ErrorMessage = "متن سوال الزامی است")]
    [Display(Name = "متن سوال")]
    public string Text { get; set; } = "";

    [Display(Name = "تصویر سوال")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "دسته‌بندی الزامی است")]
    [Display(Name = "دسته‌بندی")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "گزینه اول الزامی است")]
    [Display(Name = "گزینه ۱")]
    public string Option1 { get; set; } = "";

    [Required(ErrorMessage = "گزینه دوم الزامی است")]
    [Display(Name = "گزینه ۲")]
    public string Option2 { get; set; } = "";

    [Required(ErrorMessage = "گزینه سوم الزامی است")]
    [Display(Name = "گزینه ۳")]
    public string Option3 { get; set; } = "";

    [Required(ErrorMessage = "گزینه چهارم الزامی است")]
    [Display(Name = "گزینه ۴")]
    public string Option4 { get; set; } = "";

    [Required(ErrorMessage = "پاسخ صحیح الزامی است")]
    [Range(1, 4, ErrorMessage = "پاسخ صحیح باید بین ۱ تا ۴ باشد")]
    [Display(Name = "شماره پاسخ صحیح")]
    public int CorrectOptionIndex { get; set; }

    public List<CategorySelectItem>? Categories { get; set; }
}

public class EditQuestionViewModel : CreateQuestionViewModel
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
}

// Category View Models
public class CategoryListViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = new();
}

public class CategoryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? IconUrl { get; set; }
    public int QuestionCount { get; set; }
    public int GamesPlayed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [Display(Name = "نام دسته‌بندی")]
    public string Name { get; set; } = "";

    [Display(Name = "آیکون")]
    public string? IconUrl { get; set; }

    [Display(Name = "توضیحات")]
    public string? Description { get; set; }
}

public class EditCategoryViewModel : CreateCategoryViewModel
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
}

// Daily Reward View Models
public class DailyRewardListViewModel
{
    public List<DailyRewardAdminViewModel> Rewards { get; set; } = new();
}

public class DailyRewardAdminViewModel
{
    public Guid Id { get; set; }
    public int Day { get; set; }
    public int CoinReward { get; set; }
    public string? SpecialReward { get; set; }
    public bool IsActive { get; set; }
}

public class EditDailyRewardViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "روز الزامی است")]
    [Range(1, 20, ErrorMessage = "روز باید بین 1 تا 20 باشد")]
    [Display(Name = "روز")]
    public int Day { get; set; }

    [Required(ErrorMessage = "تعداد سکه الزامی است")]
    [Display(Name = "سکه جایزه")]
    public int CoinReward { get; set; }

    [Display(Name = "جایزه ویژه")]
    public string? SpecialReward { get; set; }

    [Display(Name = "فعال باشد")]
    public bool IsActive { get; set; } = true;
}

// User View Models
public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
}

public class UserViewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public int Coins { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalGames { get; set; }
    public UserLevel Level { get; set; }
    public bool IsActive { get; set; }
    public bool IsGuest { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class EditUserViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [Display(Name = "نام کاربری")]
    public string Username { get; set; } = "";

    [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
    [Display(Name = "ایمیل")]
    public string? Email { get; set; }

    [Display(Name = "شماره تلفن")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "سکه")]
    public int Coins { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;
}

// Reported Questions
public class ReportedQuestionListViewModel
{
    public List<ReportedQuestionViewModel> Reports { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class ReportedQuestionViewModel
{
    public Guid ReportId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string ReporterUsername { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime ReportedAt { get; set; }
    public bool IsResolved { get; set; }
}

// Common
public class CategorySelectItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

// Daily Challenge
public class DailyChallengeViewModel
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public int ParticipantsCount { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsActive { get; set; }
    public List<DailyChallengeQuestionViewModel> Questions { get; set; } = new();
}

public class DailyChallengeQuestionViewModel
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public int OrderIndex { get; set; }
}

public class CreateDailyChallengeViewModel
{
    [Required(ErrorMessage = "تاریخ الزامی است")]
    [Display(Name = "تاریخ")]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "سوالات")]
    public List<Guid> SelectedQuestionIds { get; set; } = new();

    public List<QuestionViewModel>? AvailableQuestions { get; set; }
}

// Group Battle
public class GroupBattleListViewModel
{
    public List<GroupBattleViewModel> Battles { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class GroupBattleViewModel
{
    public Guid Id { get; set; }
    public string Group1Name { get; set; } = "";
    public string Group2Name { get; set; } = "";
    public int Group1Score { get; set; }
    public int Group2Score { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsCompleted { get; set; }
}

// Store / Shop
public class StoreListViewModel
{
    public List<StoreItemViewModel> Items { get; set; } = new();
}

public class StoreItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int CoinAmount { get; set; }
    public decimal PriceInToman { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; }
    public decimal? DiscountPercent { get; set; }
    public int OrderIndex { get; set; }
}

public class CreateStoreItemViewModel
{
    [Required(ErrorMessage = "نام بسته الزامی است")]
    [Display(Name = "نام بسته")]
    public string Name { get; set; } = "";

    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "تعداد سکه الزامی است")]
    [Range(1, 1000000, ErrorMessage = "تعداد سکه باید بین ۱ تا ۱,۰۰۰,۰۰۰ باشد")]
    [Display(Name = "تعداد سکه")]
    public int CoinAmount { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(1000, 100000000, ErrorMessage = "قیمت باید بین ۱,۰۰۰ تا ۱۰۰,۰۰۰,۰۰۰ تومان باشد")]
    [Display(Name = "قیمت (تومان)")]
    public decimal PriceInToman { get; set; }

    [Display(Name = "تصویر")]
    public string? ImageUrl { get; set; }

    [Display(Name = "محبوب")]
    public bool IsPopular { get; set; }

    [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین ۰ تا ۱۰۰ باشد")]
    [Display(Name = "درصد تخفیف")]
    public decimal? DiscountPercent { get; set; }

    [Display(Name = "ترتیب نمایش")]
    public int OrderIndex { get; set; }
}

public class EditStoreItemViewModel : CreateStoreItemViewModel
{
    public Guid Id { get; set; }
}

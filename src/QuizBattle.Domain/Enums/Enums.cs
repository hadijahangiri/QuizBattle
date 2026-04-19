namespace QuizBattle.Domain.Enums;

public enum UserLevel
{
    Beginner = 1,
    Novice = 2,
    Intermediate = 3,
    Advanced = 4,
    Expert = 5,
    Master = 6,
    GrandMaster = 7,
    Legend = 8
}

public enum GameStatus
{
    Pending = 0,
    InProgress = 1,
    WaitingForOpponent = 2,
    Completed = 3,
    Cancelled = 4,
    Timeout = 5
}

public enum RoundStatus
{
    NotStarted = 0,
    Player1Turn = 1,
    Player2Turn = 2,
    Completed = 3
}

public enum HelperType
{
    RemoveTwoOptions = 1,
    DoubleAnswer = 2,
    AddTime = 3
}

public enum QuestionReaction
{
    Like = 1,
    Dislike = 2,
    Report = 3
}

public enum GroupRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

public enum MembershipStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum GroupBattleStatus
{
    WaitingForMatch = 0,
    Matched = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TransactionType
{
    Purchase = 1,
    Gift = 2,
    Reward = 3,
    HelperUsage = 4,
    CategoryChange = 5
}

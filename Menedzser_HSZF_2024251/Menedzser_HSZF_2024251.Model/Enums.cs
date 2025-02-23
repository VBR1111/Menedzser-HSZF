namespace Menedzser_HSZF_2024251.Model
{
    public enum Position { Goalkeeper, Defender, Midfielder, Forward }
    public enum Performance { Critical, Low, Medium, High }
    public enum PhysicalCondition { Healthy, Injured }
    public enum TaskType { Training, Match, Transfer, Recovery }
    public enum MatchResult { Loss, Draw, Win }
    public enum TransferStatus { Pending, Accepted, Rejected, Cancelled, Completed }
    public enum PlayerStatus { Available, Active, TransferListed, Retired, Negotiating }
    public enum SeasonStatus { Active, Completed, Failed }
}

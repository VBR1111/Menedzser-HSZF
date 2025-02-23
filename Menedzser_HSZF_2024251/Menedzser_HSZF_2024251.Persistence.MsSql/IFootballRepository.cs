using Menedzser_HSZF_2024251.Model;

namespace Menedzser_HSZF_2024251.Persistence.MsSql
{
    public interface IFootballRepository
    {
        DateTime CurrentDate { get; }
        void SetCurrentDate(DateTime date);
        void CreateTeam(Team team);
        Team GetTeam(int id);
        void UpdateTeam(Team team);
        void CreatePlayer(Player player);
        Player GetPlayer(int id);
        void UpdatePlayer(Player player);
        void DeletePlayer(int id);
        void CreateTask(TeamTask task);
        void UpdateTask(TeamTask task);
        TeamTask GetTask(int id);
        void UpdateSkill(Skill skill);
        void CreateSkill(Skill skill);
        IEnumerable<Player> GetAllPlayers();
        IEnumerable<Player> GetHealthyPlayersByTeam(int teamId);
        Team GetCurrentTeam();
        void SetCurrentTeam(Team team);
        TeamStatistics GetTeamStatistics(int teamId);
        IEnumerable<Team> GetAllTeams();
        TeamStatistics CalculateTeamStatistics(int teamId);
        void UpdateTeamStatistics(TeamStatistics statistics);
        void CreateTransferOffer(TransferOffer offer);
        TransferOffer GetTransferOffer(int id);
        void UpdateTransferOffer(TransferOffer offer);
        IEnumerable<TransferOffer> GetTransferOffersForTeam(int teamId);
        IEnumerable<Player> GetTransferListedPlayers();
        void CreateSeason(Season season);
        void UpdateSeason(Season season);
        Season GetCurrentSeason(int teamId);
        void UpdatePlayerSalary(string playerId, decimal newSalary);
        IEnumerable<Player> GetPlayersWithExpiringContracts(int monthsThreshold);
        IEnumerable<TeamTask> GetTasksForTeam(int teamId);
        List<TransferOffer> GetPendingTransferOffers(int teamId);
        TransferOffer GetTransferOfferWithDetails(int transferOfferId);
        void SaveChanges();
    }
}
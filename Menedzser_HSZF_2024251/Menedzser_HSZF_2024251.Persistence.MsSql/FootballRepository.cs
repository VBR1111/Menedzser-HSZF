using Menedzser_HSZF_2024251.Model;
using Microsoft.EntityFrameworkCore;

namespace Menedzser_HSZF_2024251.Persistence.MsSql
{
    public class FootballRepository : IFootballRepository
    {
        private DateTime currentDate;
        private readonly FootballDbContext _context;
        private Team _currentTeam;

        public FootballRepository(FootballDbContext context)
        {
            _context = context;
            currentDate = new DateTime(DateTime.Now.Year, 1, 1);
        }

        public void SetCurrentDate(DateTime date)
        {
            currentDate = date;
        }

        public DateTime CurrentDate => currentDate;


        public void CreateTeam(Team team)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));

            _context.Teams.Add(team);
            _context.SaveChanges();
        }


        public Team GetTeam(int id)
        {
            return _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Tasks)
                .FirstOrDefault(t => t.Id == id);
        }


        public void UpdateTeam(Team team)
        {
            _context.Entry(team).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public void CreatePlayer(Player player)
        {
            _context.Players.Add(player);
            _context.SaveChanges();
        }


        public Player GetPlayer(int id)
        {
            return _context.Players
                .Include(p => p.Skills)
                .Include(p => p.Team)
                .FirstOrDefault(p => p.Id == id);
        }


        public void UpdatePlayer(Player player)
        {
            _context.Entry(player).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public void DeletePlayer(int id)
        {
            var player = _context.Players.Find(id);
            if (player != null)
            {
                _context.Players.Remove(player);
                _context.SaveChanges();
            }
        }


        public void CreateTask(TeamTask task)
        {
            _context.Tasks.Add(task);
            _context.SaveChanges();
        }


        public void UpdateSkill(Skill skill)
        {
            _context.Entry(skill).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public void CreateSkill(Skill skill)
        {
            _context.Skills.Add(skill);
            _context.SaveChanges();
        }


        public IEnumerable<Player> GetAllPlayers()
        {
            return _context.Players
                .Include(p => p.Skills)
                .Include(p => p.Team)
                .ToList();
        }

        public IEnumerable<Player> GetHealthyPlayersByTeam(int teamId)
        {
            return _context.Players
                .Include(p => p.Skills)
                .Where(p => p.TeamId == teamId && p.PhysicalCondition == PhysicalCondition.Healthy)
                .ToList();
        }

        public Team GetCurrentTeam()
        {
            if (_currentTeam == null)
                throw new InvalidOperationException("No team selected! Please select a team first.");

            return _currentTeam;
        }

        public TeamTask GetTask(int id)
        {
            return _context.Tasks
                .Include(t => t.Team)
                .Include(t => t.PlayerTasks)
                    .ThenInclude(pt => pt.Player)
                .FirstOrDefault(t => t.Id == id);
        }

        public TeamStatistics GetTeamStatistics(int teamId)
        {
            var stats = _context.Tasks
                .Where(t => t.TeamId == teamId && t.Type == TaskType.Match && t.Result.HasValue)
                .GroupBy(t => t.TeamId)
                .Select(g => new TeamStatistics
                {
                    TeamId = g.Key,
                    Wins = g.Count(t => t.Result == MatchResult.Win),
                    Draws = g.Count(t => t.Result == MatchResult.Draw),
                    Losses = g.Count(t => t.Result == MatchResult.Loss),
                    GoalsScored = g.Sum(t => t.GoalsScored ?? 0),
                    GoalsConceded = g.Sum(t => t.GoalsConceded ?? 0)
                })
                .FirstOrDefault();

            return stats ?? new TeamStatistics { TeamId = teamId };
        }

        public IEnumerable<Team> GetAllTeams()
        {
            return _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Tasks)
                    .ThenInclude(t => t.PlayerTasks)
                        .ThenInclude(pt => pt.Player)
                .ToList();
        }

        public TeamStatistics CalculateTeamStatistics(int teamId)
        {
            var team = _context.Teams
                .Include(t => t.Tasks)
                .FirstOrDefault(t => t.Id == teamId);

            if (team == null)
                return null;

            var matches = team.Tasks.Where(t => t.Type == TaskType.Match && t.Result.HasValue);

            return new TeamStatistics
            {
                TeamId = team.Id,
                Wins = matches.Count(t => t.Result == MatchResult.Win),
                Draws = matches.Count(t => t.Result == MatchResult.Draw),
                Losses = matches.Count(t => t.Result == MatchResult.Loss),
                GoalsScored = matches.Sum(t => t.GoalsScored ?? 0),
                GoalsConceded = matches.Sum(t => t.GoalsConceded ?? 0)
            };
        }


        public void UpdateTask(TeamTask task)
        {
            _context.Entry(task).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public void UpdateTeamStatistics(TeamStatistics statistics)
        {
            _context.Entry(statistics).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void CreateTransferOffer(TransferOffer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            _context.TransferOffers.Add(offer);
            _context.SaveChanges();
        }

        public TransferOffer GetTransferOffer(int id)
        {
            return _context.TransferOffers
                .Include(t => t.Player)
                .Include(t => t.FromTeam)
                .Include(t => t.ToTeam)
                .FirstOrDefault(t => t.Id == id);
        }

        public void UpdateTransferOffer(TransferOffer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            _context.Entry(offer).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public IEnumerable<TransferOffer> GetTransferOffersForTeam(int teamId)
        {
            return _context.TransferOffers
                .Include(t => t.Player)
                .Include(t => t.FromTeam)
                .Include(t => t.ToTeam)
                .Where(t => t.FromTeamId == teamId || t.ToTeamId == teamId)
                .ToList();
        }


        public IEnumerable<Player> GetTransferListedPlayers()
        {
            return _context.Players
                .Include(p => p.Team)
                .Include(p => p.Skills)
                .Where(p => p.Status == PlayerStatus.TransferListed && p.TeamId != _currentTeam.Id)
                .ToList();
        }


        public void CreateSeason(Season season)
        {
            if (season == null)
                throw new ArgumentNullException(nameof(season));

            _context.Seasons.Add(season);
            _context.SaveChanges();
        }


        public void UpdateSeason(Season season)
        {
            if (season == null)
                throw new ArgumentNullException(nameof(season));

            _context.Entry(season).State = EntityState.Modified;
            _context.SaveChanges();
        }


        public Season GetCurrentSeason(int teamId)
        {
            return _context.Seasons
                .Include(s => s.Team)
                .FirstOrDefault(s => s.TeamId == teamId && s.Status == SeasonStatus.Active);
        }

        public void UpdatePlayerSalary(string playerId, decimal newSalary)
        {
            var player = _context.Players.Find(playerId);
            if (player != null)
            {
                player.WeeklySalary = newSalary;
                _context.SaveChanges();
            }
        }

        public IEnumerable<Player> GetPlayersWithExpiringContracts(int monthsThreshold)
        {
            var thresholdDate = currentDate.AddMonths(monthsThreshold);
            return _context.Players
                .Include(p => p.Team)
                .Include(p => p.Skills)
                .Where(p => p.ContractEnd <= thresholdDate)
                .ToList();
        }

        public IEnumerable<TeamTask> GetTasksForTeam(int teamId)
        {
            return _context.Tasks
                .Include(t => t.PlayerTasks)
                    .ThenInclude(pt => pt.Player)
                .Where(t => t.TeamId == teamId)
                .ToList();
        }

        public void SetCurrentTeam(Team team)
        {
            _currentTeam = team;
        }

        public List<TransferOffer> GetPendingTransferOffers(int teamId)
        {
            return _context.TransferOffers
                .Include(o => o.Player)
                .Include(o => o.FromTeam)
                .Include(o => o.ToTeam)
                .Where(o => o.ToTeamId == teamId && o.Status == TransferStatus.Pending)
                .ToList();
        }

        public TransferOffer GetTransferOfferWithDetails(int transferOfferId)
        {
            return _context.TransferOffers
                .Include(o => o.Player)
                .Include(o => o.FromTeam)
                .Include(o => o.ToTeam)
                .FirstOrDefault(o => o.Id == transferOfferId);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}

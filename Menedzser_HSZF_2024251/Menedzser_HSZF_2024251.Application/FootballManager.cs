using Menedzser_HSZF_2024251.Model;
using Menedzser_HSZF_2024251.Persistence.MsSql;

namespace Menedzser_HSZF_2024251.Application
{

    public class FootballManager
    {

        public event EventHandler<PlayerPerformanceChangedEventArgs> PlayerPerformanceChanged;
        public event EventHandler<PlayerInjuryEventArgs> PlayerInjured;
        public event EventHandler<TaskCompletedEventArgs> TaskCompleted;

        private readonly IXmlReportGenerator _reportGenerator;
        private readonly IFootballRepository _repository;
        private Season _currentSeason;
        private Dictionary<int, TaskJson> _availableTasks;

        private Random random = new Random();

        public FootballManager(IFootballRepository repository, IXmlReportGenerator reportGenerator)
        {
            _repository = repository;
            _reportGenerator = reportGenerator;
        }


        public DateTime CurrentDate => _repository.CurrentDate;


        public void SetCurrentDate(DateTime date)
        {
            _repository.SetCurrentDate(date);
        }


        public void InitializeFromJson(FootballData data)
        {
            foreach (var teamJson in data.Teams)
            {
                var team = new Team
                {
                    Name = teamJson.TeamName,
                    Budget = teamJson.Budget,
                    StaffCount = teamJson.StaffCount
                };
                _repository.CreateTeam(team);

                var teamPlayers = data.Players.Where(p => teamJson.Players.Contains(p.PlayerId));
                foreach (var playerJson in teamPlayers)
                {
                    var player = new Player(_repository.CurrentDate)
                    {
                        Name = playerJson.Name,
                        Position = ParsePosition(playerJson.Position),
                        Performance = ParsePerformance(playerJson.Performance),
                        PhysicalCondition = ParseCondition(playerJson.PhysicalCondition),
                        TeamId = team.Id
                    };

                    foreach (var skill in playerJson.Skills)
                    {
                        player.Skills.Add(new Skill
                        {
                            Name = skill.Key,
                            Value = skill.Value
                        });
                    }
                    _repository.CreatePlayer(player);
                }
            }
            _availableTasks = data.Tasks.ToDictionary(t => t.TaskId);
        }


        public void EvaluateTrainingResult(TeamTask task)
        {
            TaskJson taskTemplate = _availableTasks[task.Id];

            var successChance = int.Parse(taskTemplate.SuccessChance.TrimEnd('%'));
            bool isSuccessful = random.Next(100) < successChance;

            foreach (var playerTask in task.PlayerTasks)
            {
                var player = playerTask.Player;

                foreach (var impact in taskTemplate.Impact.Where(i => i.Key != "injury_chance"))
                {
                    int changeValue = int.Parse(impact.Value);
                    var skill = player.Skills.FirstOrDefault(s => s.Name.ToLower() == impact.Key.ToLower());

                    if (skill != null)
                    {
                        skill.Value += isSuccessful ? changeValue : -changeValue;
                        skill.Value = Math.Max(0, Math.Min(100, skill.Value));
                        _repository.UpdateSkill(skill);
                    }
                }

                if (taskTemplate.Impact.ContainsKey("injury_chance"))
                {
                    var injuryChance = int.Parse(taskTemplate.Impact["injury_chance"].TrimEnd('%'));
                    if (random.Next(100) < injuryChance)
                    {
                        player.PhysicalCondition = PhysicalCondition.Injured;
                        _repository.UpdatePlayer(player);
                    }
                }
            }
        }

        public IEnumerable<Player> GetAllPlayers()
        {
            return _repository.GetAllPlayers();
        }


        public Team GetCurrentTeam()
        {
            return _repository.GetCurrentTeam();
        }


        public void AddPlayer(Player player)
        {
            var team = _repository.GetCurrentTeam();
            player.TeamId = team.Id;
            _repository.CreatePlayer(player);
        }


        private bool HasTaskOnDate(DateTime date, int teamId)
        {
            var tasks = _repository.GetTasksForTeam(teamId);
            return tasks.Any(t => t.StartTime.Date == date.Date);
        }


        public void ScheduleMatch(TeamTask matchTask)
        {
            var team = _repository.GetCurrentTeam();
            var healthyPlayers = _repository.GetHealthyPlayersByTeam(team.Id);

            if (healthyPlayers.Count() < 11)
            {
                throw new InvalidOperationException("Not enough healthy players for a match");
            }

            if (team.Budget < 5000)
            {
                throw new InvalidOperationException("Insufficient budget for match");
            }

            matchTask.TeamId = team.Id;
            _repository.CreateTask(matchTask);
        }


        public void EvaluateTraining(int trainingId, int impact)
        {
            var training = _repository.GetTask(trainingId);
            if (training == null || training.Type != TaskType.Training)
                throw new InvalidOperationException("Invalid training session");

            foreach (var playerTask in training.PlayerTasks)
            {
                var player = playerTask.Player;

                if (impact > 0 && player.Performance < Performance.High)
                {
                    player.Performance++;
                }
                else if (impact < 0 && player.Performance > Performance.Critical)
                {
                    player.Performance--;
                }

                if (random.Next(100) < 5)
                {
                    player.PhysicalCondition = PhysicalCondition.Injured;
                }

                foreach (var skill in player.Skills)
                {
                    if (impact > 0)
                    {
                        skill.Value = Math.Min(100, skill.Value + random.Next(1, 3));
                    }
                    else if (impact < 0)
                    {
                        skill.Value = Math.Max(0, skill.Value - random.Next(1, 3));
                    }
                }
                _repository.UpdatePlayer(player);
            }

            training.Description += $"\nEvaluated: {_repository.CurrentDate} - Impact: {(impact > 0 ? "Positive" : impact < 0 ? "Negative" : "Neutral")}";
            _repository.UpdateTask(training);
        }


        public void RecordMatchResult(int matchId, MatchResult result, int goalsScored, int goalsConceded)
        {
            var match = _repository.GetTask(matchId);
            if (match == null || match.Type != TaskType.Match)
                throw new InvalidOperationException("Invalid match ID");

            match.Result = result;
            match.GoalsScored = goalsScored;
            match.GoalsConceded = goalsConceded;

            foreach (var playerTask in match.PlayerTasks)
            {
                var player = playerTask.Player;
                UpdatePlayerPerformance(player, result);
            }

            _repository.UpdateTask(match);

            UpdateSeasonStats(result);
        }

        public void UpdatePlayerPerformance(Player player, MatchResult result)
        {
            switch (result)
            {
                case MatchResult.Win:
                    if (player.Performance < Performance.High)
                        player.Performance++;
                    break;
                case MatchResult.Loss:
                    if (player.Performance > Performance.Critical)
                        player.Performance--;
                    break;
            }
            _repository.UpdatePlayer(player);
        }


        public void CreateTeam(string name, decimal initialBudget, int staffCount)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Team name cannot be empty");

            if (initialBudget < 0)
                throw new ArgumentException("Initial budget cannot be negative");

            if (staffCount < 1)
                throw new ArgumentException("Staff count must be at least 1");

            var team = new Team
            {
                Name = name,
                Budget = initialBudget,
                StaffCount = staffCount
            };

            _repository.CreateTeam(team);

            SelectTeam(team.Id);
        }

        public TeamStatistics GetTeamStatistics()
        {
            var team = GetCurrentTeam();
            return _repository.CalculateTeamStatistics(team.Id);
        }


        public void SelectTeam(int teamId)
        {
            var team = _repository.GetTeam(teamId);
            if (team == null)
                throw new InvalidOperationException("Team not found");

            _repository.SetCurrentTeam(team);
        }


        public void UpdateTeamBudget(int teamId, decimal newBudget)
        {
            if (newBudget < 1)
                throw new ArgumentException("Budget cannot be less than one.");

            var team = _repository.GetTeam(teamId);
            if (team == null)
                throw new InvalidOperationException("Team not found");

            if (newBudget < 0)
                throw new InvalidOperationException("Budget cannot be negative");

            team.Budget = newBudget;

            _repository.UpdateTeam(team);
        }


        public IEnumerable<Player> GetPlayersWithExpiringContracts()
        {
            const int WARNING_MONTHS = 6;
            return _repository.GetPlayersWithExpiringContracts(WARNING_MONTHS);
        }

        public bool RenewContract(int playerId, int extensionYears, decimal salaryIncrease)
        {
            var player = _repository.GetPlayer(playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found");

            var newSalary = player.WeeklySalary + salaryIncrease;
            var newEndDate = player.ContractEnd.AddYears(extensionYears);

            bool willAccept = CalculateContractAcceptanceProbability(player, salaryIncrease);

            if (willAccept)
            {
                UpdatePlayerContract(playerId, newEndDate, newSalary);
                return true;
            }

            return false;
        }


        private bool CalculateContractAcceptanceProbability(Player player, decimal salaryIncrease)
        {
            int baseChance = 50;

            switch (player.Performance)
            {
                case Performance.High: baseChance -= 20; break;
                case Performance.Critical: baseChance += 20; break;
            }

            if (player.WeeklySalary > 0)
            {
                decimal increasePercentage = (salaryIncrease / player.WeeklySalary) * 100;
                baseChance += (int)(increasePercentage / 10) * 5;
            }

            return random.Next(100) < baseChance;
        }


        public void ListPlayerForTransfer(int playerId, decimal askingPrice)
        {
            var player = _repository.GetPlayer(playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found");

            player.Status = PlayerStatus.TransferListed;
            player.TransferValue = askingPrice;
            _repository.UpdatePlayer(player);
        }


        public void MakeTransferOffer(int playerId, decimal offerAmount, decimal weeklySalary)
        {
            var currentTeam = GetCurrentTeam();
            var player = _repository.GetPlayer(playerId);

            if (player == null)
                throw new InvalidOperationException("Player not found");

            if (currentTeam.Budget < offerAmount)
                throw new InvalidOperationException("Insufficient funds for transfer");

            var offer = new TransferOffer(_repository.CurrentDate)
            {
                PlayerId = playerId,
                FromTeamId = currentTeam.Id,
                ToTeamId = player.TeamId,
                OfferedAmount = offerAmount,
                OfferedWeeklySalary = weeklySalary,
                Status = TransferStatus.Pending
            };

            _repository.CreateTransferOffer(offer);
        }


        public void RespondToTransferOffer(int offerId, bool accept)
        {
            var offer = _repository.GetTransferOffer(offerId);
            if (offer == null)
                throw new InvalidOperationException("Transfer offer not found");

            if (accept)
            {
                var player = _repository.GetPlayer(offer.PlayerId);
                var fromTeam = _repository.GetTeam(offer.FromTeamId);
                var toTeam = _repository.GetTeam(offer.ToTeamId);

                fromTeam.Budget -= offer.OfferedAmount;
                toTeam.Budget += offer.OfferedAmount;

                player.TeamId = fromTeam.Id;
                player.Status = PlayerStatus.Active;
                player.WeeklySalary = offer.OfferedWeeklySalary;
                player.ContractStart = _repository.CurrentDate;
                player.ContractEnd = _repository.CurrentDate.AddYears(2);

                offer.Status = TransferStatus.Completed;
                offer.ResponseDate = _repository.CurrentDate;

                _repository.UpdateTeam(fromTeam);
                _repository.UpdateTeam(toTeam);
                _repository.UpdatePlayer(player);
                _repository.UpdateTransferOffer(offer);
            }
            else
            {
                offer.Status = TransferStatus.Rejected;
                offer.ResponseDate = _repository.CurrentDate;
                _repository.UpdateTransferOffer(offer);
            }
        }


        public void StartNewSeason()
        {
            var team = GetCurrentTeam();
            if (team == null)
                throw new InvalidOperationException("No team selected!");

            _currentSeason = new Season(_repository.CurrentDate)
            {
                TeamId = team.Id,
                StartDate = _repository.CurrentDate,
                EndDate = _repository.CurrentDate.AddYears(1),
                StartingBudget = team.Budget,
                Status = SeasonStatus.Active
            };
            _repository.CreateSeason(_currentSeason);
        }


        public bool CheckGameOver()
        {
            if (_currentSeason == null)
                return false;

            var team = GetCurrentTeam();
            bool isSeasonComplete = _repository.CurrentDate >= _currentSeason.EndDate;
            bool hasValidPlayers = team.Players.Any(p => p.Performance > Performance.Critical);
            bool isTeamSolvent = team.Budget > 0;

            if (isSeasonComplete || !hasValidPlayers || !isTeamSolvent)
            {
                bool isVictory = isSeasonComplete && hasValidPlayers && isTeamSolvent;

                _currentSeason.Status = isVictory ? SeasonStatus.Completed : SeasonStatus.Failed;
                _currentSeason.EndingBudget = team.Budget;
                _repository.UpdateSeason(_currentSeason);

                return true;
            }

            return false;
        }


        public SeasonSummary GetSeasonSummary()
        {
            if (_currentSeason == null)
                throw new InvalidOperationException("No active season");

            var team = GetCurrentTeam();
            return new SeasonSummary
            {
                DaysRemaining = (_currentSeason.EndDate - _repository.CurrentDate).Days,
                StartingBudget = _currentSeason.StartingBudget,
                CurrentBudget = team.Budget,
                TotalPlayers = team.Players.Count,
                HealthyPlayers = team.Players.Count(p => p.PhysicalCondition == PhysicalCondition.Healthy),
                TopPerformers = team.Players.Count(p => p.Performance == Performance.High),
                Wins = _currentSeason.Wins,
                Draws = _currentSeason.Draws,
                Losses = _currentSeason.Losses,
                EndDate = _currentSeason.EndDate
            };
        }


        public void UpdateSeasonStats(MatchResult result)
        {
            if (_currentSeason == null)
                throw new InvalidOperationException("No active season");

            _currentSeason.TotalMatches++;
            switch (result)
            {
                case MatchResult.Win:
                    _currentSeason.Wins++;
                    break;
                case MatchResult.Draw:
                    _currentSeason.Draws++;
                    break;
                case MatchResult.Loss:
                    _currentSeason.Losses++;
                    break;
            }

            _repository.UpdateSeason(_currentSeason);
        }

        public IEnumerable<TransferOffer> GetPendingTransferOffers()
        {
            var team = GetCurrentTeam();
            return _repository.GetTransferOffersForTeam(team.Id)
                .Where(o => o.Status == TransferStatus.Pending);
        }


        public IEnumerable<Player> GetAvailableTransferTargets()
        {
            return _repository.GetTransferListedPlayers();
        }


        public IEnumerable<Team> GetAllTeams()
        {
            return _repository.GetAllTeams();
        }


        private static Position ParsePosition(string position) =>
            position?.ToLower() switch
            {
                "védő" => Position.Defender,
                "támadó" => Position.Forward,
                "középpályás" => Position.Midfielder,
                "kapus" => Position.Goalkeeper,
                _ => Position.Forward
            };


        private static Performance ParsePerformance(string performance) =>
            performance.ToLower() switch
            {
                "magas" => Performance.High,
                "közepes" => Performance.Medium,
                "alacsony" => Performance.Low,
                "kritikus" => Performance.Critical,
                _ => Performance.Medium
            };


        private static PhysicalCondition ParseCondition(string condition) =>
            condition?.ToLower() switch
            {
                "egészséges" => PhysicalCondition.Healthy,
                "sérült" => PhysicalCondition.Injured,
                _ => PhysicalCondition.Healthy
            };


        public void PerformDailyEvaluation()
        {
            var team = GetCurrentTeam();
            if (team == null)
                throw new InvalidOperationException("No team selected");

            Console.WriteLine($"Processing tasks for: {_repository.CurrentDate:yyyy-MM-dd}");

            var todaysTasks = _repository.GetTasksForTeam(team.Id)
                .Where(t => t.StartTime.Date == _repository.CurrentDate.Date && !t.Result.HasValue);

            foreach (var task in todaysTasks)
            {
                Console.WriteLine($"Processing task: {task.Name}, Type: {task.Type}");
                switch (task.Type)
                {
                    case TaskType.Match:
                        SimulateMatch(task);
                        break;
                    case TaskType.Training:
                        SimulateTraining(task);
                        break;
                }
            }

            decimal dailySalaries = team.Players.Sum(p => p.WeeklySalary) / 7;
            team.Budget -= dailySalaries;

            foreach (var player in team.Players.ToList())
            {
                if (player.PhysicalCondition == PhysicalCondition.Injured)
                {
                    if (random.Next(100) < 10)
                    {
                        player.PhysicalCondition = PhysicalCondition.Healthy;
                        _repository.UpdatePlayer(player);
                    }
                }
                int chance = random.Next(100);
                if (chance < 5)
                {
                    if (chance < 2 && player.Performance > Performance.Critical)
                        player.Performance--;
                    else if (chance >= 2 && player.Performance < Performance.High)
                        player.Performance++;
                    _repository.UpdatePlayer(player);
                }
            }

            _repository.UpdateTeam(team);

            if (_currentSeason != null)
            {
                _currentSeason.EndingBudget = team.Budget;
                _repository.UpdateSeason(_currentSeason);
            }

            _repository.SetCurrentDate(_repository.CurrentDate.AddDays(1));

            if (_repository.CurrentDate >= _currentSeason.EndDate)
            {
                _currentSeason.Status = SeasonStatus.Completed;
                _repository.UpdateSeason(_currentSeason);
            }
        }


        private void SimulateMatch(TeamTask match)
        {
            var result = random.Next(3) switch
            {
                0 => MatchResult.Loss,
                1 => MatchResult.Draw,
                2 => MatchResult.Win,
                _ => MatchResult.Draw
            };

            match.Result = result;
            match.GoalsScored = result switch
            {
                MatchResult.Win => random.Next(1, 5),
                MatchResult.Draw => random.Next(0, 3),
                MatchResult.Loss => random.Next(0, 2),
                _ => 0
            };
            match.GoalsConceded = result switch
            {
                MatchResult.Win => match.GoalsScored - random.Next(1, 3),
                MatchResult.Draw => match.GoalsScored,
                MatchResult.Loss => match.GoalsScored + random.Next(1, 3),
                _ => 0
            };

            var affectedPlayers = new List<Player>();
            foreach (var playerTask in match.PlayerTasks)
            {
                var player = playerTask.Player;
                var oldPerformance = player.Performance;
                UpdatePlayerPerformance(player, result);

                if (oldPerformance != player.Performance)
                {
                    OnPlayerPerformanceChanged(new PlayerPerformanceChangedEventArgs(player, oldPerformance, player.Performance));
                }

                if (random.Next(100) < 30)
                {
                    player.PhysicalCondition = PhysicalCondition.Injured;
                    _repository.UpdatePlayer(player);
                    OnPlayerInjured(new PlayerInjuryEventArgs(player, _repository.CurrentDate));
                }

                affectedPlayers.Add(player);
            }

            _repository.UpdateTask(match);
            UpdateSeasonStats(result);

            OnTaskCompleted(new TaskCompletedEventArgs(match, true, affectedPlayers));
        }


        private void SimulateTraining(TeamTask training)
        {
            bool isSuccessful = random.Next(100) < 70;

            var affectedPlayers = new List<Player>();
            foreach (var playerTask in training.PlayerTasks)
            {
                var player = playerTask.Player;
                var oldPerformance = player.Performance;

                foreach (var skill in player.Skills)
                {
                    if (isSuccessful)
                    {
                        skill.Value = Math.Min(100, skill.Value + random.Next(1, 3));
                    }
                    else
                    {
                        skill.Value = Math.Max(0, skill.Value - random.Next(1));
                    }
                    _repository.UpdateSkill(skill);
                }

                if (oldPerformance != player.Performance)
                {
                    OnPlayerPerformanceChanged(new PlayerPerformanceChangedEventArgs(player, oldPerformance, player.Performance));
                }

                if (random.Next(100) < 10)
                {
                    player.PhysicalCondition = PhysicalCondition.Injured;
                    _repository.UpdatePlayer(player);
                    OnPlayerInjured(new PlayerInjuryEventArgs(player, _repository.CurrentDate));
                }

                affectedPlayers.Add(player);
            }

            training.Result = isSuccessful ? MatchResult.Win : MatchResult.Loss;

            _repository.UpdateTask(training);

            OnTaskCompleted(new TaskCompletedEventArgs(training, isSuccessful, affectedPlayers));
        }


        public void CancelTransferOffer(int offerId)
        {
            var offer = _repository.GetTransferOffer(offerId);
            if (offer == null)
                throw new InvalidOperationException("Transfer offer not found");

            var currentTeam = GetCurrentTeam();
            if (offer.FromTeamId != currentTeam.Id)
                throw new InvalidOperationException("Can only cancel your own offers");

            offer.Status = TransferStatus.Cancelled;
            offer.ResponseDate = _repository.CurrentDate;
            _repository.UpdateTransferOffer(offer);
        }

        public void UpdatePlayerContract(int playerId, DateTime newEndDate, decimal newWeeklySalary)
        {
            if (newWeeklySalary < 0)
                throw new ArgumentException("Salary cannot be negative");

            var player = _repository.GetPlayer(playerId);
            var team = GetCurrentTeam();

            if (player == null || player.TeamId != team.Id)
                throw new InvalidOperationException("Player not found or not in your team");

            decimal salaryIncrease = newWeeklySalary - player.WeeklySalary;
            decimal yearlyIncrease = salaryIncrease * 52;
            if (team.Budget < yearlyIncrease)
                throw new InvalidOperationException("Insufficient budget for salary increase");

            player.ContractEnd = newEndDate;
            player.WeeklySalary = newWeeklySalary;
            player.Status = PlayerStatus.Active;

            _repository.UpdatePlayer(player);

            team.Budget -= yearlyIncrease;
            _repository.UpdateTeam(team);
        }


        public IEnumerable<TaskJson> GetAvailableTrainingTasks()
        {
            return _availableTasks.Values;
        }


        public void ScheduleTraining(int trainingId, DateTime startTime)
        {
            if (!_availableTasks.TryGetValue(trainingId, out var taskTemplate))
                throw new InvalidOperationException("Invalid training ID");

            var team = GetCurrentTeam();
            if (team.Budget < taskTemplate.Requirements["money"])
                throw new InvalidOperationException($"Insufficient budget. Need ${taskTemplate.Requirements["money"]}");

            if (team.StaffCount < taskTemplate.Requirements["staff"])
                throw new InvalidOperationException($"Insufficient staff. Need {taskTemplate.Requirements["staff"]} staff members");

            var task = new TeamTask
            {
                Name = taskTemplate.Name,
                Type = TaskType.Training,
                Duration = taskTemplate.Duration,
                StartTime = startTime,
                TeamId = team.Id,
                Description = $"Success Chance: {taskTemplate.SuccessChance}"
            };

            team.Budget -= taskTemplate.Requirements["money"];
            _repository.UpdateTeam(team);
            _repository.CreateTask(task);
        }


        public IEnumerable<Player> GetTopPerformers(int count = 5)
        {
            return _repository.GetAllPlayers()
                .OrderByDescending(p => p.Skills.Average(s => s.Value))
                .Take(count);
        }


        public IEnumerable<Player> GetPlayersByPerformance(Performance performance)
        {
            return _repository.GetAllPlayers()
                .Where(p => p.Performance == performance);
        }


        public void UpdatePlayerStatus(int playerId, PlayerStatus newStatus)
        {
            var player = _repository.GetPlayer(playerId) ??
                throw new InvalidOperationException("Player not found");

            player.Status = newStatus;
            _repository.UpdatePlayer(player);
        }


        public TaskReport GetReport(string fileName)
        {
            return _reportGenerator.LoadReport(fileName);
        }

        public void GenerateReport(TaskReport report, DateTime currentDate)
        {
            _reportGenerator.GenerateReport(report, currentDate);
        }

        public IEnumerable<TeamTask> GetTasksForDate(DateTime date)
        {
            var team = GetCurrentTeam();
            return _repository.GetTasksForTeam(team.Id)
                .Where(t => t.StartTime.Date == date.Date);
        }

        public List<TransferOffer> GetTeamsPendingTransferOffers(int teamId)
        {
            return _repository.GetPendingTransferOffers(teamId);
        }

        public void AcceptTransferOffer(int transferOfferId)
        {
            var offer = _repository.GetTransferOfferWithDetails(transferOfferId);
            if (offer != null)
            {
                offer.Status = TransferStatus.Accepted;
                offer.ResponseDate = CurrentDate;

                var player = _repository.GetPlayer(offer.PlayerId);
                player.TeamId = offer.ToTeamId;
                player.WeeklySalary = offer.OfferedWeeklySalary;
                var fromTeam = _repository.GetTeam(offer.FromTeamId);
                var toTeam = _repository.GetTeam(offer.ToTeamId);

                if (fromTeam.Players != null)
                    fromTeam.Players.Remove(player);
                if (toTeam.Players != null)
                    toTeam.Players.Add(player);

                fromTeam.Budget -= offer.OfferedAmount;
                toTeam.Budget += offer.OfferedAmount;

                _repository.SaveChanges();
            }
        }

        public void RejectTransferOffer(int transferOfferId)
        {
            var offer = _repository.GetTransferOffer(transferOfferId);
            if (offer != null)
            {
                offer.Status = TransferStatus.Rejected;
                offer.ResponseDate = _repository.CurrentDate;
                _repository.SaveChanges();
            }
        }

        protected virtual void OnPlayerPerformanceChanged(PlayerPerformanceChangedEventArgs e)
        {
            PlayerPerformanceChanged?.Invoke(this, e);
        }

        protected virtual void OnPlayerInjured(PlayerInjuryEventArgs e)
        {
            PlayerInjured?.Invoke(this, e);
        }


        public virtual void OnTaskCompleted(TaskCompletedEventArgs e)
        {
            TaskCompleted?.Invoke(this, e);

            var report = new TaskReport
            {
                TaskName = e.Task.Name,
                TeamName = e.Task.Team.Name,
                ExecutionDate = _repository.CurrentDate,
                Success = e.Success,
                RemainingBudget = e.Task.Team.Budget,
                AffectedPlayers = e.AffectedPlayers.Select(p => p.Name).ToList()
            };

            _reportGenerator.GenerateReport(report, _repository.CurrentDate);
        }

    }
}

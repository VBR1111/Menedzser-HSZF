using ConsoleTools;
using Menedzser_HSZF_2024251.Application;
using Menedzser_HSZF_2024251.Model;
using Menedzser_HSZF_2024251.Persistence.MsSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;


namespace Menedzser_HSZF_2024251.Console
{
    public class Program
    {
        private static FootballManager _manager;
        private static readonly string _reportPath = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName ?? string.Empty, "reports");
        private static readonly string _dataPath = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName ?? string.Empty, "data.json");


        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<FootballDbContext>();
                    services.AddSingleton<IXmlReportGenerator>(sp => new XmlReportGenerator(_reportPath));
                    services.AddSingleton<IFootballRepository, FootballRepository>();
                    services.AddSingleton<FootballManager>();
                })
                .Build();

            await host.StartAsync();

            using (var scope = host.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                _manager = serviceProvider.GetRequiredService<FootballManager>();

                // Configure event handlers
                ConfigureEventHandlers();

                // Create reports directory and load initial data
                Directory.CreateDirectory(_reportPath);
                LoadInitialData();

                // Start the application flow
                SelectTeamAtStart();
                StartNewSeason();
                MainMenu();
            }

            await host.StopAsync();
        }

        private static void ConfigureEventHandlers()
        {
            _manager.PlayerPerformanceChanged += (sender, e) =>
            {
                System.Console.WriteLine($"\nPerformance changed for {e.Player.Name}: {e.OldPerformance} -> {e.NewPerformance}");
            };

            _manager.PlayerInjured += (sender, e) =>
            {
                System.Console.WriteLine($"\n{e.Player.Name} got injured!");
            };

            _manager.TaskCompleted += (sender, e) =>
            {
                if (e.Task.Type == TaskType.Match)
                {
                    System.Console.WriteLine($"\nMatch completed: {e.Task.Name}");
                    System.Console.WriteLine($"Result: {e.Task.Result}");
                    System.Console.WriteLine($"Score: {e.Task.GoalsScored}-{e.Task.GoalsConceded}");
                }
                else
                {
                    System.Console.WriteLine($"\nTraining completed: {e.Task.Name}");
                    System.Console.WriteLine($"Success: {e.Success}");
                }
                System.Console.WriteLine("Affected players:");
                foreach (var player in e.AffectedPlayers)
                {
                    System.Console.WriteLine($"- {player.Name}");
                }
            };
        }

        private static void SelectTeamAtStart()
        {
            while (true)
            {
                System.Console.Clear();
                System.Console.WriteLine("Welcome to Football Manager!");
                System.Console.WriteLine("\nYou need to select a team to start:");
                System.Console.WriteLine("1. Create New Team");
                System.Console.WriteLine("2. Select Existing Team");
                System.Console.WriteLine("3. Exit");

                switch (System.Console.ReadLine())
                {
                    case "1":
                        if (CreateNewTeam())
                        {
                            var team = _manager.GetCurrentTeam();
                            System.Console.Write("\nHow much player do you want to add: ");
                            if (!int.TryParse(System.Console.ReadLine(), out int playerCount))
                            {
                                System.Console.WriteLine("Invalid player count!");
                                return;
                            }

                            for (int i = 0; i < playerCount; i++)
                            {
                                AddPlayer();
                            }
                            return;
                        }
                        break;
                    case "2":
                        if (SelectExistingTeam())
                            return;
                        break;
                    case "3":
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static bool CreateNewTeam()
        {
            System.Console.WriteLine("\nYou have to add enough player and staff to the team to be a valid one!");

            try
            {
                System.Console.Write("\nTeam Name: ");
                string name = System.Console.ReadLine();

                System.Console.Write("Initial Budget: ");
                if (!decimal.TryParse(System.Console.ReadLine(), out decimal budget))
                {
                    System.Console.WriteLine("Invalid budget amount!");
                    return false;
                }

                System.Console.Write("Staff Count: ");
                if (!int.TryParse(System.Console.ReadLine(), out int staffCount))
                {
                    System.Console.WriteLine("Invalid staff count!");
                    return false;
                }

                _manager.CreateTeam(name, budget, staffCount);

                System.Console.WriteLine("\nTeam created successfully!");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError creating team: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                return false;
            }
        }

        private static bool SelectExistingTeam()
        {
            try
            {
                System.Console.Clear();

                var teams = _manager.GetAllTeams();
                if (!teams.Any())
                {
                    System.Console.WriteLine("\nNo teams available. Please create a new team.");
                    System.Console.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    return false;
                }

                System.Console.WriteLine("\nAvailable Teams:\n");
                foreach (var team in teams)
                {
                    System.Console.WriteLine($"ID: {team.Id}");
                    System.Console.WriteLine($"Name: {team.Name}");
                    System.Console.WriteLine($"Budget: ${team.Budget}");
                    System.Console.WriteLine($"Players: {team.Players.Count}");
                    System.Console.WriteLine("-------------------");
                }

                System.Console.Write("\nEnter Team ID to select: ");
                if (!int.TryParse(System.Console.ReadLine(), out int teamId))
                {
                    System.Console.WriteLine("Invalid match ID! Please enter a number.");
                    return false;
                }
                _manager.SelectTeam(teamId);

                System.Console.WriteLine("\nTeam selected successfully!");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError selecting team: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                return false;
            }
        }


        private static void MainMenu()
        {
            try
            {
                _manager.GetCurrentTeam();
            }
            catch (InvalidOperationException)
            {
                System.Console.WriteLine("No team selected. Please select a team first.");
                SelectTeamAtStart();
            }

            while (true)
            {
                if (_manager.CheckGameOver())
                {
                    ShowGameOverScreen();
                    return;
                }

                DisplaySeasonStatus();

                System.Console.Clear();

                System.Console.WriteLine($"\nCurrent Date: {_manager.CurrentDate:yyyy-MM-dd}");
                System.Console.WriteLine($"Current Team: {_manager.GetCurrentTeam().Name}");
                System.Console.WriteLine("\nFootball Manager Menu:");

                new ConsoleMenu(args: new string[0], level: 0)
                    .Add("Team Management", () => TeamManagement())
                    .Add("Player Management", () => PlayerManagement())
                    .Add("Training Management", () => TrainingManagement())
                    .Add("Match Management", () => MatchManagement())
                    .Add("Current Team Performance Reports", () => ViewPerformanceReports())
                    .Add("View XML Reports", () => ViewReports())
                    .Add("View Season Summary", () => ViewSeasonSummary())
                    .Add("Queries and Statistics", () => QueriesAndStats())
                    .Add("End Day", () => EndDay())
                    .Add("Exit", ConsoleMenu.Close)
                    .Configure(config =>
                    {
                        config.Selector = "--> ";
                        config.EnableFilter = false;
                        config.Title = "Football Manager Main Menu";
                        config.EnableBreadcrumb = true;
                        config.WriteHeaderAction = () =>
                        {
                            var team = _manager.GetCurrentTeam();
                            System.Console.WriteLine(new string('=', System.Console.WindowWidth - 1));
                            System.Console.WriteLine($"Current Date: {_manager.CurrentDate:yyyy-MM-dd}");
                            System.Console.WriteLine($"Current Team: {team.Name}");
                            System.Console.WriteLine($"Budget: ${team.Budget:N2}");
                            System.Console.WriteLine(new string('=', System.Console.WindowWidth - 1));
                            System.Console.WriteLine();
                        };
                        config.WriteBreadcrumbAction = titles => System.Console.WriteLine(string.Join(" / ", titles));
                    }).Show();
            }
        }

        private static void LoadInitialData()
        {
            var data = JsonDataHandler.LoadData(_dataPath);
            System.Console.WriteLine(data);
            _manager.InitializeFromJson(data);
        }


        private static void TeamManagement()
        {
            new ConsoleMenu(args: new string[0], level: 1)
                .Add("Create New Team", () => {
                    CreateNewTeam();

                    var team = _manager.GetCurrentTeam();
                    System.Console.Write("\nHow much player do you want to add: ");
                    if (!int.TryParse(System.Console.ReadLine(), out int playerCount))
                    {
                        System.Console.WriteLine("Invalid player count!");
                        return;
                    }

                    for (int i = 0; i < playerCount; i++)
                    {
                        AddPlayer();
                    }
                })
                .Add("List Teams", () => ListTeams())
                .Add("Select Team", () => SelectTeam())
                .Add("View Team Details", () => ViewTeamDetails())
                .Add("Update Team Budget", () => UpdateTeamBudget())
                .Add("Back", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Selector = "--> ";
                    config.EnableFilter = false;
                    config.Title = "Team Management";
                    config.EnableBreadcrumb = true;
                }).Show();
        }


        private static void ViewSeasonSummary()
        {
            try
            {
                var summary = _manager.GetSeasonSummary();
                System.Console.Clear();
                System.Console.WriteLine("Season Summary");
                System.Console.WriteLine("-------------");
                System.Console.WriteLine($"Days Remaining: {summary.DaysRemaining}");
                System.Console.WriteLine($"\nFinancial Summary:");
                System.Console.WriteLine($"Starting Budget: ${summary.StartingBudget}");
                System.Console.WriteLine($"Current Budget: ${summary.CurrentBudget}");
                System.Console.WriteLine($"Budget Change: ${summary.CurrentBudget - summary.StartingBudget}");

                System.Console.WriteLine($"\nTeam Summary:");
                System.Console.WriteLine($"Total Players: {summary.TotalPlayers}");
                System.Console.WriteLine($"Healthy Players: {summary.HealthyPlayers}");
                System.Console.WriteLine($"Top Performers: {summary.TopPerformers}");

                System.Console.WriteLine($"\nResults:");
                System.Console.WriteLine($"Wins: {summary.Wins}");
                System.Console.WriteLine($"Draws: {summary.Draws}");
                System.Console.WriteLine($"Losses: {summary.Losses}");
                System.Console.WriteLine($"Win Rate: {(summary.TotalPlayers > 0 ? (summary.Wins * 100.0 / summary.TotalPlayers).ToString("F1") : "0")}%");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing season summary: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ShowGameOverScreen()
        {
            System.Console.Clear();
            var summary = _manager.GetSeasonSummary();
            var team = _manager.GetCurrentTeam();
            var isVictory = summary.IsVictory;

            System.Console.ForegroundColor = isVictory ? ConsoleColor.Green : ConsoleColor.Red;
            System.Console.WriteLine(isVictory ? "Congratulations! You've Won!" : "Game Over!");
            System.Console.WriteLine("======================");
            System.Console.ResetColor();

            System.Console.WriteLine($"\nTeam: {team.Name}");
            System.Console.WriteLine($"\nSeason Statistics:");
            System.Console.WriteLine($"Starting Budget: ${summary.StartingBudget:N2}");
            System.Console.WriteLine($"Final Budget: ${summary.CurrentBudget:N2}");
            System.Console.WriteLine($"Budget Change: ${summary.CurrentBudget - summary.StartingBudget:N2}");

            System.Console.WriteLine($"\nMatch Results:");
            System.Console.WriteLine($"Wins: {summary.Wins}");
            System.Console.WriteLine($"Draws: {summary.Draws}");
            System.Console.WriteLine($"Losses: {summary.Losses}");
            System.Console.WriteLine($"Win Rate: {(summary.TotalMatches > 0 ? (double)summary.Wins / summary.TotalMatches * 100 : 0):F1}%");

            System.Console.WriteLine($"\nTeam Status:");
            System.Console.WriteLine($"Total Players: {summary.TotalPlayers}");
            System.Console.WriteLine($"Healthy Players: {summary.HealthyPlayers}");
            System.Console.WriteLine($"Top Performers: {summary.TopPerformers}");

            if (isVictory)
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("\nVictory Conditions Met:");
                System.Console.WriteLine("✓ Completed full season");
                System.Console.WriteLine("✓ Maintained positive budget");
                System.Console.WriteLine("✓ Kept players performing above critical level");
                System.Console.ResetColor();
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("\nGame Over Reason:");
                if (team.Budget <= 0)
                    System.Console.WriteLine("✗ Team went bankrupt!");
                if (!team.Players.Any(p => p.Performance > Performance.Critical))
                    System.Console.WriteLine("✗ All players performing at critical level!");
                if (_manager.CurrentDate < summary.EndDate)
                    System.Console.WriteLine("✗ Season not completed!");
                System.Console.ResetColor();
            }

            var finalReport = new TaskReport
            {
                TaskName = "Season End Report",
                TeamName = team.Name,
                ExecutionDate = _manager.CurrentDate,
                Success = isVictory,
                RemainingBudget = team.Budget,
                AffectedPlayers = team.Players.Select(p =>
                    $"{p.Name} - {p.Performance} - {p.PhysicalCondition}").ToList()
            };

            _manager.GenerateReport(finalReport, _manager.CurrentDate);

            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
            Environment.Exit(0);
        }


        private static void StartNewSeason()
        {
            try
            {
                System.Console.Clear();
                System.Console.WriteLine("Starting New Season...");

                _manager.StartNewSeason();

                System.Console.WriteLine("\nNew season started successfully!");
                System.Console.WriteLine("\nInitial Status:");
                var summary = _manager.GetSeasonSummary();
                System.Console.WriteLine($"Starting Budget: ${summary.StartingBudget}");
                System.Console.WriteLine($"Total Players: {summary.TotalPlayers}");
                System.Console.WriteLine($"Healthy Players: {summary.HealthyPlayers}");
                System.Console.WriteLine($"Days Until Season End: {summary.DaysRemaining}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError starting new season: {ex.Message}");
            }

            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void DisplaySeasonStatus()
        {
            try
            {
                var summary = _manager.GetSeasonSummary();
                var team = _manager.GetCurrentTeam();

                System.Console.WriteLine($"Team: {team.Name}");
                System.Console.WriteLine($"Current Budget: ${team.Budget}");
                System.Console.WriteLine($"Days Remaining in Season: {summary.DaysRemaining}");
                System.Console.WriteLine($"Record: W{summary.Wins}-D{summary.Draws}-L{summary.Losses}");
            }
            catch (InvalidOperationException)
            {
                System.Console.WriteLine("No active season. Start a new season to begin!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error displaying season status: {ex.Message}");
            }
        }


        private static void ViewPerformanceReports()
        {
            try
            {
                System.Console.Clear();
                var team = _manager.GetCurrentTeam();
                System.Console.WriteLine($"\nTeam Performance Report for {team.Name}");

                var stats = _manager.GetTeamStatistics();
                System.Console.WriteLine($"\nMatch Statistics:");
                System.Console.WriteLine($"Wins: {stats.Wins}");
                System.Console.WriteLine($"Draws: {stats.Draws}");
                System.Console.WriteLine($"Losses: {stats.Losses}");
                System.Console.WriteLine($"Goals Scored: {stats.GoalsScored}");
                System.Console.WriteLine($"Goals Conceded: {stats.GoalsConceded}");

                System.Console.WriteLine($"\nPlayer Performances:");
                foreach (var player in team.Players)
                {
                    System.Console.WriteLine($"{player.Name}: {player.Performance} - {player.PhysicalCondition}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing performance reports: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ViewReports()
        {
            try
            {
                var reports = Directory.GetFiles(_reportPath, "*.xml");
                System.Console.Clear();
                if (reports.Length == 0)
                {
                    System.Console.WriteLine("No reports found.");
                }
                else
                {
                    foreach (var reportPath in reports)
                    {
                        var report = _manager.GetReport(reportPath);
                        System.Console.WriteLine($"\nReport: {Path.GetFileName(reportPath)}");
                        System.Console.WriteLine($"Task: {report.TaskName}");
                        System.Console.WriteLine($"Team: {report.TeamName}");
                        System.Console.WriteLine($"Date: {report.ExecutionDate}");
                        System.Console.WriteLine($"Success: {report.Success}");
                        System.Console.WriteLine($"Remaining Budget: {report.RemainingBudget}");
                        System.Console.WriteLine("Affected Players:");
                        foreach (var player in report.AffectedPlayers)
                        {
                            System.Console.WriteLine($"- {player}");
                        }
                        System.Console.WriteLine("-------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing reports: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void QueriesAndStats()
        {
            new ConsoleMenu(args: new string[0], level: 1)
            .Add("Top Performers", () => ShowTopPerformers())
            .Add("Players by Performance Level", () => ShowPlayersByPerformance())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Queries and Statistics";
                config.EnableBreadcrumb = true;
            }).Show();
        }


        private static void ShowTopPerformers()
        {
            try
            {
                System.Console.Clear();
                System.Console.WriteLine("Top Performing Players:");
                var players = _manager.GetTopPerformers();
                DisplayPlayers(players);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ShowPlayersByPerformance()
        {
            try
            {
                System.Console.Clear();
                System.Console.WriteLine("Select Performance Level:");
                System.Console.WriteLine("1. High");
                System.Console.WriteLine("2. Medium");
                System.Console.WriteLine("3. Low");
                System.Console.WriteLine("4. Critical");

                if (int.TryParse(System.Console.ReadLine(), out int choice))
                {
                    Performance performance = choice switch
                    {
                        1 => Performance.High,
                        2 => Performance.Medium,
                        3 => Performance.Low,
                        4 => Performance.Critical,
                        _ => throw new InvalidOperationException("Invalid choice")
                    };

                    var players = _manager.GetPlayersByPerformance(performance);
                    DisplayPlayers(players);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void EndDay()
        {
            try
            {
                var previousDate = _manager.CurrentDate;
                _manager.PerformDailyEvaluation();

                System.Console.Clear();
                System.Console.WriteLine($"Date: {previousDate:yyyy-MM-dd} -> {_manager.CurrentDate:yyyy-MM-dd}");
                System.Console.WriteLine("\nEnd of Day Summary:");
                var team = _manager.GetCurrentTeam();
                System.Console.WriteLine($"Budget: ${team.Budget}");
                System.Console.WriteLine($"Daily Wages Paid: ${team.DailyWages}");
                System.Console.WriteLine($"Healthy Players: {team.HealthyPlayerCount}");
                System.Console.WriteLine($"Injured Players: {team.InjuredPlayerCount}");
                System.Console.WriteLine($"Win Rate: {team.WinRate:P1}");

                var todaysTasks = _manager.GetTasksForDate(previousDate)
                    .Where(t => t.StartTime.Date == previousDate.Date);

                if (todaysTasks.Any())
                {
                    System.Console.WriteLine("\nToday's Results:");
                    foreach (var task in todaysTasks)
                    {
                        System.Console.WriteLine($"{task.Name}: {(task.Result?.ToString() ?? "Not completed")}");
                        if (task.Type == TaskType.Match && task.Result.HasValue)
                        {
                            System.Console.WriteLine($"Score: {task.GoalsScored}-{task.GoalsConceded}");
                        }
                    }
                }

                System.Console.WriteLine("\nPress any key to continue to next day...");
                System.Console.ReadKey();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error during daily evaluation: {ex.Message}");
                System.Console.WriteLine("\nPress any key to continue...");
                System.Console.ReadKey();
            }
        }


        private static void ListTeams()
        {
            try
            {
                var teams = _manager.GetAllTeams();
                System.Console.Clear();
                System.Console.WriteLine("Available Teams:\n");

                foreach (var team in teams)
                {
                    System.Console.WriteLine($"ID: {team.Id}");
                    System.Console.WriteLine($"Name: {team.Name}");
                    System.Console.WriteLine($"Budget: ${team.Budget}");
                    System.Console.WriteLine($"Staff Count: {team.StaffCount}");
                    System.Console.WriteLine($"Players: {team.Players.Count}");
                    System.Console.WriteLine("-------------------");
                }

                if (!teams.Any())
                {
                    System.Console.WriteLine("No teams found.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error listing teams: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void DisplayAvailableTeams()
        {
            System.Console.Clear();
            System.Console.WriteLine("Available Teams:");
            System.Console.WriteLine("---------------");

            var teams = _manager.GetAllTeams();
            foreach (var team in teams)
            {
                System.Console.WriteLine($"\nID: {team.Id}");
                System.Console.WriteLine($"Name: {team.Name}");
                System.Console.WriteLine($"Budget: ${team.Budget}");
                System.Console.WriteLine($"Staff: {team.StaffCount}");
                System.Console.WriteLine($"Healthy Players: {team.Players.Count(p => p.PhysicalCondition == PhysicalCondition.Healthy)}");
                System.Console.WriteLine($"Total Players: {team.Players.Count}");
                System.Console.WriteLine("Star Players:");
                foreach (var player in team.Players.Where(p => p.Performance == Performance.High).Take(3))
                {
                    System.Console.WriteLine($"- {player.Name} ({player.Position})");
                }
                System.Console.WriteLine("-------------------");
            }
        }


        private static void SelectTeam()
        {
            try
            {
                DisplayAvailableTeams();

                System.Console.Write("\nEnter Team ID to select: ");
                if (!int.TryParse(System.Console.ReadLine(), out int teamId))
                {
                    System.Console.WriteLine("Invalid team ID!");
                    return;
                }

                _manager.SelectTeam(teamId);
                System.Console.WriteLine("\nTeam selected successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ViewTeamDetails()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                System.Console.Clear();
                System.Console.WriteLine($"Team Details for: {team.Name}");
                System.Console.WriteLine($"Budget: ${team.Budget}");
                System.Console.WriteLine($"Staff Count: {team.StaffCount}");

                System.Console.WriteLine("\nPlayers:");
                foreach (var player in team.Players)
                {
                    System.Console.WriteLine($"- {player.Name}");
                    System.Console.WriteLine($"  Position: {player.Position}");
                    System.Console.WriteLine($"  Performance: {player.Performance}");
                    System.Console.WriteLine($"  Condition: {player.PhysicalCondition}");

                    if (player.Skills.Any())
                    {
                        System.Console.WriteLine($"  Skills:");
                        foreach (var skill in player.Skills)
                        {
                            System.Console.WriteLine($"    {skill.Name}: {skill.Value}");
                        }
                    }
                    System.Console.WriteLine();
                }

                System.Console.WriteLine("\nScheduled Tasks:");
                foreach (var task in team.Tasks.OrderBy(t => t.StartTime))
                {
                    System.Console.WriteLine($"- {task.Name}");
                    System.Console.WriteLine($"  Type: {task.Type}");
                    System.Console.WriteLine($"  Date: {task.StartTime}");
                    System.Console.WriteLine($"  Duration: {task.Duration} minutes\n");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing team details: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void UpdateTeamBudget()
        {
            try
            {
                System.Console.Clear();
                var team = _manager.GetCurrentTeam();
                System.Console.WriteLine($"Current budget: ${team.Budget}");

                System.Console.Write("Enter new budget amount: $");
                if (decimal.TryParse(System.Console.ReadLine(), out decimal newBudget))
                {
                    if (newBudget < 0)
                    {
                        System.Console.WriteLine("Budget cannot be negative!");
                        return;
                    }

                    _manager.UpdateTeamBudget(team.Id, newBudget);
                    System.Console.WriteLine($"Budget updated successfully to ${newBudget}");
                }
                else
                {
                    System.Console.WriteLine("Invalid amount entered!");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error updating budget: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void TrainingManagement()
        {
            new ConsoleMenu(args: new string[0], level: 1)
            .Add("Schedule Training", () => ScheduleTraining())
            .Add("View Training Schedule", () => ViewTrainingSchedule())
            .Add("Evaluate Training Results", () => EvaluateTrainingResults())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Training Management";
                config.EnableBreadcrumb = true;
            }).Show();
        }


        private static void ScheduleTraining()
        {
            try
            {
                System.Console.Clear();
                System.Console.WriteLine("Available Training Types:");
                System.Console.WriteLine("------------------------");

                var tasks = _manager.GetAvailableTrainingTasks();
                foreach (var task in tasks)
                {
                    System.Console.WriteLine($"\nID: {task.TaskId}");
                    System.Console.WriteLine($"Name: {task.Name}");
                    System.Console.WriteLine($"Duration: {task.Duration} minutes");
                    System.Console.WriteLine($"Success Chance: {task.SuccessChance}");
                    System.Console.WriteLine("Impact:");
                    foreach (var impact in task.Impact)
                    {
                        if (impact.Key != "injury_chance")
                            System.Console.WriteLine($"  - {impact.Key}: {impact.Value}");
                    }
                    System.Console.WriteLine($"Injury Risk: {task.Impact.GetValueOrDefault("injury_chance", "0%")}");
                    System.Console.WriteLine("Requirements:");
                    System.Console.WriteLine($"  - Staff needed: {task.Requirements["staff"]}");
                    System.Console.WriteLine($"  - Cost: ${task.Requirements["money"]}");
                    System.Console.WriteLine("-------------------");
                }

                System.Console.Write("\nEnter Training ID (or press Enter to cancel): ");
                if (!int.TryParse(System.Console.ReadLine(), out int trainingId))
                {
                    System.Console.WriteLine("Invalid training ID!");
                    return;
                }

                System.Console.WriteLine("\nWhen to schedule the training?");
                System.Console.WriteLine("1. Today");
                System.Console.WriteLine("2. Tomorrow");
                System.Console.WriteLine("3. This week");
                System.Console.WriteLine("4. Next week");
                System.Console.Write("\nSelect option (1-4): ");

                DateTime startTime = _manager.CurrentDate;
                switch (System.Console.ReadLine())
                {
                    case "1":
                        startTime = startTime.AddHours(10);
                        break;
                    case "2":
                        startTime = startTime.AddDays(1).AddHours(10);
                        break;
                    case "3":
                        System.Console.WriteLine("\nSelect day:");
                        System.Console.WriteLine("1. Monday");
                        System.Console.WriteLine("2. Tuesday");
                        System.Console.WriteLine("3. Wednesday");
                        System.Console.WriteLine("4. Thursday");
                        System.Console.WriteLine("5. Friday");
                        System.Console.WriteLine("6. Saturday");
                        System.Console.WriteLine("7. Sunday");

                        if (int.TryParse(System.Console.ReadLine(), out int dayChoice) && dayChoice >= 1 && dayChoice <= 7)
                        {
                            DateTime currentDay = _manager.CurrentDate;
                            var targetDay = (DayOfWeek)(dayChoice - 1);
                            int daysToAdd = ((int)targetDay - (int)currentDay.DayOfWeek + 7) % 7;
                            startTime = currentDay.AddDays(daysToAdd).AddHours(10);
                        }
                        else
                        {
                            System.Console.WriteLine("Invalid day selection!");
                            return;
                        }
                        break;
                    case "4":
                        System.Console.WriteLine("\nSelect day next week:");
                        System.Console.WriteLine("1. Monday");
                        System.Console.WriteLine("2. Tuesday");
                        System.Console.WriteLine("3. Wednesday");
                        System.Console.WriteLine("4. Thursday");
                        System.Console.WriteLine("5. Friday");
                        System.Console.WriteLine("6. Saturday");
                        System.Console.WriteLine("7. Sunday");

                        if (int.TryParse(System.Console.ReadLine(), out int nextWeekDayChoice) && nextWeekDayChoice >= 1 && nextWeekDayChoice <= 7)
                        {
                            DateTime currentDay = _manager.CurrentDate;
                            var targetDay = (DayOfWeek)(nextWeekDayChoice - 1);
                            int daysToAdd = ((int)targetDay - (int)currentDay.DayOfWeek + 7) % 7 + 7;
                            startTime = currentDay.AddDays(daysToAdd).AddHours(10);
                        }
                        else
                        {
                            System.Console.WriteLine("Invalid day selection!");
                            return;
                        }
                        break;
                    default:
                        System.Console.WriteLine("Invalid option!");
                        return;
                }

                _manager.ScheduleTraining(trainingId, startTime);
                System.Console.WriteLine($"\nTraining scheduled for {startTime:yyyy-MM-dd HH:mm}");
                System.Console.WriteLine("Training scheduled successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ViewTrainingSchedule()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                var trainings = team.Tasks
                    .Where(t => t.Type == TaskType.Training)
                    .OrderBy(t => t.StartTime);

                System.Console.Clear();
                System.Console.WriteLine($"Training Schedule for {team.Name}:\n");

                if (!trainings.Any())
                {
                    System.Console.WriteLine("No training sessions scheduled.");
                    return;
                }

                foreach (var training in trainings)
                {
                    System.Console.WriteLine($"Training: {training.Name}");
                    System.Console.WriteLine($"Date: {training.StartTime}");
                    System.Console.WriteLine($"Duration: {training.Duration} minutes");
                    System.Console.WriteLine("Participating Players:");
                    foreach (var pt in training.PlayerTasks)
                    {
                        System.Console.WriteLine($"- {pt.Player.Name}");
                    }
                    System.Console.WriteLine("-------------------");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing training schedule: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void EvaluateTrainingResults()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                var trainings = team.Tasks
                    .Where(t => t.Type == TaskType.Training && t.StartTime.Date <= _manager.CurrentDate.Date)
                    .OrderBy(t => t.StartTime);

                if (!trainings.Any())
                {
                    System.Console.WriteLine("No completed training sessions to evaluate.");
                    System.Console.ReadKey();
                    return;
                }

                System.Console.WriteLine("Select training to evaluate:");
                foreach (var training in trainings)
                {
                    System.Console.WriteLine($"{training.Id}: {training.Name} - {training.StartTime}");
                }

                System.Console.Write("\nEnter training ID: ");
                if (!int.TryParse(System.Console.ReadLine(), out int trainingId))
                {
                    System.Console.WriteLine("Invalid training ID! Please enter a number.");
                    return;
                }

                System.Console.WriteLine("\nEnter performance impact (1: Improved, 0: No Change, -1: Declined):");
                int.TryParse(System.Console.ReadLine(), out int impact);

                _manager.EvaluateTraining(trainingId, impact);
                System.Console.WriteLine("Training evaluation completed successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error evaluating training: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void MatchManagement()
        {
             new ConsoleMenu(args: new string[0], level: 1)
            .Add("Schedule Match", () => ScheduleMatch())
            .Add("View Match Schedule", () => RecordMatchResult())
            .Add("Record Match Result", () => EvaluateTrainingResults())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Match Management";
                config.EnableBreadcrumb = true;
            }).Show();
        }


        private static void ScheduleMatch()
        {
            try
            {
                System.Console.Clear();
                System.Console.WriteLine("Schedule Match");
                System.Console.WriteLine("--------------");

                var currentTeam = _manager.GetCurrentTeam();
                var allTeams = _manager.GetAllTeams();

                System.Console.WriteLine("\nAvailable Opponent Teams:");
                foreach (var team in allTeams.Where(t => t.Id != currentTeam.Id))
                {
                    System.Console.WriteLine($"\nID: {team.Id}");
                    System.Console.WriteLine($"Name: {team.Name}");
                    System.Console.WriteLine($"Healthy Players: {team.Players.Count(p => p.PhysicalCondition == PhysicalCondition.Healthy)}");
                    System.Console.WriteLine("Key Players:");
                    foreach (var player in team.Players.Where(p => p.Performance == Performance.High).Take(3))
                    {
                        System.Console.WriteLine($"- {player.Name} ({player.Position})");
                    }
                    System.Console.WriteLine("-------------------");
                }

                System.Console.Write("\nEnter opponent team ID (or press Enter to cancel): ");
                if (!int.TryParse(System.Console.ReadLine(), out int opponentId))
                {
                    System.Console.WriteLine("Invalid team ID!");
                    return;
                }

                System.Console.WriteLine("\nWhen to schedule the match?");
                System.Console.WriteLine("1. Today");
                System.Console.WriteLine("2. Tomorrow");
                System.Console.WriteLine("3. This weekend");
                System.Console.WriteLine("4. Next weekend");
                System.Console.WriteLine("5. Select specific day");

                DateTime matchTime = _manager.CurrentDate;
                switch (System.Console.ReadLine())
                {
                    case "1":
                        matchTime = matchTime.AddHours(10);
                        break;
                    case "2":
                        matchTime = matchTime.AddDays(1).AddHours(15);
                        break;
                    case "3":
                        var nextSaturday = matchTime.AddDays((int)(DayOfWeek.Saturday - matchTime.DayOfWeek + 7) % 7);
                        matchTime = nextSaturday.AddHours(15);
                        break;
                    case "4":
                        var nextNextSaturday = matchTime.AddDays((int)(DayOfWeek.Saturday - matchTime.DayOfWeek + 14) % 14);
                        matchTime = nextNextSaturday.AddHours(15);
                        break;
                    case "5":
                        System.Console.WriteLine("\nSelect day:");
                        System.Console.WriteLine("1. Saturday");
                        System.Console.WriteLine("2. Sunday");

                        int daysToAdd = System.Console.ReadLine() switch
                        {
                            "1" => (int)(DayOfWeek.Saturday - matchTime.DayOfWeek + 7) % 7,
                            "2" => (int)(DayOfWeek.Sunday - matchTime.DayOfWeek + 7) % 7,
                            _ => throw new InvalidOperationException("Invalid day selection")
                        };
                        matchTime = matchTime.AddDays(daysToAdd).AddHours(15);
                        break;
                    default:
                        System.Console.WriteLine("Invalid option!");
                        return;
                }

                var matchTask = new TeamTask
                {
                    Name = $"Match vs {allTeams.First(t => t.Id == opponentId).Name}",
                    Type = TaskType.Match,
                    Duration = 90,
                    StartTime = matchTime,
                    Description = "Regular match"
                };

                _manager.ScheduleMatch(matchTask);
                System.Console.WriteLine($"\nMatch scheduled for {matchTime:yyyy-MM-dd HH:mm}");
                System.Console.WriteLine("Match scheduled successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ViewMatchSchedule()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                var matches = team.Tasks
                    .Where(t => t.Type == TaskType.Match)
                    .OrderBy(t => t.StartTime);

                System.Console.Clear();
                System.Console.WriteLine($"Match Schedule for {team.Name}:\n");

                if (!matches.Any())
                {
                    System.Console.WriteLine("No matches scheduled.");
                    return;
                }

                foreach (var match in matches)
                {
                    System.Console.WriteLine($"Match: {match.Name}");
                    System.Console.WriteLine($"Date: {match.StartTime}");
                    System.Console.WriteLine($"Duration: {match.Duration} minutes");

                    if (match.StartTime.Date < _manager.CurrentDate.Date)
                    {
                        System.Console.WriteLine("Result: " + (match.Result.HasValue
                            ? $"{match.Result} ({match.GoalsScored}-{match.GoalsConceded})"
                            : "Not recorded"));
                    }
                    else
                    {
                        System.Console.WriteLine("Status: Upcoming");
                    }

                    System.Console.WriteLine("Squad:");
                    foreach (var pt in match.PlayerTasks)
                    {
                        var player = pt.Player;
                        System.Console.WriteLine($"- {player.Name} ({player.Position}) - {player.Performance} - {player.PhysicalCondition}");
                    }
                    System.Console.WriteLine("-------------------");
                }

                var stats = _manager.GetTeamStatistics();
                System.Console.WriteLine("\nTeam Statistics:");
                System.Console.WriteLine($"Wins: {stats.Wins}");
                System.Console.WriteLine($"Draws: {stats.Draws}");
                System.Console.WriteLine($"Losses: {stats.Losses}");
                System.Console.WriteLine($"Goals Scored: {stats.GoalsScored}");
                System.Console.WriteLine($"Goals Conceded: {stats.GoalsConceded}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error viewing match schedule: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void RecordMatchResult()
        {
            try
            {
                System.Console.Write("Match ID: ");
                if (!int.TryParse(System.Console.ReadLine(), out int matchId))
                {
                    System.Console.WriteLine("Invalid Match ID! Please enter a number.");
                    return;
                }

                System.Console.WriteLine("Result (0:Loss, 1:Draw, 2:Win): ");
                Enum.TryParse(System.Console.ReadLine(), out MatchResult result);

                System.Console.Write("Goals Scored: ");
                int.TryParse(System.Console.ReadLine(), out int goalsScored);

                System.Console.Write("Goals Conceded: ");
                int.TryParse(System.Console.ReadLine(), out int goalsConceded);

                _manager.RecordMatchResult(matchId, result, goalsScored, goalsConceded);
                System.Console.WriteLine("Match result recorded successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error recording match result: {ex.Message}");
            }
            System.Console.ReadKey();
        }

        private static void PlayerManagement()
        {
            new ConsoleMenu(args: new string[0], level: 1)
            .Add("List Players", () => ListPlayers())
            .Add("Add Player", () => AddPlayer())
            .Add("Update Player Status", () => UpdatePlayerStatus())
            .Add("Manage Contracts", () => ManageContracts())
            .Add("Transfer Market", () => TransferMarket())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Player Management";
                config.EnableBreadcrumb = true;
            }).Show();
        }


        private static void ListPlayers()
        {
            System.Console.Clear();

            var players = _manager.GetCurrentTeam().Players;
            foreach (var player in players)
            {
                System.Console.WriteLine($"ID: {player.Id}");
                System.Console.WriteLine($"Name: {player.Name}");
                System.Console.WriteLine($"Position: {player.Position}");
                System.Console.WriteLine("-------------------");
            }
            System.Console.WriteLine("\nPress any key...");
            System.Console.ReadKey();
        }


        private static void AddPlayer()
        {
            try
            {
                System.Console.Write("Name: ");
                string name = System.Console.ReadLine();

                System.Console.WriteLine("Position (0:Goalkeeper, 1:Defender, 2:Midfielder, 3:Forward): ");
                Enum.TryParse<Position>(System.Console.ReadLine(), out Position position);

                System.Console.WriteLine("Performance (0:Critical, 1:Low, 2:Medium, 3:High): ");
                Enum.TryParse<Performance>(System.Console.ReadLine(), out Performance performance);

                System.Console.WriteLine("Physical Condition (0:Healthy, 1:Injured): ");
                Enum.TryParse<PhysicalCondition>(System.Console.ReadLine(), out PhysicalCondition condition);

                var player = new Player(_manager.CurrentDate)
                {
                    Name = name,
                    Position = position,
                    Performance = performance,
                    PhysicalCondition = condition
                };

                _manager.AddPlayer(player);
                System.Console.WriteLine("Player added successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error adding player: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void TransferMarket()
        {

            new ConsoleMenu(args: new string[0], level: 1)
            .Add("View Transfer Listed Players", () => ViewTransferListedPlayers())
            .Add("List Player for Transfer", () => ListPlayerForTransfer())
            .Add("Make Transfer Offer", () => MakeTransferOffer())
            .Add("View Pending Offers", () => ViewPendingOffers())
            .Add("Manage Transfer Offers", () => ManageTransferOffers())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Transfer Market";
                config.EnableBreadcrumb = true;
            }).Show();
        }


        private static void ViewTransferListedPlayers()
        {
            try
            {
                var players = _manager.GetAvailableTransferTargets();
                System.Console.WriteLine("\nTransfer Listed Players:");
                foreach (var player in players)
                {
                    System.Console.WriteLine($"ID: {player.Id}");
                    System.Console.WriteLine($"Name: {player.Name}");
                    System.Console.WriteLine($"Position: {player.Position}");
                    System.Console.WriteLine($"Transfer Value: ${player.TransferValue}");
                    System.Console.WriteLine($"Current Team: {player.Team.Name}");
                    System.Console.WriteLine("-------------------");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ListPlayerForTransfer()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                var availablePlayers = team.Players.Where(p => p.Status != PlayerStatus.TransferListed);

                DisplayAvailablePlayers(availablePlayers, "Players Available for Transfer Listing");

                System.Console.Write("\nEnter Player ID to list for transfer (or press Enter to cancel): ");
                if (!int.TryParse(System.Console.ReadLine(), out int playerId))
                {
                    System.Console.WriteLine("Invalid player ID!");
                    return;
                }

                System.Console.Write("Enter asking price: $");
                if (decimal.TryParse(System.Console.ReadLine(), out decimal price))
                {
                    _manager.ListPlayerForTransfer(playerId, price);
                    System.Console.WriteLine("\nPlayer listed for transfer successfully!");
                }
                else
                {
                    System.Console.WriteLine("\nInvalid price entered!");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        private static void DisplayAvailablePlayers(IEnumerable<Player> players, string title = "Available Players")
        {
            System.Console.Clear();
            System.Console.WriteLine(title);
            System.Console.WriteLine(new string('-', title.Length));

            foreach (var player in players)
            {
                System.Console.WriteLine($"\nID: {player.Id}");
                System.Console.WriteLine($"Name: {player.Name}");
                System.Console.WriteLine($"Position: {player.Position}");
                System.Console.WriteLine($"Performance: {player.Performance}");
                System.Console.WriteLine($"Condition: {player.PhysicalCondition}");
                if (player.Team != null)
                    System.Console.WriteLine($"Current Team: {player.Team.Name}");

                System.Console.WriteLine("Skills:");
                foreach (var skill in player.Skills)
                {
                    System.Console.WriteLine($"  - {skill.Name}: {skill.Value}");
                }
                System.Console.WriteLine("-------------------");
            }
        }

        private static void MakeTransferOffer()
        {
            try
            {
                var transferListedPlayers = _manager.GetAvailableTransferTargets();
                if (!transferListedPlayers.Any())
                {
                    System.Console.WriteLine("No players are currently listed for transfer.");
                    System.Console.ReadKey();
                    return;
                }

                System.Console.WriteLine("Transfer Listed Players:");
                foreach (var player in transferListedPlayers)
                {
                    System.Console.WriteLine($"ID: {player.Id} - {player.Name} ({player.Team.Name})");
                    System.Console.WriteLine($"Asking Price: ${player.TransferValue:N2}");
                    System.Console.WriteLine($"Current Salary: ${player.WeeklySalary:N2}/week\n");
                }

                System.Console.Write("\nEnter Player ID to make offer (or press Enter to cancel): ");
                if (!int.TryParse(System.Console.ReadLine(), out int playerId))
                    return;

                var selectedPlayer = transferListedPlayers.FirstOrDefault(p => p.Id == playerId);
                if (selectedPlayer == null)
                {
                    System.Console.WriteLine("Invalid player ID!");
                    return;
                }

                System.Console.WriteLine($"\nAsking Price: ${selectedPlayer.TransferValue:N2}");
                System.Console.Write("Enter transfer offer amount: $");
                if (!decimal.TryParse(System.Console.ReadLine(), out decimal offerAmount))
                {
                    System.Console.WriteLine("Invalid amount!");
                    return;
                }

                if (offerAmount < selectedPlayer.TransferValue)
                {
                    System.Console.WriteLine($"\nWarning: Your offer is below the asking price of ${selectedPlayer.TransferValue:N2}");
                    System.Console.Write("Do you want to continue with this offer? (y/n): ");
                    if (System.Console.ReadLine().ToLower() != "y")
                        return;
                }

                System.Console.Write("Enter offered weekly salary: $");
                if (!decimal.TryParse(System.Console.ReadLine(), out decimal weeklySalary))
                {
                    System.Console.WriteLine("Invalid salary!");
                    return;
                }

                try
                {
                    _manager.MakeTransferOffer(playerId, offerAmount, weeklySalary);
                    System.Console.WriteLine("\nTransfer offer made successfully!");
                }
                catch (InvalidOperationException ex)
                {
                    System.Console.WriteLine($"\nCannot make transfer offer: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }

            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        private static void ViewPendingOffers()
        {
            try
            {
                var offers = _manager.GetPendingTransferOffers();
                if (!offers.Any())
                {
                    System.Console.WriteLine("\nNo pending transfer offers found.");
                    System.Console.WriteLine("\nPress any key to continue...");
                    System.Console.ReadKey();
                    return;
                }

                System.Console.Clear();
                System.Console.WriteLine("Pending Transfer Offers:");
                System.Console.WriteLine("-----------------------");

                foreach (var offer in offers)
                {
                    System.Console.WriteLine($"\nOffer ID: {offer.Id}");
                    System.Console.WriteLine($"Player: {offer.Player.Name}");
                    System.Console.WriteLine($"From Team: {offer.FromTeam.Name}");
                    System.Console.WriteLine($"To Team: {offer.ToTeam.Name}");
                    System.Console.WriteLine($"Amount: ${offer.OfferedAmount}");
                    System.Console.WriteLine($"Weekly Salary: ${offer.OfferedWeeklySalary}");
                    System.Console.WriteLine($"Status: {offer.Status}");

                    if (offer.ToTeamId == _manager.GetCurrentTeam().Id)
                    {
                        System.Console.WriteLine("\nOptions for this offer:");
                        System.Console.WriteLine("1. Accept");
                        System.Console.WriteLine("2. Reject");
                        System.Console.WriteLine("3. Skip");

                        System.Console.Write("\nChoose an option: ");
                        string choice = System.Console.ReadLine();

                        switch (choice)
                        {
                            case "1":
                                _manager.RespondToTransferOffer(offer.Id, true);
                                System.Console.WriteLine("Offer accepted!");
                                break;
                            case "2":
                                _manager.RespondToTransferOffer(offer.Id, false);
                                System.Console.WriteLine("Offer rejected!");
                                break;
                            case "3":
                            default:
                                System.Console.WriteLine("Offer skipped.");
                                break;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("\nOptions for this offer:");
                        System.Console.WriteLine("1. Cancel Offer");
                        System.Console.WriteLine("2. Skip");

                        System.Console.Write("\nChoose an option: ");
                        if (System.Console.ReadLine() == "1")
                        {
                            _manager.CancelTransferOffer(offer.Id);
                            System.Console.WriteLine("Offer cancelled!");
                        }
                    }

                    System.Console.WriteLine("-------------------");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }

            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        private static void ManageTransferOffers()
        {
            System.Console.Clear();
            System.Console.WriteLine("Manage Transfer Offers:");

            var transferOffers = _manager.GetTeamsPendingTransferOffers(_manager.GetCurrentTeam().Id);

            if (transferOffers.Count == 0)
            {
                System.Console.WriteLine("No pending offers.");
                System.Console.ReadKey();
                return;
            }

            for (int i = 0; i < transferOffers.Count; i++)
            {
                var offer = transferOffers[i];
                System.Console.WriteLine($"{i + 1}. {offer.Player.Name} from {offer.FromTeam.Name} - {offer.OfferedAmount:C}");
                System.Console.WriteLine($"   Offered Weekly Salary: {offer.OfferedWeeklySalary:C}");
                System.Console.WriteLine($"   Offer Date: {offer.OfferDate:d}");
            }

            System.Console.WriteLine("\nSelect offer number to manage (0 to return):");
            if (int.TryParse(System.Console.ReadLine(), out int choice) && choice > 0 && choice <= transferOffers.Count)
            {
                var selectedOffer = transferOffers[choice - 1];

                System.Console.WriteLine($"\nSelected offer: {selectedOffer.Player.Name}");
                System.Console.WriteLine($"From Team: {selectedOffer.FromTeam.Name}");
                System.Console.WriteLine($"Transfer Amount: {selectedOffer.OfferedAmount:C}");
                System.Console.WriteLine($"Weekly Salary: {selectedOffer.OfferedWeeklySalary:C}");
                System.Console.WriteLine("\n1. Accept");
                System.Console.WriteLine("2. Reject");
                System.Console.WriteLine("3. Back");

                switch (System.Console.ReadLine())
                {
                    case "1":
                        _manager.AcceptTransferOffer(selectedOffer.Id);
                        System.Console.WriteLine("Transfer offer accepted!");
                        break;

                    case "2":
                        _manager.RejectTransferOffer(selectedOffer.Id);
                        System.Console.WriteLine("Transfer offer rejected!");
                        break;
                }
            }

            System.Console.ReadKey();
        }

        private static void UpdatePlayerStatus()
        {
            try
            {
                var players = _manager.GetCurrentTeam().Players;
                System.Console.Clear();
                System.Console.WriteLine("Current Players:");
                foreach (var player in players)
                {
                    System.Console.WriteLine($"\nID: {player.Id}");
                    System.Console.WriteLine($"Name: {player.Name}");
                    System.Console.WriteLine($"Current Status: {player.Status}");
                    System.Console.WriteLine($"Team: {player.Team?.Name ?? "No team"}");
                    System.Console.WriteLine("-------------------");
                }

                System.Console.Write("\nEnter Player ID: ");
                if (!int.TryParse(System.Console.ReadLine(), out int playerId))
                {
                    System.Console.WriteLine("Invalid player ID!");
                    return;
                }

                System.Console.Clear();

                System.Console.WriteLine($"\nSelected player id: {playerId}");
                System.Console.WriteLine("\nSelect new status:");
                System.Console.WriteLine("1. Available");
                System.Console.WriteLine("2. Active");
                System.Console.WriteLine("3. TransferListed");
                System.Console.WriteLine("4. Retired");
                System.Console.WriteLine("5. Negotiating");

                if (!int.TryParse(System.Console.ReadLine(), out int statusChoice))
                {
                    System.Console.WriteLine("Invalid selection!");
                    return;
                }

                PlayerStatus newStatus = statusChoice switch
                {
                    1 => PlayerStatus.Available,
                    2 => PlayerStatus.Active,
                    3 => PlayerStatus.TransferListed,
                    4 => PlayerStatus.Retired,
                    5 => PlayerStatus.Negotiating,
                    _ => throw new InvalidOperationException("Invalid status selection")
                };

                _manager.UpdatePlayerStatus(playerId, newStatus);
                System.Console.WriteLine("Player status updated successfully!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ManageContracts()
        {
            new ConsoleMenu(args: new string[0], level: 1)
            .Add("View All Contracts", () => ViewAllContracts())
            .Add("View Expiring Contracts", () => ViewExpiringContracts())
            .Add("Renew Contract", () => RenewPlayerContract())
            .Add("Back", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = false;
                config.Title = "Transfer Market";
                config.EnableBreadcrumb = true;
            }).Show();
        }

        private static void ViewAllContracts()
        {
            try
            {
                var team = _manager.GetCurrentTeam();
                System.Console.WriteLine("\nCurrent Contracts:");
                foreach (var player in team.Players)
                {
                    DisplayContractDetails(player);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void ViewExpiringContracts()
        {
            try
            {
                var expiringContracts = _manager.GetPlayersWithExpiringContracts();
                System.Console.WriteLine("\nExpiring Contracts (Next 6 months):");
                foreach (var player in expiringContracts)
                {
                    DisplayContractDetails(player);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        private static void DisplayContractDetails(Player player)
        {
            System.Console.WriteLine($"\nPlayer: {player.Name}");
            System.Console.WriteLine($"Position: {player.Position}");
            System.Console.WriteLine($"Performance: {player.Performance}");
            System.Console.WriteLine($"Weekly Salary: ${player.WeeklySalary}");
            System.Console.WriteLine($"Contract Starts: {player.ContractStart.ToShortDateString()}");
            System.Console.WriteLine($"Contract Ends: {player.ContractEnd.ToShortDateString()}");
            System.Console.WriteLine($"Status: {player.Status}");

            var monthsRemaining = (player.ContractEnd - _manager.CurrentDate).Days / 30;
            System.Console.WriteLine($"Months Remaining: {monthsRemaining}");
            System.Console.WriteLine("-------------------");
        }


        private static void RenewPlayerContract()
        {
            try
            {
                ViewExpiringContracts();

                System.Console.Write("\nEnter Player ID to renew contract: ");
                if (!int.TryParse(System.Console.ReadLine(), out int playerId))
                {
                    System.Console.WriteLine("Invalid player ID! Please enter a number.");
                    return;
                }

                System.Console.Write("Enter contract extension (years): ");
                if (!int.TryParse(System.Console.ReadLine(), out int years))
                    return;

                System.Console.Write("Enter weekly salary increase: $");
                if (!decimal.TryParse(System.Console.ReadLine(), out decimal increase))
                    return;

                bool accepted = _manager.RenewContract(playerId, years, increase);
                if (accepted)
                {
                    System.Console.WriteLine("Contract renewed successfully!");
                }
                else
                {
                    System.Console.WriteLine("Player rejected the contract offer.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }


        private static void DisplayPlayers(IEnumerable<Player> players)
        {
            if (!players.Any())
            {
                System.Console.WriteLine("No players found.");
                return;
            }

            foreach (var player in players)
            {
                System.Console.WriteLine($"\nID: {player.Id}");
                System.Console.WriteLine($"Name: {player.Name}");
                System.Console.WriteLine($"Team: {player.Team?.Name ?? "No team"}");
                System.Console.WriteLine($"Position: {player.Position}");
                System.Console.WriteLine($"Performance: {player.Performance}");
                System.Console.WriteLine($"Condition: {player.PhysicalCondition}");

                if (player.Skills.Any())
                {
                    System.Console.WriteLine("Skills:");
                    foreach (var skill in player.Skills)
                    {
                        System.Console.WriteLine($"  - {skill.Name}: {skill.Value}");
                    }
                }
                System.Console.WriteLine("-------------------");
            }
        }

    }
}

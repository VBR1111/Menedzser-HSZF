using Menedzser_HSZF_2024251.Application;
using Menedzser_HSZF_2024251.Model;
using NUnit.Framework;
using Moq;
using Menedzser_HSZF_2024251.Persistence.MsSql;

namespace Menedzser_HSZF_2024251.Test
{
    [TestFixture]
    public class FootballManagerTests
    {
        private Mock<IFootballRepository> _mockRepo;
        private Mock<IXmlReportGenerator> _mockReportGenerator;
        private FootballManager _manager;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IFootballRepository>();
            _mockReportGenerator = new Mock<IXmlReportGenerator>();
            _manager = new FootballManager(_mockRepo.Object, _mockReportGenerator.Object);
        }

        [Test]
        public void StartNewSeason_WithValidTeam_CreatesNewSeason()
        {
            // Arrange
            var team = new Team { Id = 1, Budget = 1000000 };
            _mockRepo.Setup(r => r.GetCurrentTeam()).Returns(team);
            _mockRepo.Setup(r => r.CurrentDate).Returns(new DateTime(2024, 1, 1));

            // Act
            _manager.StartNewSeason();

            // Assert
            _mockRepo.Verify(r => r.CreateSeason(It.Is<Season>(s =>
                s.TeamId == team.Id &&
                s.StartingBudget == team.Budget)), Times.Once);
        }

        [Test]
        public void StartNewSeason_WithNoTeamSelected_ThrowsException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetCurrentTeam()).Returns((Team)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _manager.StartNewSeason());
        }

        [Test]
        public void GetTeamStatistics_CalculatesCorrectly()
        {
            // Arrange
            var team = new Team { Id = 1 };
            var tasks = new List<TeamTask>
            {
                new TeamTask {
                    Type = TaskType.Match,
                    Result = MatchResult.Win,
                    TeamId = 1,
                    Team = team,
                    GoalsScored = 2,
                    GoalsConceded = 1
                },
                new TeamTask {
                    Type = TaskType.Match,
                    Result = MatchResult.Win,
                    TeamId = 1,
                    Team = team,
                    GoalsScored = 3,
                    GoalsConceded = 0
                },
                new TeamTask {
                    Type = TaskType.Match,
                    Result = MatchResult.Loss,
                    TeamId = 1,
                    Team = team,
                    GoalsScored = 1,
                    GoalsConceded = 2
                }
            };
            team.Tasks = tasks;

            var teamStats = new TeamStatistics
            {
                TeamId = 1,
                Wins = 2,
                Draws = 0,
                Losses = 1,
                GoalsScored = 6,
                GoalsConceded = 3
            };

            _mockRepo.Setup(r => r.GetCurrentTeam()).Returns(team);
            _mockRepo.Setup(r => r.CalculateTeamStatistics(1)).Returns(teamStats);

            // Act
            var stats = _manager.GetTeamStatistics();

            // Assert
            Assert.That(stats.Wins, Is.EqualTo(2));
            Assert.That(stats.Losses, Is.EqualTo(1));
            Assert.That(stats.Draws, Is.EqualTo(0));
            Assert.That(stats.GoalsScored, Is.EqualTo(6));
            Assert.That(stats.GoalsConceded, Is.EqualTo(3));
        }

        [Test]
        public void UpdateTeamBudget_WithZeroBudget_ThrowsException()
        {
            // Arrange
            var team = new Team { Id = 1 };
            _mockRepo.Setup(r => r.GetTeam(1)).Returns(team);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _manager.UpdateTeamBudget(1, 0));
        }

        [Test]
        public void OnTaskCompleted_GeneratesReport()
        {
            // Arrange
            var team = new Team { Id = 1, Name = "Test Team" };
            var task = new TeamTask
            {
                Id = 1,
                Name = "Test Match",
                Team = team,
                TeamId = team.Id
            };
            var players = new List<Player>
            {
                new Player { Name = "Test Player" }
            };

            _mockRepo.Setup(r => r.CurrentDate).Returns(DateTime.Now);

            // Act
            _manager.OnTaskCompleted(new TaskCompletedEventArgs(task, true, players));

            // Assert
            _mockReportGenerator.Verify(r => r.GenerateReport(
                It.IsAny<TaskReport>(),
                It.IsAny<DateTime>()),
                Times.Once);
        }

        [Test]
        public void GetPlayersWithExpiringContracts_ReturnsCorrectPlayers()
        {
            // Arrange
            var players = new List<Player>
       {
           new Player { ContractEnd = DateTime.Now.AddMonths(1) },
           new Player { ContractEnd = DateTime.Now.AddMonths(12) }
       };
            _mockRepo.Setup(r => r.GetPlayersWithExpiringContracts(6))
                .Returns(players.Where(p => p.ContractEnd <= DateTime.Now.AddMonths(6)));

            // Act
            var result = _manager.GetPlayersWithExpiringContracts();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateTeam_WithNegativeBudget_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _manager.CreateTeam("Test Team", -1000, 1));
        }

        [Test]
        public void UpdatePlayerContract_ValidPlayerInvalidSalary_ThrowsException()
        {
            // Arrange
            var player = new Player { Id = 1 };
            _mockRepo.Setup(r => r.GetPlayer(1)).Returns(player);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _manager.UpdatePlayerContract(1, DateTime.Now, -100));
        }

        [Test]
        public void CreateTeam_WithNullName_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _manager.CreateTeam(null, 1000, 1));
        }

        [Test]
        public void ScheduleMatch_WithInsufficientPlayers_ThrowsException()
        {
            // Arrange
            var team = new Team { Id = 1, Budget = 10000 };
            var players = new List<Player> { new Player() };

            _mockRepo.Setup(r => r.GetCurrentTeam()).Returns(team);
            _mockRepo.Setup(r => r.GetHealthyPlayersByTeam(1)).Returns(players);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _manager.ScheduleMatch(new TeamTask()));
        }
    }
}

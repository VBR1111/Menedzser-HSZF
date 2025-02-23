using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Menedzser_HSZF_2024251.Model
{
    public class Team
    {
        private readonly Action<object, string> _lazyLoader;
        private ICollection<Player> _players;
        private ICollection<TeamTask> _tasks;

        public Team() { }

        public Team(Action<object, string> lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public decimal Budget { get; set; }
        public int StaffCount { get; set; }

        public virtual ICollection<Player> Players
        {
            get => _lazyLoader?.Load(this, ref _players) ?? (_players ??= new List<Player>());
            set => _players = value;
        }

        public virtual ICollection<TeamTask> Tasks
        {
            get => _lazyLoader?.Load(this, ref _tasks) ?? (_tasks ??= new List<TeamTask>());
            set => _tasks = value;
        }

        public decimal DailyWages => Players.Sum(p => p.WeeklySalary) / 7;
        public int InjuredPlayerCount => Players.Count(p => p.PhysicalCondition == PhysicalCondition.Injured);
        public int HealthyPlayerCount => Players.Count(p => p.PhysicalCondition == PhysicalCondition.Healthy);
        public double WinRate => Tasks.Count(t => t.Type == TaskType.Match) > 0
            ? (double)Tasks.Count(t => t.Type == TaskType.Match && t.Result == MatchResult.Win) / Tasks.Count(t => t.Type == TaskType.Match)
            : 0;

        private T Load<T>(ref T navigation, [CallerMemberName] string navigationName = null)
        {
            _lazyLoader?.Invoke(this, navigationName);
            return navigation;
        }
    }
}

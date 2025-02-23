using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class TeamTask
    {
        public TeamTask()
        {
            PlayerTasks = new List<PlayerTeamTask>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }
        public TaskType Type { get; set; }
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
        public int? GoalsScored { get; set; }
        public int? GoalsConceded { get; set; }
        public MatchResult? Result { get; set; }

        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }

        public virtual ICollection<PlayerTeamTask> PlayerTasks { get; set; }
    }
}

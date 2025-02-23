using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class Player
    {
        public Player() { }

        public Player(DateTime currentDate)
        {
            PlayerTasks = new List<PlayerTeamTask>();
            Skills = new List<Skill>();
            ContractStart = currentDate;
            ContractEnd = currentDate.AddYears(1);
            Status = PlayerStatus.Available;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public Position Position { get; set; }

        [Required]
        public Performance Performance { get; set; }

        [Required]
        public PhysicalCondition PhysicalCondition { get; set; }
        public DateTime ContractStart { get; set; }
        public DateTime ContractEnd { get; set; }
        public decimal WeeklySalary { get; set; }
        public decimal TransferValue { get; set; }
        public PlayerStatus Status { get; set; }

        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }

        public virtual ICollection<PlayerTeamTask> PlayerTasks { get; set; }
        public virtual ICollection<Skill> Skills { get; set; }
    }

}

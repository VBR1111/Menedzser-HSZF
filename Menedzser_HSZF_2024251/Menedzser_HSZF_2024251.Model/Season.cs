using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Menedzser_HSZF_2024251.Model
{
    public class Season
    {
        public Season() { }

        public Season(DateTime currentDate)
        {
            StartDate = currentDate;
            EndDate = StartDate.AddDays(365);
            Status = SeasonStatus.Active;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SeasonStatus Status { get; set; }
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }

        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public decimal StartingBudget { get; set; }
        public decimal EndingBudget { get; set; }
    }
}

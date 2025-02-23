using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Application
{
    public class SeasonSummary
    {
        public int DaysRemaining { get; set; }
        public decimal StartingBudget { get; set; }
        public decimal CurrentBudget { get; set; }
        public int TotalPlayers { get; set; }
        public int HealthyPlayers { get; set; }
        public int TopPerformers { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public DateTime EndDate { get; set; }

        public int TotalMatches => Wins + Draws + Losses;
        public bool IsVictory => DaysRemaining <= 0 && CurrentBudget > 0 && TopPerformers > 0;
    }
}

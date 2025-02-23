using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class TeamStatistics
    {
        public TeamStatistics()
        {
            Wins = 0;
            Draws = 0;
            Losses = 0;
            GoalsScored = 0;
            GoalsConceded = 0;
        }

        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsConceded { get; set; }
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }
    }
}

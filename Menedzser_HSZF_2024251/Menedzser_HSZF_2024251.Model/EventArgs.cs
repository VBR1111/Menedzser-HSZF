using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class PlayerPerformanceChangedEventArgs : EventArgs
    {
        public Player Player { get; }
        public Performance OldPerformance { get; }
        public Performance NewPerformance { get; }

        public PlayerPerformanceChangedEventArgs(Player player, Performance oldPerformance, Performance newPerformance)
        {
            Player = player;
            OldPerformance = oldPerformance;
            NewPerformance = newPerformance;
        }
    }

    public class PlayerInjuryEventArgs : EventArgs
    {
        public Player Player { get; }
        public DateTime InjuryDate { get; }

        public PlayerInjuryEventArgs(Player player, DateTime currentDate)
        {
            Player = player;
            InjuryDate = currentDate;
        }
    }

    public class TaskCompletedEventArgs : EventArgs
    {
        public TeamTask Task { get; }
        public bool Success { get; }
        public List<Player> AffectedPlayers { get; }

        public TaskCompletedEventArgs(TeamTask task, bool success, List<Player> affectedPlayers)
        {
            Task = task;
            Success = success;
            AffectedPlayers = affectedPlayers;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class TransferReport
    {
        public string PlayerName { get; set; }
        public string FromTeam { get; set; }
        public string ToTeam { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int Season { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Model
{
    public class PlayerTeamTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public virtual Player Player { get; set; }

        public int TaskId { get; set; }
        public virtual TeamTask Task { get; set; }
    }
}

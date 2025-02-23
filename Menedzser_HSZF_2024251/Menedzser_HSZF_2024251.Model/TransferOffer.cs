using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Menedzser_HSZF_2024251.Model
{
    public class TransferOffer
    {
        public TransferOffer() {}

        public TransferOffer(DateTime currentDate)
        {
            OfferDate = currentDate;
            Status = TransferStatus.Pending;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public virtual Player Player { get; set; }

        [ForeignKey("FromTeam")]
        public int FromTeamId { get; set; }
        public virtual Team FromTeam { get; set; }

        [ForeignKey("ToTeam")]
        public int ToTeamId { get; set; }
        public virtual Team ToTeam { get; set; }

        public decimal OfferedAmount { get; set; }
        public decimal OfferedWeeklySalary { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public TransferStatus Status { get; set; }
    }
}

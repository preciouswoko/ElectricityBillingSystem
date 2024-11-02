using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricityBillingSystem.Domain.Models
{
    public class Wallet
    {
        public Guid Id { get; set; }
        public decimal Balance { get; set; }
        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public User? User { get; set; }
        //public string UserId { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

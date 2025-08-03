using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Lines
    {
        [Key]
        public long Id { get; set; }

        [Column("nama")]
        [StringLength(50)]
        public string? Nama { get; set; }
        public virtual ICollection<Robots>? Robots { get; set; }
        public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
    }
}

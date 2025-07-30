using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Ng_Categories
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [Column("category")]
        public string Category { get; set; }

        [Required]
        [Column("sub_category")]
        public string SubCategory { get; set; }
        //public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
    }
}

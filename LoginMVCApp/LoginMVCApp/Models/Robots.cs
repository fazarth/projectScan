using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Robots
    {
        [Key]
        public long Id { get; set; }

        [Required]
        //[Display(Name = "Line")]
        [Column("line_id")]
        public long LineId { get; set; }

        [Required]
        [StringLength(50)]
        public string Nama { get; set; }

        [ForeignKey("LineId")]
        public virtual Lines? Line { get; set; }
    }
}

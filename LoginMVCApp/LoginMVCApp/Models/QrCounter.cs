using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Qr_Counter
    {
        [Key]
        public int Id { get; set; }

        [Column("InvId")]
        public string InvId { get; set; }

        [Column("YearMonth")]
        public string YearMonth { get; set; }

        [Column("LastNumber")]
        public int LastNumber { get; set; }
    }
}

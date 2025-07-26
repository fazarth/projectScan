using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Inventories
    {
        [Key]
        public long Id { get; set; }

        [Column("project")]
        [StringLength(50)]
        public string? Project { get; set; }

        [Column("inv_id")]
        public string? InvId { get; set; }

        [Column("warna")]
        [StringLength(50)]
        public string? Warna { get; set; }

        [Column("tipe")]
        [StringLength(50)]
        public string? Tipe { get; set; }

        [Column("part_no")]
        [StringLength(50)]
        public string? PartNo { get; set; }

        [Column("part_name")]
        [StringLength(50)]
        public string? PartName { get; set; }

        [Column("barcode")]
        [StringLength(50)]
        public string? Barcode { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("created_by")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }
    }
}

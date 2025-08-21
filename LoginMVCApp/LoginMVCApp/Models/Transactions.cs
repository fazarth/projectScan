
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Transactions
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [Column("inv_id")]
        public long InvId { get; set; }

        [Required]
        [Column("barcode")]
        public string Barcode { get; set; }

        [Required]
        [Column("line_id")]
        public long LineId { get; set; }

        [Required]
        [Column("robot_id")]
        public long RobotId { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [Column("role")]
        [StringLength(10)]
        public string Role { get; set; } = "POLESH";

        [Required]
        [Column("status")]
        [StringLength(10)]
        public string Status { get; set; } = "OK"; // atau POLESH / NG

        [Column("ng_detail_id")]
        public long? NgDetailId { get; set; }

        [Required]
        [Column("qty")]
        public int Qty { get; set; }

        [Required]
        [Column("shift")]
        [StringLength(10)]
        public string Shift { get; set; } = "0";

        [Column("opposite_shift")]
        public string OppositeShift { get; set; } = "0";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("is_return")]
        public bool Is_Return{ get; set; } = false;

        // ======== NAVIGATION PROPERTIES =========

        [ForeignKey("InvId")]
        public virtual Inventories? Inventory { get; set; }

        [ForeignKey("LineId")]
        public virtual Lines? Line { get; set; }

        [ForeignKey("NgDetailId")]
        public virtual Ng_Categories? NgCategory{ get; set; }

        [ForeignKey("RobotId")]
        public virtual Robots? Robot { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
    }
}
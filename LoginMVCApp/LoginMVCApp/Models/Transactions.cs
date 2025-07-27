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

        [Required]
        [Column("qty")]
        public int Qty { get; set; }

        [Required]
        [Column("shift")]
        [StringLength(10)]
        public string Shift { get; set; } = "0";

        [Column("opposite_shift")]
        public bool OppositeShift { get; set; } = false;

        [Column("checker_id")]
        public long? CheckerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ======== NAVIGATION PROPERTIES =========

        [ForeignKey("InvId")]
        public virtual Inventories? Inventory { get; set; }

        [ForeignKey("LineId")]
        public virtual Lines? Line { get; set; }

        [ForeignKey("RobotId")]
        public virtual Robots? Robot { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [ForeignKey("CheckerId")]
        public virtual Transactions? Checker { get; set; }
    }
}

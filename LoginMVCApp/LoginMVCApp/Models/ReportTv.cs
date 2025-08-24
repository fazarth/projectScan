using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class ReportTv
    {
        [Key]
        public int Id { get; set; }

        [Column("production_plan")]
        public int ProductionPlan { get; set; }

        [Column("straight_pass")]
        public int StraightPass { get; set; }

        [Column("tamu_straight")]
        public int TamuStraight { get; set; }

        [Column("tamu_poles_ok")]
        public int TamuPolesOk { get; set; }

        [Column("tamu_poles_repair")]
        public int TamuPolesRepair { get; set; }

        [Column("line")]
        public int Line { get; set; }
    }
}

namespace LoginMVCApp.ViewModels
{
    public class DashboardViewModel
    {
        // Header
        public int? LineId { get; set; }
        public string? Shift { get; set; }
        public DateTime? Tanggal { get; set; }

        // ReportTV
        public int? ProductionPlan { get; set; }
        public decimal? ProductivityPct { get; set; }      // 0..100
        public decimal? StraightPassPct { get; set; }      // 0..100
        public decimal? PoleshPct => (ProductivityPct ?? 0) - (StraightPassPct ?? 0);

        // Target
        public int? JamKerja { get; set; } = 8;
        public int? TargetPerJam { get; set; }
        public int? TargetCumulativeSaatIni { get; set; }

        // Achieve (realtime)
        public int? AchieveCount { get; set; }
        public decimal? AchievePct { get; set; }           // vs targetCumulativeSaatIni

        // Checker breakdown (ungu & kuning)
        public int? CheckerOk { get; set; }
        public int? CheckerPolesh { get; set; }
        public int? CheckerTotal => (CheckerOk ?? 0) + (CheckerPolesh ?? 0);
        public decimal? CheckerOkPct { get; set; }         // 0..100
        public decimal? CheckerPoleshPct { get; set; }     // 100 - ok%

        // Panel Poles kanan
        public int? PolesOk { get; set; }
        public int? PolesNg { get; set; }

        // Row bawah
        public int? SpQcOk { get; set; }       // Checker OK
        public int? PolQcOk { get; set; }      // Poles OK
        public int? Repair { get; set; }       // NG/REPAIR
        public int? Total => (SpQcOk ?? 0) + (PolQcOk ?? 0) + (Repair ?? 0);
    }
}

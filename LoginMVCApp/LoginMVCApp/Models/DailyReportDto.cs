using System;
using System.Collections.Generic;

namespace LoginMVCApp.Models
{
    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public string Shift { get; set; }
        public string Line { get; set; }

        public string Robot { get; set; }
        public string Part { get; set; }
        public string Project { get; set; }
        public string Type { get; set; }
        public string InvId { get; set; }
        public string Color { get; set; }

        public int QtyOk { get; set; }
        public int QtyNg { get; set; }
        public int QtyNgPolish { get; set; }
        public int QtyNgTouchUp { get; set; }
        public int QtyNgMigaki { get; set; }

        // Perhitungan otomatis
        public int TotalProd => QtyOk + QtyNg;

        public int Migaki => QtyNgMigaki;

        public double PercentOk => (TotalProd == 0) ? 0 : (double)QtyOk / TotalProd * 100;

        // Masih disimpan kalau perlu breakdown detail
        public Dictionary<string, int> DetailNg { get; set; } = new();
    }
}
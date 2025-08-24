
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class DailyReportDto
    {
        public string Robot { get; set; }
        public string Part { get; set; }
        public string Project { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }

        public int QtyOk { get; set; }
        public int QtyNg { get; set; }
        public double PercentOk => (QtyOk + QtyNg) == 0 ? 0 : (double)QtyOk / (QtyOk + QtyNg) * 100;

        public Dictionary<string, int> DetailNg { get; set; } = new();
    }
}
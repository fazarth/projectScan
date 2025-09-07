namespace LoginMVCApp.Helpers
{
    public static class ShiftTime
    {
        // Shift 1: 07:00-15:00, Shift 2: 15:00-23:00, Shift 3: 23:00-07:00
        public static (TimeSpan start, TimeSpan end) GetRange(string shift) => shift switch
        {
            "1" => (new TimeSpan(8, 0, 0), new TimeSpan(20, 59, 59)),
            "2" => (new TimeSpan(21, 0, 0), new TimeSpan(7, 59, 59)),
        };

        public static int GetJamBerjalan(DateTime nowLocal, string shift, int jamKerja)
        {
            var (start, end) = GetRange(shift);
            var t = nowLocal.TimeOfDay;

            bool crossesMidnight = end < start;
            var elapsed = crossesMidnight
                ? (t >= start ? t - start : (new TimeSpan(24, 0, 0) - start) + t)
                : (t <= end ? t - start : end - start);

            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            var jam = (int)Math.Ceiling(elapsed.TotalHours);
            if (jam < 1) jam = 1;
            if (jam > jamKerja) jam = jamKerja;
            return jam;
        }
    }

}

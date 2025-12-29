using Microsoft.Data.SqlClient;
using QcChapWai.Data;
using System.Data;

namespace QcChapWai.Services
{
    public class InspectCodeService
    {
        private readonly SqlHelper _sqlHelper;

        public InspectCodeService(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        /// <summary>
        /// Generate InspectCode: Plant+Zone+YYMMDD+Running (4 digits)
        /// Example: KBA2512220001
        /// </summary>
        public async Task<string> GenerateInspectCodeAsync(string plant, string zone)
        {
            // 1. สร้าง Prefix: Plant + Zone + YYMMDD
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyMMdd"); // 251222
            var prefix = $"{plant}{zone}{datePrefix}"; // KB + A + 251222 = KBA251222

            // 2. หา Running Number สูงสุดของวันนี้
            var sql = @"
                SELECT TOP 1 InspectCode
                FROM InspectionChecklist
                WHERE InspectCode LIKE @Prefix + '%'
                ORDER BY InspectCode DESC";

            var lastCode = await _sqlHelper.ExecuteReaderAsync<string?>(sql,
                reader => reader.IsDBNull("InspectCode") ? null : reader.GetString("InspectCode"),
                new SqlParameter("@Prefix", prefix));

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastCode) && lastCode.Length >= prefix.Length + 4)
            {
                // ดึงเลข Running 4 หลักสุดท้าย
                var runningPart = lastCode.Substring(prefix.Length);

                if (int.TryParse(runningPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            // 3. สร้าง InspectCode ใหม่
            var inspectCode = $"{prefix}{nextNumber:D4}"; // D4 = 4 digits with leading zeros

            return inspectCode;
        }

        /// <summary>
        /// ตรวจสอบว่า InspectCode ซ้ำหรือไม่ (Double-check)
        /// </summary>
        public async Task<bool> IsInspectCodeExistsAsync(string inspectCode)
        {
            var sql = "SELECT COUNT(*) FROM InspectionChecklist WHERE InspectCode = @InspectCode";
            var count = await _sqlHelper.ExecuteScalarAsync(sql, new SqlParameter("@InspectCode", inspectCode));
            return Convert.ToInt32(count) > 0;
        }

        /// <summary>
        /// Generate InspectCode พร้อมตรวจสอบซ้ำ (Thread-safe)
        /// </summary>
        public async Task<string> GenerateUniqueInspectCodeAsync(string plant, string zone, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var inspectCode = await GenerateInspectCodeAsync(plant, zone);

                // Double-check ว่าไม่ซ้ำ
                var exists = await IsInspectCodeExistsAsync(inspectCode);

                if (!exists)
                {
                    return inspectCode;
                }

                // ถ้าซ้ำ รอ 100ms แล้วลองใหม่
                await Task.Delay(100);
            }

            throw new Exception("ไม่สามารถสร้าง InspectCode ที่ไม่ซ้ำได้ กรุณาลองใหม่อีกครั้ง");
        }

        /// <summary>
        /// ดึงรายการ InspectCode ของวันนี้
        /// </summary>
        public async Task<List<string>> GetTodayInspectCodesAsync(string plant, string zone)
        {
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyMMdd");
            var prefix = $"{plant}{zone}{datePrefix}";

            var sql = @"
                SELECT InspectCode
                FROM InspectionChecklist
                WHERE InspectCode LIKE @Prefix + '%'
                ORDER BY InspectCode";

            var codes = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => reader.GetString("InspectCode"),
                new SqlParameter("@Prefix", prefix));

            return codes;
        }

        /// <summary>
        /// สถิติการใช้งาน InspectCode
        /// </summary>
        public async Task<Dictionary<string, int>> GetDailyStatisticsAsync(DateTime date)
        {
            var datePrefix = date.ToString("yyMMdd");

            var sql = @"
                SELECT 
                    SUBSTRING(InspectCode, 1, 9) as DatePrefix,
                    COUNT(*) as Count
                FROM InspectionChecklist
                WHERE InspectCode LIKE '%' + @DatePrefix + '%'
                GROUP BY SUBSTRING(InspectCode, 1, 9)
                ORDER BY DatePrefix";

            var stats = new Dictionary<string, int>();

            var results = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => new
                {
                    Prefix = reader.GetString("DatePrefix"),
                    Count = reader.GetInt32("Count")
                },
                new SqlParameter("@DatePrefix", datePrefix));

            foreach (var result in results)
            {
                stats[result.Prefix] = result.Count;
            }

            return stats;
        }
    }
}
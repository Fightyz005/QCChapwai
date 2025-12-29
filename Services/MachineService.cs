using Microsoft.Data.SqlClient;
using QcChapWai.Data;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class MachineService
    {
        private readonly SqlHelper _sqlHelper;

        public MachineService(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        /// <summary>
        /// ดึง Zone ทั้งหมดที่ Active
        /// </summary>
        public async Task<List<string>> GetActiveZonesAsync(string plant = "KB01")
        {
            var sql = @"
                SELECT DISTINCT Zone
                FROM MachineMaster
                WHERE Plant = @Plant AND IsActive = 1
                ORDER BY Zone";

            var zones = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => reader.GetString("Zone"),
                new SqlParameter("@Plant", plant));

            return zones;
        }

        /// <summary>
        /// ดึง Process ตาม Zone
        /// </summary>
        public async Task<List<string>> GetProcessesByZoneAsync(string zone, string plant = "KB01")
        {
            var sql = @"
                SELECT DISTINCT Process
                FROM MachineMaster
                WHERE Plant = @Plant AND Zone = @Zone AND IsActive = 1
                ORDER BY Process";

            var processes = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => reader.GetString("Process"),
                new SqlParameter("@Plant", plant),
                new SqlParameter("@Zone", zone));

            return processes;
        }

        /// <summary>
        /// ดึง Machine ตาม Zone และ Process
        /// </summary>
        public async Task<List<MachineSelectionViewModel>> GetMachinesByZoneAndProcessAsync(
            string zone,
            string process,
            string plant = "KB01")
        {
            var sql = @"
                SELECT Zone, Process, Machine, Storage
                FROM MachineMaster
                WHERE Plant = @Plant 
                  AND Zone = @Zone 
                  AND Process = @Process 
                  AND IsActive = 1
                ORDER BY Machine";

            var machines = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => new MachineSelectionViewModel
                {
                    Zone = reader.GetString("Zone"),
                    Process = reader.GetString("Process"),
                    Machine = reader.GetString("Machine"),
                    Storage = reader.IsDBNull("Storage") ? "" : reader.GetString("Storage")
                },
                new SqlParameter("@Plant", plant),
                new SqlParameter("@Zone", zone),
                new SqlParameter("@Process", process));

            return machines;
        }

        /// <summary>
        /// ค้นหา Machine (Auto-complete)
        /// </summary>
        public async Task<List<MachineSelectionViewModel>> SearchMachinesAsync(
            string searchTerm,
            string? zone = null,
            string? process = null,
            string plant = "KB01")
        {
            var sql = @"
                SELECT Zone, Process, Machine, Storage
                FROM MachineMaster
                WHERE Plant = @Plant 
                  AND IsActive = 1
                  AND Machine LIKE @SearchTerm";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Plant", plant),
                new SqlParameter("@SearchTerm", $"%{searchTerm}%")
            };

            if (!string.IsNullOrEmpty(zone))
            {
                sql += " AND Zone = @Zone";
                parameters.Add(new SqlParameter("@Zone", zone));
            }

            if (!string.IsNullOrEmpty(process))
            {
                sql += " AND Process = @Process";
                parameters.Add(new SqlParameter("@Process", process));
            }

            sql += " ORDER BY Machine";

            var machines = await _sqlHelper.ExecuteReaderListAsync(sql,
                reader => new MachineSelectionViewModel
                {
                    Zone = reader.GetString("Zone"),
                    Process = reader.GetString("Process"),
                    Machine = reader.GetString("Machine"),
                    Storage = reader.IsDBNull("Storage") ? "" : reader.GetString("Storage")
                },
                parameters.ToArray());

            return machines;
        }

        /// <summary>
        /// ตรวจสอบว่า Machine มีอยู่จริงหรือไม่
        /// </summary>
        public async Task<MachineMaster?> GetMachineByNameAsync(string machineName, string plant = "KB01")
        {
            var sql = @"
                SELECT MachineId, Plant, Zone, Process, Storage, Machine, IsActive, CreatedAt
                FROM MachineMaster
                WHERE Plant = @Plant AND Machine = @Machine AND IsActive = 1";

            return await _sqlHelper.ExecuteReaderAsync(sql,
                reader => new MachineMaster
                {
                    MachineId = reader.GetInt32("MachineId"),
                    Plant = reader.GetString("Plant"),
                    Zone = reader.GetString("Zone"),
                    Process = reader.GetString("Process"),
                    Storage = reader.IsDBNull("Storage") ? null : reader.GetString("Storage"),
                    Machine = reader.GetString("Machine"),
                    IsActive = reader.GetBoolean("IsActive"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                },
                new SqlParameter("@Plant", plant),
                new SqlParameter("@Machine", machineName));
        }
    }
}
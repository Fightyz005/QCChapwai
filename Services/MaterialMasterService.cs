using Microsoft.Data.SqlClient;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class MaterialMasterService
    {
        private readonly string _connectionString;

        public MaterialMasterService(IConfiguration configuration)
        {
            // ✅ ใช้ MaterialConnection แทน DefaultConnection
            _connectionString = configuration.GetConnectionString("MaterialConnection") ??
                throw new ArgumentNullException("MaterialConnection not found");
        }

        /// <summary>
        /// ดึงข้อมูล Material ทั้งหมดจาก ZMATMASDW
        /// </summary>
        public async Task<List<MaterialMasterViewModel>> GetAllMaterialsAsync()
        {
            var materials = new List<MaterialMasterViewModel>();

            var sql = @"
                SELECT MATNR, MAKTX, GROES, MEINS, WGBEZ, MTART
                FROM [DW_KPI].[dbo].[ZMATMASDW]
                WHERE MTART IN ('ZFG','ZSF','ZSK','ZSV')
                AND LVORM <> 'X'
                AND MANDT = '910'
                ORDER BY MATNR";

            // ✅ ใช้ Connection String เป็นของตัวเอง
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                materials.Add(new MaterialMasterViewModel
                {
                    MaterialNumber = reader.GetString("MATNR"),
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    Unit = reader.IsDBNull("MEINS") ? "" : reader.GetString("MEINS"),
                    TypeOfFilm = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ"),
                    MaterialType = reader.IsDBNull("MTART") ? "" : reader.GetString("MTART")
                });
            }

            return materials;
        }

        /// <summary>
        /// ดึงข้อมูล Material ตาม Material Number
        /// </summary>
        public async Task<MaterialMasterViewModel?> GetMaterialByNumberAsync(string materialNumber)
        {
            var sql = @"
                SELECT MATNR, MAKTX, GROES, MEINS, WGBEZ, MTART
                FROM [DW_KPI].[dbo].[ZMATMASDW]
                WHERE MATNR = @MaterialNumber
                AND MTART IN ('ZFG','ZSF','ZSK','ZSV')
                AND LVORM <> 'X'
                AND MANDT = '910'";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@MaterialNumber", materialNumber);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new MaterialMasterViewModel
                {
                    MaterialNumber = reader.GetString("MATNR"),
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    Unit = reader.IsDBNull("MEINS") ? "" : reader.GetString("MEINS"),
                    TypeOfFilm = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ"),
                    MaterialType = reader.IsDBNull("MTART") ? "" : reader.GetString("MTART")
                };
            }

            return null;
        }

        /// <summary>
        /// ค้นหา Material
        /// </summary>
        public async Task<List<MaterialMasterViewModel>> SearchMaterialsAsync(string searchTerm)
        {
            var materials = new List<MaterialMasterViewModel>();

            var sql = @"
                SELECT MATNR, MAKTX, GROES, MEINS, WGBEZ, MTART
                FROM [DW_KPI].[dbo].[ZMATMASDW]
                WHERE MTART IN ('ZFG','ZSF','ZSK','ZSV')
                AND LVORM <> 'X'
                AND MANDT = '910'
                AND (MATNR LIKE @SearchTerm OR MAKTX LIKE @SearchTerm)
                ORDER BY MATNR";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                materials.Add(new MaterialMasterViewModel
                {
                    MaterialNumber = reader.GetString("MATNR"),
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    Unit = reader.IsDBNull("MEINS") ? "" : reader.GetString("MEINS"),
                    TypeOfFilm = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ"),
                    MaterialType = reader.IsDBNull("MTART") ? "" : reader.GetString("MTART")
                });
            }

            return materials;
        }
    }
}
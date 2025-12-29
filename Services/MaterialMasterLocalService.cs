using Microsoft.Data.SqlClient;
using QcChapWai.Data;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class MaterialMasterLocalService
    {
        private readonly SqlHelper _sqlHelper;
        private readonly MaterialMasterService _materialMasterService;

        public MaterialMasterLocalService(SqlHelper sqlHelper, MaterialMasterService materialMasterService)
        {
            _sqlHelper = sqlHelper;
            _materialMasterService = materialMasterService;
        }

        /// <summary>
        /// บันทึก Material Master ทีละ record
        /// </summary>
        public async Task<(bool success, string message)> SaveMaterialMasterAsync(string materialCode, string createdBy)
        {
            try
            {
                // 1. เช็คว่ามีข้อมูลซ้ำหรือไม่
                var existingCheck = await CheckMaterialExistsAsync(materialCode);
                if (existingCheck)
                {
                    return (false, $"มีรายการ Material Code: {materialCode} อยู่ในระบบแล้ว");
                }

                // 2. ดึงข้อมูลจาก ZMATMASDW
                var materialData = await _materialMasterService.GetMaterialByNumberAsync(materialCode);

                if (materialData == null)
                {
                    return (false, $"ไม่พบข้อมูล Material Code: {materialCode} ใน ZMATMASDW");
                }

                // 3. บันทึกลง MaterialMaster Table
                var sql = @"
                    INSERT INTO MaterialMaster (
                        MaterialCode, ProductName, Size, Unit, Plant, FilmType, 
                        IsActive, CreatedDate, CreatedBy
                    ) VALUES (
                        @MaterialCode, @ProductName, @Size, @Unit, @Plant, @FilmType,
                        @IsActive, @CreatedDate, @CreatedBy
                    )";

                var parameters = new[]
                {
                    new SqlParameter("@MaterialCode", materialData.MaterialNumber),
                    new SqlParameter("@ProductName", materialData.MaterialDescription),
                    new SqlParameter("@Size", (object?)materialData.Size ?? DBNull.Value),
                    new SqlParameter("@Unit", (object?)materialData.Unit ?? DBNull.Value),
                    new SqlParameter("@Plant", "KB01"),
                    new SqlParameter("@FilmType", (object?)materialData.TypeOfFilm ?? DBNull.Value),
                    new SqlParameter("@IsActive", true),
                    new SqlParameter("@CreatedDate", DateTime.Now),
                    new SqlParameter("@CreatedBy", createdBy)
                };

                await _sqlHelper.ExecuteNonQueryAsync(sql, parameters);

                return (true, $"บันทึก Material Code: {materialCode} สำเร็จ");
            }
            catch (Exception ex)
            {
                return (false, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        /// <summary>
        /// เช็คว่ามี Material Code ซ้ำหรือไม่
        /// </summary>
        public async Task<bool> CheckMaterialExistsAsync(string materialCode)
        {
            var sql = "SELECT COUNT(*) FROM MaterialMaster WHERE MaterialCode = @MaterialCode";
            var result = await _sqlHelper.ExecuteScalarAsync(sql, new SqlParameter("@MaterialCode", materialCode));
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// ดึงข้อมูล MaterialMaster ทั้งหมด
        /// </summary>
        public async Task<List<MaterialMaster>> GetAllMaterialMasterAsync()
        {
            var sql = @"
                SELECT MaterialId, MaterialCode, ProductName, Size, Unit, Plant, 
                       FilmType, IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM MaterialMaster
                WHERE IsActive = 1
                ORDER BY MaterialId DESC";

            return await _sqlHelper.ExecuteReaderListAsync(sql, MapMaterialMaster);
        }

        /// <summary>
        /// ดึงข้อมูล MaterialMaster ตาม MaterialCode
        /// </summary>
        public async Task<MaterialMaster?> GetMaterialMasterByCodeAsync(string materialCode)
        {
            var sql = @"
                SELECT MaterialId, MaterialCode, ProductName, Size, Unit, Plant, 
                       FilmType, IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM MaterialMaster
                WHERE MaterialCode = @MaterialCode AND IsActive = 1";

            return await _sqlHelper.ExecuteReaderAsync(sql, MapMaterialMaster,
                new SqlParameter("@MaterialCode", materialCode));
        }

        private static MaterialMaster MapMaterialMaster(SqlDataReader reader)
        {
            return new MaterialMaster
            {
                MaterialId = reader.GetInt32("MaterialId"),
                MaterialCode = reader.GetString("MaterialCode"),
                ProductName = reader.GetString("ProductName"),
                Size = reader.IsDBNull("Size") ? null : reader.GetString("Size"),
                Unit = reader.IsDBNull("Unit") ? null : reader.GetString("Unit"),
                Plant = reader.GetString("Plant"),
                FilmType = reader.IsDBNull("FilmType") ? null : reader.GetString("FilmType"),
                IsActive = reader.GetBoolean("IsActive"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                UpdatedDate = reader.IsDBNull("UpdatedDate") ? null : reader.GetDateTime("UpdatedDate"),
                UpdatedBy = reader.IsDBNull("UpdatedBy") ? null : reader.GetString("UpdatedBy")
            };
        }
    }
}
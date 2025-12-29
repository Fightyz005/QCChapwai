using Microsoft.Data.SqlClient;
using QcChapWai.Data;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class MaterialService
    {
        private readonly SqlHelper _sqlHelper;

        public MaterialService(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public async Task<List<DocumentInspection>> GetAllMaterialsAsync(string? searchTerm = null, string? docFg = null, string? docPlant = null)
        {
            var sql = @"
                SELECT DocId, DocPlant, DocProcess, DocInspection, DocUnit, DocMin, DocMax, DocStd,
                       DocIsLr, DocLeft, DocIsMm, DocMinPlus, DocRemark, DocCreateDate,
                       DocCustomer, DocSo, DocSoItem, DocFg, DocFgItem, DocSize, DocTypeOfFilm,
                       DocMachineNo, DocLotno, DocPassed, DocNcNo, DocApproverA, DocApproverB,
                       DocRemarkA, DocRemarkB, DocOrderSort, DocHide, DocDataType
                FROM DocumentInspection 
                WHERE DocHide = 0";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                sql += " AND (DocInspection LIKE @searchTerm OR DocFgItem LIKE @searchTerm OR DocCustomer LIKE @searchTerm OR DocFg LIKE @searchTerm)";
                parameters.Add(new SqlParameter("@searchTerm", $"%{searchTerm}%"));
            }

            if (!string.IsNullOrEmpty(docFg) && docFg != "all")
            {
                sql += " AND DocFg = @docFg";
                parameters.Add(new SqlParameter("@docFg", docFg));
            }

            if (!string.IsNullOrEmpty(docPlant) && docPlant != "all")
            {
                sql += " AND DocPlant = @docPlant";
                parameters.Add(new SqlParameter("@docPlant", docPlant));
            }

            sql += " ORDER BY DocFg, DocFgItem, DocOrderSort";

            return await _sqlHelper.ExecuteReaderListAsync(sql, MapDocumentInspection, parameters.ToArray());
        }

        /// <summary>
        /// ดึงข้อมูล Material Master ทั้งหมด (สำหรับ Index)
        /// </summary>
        public async Task<List<MaterialMaster>> GetAllMaterialMastersAsync()
        {
            var sql = @"
                SELECT MaterialId, MaterialCode, ProductName, Size, Unit, Plant, 
                       FilmType, IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM MaterialMaster
                WHERE IsActive = 1
                ORDER BY MaterialCode";

            return await _sqlHelper.ExecuteReaderListAsync(sql, reader => new MaterialMaster
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
            });
        }

        /// <summary>
        /// ดึง Parameters ตาม Material Code
        /// </summary>
        public async Task<List<DocumentInspection>> GetParametersByMaterialCodeAsync(string materialCode)
        {
            var sql = @"
                SELECT DocId, DocPlant, DocProcess, DocInspection, DocUnit, DocMin, DocMax, DocStd,
                       DocIsLr, DocLeft, DocIsMm, DocMinPlus, DocRemark, DocCreateDate,
                       DocCustomer, DocSo, DocSoItem, DocFg, DocFgItem, DocSize, DocTypeOfFilm,
                       DocMachineNo, DocLotno, DocPassed, DocNcNo, DocApproverA, DocApproverB,
                       DocRemarkA, DocRemarkB, DocOrderSort, DocHide, DocDataType
                FROM DocumentInspection 
                WHERE DocFg = @MaterialCode AND DocHide = 0
                ORDER BY DocOrderSort";

            return await _sqlHelper.ExecuteReaderListAsync(sql, MapDocumentInspection,
                new SqlParameter("@MaterialCode", materialCode));
        }


        public async Task<DocumentInspection?> GetMaterialByIdAsync(int id)
        {
            var sql = @"
                SELECT DocId, DocPlant, DocProcess, DocInspection, DocUnit, DocMin, DocMax, DocStd,
                       DocIsLr, DocLeft, DocIsMm, DocMinPlus, DocRemark, DocCreateDate,
                       DocCustomer, DocSo, DocSoItem, DocFg, DocFgItem, DocSize, DocTypeOfFilm,
                       DocMachineNo, DocLotno, DocPassed, DocNcNo, DocApproverA, DocApproverB,
                       DocRemarkA, DocRemarkB, DocOrderSort, DocHide, DocDataType
                FROM DocumentInspection 
                WHERE DocId = @id";

            return await _sqlHelper.ExecuteReaderAsync(sql, MapDocumentInspection, new SqlParameter("@id", id));
        }

        public async Task<int> CreateMaterialAsync(DocumentInspection material)
        {
            var sql = @"
                INSERT INTO DocumentInspection (
                    DocPlant, DocProcess, DocInspection, DocUnit, DocMin, DocMax, DocStd,
                    DocIsLr, DocLeft, DocIsMm, DocMinPlus, DocRemark, DocCreateDate,
                    DocCustomer, DocSo, DocSoItem, DocFg, DocFgItem, DocSize, DocTypeOfFilm,
                    DocMachineNo, DocLotno, DocPassed, DocNcNo, DocApproverA, DocApproverB,
                    DocRemarkA, DocRemarkB, DocOrderSort, DocHide, DocDataType
                ) VALUES (
                    @DocPlant, @DocProcess, @DocInspection, @DocUnit, @DocMin, @DocMax, @DocStd,
                    @DocIsLr, @DocLeft, @DocIsMm, @DocMinPlus, @DocRemark, @DocCreateDate,
                    @DocCustomer, @DocSo, @DocSoItem, @DocFg, @DocFgItem, @DocSize, @DocTypeOfFilm,
                    @DocMachineNo, @DocLotno, @DocPassed, @DocNcNo, @DocApproverA, @DocApproverB,
                    @DocRemarkA, @DocRemarkB, @DocOrderSort, @DocHide, @DocDataType
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var parameters = CreateParametersFromMaterial(material);
            var result = await _sqlHelper.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateMaterialAsync(DocumentInspection material)
        {
            var sql = @"
                UPDATE DocumentInspection SET 
                    DocPlant = @DocPlant, DocProcess = @DocProcess, DocInspection = @DocInspection, 
                    DocUnit = @DocUnit, DocMin = @DocMin, DocMax = @DocMax, DocStd = @DocStd,
                    DocIsLr = @DocIsLr, DocLeft = @DocLeft, DocIsMm = @DocIsMm, DocMinPlus = @DocMinPlus, 
                    DocRemark = @DocRemark, DocCustomer = @DocCustomer, DocSo = @DocSo, DocSoItem = @DocSoItem, 
                    DocFg = @DocFg, DocFgItem = @DocFgItem, DocSize = @DocSize, DocTypeOfFilm = @DocTypeOfFilm,
                    DocMachineNo = @DocMachineNo, DocLotno = @DocLotno, DocPassed = @DocPassed, 
                    DocNcNo = @DocNcNo, DocApproverA = @DocApproverA, DocApproverB = @DocApproverB,
                    DocRemarkA = @DocRemarkA, DocRemarkB = @DocRemarkB, DocOrderSort = @DocOrderSort, DocHide = @DocHide,
                    DocDataType = @DocDataType
                WHERE DocId = @DocId";

            var parameters = CreateParametersFromMaterial(material, includeId: true);
            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteMaterialAsync(int id)
        {
            var sql = "DELETE FROM DocumentInspection WHERE DocId = @id";
            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, new SqlParameter("@id", id));
            return rowsAffected > 0;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var sql = @"
                UPDATE DocumentInspection 
                SET DocPassed = CASE WHEN DocPassed = 1 THEN 0 ELSE 1 END 
                WHERE DocId = @id;
                SELECT DocPassed FROM DocumentInspection WHERE DocId = @id;";

            var result = await _sqlHelper.ExecuteScalarAsync(sql, new SqlParameter("@id", id));
            return Convert.ToBoolean(result);
        }

        public async Task<List<string>> GetUniqueFgsAsync()
        {
            var sql = "SELECT DISTINCT DocFg FROM DocumentInspection WHERE DocHide = 0 ORDER BY DocFg";
            var results = await _sqlHelper.ExecuteReaderListAsync(sql, reader => reader.GetString("DocFg"));
            return results;
        }

        public async Task<List<string>> GetUniquePlantsAsync()
        {
            var sql = "SELECT DISTINCT DocPlant FROM DocumentInspection WHERE DocHide = 0 ORDER BY DocPlant";
            var results = await _sqlHelper.ExecuteReaderListAsync(sql, reader => reader.GetString("DocPlant"));
            return results;
        }

        private static DocumentInspection MapDocumentInspection(SqlDataReader reader)
        {
            return new DocumentInspection
            {
                DocId = reader.GetInt32("DocId"),
                DocPlant = reader.GetString("DocPlant"),
                DocProcess = reader.GetString("DocProcess"),
                DocInspection = reader.GetString("DocInspection"),
                DocUnit = reader.GetString("DocUnit"),
                DocMin = reader.GetDecimal("DocMin"),
                DocMax = reader.GetDecimal("DocMax"),
                DocStd = reader.GetDecimal("DocStd"),
                DocIsLr = reader.GetBoolean("DocIsLr"),
                DocLeft = reader.IsDBNull("DocLeft") ? null : reader.GetDecimal("DocLeft"),
                DocIsMm = reader.GetBoolean("DocIsMm"),
                DocMinPlus = reader.IsDBNull("DocMinPlus") ? null : reader.GetDecimal("DocMinPlus"),
                DocRemark = reader.IsDBNull("DocRemark") ? null : reader.GetString("DocRemark"),
                DocCreateDate = reader.IsDBNull("DocCreateDate") ? null : reader.GetDateTime("DocCreateDate"),
                DocCustomer = reader.GetString("DocCustomer"),
                DocSo = reader.GetString("DocSo"),
                DocSoItem = reader.GetString("DocSoItem"),
                DocFg = reader.GetString("DocFg"),
                DocFgItem = reader.GetString("DocFgItem"),
                DocSize = reader.GetString("DocSize"),
                DocTypeOfFilm = reader.GetString("DocTypeOfFilm"),
                DocMachineNo = reader.IsDBNull("DocMachineNo") ? null : reader.GetString("DocMachineNo"),
                DocLotno = reader.IsDBNull("DocLotno") ? null : reader.GetString("DocLotno"),
                DocPassed = reader.GetBoolean("DocPassed"),
                DocNcNo = reader.IsDBNull("DocNcNo") ? null : reader.GetString("DocNcNo"),
                DocApproverA = reader.IsDBNull("DocApproverA") ? null : reader.GetString("DocApproverA"),
                DocApproverB = reader.IsDBNull("DocApproverB") ? null : reader.GetString("DocApproverB"),
                DocRemarkA = reader.IsDBNull("DocRemarkA") ? null : reader.GetString("DocRemarkA"),
                DocRemarkB = reader.IsDBNull("DocRemarkB") ? null : reader.GetString("DocRemarkB"),
                DocOrderSort = reader.GetInt32("DocOrderSort"),
                DocHide = reader.GetBoolean("DocHide"),
                DocDataType = reader.IsDBNull("DocDataType") ? null : reader.GetString("DocDataType")
            };
        }

        private static SqlParameter[] CreateParametersFromMaterial(DocumentInspection material, bool includeId = false)
        {
            var parameters = new List<SqlParameter>
            {
                new("@DocPlant", material.DocPlant),
                new("@DocProcess", material.DocProcess),
                new("@DocInspection", material.DocInspection),
                new("@DocUnit", material.DocUnit),
                new("@DocMin", material.DocMin),
                new("@DocMax", material.DocMax),
                new("@DocStd", material.DocStd),
                new("@DocIsLr", material.DocIsLr),
                new("@DocLeft", (object?)material.DocLeft ?? DBNull.Value),
                new("@DocIsMm", material.DocIsMm),
                new("@DocMinPlus", (object?)material.DocMinPlus ?? DBNull.Value),
                new("@DocRemark", (object?)material.DocRemark ?? DBNull.Value),
                new("@DocCreateDate", (object?)material.DocCreateDate ?? DBNull.Value),
                new("@DocCustomer", material.DocCustomer),
                new("@DocSo", material.DocSo),
                new("@DocSoItem", material.DocSoItem),
                new("@DocFg", material.DocFg),
                new("@DocFgItem", material.DocFgItem),
                new("@DocSize", material.DocSize),
                new("@DocTypeOfFilm", material.DocTypeOfFilm),
                new("@DocMachineNo", (object?)material.DocMachineNo ?? DBNull.Value),
                new("@DocLotno", (object?)material.DocLotno ?? DBNull.Value),
                new("@DocPassed", material.DocPassed),
                new("@DocNcNo", (object?)material.DocNcNo ?? DBNull.Value),
                new("@DocApproverA", (object?)material.DocApproverA ?? DBNull.Value),
                new("@DocApproverB", (object?)material.DocApproverB ?? DBNull.Value),
                new("@DocRemarkA", (object?)material.DocRemarkA ?? DBNull.Value),
                new("@DocRemarkB", (object?)material.DocRemarkB ?? DBNull.Value),
                new("@DocOrderSort", material.DocOrderSort),
                new("@DocHide", material.DocHide),
                new("@DocDataType", material.DocDataType)
            };

            if (includeId)
            {
                parameters.Add(new SqlParameter("@DocId", material.DocId));
            }

            return parameters.ToArray();
        }
    }
}
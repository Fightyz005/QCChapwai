using Microsoft.Data.SqlClient;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class ProductionOrderService
    {
        private readonly string _connectionString;

        public ProductionOrderService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MaterialConnection") ??
                throw new ArgumentNullException("MaterialConnection not found");
        }

        /// <summary>
        /// ✅ ดึงข้อมูล Production Order ตาม AUFNR พร้อม Validate MaterialMaster
        /// </summary>
        public async Task<ProductionOrderValidationResult> GetAndValidateProductionOrderAsync(
            string productionOrderNumber,
            MaterialMasterLocalService materialMasterService)
        {
            var result = new ProductionOrderValidationResult();

            // 1. ดึงข้อมูลจาก ZQC_PPORDER_DW
            var sql = @"
                SELECT [AUFNR], [KDAUF], [KDPOS], [KUNNR], [NAME1], 
                       [PLNBEZ], [MAKTX], [GROES], [WGBEZ]
                FROM [DW_KPI].[dbo].[ZQC_PPORDER_DW]
                WHERE [AUFNR] = @ProductionOrder";

            string ppOrder = string.Empty;

            if (productionOrderNumber.Length == 8)
            {
                ppOrder = "0000" + productionOrderNumber;
            }
            else
            {
                ppOrder = productionOrderNumber;
            }

                using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductionOrder", ppOrder);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                result.Success = false;
                result.Message = $"ไม่พบ Production Order: {productionOrderNumber}";
                return result;
            }

            // 2. อ่านข้อมูล
            result.ProductionOrder = new ProductionOrderViewModel
            {
                OrderNumber = reader.GetString("AUFNR"),
                SalesOrder = reader.IsDBNull("KDAUF") ? "" : reader.GetString("KDAUF"),
                SalesOrderItem = reader.IsDBNull("KDPOS") ? "" : reader.GetString("KDPOS"),
                CustomerNumber = reader.IsDBNull("KUNNR") ? "" : reader.GetString("KUNNR"),
                CustomerName = reader.IsDBNull("NAME1") ? "" : reader.GetString("NAME1"),
                Plant = "KB01", // ✅ Fixed Plant
                MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                MaterialGroupDesc = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ")
            };

            // ✅ PLNBEZ คือ Material Code ที่ต้องตรวจสอบ
            string materialCode = reader.IsDBNull("PLNBEZ") ? "" : reader.GetString("PLNBEZ");
            result.MaterialCode = materialCode;

            await connection.CloseAsync();

            // 3. ✅ ตรวจสอบว่ามี Material Code ใน MaterialMaster หรือไม่
            if (!string.IsNullOrEmpty(materialCode))
            {
                var materialMaster = await materialMasterService.GetMaterialMasterByCodeAsync(materialCode);

                if (materialMaster != null)
                {
                    result.Success = true;
                    result.HasMaterialMaster = true;
                    result.Message = $"พบข้อมูล Material Code: {materialCode}";
                }
                else
                {
                    result.Success = false;
                    result.HasMaterialMaster = false;
                    result.Message = $"ยังไม่มี Material Code '{materialCode}' ใน Material Master";
                }
            }
            else
            {
                result.Success = false;
                result.HasMaterialMaster = false;
                result.Message = "ไม่พบ Material Code (PLNBEZ) ในข้อมูล Production Order";
            }

            return result;
        }

        /// <summary>
        /// ดึงข้อมูล Production Order ตาม Order Number (เดิม)
        /// </summary>
        public async Task<ProductionOrderViewModel?> GetProductionOrderByNumberAsync(string orderNumber)
        {
            var sql = @"
                SELECT [AUFNR], [KDAUF], [KDPOS], [KUNNR], [NAME1], 
                       [PLNBEZ], [MAKTX], [GROES], [WGBEZ]
                FROM [DW_KPI].[dbo].[ZQC_PPORDER_DW]
                WHERE [AUFNR] = @OrderNumber";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OrderNumber", orderNumber);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ProductionOrderViewModel
                {
                    OrderNumber = reader.GetString("AUFNR"),
                    SalesOrder = reader.IsDBNull("KDAUF") ? "" : reader.GetString("KDAUF"),
                    SalesOrderItem = reader.IsDBNull("KDPOS") ? "" : reader.GetString("KDPOS"),
                    CustomerNumber = reader.IsDBNull("KUNNR") ? "" : reader.GetString("KUNNR"),
                    CustomerName = reader.IsDBNull("NAME1") ? "" : reader.GetString("NAME1"),
                    Plant = "KB01",
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    MaterialGroupDesc = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ")
                };
            }

            return null;
        }

        /// <summary>
        /// ดึงข้อมูล Production Order ทั้งหมด (เดิม)
        /// </summary>
        public async Task<List<ProductionOrderViewModel>> GetAllProductionOrdersAsync()
        {
            var orders = new List<ProductionOrderViewModel>();

            var sql = @"
                SELECT TOP (1000) 
                    [AUFNR], [KDAUF], [KDPOS], [KUNNR], [NAME1], 
                    [PLNBEZ], [MAKTX], [GROES], [WGBEZ]
                FROM [DW_KPI].[dbo].[ZQC_PPORDER_DW]
                ORDER BY [AUFNR] DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new ProductionOrderViewModel
                {
                    OrderNumber = reader.GetString("AUFNR"),
                    SalesOrder = reader.IsDBNull("KDAUF") ? "" : reader.GetString("KDAUF"),
                    SalesOrderItem = reader.IsDBNull("KDPOS") ? "" : reader.GetString("KDPOS"),
                    CustomerNumber = reader.IsDBNull("KUNNR") ? "" : reader.GetString("KUNNR"),
                    CustomerName = reader.IsDBNull("NAME1") ? "" : reader.GetString("NAME1"),
                    Plant = "KB01",
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    MaterialGroupDesc = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ")
                });
            }

            return orders;
        }

        /// <summary>
        /// ค้นหา Production Order (เดิม)
        /// </summary>
        public async Task<List<ProductionOrderViewModel>> SearchProductionOrdersAsync(string searchTerm)
        {
            var orders = new List<ProductionOrderViewModel>();

            var sql = @"
                SELECT TOP (100) 
                    [AUFNR], [KDAUF], [KDPOS], [KUNNR], [NAME1], 
                    [PLNBEZ], [MAKTX], [GROES], [WGBEZ]
                FROM [DW_KPI].[dbo].[ZQC_PPORDER_DW]
                WHERE [AUFNR] LIKE @SearchTerm 
                   OR [KDAUF] LIKE @SearchTerm
                   OR [NAME1] LIKE @SearchTerm
                   OR [MAKTX] LIKE @SearchTerm
                ORDER BY [AUFNR] DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new ProductionOrderViewModel
                {
                    OrderNumber = reader.GetString("AUFNR"),
                    SalesOrder = reader.IsDBNull("KDAUF") ? "" : reader.GetString("KDAUF"),
                    SalesOrderItem = reader.IsDBNull("KDPOS") ? "" : reader.GetString("KDPOS"),
                    CustomerNumber = reader.IsDBNull("KUNNR") ? "" : reader.GetString("KUNNR"),
                    CustomerName = reader.IsDBNull("NAME1") ? "" : reader.GetString("NAME1"),
                    Plant = "KB01",
                    MaterialDescription = reader.IsDBNull("MAKTX") ? "" : reader.GetString("MAKTX"),
                    Size = reader.IsDBNull("GROES") ? "" : reader.GetString("GROES"),
                    MaterialGroupDesc = reader.IsDBNull("WGBEZ") ? "" : reader.GetString("WGBEZ")
                });
            }

            return orders;
        }
    }

    /// <summary>
    /// ✅ Result Model สำหรับ Validation
    /// </summary>
    public class ProductionOrderValidationResult
    {
        public bool Success { get; set; }
        public bool HasMaterialMaster { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public ProductionOrderViewModel? ProductionOrder { get; set; }
    }
}
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QcChapWai.Controllers;
using QcChapWai.Data;
using QcChapWai.Models;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;


namespace QcChapWai.Services
{
    public class ChecklistService
    {
        private readonly SqlHelper _sqlHelper;
        private readonly InspectCodeService _inspectCodeService;
        private readonly UserService _userService;

        public ChecklistService(SqlHelper sqlHelper, InspectCodeService inspectCodeService, UserService userService)
        {
            _sqlHelper = sqlHelper;
            _inspectCodeService = inspectCodeService;
            _userService = userService;
        }

        public async Task<List<ChecklistSummaryViewModel>> GetActiveChecklistsAsync()
        {
            var sql = @"
                SELECT 
                    ic.InspectCode,
                    ic.ChecklistId,
                    ic.FgCode,
                    ic.ItemName,
                    ic.SoNumber,
                    ic.Customer,
                    ic.Status,
                    ic.CreatedDate,
                    ic.CreatedBy,
                    ic.Inspector,
                    ic.Approver,
                    ic.ProductionOrder,
                    ic.SalesOrderItem,
                    ic.CustomerCode,
                    ic.MachineZone,
                    ic.MachineProcess,
                    ic.MachineName,
                    ic.MachineStorage,
                    COUNT(DISTINCT ir.RecordId) as TotalRecords,
                    COUNT(imd.MeasurementId) as TotalMeasurements,
                    SUM(CASE WHEN imd.IsPass = 1 THEN 1 ELSE 0 END) as PassedMeasurements,
                    SUM(CASE WHEN imd.IsFail = 1 THEN 1 ELSE 0 END) as FailedMeasurements
                FROM InspectionChecklist ic
                LEFT JOIN InspectionRecord ir ON ic.ChecklistId = ir.ChecklistId
                LEFT JOIN InspectionMeasurementData imd ON ir.RecordId = imd.RecordId
                WHERE ic.Status IN ('Active', 'Completed')
                GROUP BY ic.InspectCode,ic.ChecklistId, ic.FgCode, ic.ItemName, ic.SoNumber, ic.Customer, ic.Status, ic.CreatedDate, ic.CreatedBy, ic.Inspector,
                    ic.Approver,
                    ic.ProductionOrder,
                    ic.SalesOrderItem,
                    ic.CustomerCode,
                    ic.MachineZone,
                    ic.MachineProcess,
                    ic.MachineName,
                    ic.MachineStorage
                ORDER BY ic.CreatedDate DESC";

            var results = await _sqlHelper.ExecuteReaderListAsync(sql, reader => new ChecklistSummaryViewModel
            {
                InspectCode = reader.GetString("InspectCode"),
                ChecklistId = reader.GetInt32("ChecklistId"),
                FgCode = reader.GetString("FgCode"),
                ItemName = reader.GetString("ItemName"),
                Customer = reader.GetString("Customer"),
                SoNumber = reader.IsDBNull("SoNumber") ? null : reader.GetString("SoNumber"),
                Status = reader.GetString("Status"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                Inspector = reader.IsDBNull("Inspector") ? null : reader.GetString("Inspector"),
                Approver = reader.IsDBNull("Approver") ? null : reader.GetString("Approver"),
                SalesOrderItem = reader.IsDBNull("SalesOrderItem") ? null : reader.GetString("SalesOrderItem"),
                CustomerCode = reader.IsDBNull("CustomerCode") ? null : reader.GetString("CustomerCode"),
                MachineZone = reader.IsDBNull("MachineZone") ? null : reader.GetString("MachineZone"),
                MachineName = reader.IsDBNull("MachineName") ? null : reader.GetString("MachineName"),
                MachineProcess = reader.IsDBNull("MachineProcess") ? null : reader.GetString("MachineProcess"),
                MachineStorage = reader.IsDBNull("MachineStorage") ? null : reader.GetString("MachineStorage"),
                ProductionOrder = reader.IsDBNull("ProductionOrder") ? null : reader.GetString("ProductionOrder"),
                TotalRecords = reader.GetInt32("TotalRecords"),
                TotalMeasurements = reader.GetInt32("TotalMeasurements"),
                PassedMeasurements = reader.GetInt32("PassedMeasurements"),
                FailedMeasurements = reader.GetInt32("FailedMeasurements"),
                PassRate = reader.GetInt32("TotalMeasurements") > 0
                    ? Math.Round((decimal)reader.GetInt32("PassedMeasurements") / reader.GetInt32("TotalMeasurements") * 100, 1)
                    : 0
            });

            return results;
        }

        public async Task<InspectionChecklist?> GetChecklistByIdAsync(int id)
        {
            var sql = @"
                SELECT [ChecklistId]
                      ,[FgCode]
                      ,[ItemName]
                      ,[Customer]
                      ,[SoNumber]
                      ,[Plant]
                      ,[Process]
                      ,[Size]
                      ,[TypeOfFilm]
                      ,[Status]
                      ,[CreatedDate]
                      ,[CreatedBy]
                      ,[CompletedDate]
                      ,[CompletedBy]
                      ,[Remark]
                      ,[Inspector]
                      ,[Approver]
                      ,[ProductionOrder]
                      ,[SalesOrderItem]
                      ,[CustomerCode]
                      ,[MachineZone]
                      ,[MachineProcess]
                      ,[MachineName]
                      ,[MachineStorage]
                      ,[InspectCode]
                        ,[InspectorTeamA]
                      ,[InspectorTeamB]
                FROM InspectionChecklist
                WHERE ChecklistId = @id";

            return await _sqlHelper.ExecuteReaderAsync(sql, MapInspectionChecklist, new SqlParameter("@id", id));
        }

        // ✅ อัพเดท CreateChecklistAsync ใน ChecklistService.cs

        public async Task<int> CreateChecklistAsync(InspectionChecklist checklist)
        {
            // ✅ Generate InspectCode ก่อนบันทึก
            if (string.IsNullOrEmpty(checklist.InspectCode))
            {
                checklist.InspectCode = await _inspectCodeService.GenerateUniqueInspectCodeAsync(
                    checklist.Plant ?? "KB01",
                    checklist.MachineZone ?? "A"
                );
            }

            var sql = @"
                INSERT INTO InspectionChecklist (
                    FgCode, ItemName, Customer, SoNumber, Plant, Process, Size, TypeOfFilm,
                    Status, CreatedDate, CreatedBy, Remark, Inspector, Approver,
                    ProductionOrder, SalesOrderItem, CustomerCode,
                    MachineZone, MachineProcess, MachineName, MachineStorage,
                    InspectCode, InspectorTeamA, InspectorTeamB
                ) VALUES (
                    @FgCode, @ItemName, @Customer, @SoNumber, @Plant, @Process, @Size, @TypeOfFilm,
                    @Status, @CreatedDate, @CreatedBy, @Remark, @Inspector, @Approver,
                    @ProductionOrder, @SalesOrderItem, @CustomerCode,
                    @MachineZone, @MachineProcess, @MachineName, @MachineStorage,
                    @InspectCode, @InspectorTeamA, @InspectorTeamB
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var parameters = new[]
            {
                new SqlParameter("@FgCode", checklist.FgCode),
                new SqlParameter("@ItemName", checklist.ItemName),
                new SqlParameter("@Customer", checklist.Customer),
                new SqlParameter("@SoNumber", (object?)checklist.SoNumber ?? DBNull.Value),
                new SqlParameter("@Plant", checklist.Plant),
                new SqlParameter("@Process", checklist.Process),
                new SqlParameter("@Size", (object?)checklist.Size ?? DBNull.Value),
                new SqlParameter("@TypeOfFilm", (object?)checklist.TypeOfFilm ?? DBNull.Value),
                new SqlParameter("@Status", checklist.Status),
                new SqlParameter("@CreatedDate", checklist.CreatedDate),
                new SqlParameter("@CreatedBy", (object?)checklist.CreatedBy ?? DBNull.Value),
                new SqlParameter("@Remark", (object?)checklist.Remark ?? DBNull.Value),
                new SqlParameter("@Inspector", (object?)checklist.Inspector ?? DBNull.Value),
                new SqlParameter("@Approver", (object?)checklist.Approver ?? DBNull.Value),
        
                // Production Order fields
                new SqlParameter("@ProductionOrder", (object?)checklist.ProductionOrder ?? DBNull.Value),
                new SqlParameter("@SalesOrderItem", (object?)checklist.SalesOrderItem ?? DBNull.Value),
                new SqlParameter("@CustomerCode", (object?)checklist.CustomerCode ?? DBNull.Value),
        
                // Machine fields
                new SqlParameter("@MachineZone", (object?)checklist.MachineZone ?? DBNull.Value),
                new SqlParameter("@MachineProcess", (object?)checklist.MachineProcess ?? DBNull.Value),
                new SqlParameter("@MachineName", (object?)checklist.MachineName ?? DBNull.Value),
                new SqlParameter("@MachineStorage", (object?)checklist.MachineStorage ?? DBNull.Value),
        
                // ✅ InspectCode
                new SqlParameter("@InspectCode", checklist.InspectCode),
                new SqlParameter("@InspectorTeamA", checklist.InspectorTeamAId),
                new SqlParameter("@InspectorTeamB", checklist.InspectorTeamBId)
            };

            var result = await _sqlHelper.ExecuteScalarAsync(sql, parameters);
            checklist.ChecklistId = Convert.ToInt32(result);
            return checklist.ChecklistId;
        }

        public async Task<List<InspectionRecordViewModel>> GetInspectionRecordsAsync(int checklistId)
        {
            var sql = @"
                SELECT 
                    ir.RecordId,
                    ir.Shift,
                    CONVERT(varchar(5), ir.InspectionTime, 108) as Time,
                    ir.Note,
                    imd.ParameterId,
                    imd.ParameterName,
                    imd.MeasurementType,
                    imd.MeasurementValue,
                    imd.Unit,
                    imd.IsPass,
                    imd.IsFail,
                    imd.MinValue,
                    imd.MaxValue,
                    imd.StandardValue
                FROM InspectionRecord ir
                LEFT JOIN InspectionMeasurementData imd ON ir.RecordId = imd.RecordId
                WHERE ir.ChecklistId = @checklistId
                ORDER BY ir.CreatedDate, ir.RecordId, imd.ParameterId";

            var recordsDict = new Dictionary<int, InspectionRecordViewModel>();

            await _sqlHelper.ExecuteReaderListAsync(sql, reader =>
            {
                var recordId = reader.GetInt32("RecordId");

                if (!recordsDict.ContainsKey(recordId))
                {
                    recordsDict[recordId] = new InspectionRecordViewModel
                    {
                        RecordId = recordId,
                        Shift = reader.GetString("Shift"),
                        Time = reader.IsDBNull("Time") ? "" : reader.GetString("Time"),
                        Note = reader.IsDBNull("Note") ? null : reader.GetString("Note"),
                        Measurements = new List<MeasurementViewModel>()
                    };
                }

                if (!reader.IsDBNull("ParameterId"))
                {
                    recordsDict[recordId].Measurements.Add(new MeasurementViewModel
                    {
                        ParameterId = reader.GetString("ParameterId"),
                        ParameterName = reader.GetString("ParameterName"),
                        MeasurementType = reader.GetString("MeasurementType"),
                        Value = reader.IsDBNull("MeasurementValue") ? null : reader.GetString("MeasurementValue"),
                        Unit = reader.IsDBNull("Unit") ? null : reader.GetString("Unit"),
                        IsPass = reader.GetBoolean("IsPass"),
                        IsFail = reader.GetBoolean("IsFail"),
                        MinValue = reader.IsDBNull("MinValue") ? null : reader.GetDecimal("MinValue"),
                        MaxValue = reader.IsDBNull("MaxValue") ? null : reader.GetDecimal("MaxValue"),
                        StandardValue = reader.IsDBNull("StandardValue") ? null : reader.GetDecimal("StandardValue")
                    });
                }

                return recordId;
            }, new SqlParameter("@checklistId", checklistId));

            return recordsDict.Values.ToList();
        }



        public async Task SaveInspectionDataAsync(InspectionDataRequest request)
        {
            using var connection = _sqlHelper.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Update checklist with additional info
                var updateChecklistSql = @"
                    UPDATE InspectionChecklist 
                    SET Remark = @Remark, Inspector = @Inspector, Approver = @Approver
                    WHERE ChecklistId = @ChecklistId";

                await _sqlHelper.ExecuteNonQueryAsync(updateChecklistSql,
                    new SqlParameter("@ChecklistId", request.ChecklistId),
                    new SqlParameter("@Remark", (object?)request.Remark ?? DBNull.Value),
                    new SqlParameter("@Inspector", (object?)request.Inspector ?? DBNull.Value),
                    new SqlParameter("@Approver", (object?)request.Approver ?? DBNull.Value)
                );

                // Delete existing records for this checklist
                var deleteRecordsSql = "DELETE FROM InspectionRecord WHERE ChecklistId = @ChecklistId";
                await _sqlHelper.ExecuteNonQueryAsync(deleteRecordsSql, new SqlParameter("@ChecklistId", request.ChecklistId));

                // Insert new inspection records
                for (int i = 0; i < request.InspectionRows.Count; i++)
                {
                    var row = request.InspectionRows[i];

                    var insertRecordSql = @"
                        INSERT INTO InspectionRecord (ChecklistId, Shift, InspectionTime, Note, CreatedDate)
                        VALUES (@ChecklistId, @Shift, @InspectionTime, @Note, @CreatedDate);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var recordId = await _sqlHelper.ExecuteScalarAsync(insertRecordSql,
                        new SqlParameter("@ChecklistId", request.ChecklistId),
                        new SqlParameter("@Shift", row.Shift),
                        new SqlParameter("@InspectionTime", TimeSpan.Parse(row.Time)),
                        new SqlParameter("@Note", (object?)row.Note ?? DBNull.Value),
                        new SqlParameter("@CreatedDate", DateTime.Now)
                    );

                    var recordIdInt = Convert.ToInt32(recordId);

                    // Insert measurements for this record
                    var rowMeasurements = request.Measurements.Where(m => m.RowIndex == i).ToList();
                    foreach (var measurement in rowMeasurements)
                    {
                        var insertMeasurementSql = @"
                            INSERT INTO InspectionMeasurementData (
                                RecordId, ParameterId, ParameterName, MeasurementType, MeasurementValue,
                                NumericValue, ActualValue, Unit, PassFailValue, IsPass, IsFail, 
                                MinValue, MaxValue, StandardValue, CreatedDate
                            ) VALUES (
                                @RecordId, @ParameterId, @ParameterName, @MeasurementType, @MeasurementValue,
                                @NumericValue, @ActualValue, @Unit, @PassFailValue, @IsPass, @IsFail, 
                                @MinValue, @MaxValue, @StandardValue, @CreatedDate
                            )";

                        decimal? numericValue = null;
                        decimal? actualValue = null;
                        bool? passFailValue = null;
                        decimal? minValue = null;
                        decimal? maxValue = null;
                        decimal? stdValue = null;

                        if (measurement.Type == "number")
                        {
                            if (decimal.TryParse(measurement.Value, out var numVal))
                            {
                                numericValue = numVal;
                                actualValue = numVal; // เก็บค่า Actual
                            }

                            // เก็บค่า Min, Max, Std จาก measurement
                            minValue = measurement.MinValue;
                            maxValue = measurement.MaxValue;
                            stdValue = measurement.StandardValue;
                        }
                        else if (measurement.Type == "leftright")
                        {
                            // สำหรับ Left/Right ไม่ต้องเก็บค่าตัวเลข
                            passFailValue = true; // ถือว่าผ่านเสมอ
                        }

                        await _sqlHelper.ExecuteNonQueryAsync(insertMeasurementSql,
                            new SqlParameter("@RecordId", recordIdInt),
                            new SqlParameter("@ParameterId", measurement.Parameter),
                            new SqlParameter("@ParameterName", measurement.ParameterName),
                            new SqlParameter("@MeasurementType", measurement.Type),
                            new SqlParameter("@MeasurementValue", (object?)measurement.Value ?? DBNull.Value),
                            new SqlParameter("@NumericValue", (object?)numericValue ?? DBNull.Value),
                            new SqlParameter("@ActualValue", (object?)actualValue ?? DBNull.Value),
                            new SqlParameter("@Unit", (object?)measurement.Unit ?? DBNull.Value),
                            new SqlParameter("@PassFailValue", (object?)passFailValue ?? DBNull.Value),
                            new SqlParameter("@IsPass", measurement.Passed),
                            new SqlParameter("@IsFail", measurement.Failed),
                            new SqlParameter("@MinValue", (object?)minValue ?? DBNull.Value),
                            new SqlParameter("@MaxValue", (object?)maxValue ?? DBNull.Value),
                            new SqlParameter("@StandardValue", (object?)stdValue ?? DBNull.Value),
                            new SqlParameter("@CreatedDate", DateTime.Now)
                        );
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task CompleteChecklistAsync(int checklistId, string completedBy)
        {
            var sql = @"
                UPDATE InspectionChecklist 
                SET Status = 'Completed', CompletedDate = @CompletedDate, CompletedBy = @CompletedBy
                WHERE ChecklistId = @ChecklistId";

            await _sqlHelper.ExecuteNonQueryAsync(sql,
                new SqlParameter("@ChecklistId", checklistId),
                new SqlParameter("@CompletedDate", DateTime.Now),
                new SqlParameter("@CompletedBy", completedBy)
            );
        }

        public async Task<bool> DeleteChecklistAsync(int checklistId)
        {
            var sql = "DELETE FROM InspectionChecklist WHERE ChecklistId = @ChecklistId";
            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, new SqlParameter("@ChecklistId", checklistId));
            return rowsAffected > 0;
        }

        public async Task<string> ExportInspectionDataAsync(int checklistId)
        {
            var checklist = await GetChecklistByIdAsync(checklistId);
            if (checklist == null) throw new Exception("ไม่พบรายการตรวจสอบ");

            var records = await GetInspectionRecordsAsync(checklistId);

            var csv = new StringBuilder();
            csv.AppendLine("วันที่,FG Code,สินค้า,ลูกค้า,กะ,เวลา,พารามิเตอร์,ค่าที่วัดได้,หน่วย,ประเภท,ผลการตรวจ,ผู้ตรวจสอบ,หมายเหตุ");

            foreach (var record in records)
            {
                foreach (var measurement in record.Measurements)
                {
                    csv.AppendLine($"{checklist.CreatedDate:yyyy-MM-dd}," +
                                 $"{checklist.FgCode}," +
                                 $"{checklist.ItemName}," +
                                 $"{checklist.Customer}," +
                                 $"{record.Shift}," +
                                 $"{record.Time}," +
                                 $"{measurement.ParameterName}," +
                                 $"{measurement.Value}," +
                                 $"{measurement.Unit}," +
                                 $"{(measurement.MeasurementType == "number" ? "วัดค่า" : "ตรวจสอบ")}," +
                                 $"{(measurement.IsPass ? "ผ่าน" : measurement.IsFail ? "ไม่ผ่าน" : "N/A")}," +
                                 $"{checklist.Inspector}," +
                                 $"{record.Note}");
                }
            }

            return csv.ToString();
        }

        public async Task<List<InspectionParameter>> GetInspectionParametersAsync()
        {
            var sql = @"
                SELECT ParameterId, ParameterCode, ParameterNameTH, ParameterNameEN, ParameterType,
                       Unit, MinValue, MaxValue, StandardValue, Specification, HasSpecification,
                       IsActive, SortOrder, Category, CreatedDate, CreatedBy
                FROM InspectionParameter
                WHERE IsActive = 1
                ORDER BY SortOrder, ParameterCode";

            return await _sqlHelper.ExecuteReaderListAsync(sql, MapInspectionParameter);
        }

        public async Task InitializeDefaultParametersAsync()
        {
            // Check if parameters already exist
            var existingCount = await _sqlHelper.ExecuteScalarAsync("SELECT COUNT(*) FROM InspectionParameter");
            if (Convert.ToInt32(existingCount) > 0) return;

            var defaultParameters = new[]
            {
                new { Code = "avgWeight", NameTH = "น้ำหนักเฉลี่ย/ใบ", NameEN = "Average Weight 10 bags", Type = "number", Unit = "g", Min = 9.04m, Max = 9.96m, Std = 9.484m, Spec = "", Order = 1 },
                new { Code = "thickness", NameTH = "ความหนาถุง", NameEN = "Film Thickness", Type = "number", Unit = "micron", Min = 38.1m, Max = 50.8m, Std = 44.45m, Spec = "", Order = 2 },
                new { Code = "width", NameTH = "ความกว้างถุง", NameEN = "Bag Width", Type = "number", Unit = "mm", Min = 268m, Max = 275m, Std = 271m, Spec = "", Order = 3 },
                new { Code = "depth", NameTH = "ความยาวถุง", NameEN = "Bag Depth", Type = "number", Unit = "mm", Min = 274m, Max = 284m, Std = 275m, Spec = "", Order = 4 },
                new { Code = "upperZipper", NameTH = "ความสูงซิปตัวบน", NameEN = "Upper Zipper Thickness", Type = "number", Unit = "mm", Min = 1.73m, Max = 1.95m, Std = 1.86m, Spec = "", Order = 5 },
                new { Code = "lowerZipper", NameTH = "ความสูงซิปตัวล่าง", NameEN = "Lower Zipper Thickness", Type = "number", Unit = "mm", Min = 1.73m, Max = 1.95m, Std = 1.86m, Spec = "", Order = 6 },
                new { Code = "bagAppearance", NameTH = "ลักษณะปรากฎของถุง", NameEN = "Bag Appearance", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "สภาพปกติ,ไม่มีสิ่งแปลกปลอมหรือสิ่งปนเปื้อน", Order = 7 },
                new { Code = "bagColor", NameTH = "สีเนื้อถุง", NameEN = "Bag film color", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "ใส,ไม่มีสีอื่นเจือปน", Order = 8 },
                new { Code = "inkColor", NameTH = "สีพิมพ์", NameEN = "Ink", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "สีขาว", Order = 9 },
                new { Code = "inkAdhesion", NameTH = "การเกาะติดของหมึกพิมพ์", NameEN = "Ink Adhesion", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "ดึงเทปสีต้องไม่หลุด", Order = 10 },
                new { Code = "bagOdor", NameTH = "กลิ่นเนื้อถุง", NameEN = "Bag odor", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "ไม่มีกลิ่นเหม็นรุนแรงหรือกลิ่นผิดปกติ", Order = 11 },
                new { Code = "zipperOperation", NameTH = "การใช้งานซิป", NameEN = "Zipper Operation", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "ซิปเปิด-ปิดได้ปกติ ไม่แน่นหรือหลวม", Order = 12 },
                new { Code = "sealStrength", NameTH = "ความแข็งแรงของซีล", NameEN = "Seal Strength", Type = "passfail", Unit = "-", Min = 0m, Max = 0m, Std = 0m, Spec = "รอยซีล เรียบสนิท แข็งแรง ไม่มีรูตามแนวรอยซีล", Order = 13 }
            };

            foreach (var param in defaultParameters)
            {
                var sql = @"
                    INSERT INTO InspectionParameter (
                        ParameterCode, ParameterNameTH, ParameterNameEN, ParameterType, Unit,
                        MinValue, MaxValue, StandardValue, Specification, HasSpecification,
                        IsActive, SortOrder, CreatedDate, CreatedBy
                    ) VALUES (
                        @Code, @NameTH, @NameEN, @Type, @Unit,
                        @Min, @Max, @Std, @Spec, @HasSpec,
                        1, @Order, @CreatedDate, 'System'
                    )";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    new SqlParameter("@Code", param.Code),
                    new SqlParameter("@NameTH", param.NameTH),
                    new SqlParameter("@NameEN", param.NameEN),
                    new SqlParameter("@Type", param.Type),
                    new SqlParameter("@Unit", param.Unit),
                    new SqlParameter("@Min", param.Type == "number" ? param.Min : DBNull.Value),
                    new SqlParameter("@Max", param.Type == "number" ? param.Max : DBNull.Value),
                    new SqlParameter("@Std", param.Type == "number" ? param.Std : DBNull.Value),
                    new SqlParameter("@Spec", param.Spec),
                    new SqlParameter("@HasSpec", param.Type == "number" ? (param.Min > 0 || param.Max > 0) : !string.IsNullOrEmpty(param.Spec)),
                    new SqlParameter("@Order", param.Order),
                    new SqlParameter("@CreatedDate", DateTime.Now)
                );
            }
        }

        private static InspectionChecklist MapInspectionChecklist(SqlDataReader reader)
        {
            return new InspectionChecklist
            {
                ChecklistId = reader.GetInt32("ChecklistId"),
                FgCode = reader.GetString("FgCode"),
                ItemName = reader.GetString("ItemName"),
                Customer = reader.GetString("Customer"),
                SoNumber = reader.IsDBNull("SoNumber") ? null : reader.GetString("SoNumber"),
                Plant = reader.GetString("Plant"),
                Process = reader.GetString("Process"),
                Size = reader.IsDBNull("Size") ? null : reader.GetString("Size"),
                TypeOfFilm = reader.IsDBNull("TypeOfFilm") ? null : reader.GetString("TypeOfFilm"),
                Status = reader.GetString("Status"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                CompletedDate = reader.IsDBNull("CompletedDate") ? null : reader.GetDateTime("CompletedDate"),
                CompletedBy = reader.IsDBNull("CompletedBy") ? null : reader.GetString("CompletedBy"),
                Remark = reader.IsDBNull("Remark") ? null : reader.GetString("Remark"),
                Inspector = reader.IsDBNull("Inspector") ? null : reader.GetString("Inspector"),
                Approver = reader.IsDBNull("Approver") ? null : reader.GetString("Approver"),
                InspectCode = reader.IsDBNull("InspectCode") ? null : reader.GetString("InspectCode"),
                MachineName = reader.IsDBNull("MachineName") ? null : reader.GetString("MachineName"),
                MachineZone = reader.IsDBNull("MachineZone") ? null : reader.GetString("MachineZone"),
                MachineProcess = reader.IsDBNull("MachineProcess") ? null : reader.GetString("MachineProcess"),
                MachineStorage = reader.IsDBNull("MachineStorage") ? null : reader.GetString("MachineStorage"),
                CustomerCode = reader.IsDBNull("CustomerCode") ? null : reader.GetString("CustomerCode"),
                SalesOrderItem = reader.IsDBNull("SalesOrderItem") ? null : reader.GetString("SalesOrderItem"),
                ProductionOrder = reader.IsDBNull("ProductionOrder") ? null : reader.GetString("ProductionOrder"),
                InspectorTeamA = reader.IsDBNull("InspectorTeamA") ? null : reader.GetString("InspectorTeamA"),
                InspectorTeamB = reader.IsDBNull("InspectorTeamB") ? null : reader.GetString("InspectorTeamB"),
            };
        }

        private static InspectionParameter MapInspectionParameter(SqlDataReader reader)
        {
            return new InspectionParameter
            {
                ParameterId = reader.GetInt32("ParameterId"),
                ParameterCode = reader.GetString("ParameterCode"),
                ParameterNameTH = reader.GetString("ParameterNameTH"),
                ParameterNameEN = reader.GetString("ParameterNameEN"),
                ParameterType = reader.GetString("ParameterType"),
                Unit = reader.IsDBNull("Unit") ? null : reader.GetString("Unit"),
                MinValue = reader.IsDBNull("MinValue") ? null : reader.GetDecimal("MinValue"),
                MaxValue = reader.IsDBNull("MaxValue") ? null : reader.GetDecimal("MaxValue"),
                StandardValue = reader.IsDBNull("StandardValue") ? null : reader.GetDecimal("StandardValue"),
                Specification = reader.IsDBNull("Specification") ? null : reader.GetString("Specification"),
                HasSpecification = reader.GetBoolean("HasSpecification"),
                IsActive = reader.GetBoolean("IsActive"),
                SortOrder = reader.GetInt32("SortOrder"),
                Category = reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy")
            };
        }

        public async Task<byte[]> ExportChecklistToExcelAsync(int checklistId)
        {

            // Get Checklist Data
            var checklist = await GetChecklistByIdAsync(checklistId);
            if (checklist == null) throw new Exception("ไม่พบข้อมูล Checklist");

            // Get Inspection Records
            var records = await GetInspectionRecordsAsync(checklistId);

            // Get Material Data
            var materials = await _sqlHelper.ExecuteReaderListAsync(
                "SELECT * FROM DocumentInspection WHERE DocFg = @FgCode AND DocHide = 0 ORDER BY DocOrderSort",
                MapDocumentInspection,
                new SqlParameter("@FgCode", checklist.FgCode)
            );

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("QC Inspection");

            // ============================================
            // Header Section (Row 1-3)
            // ============================================
            int currentRow = 1;

            // Row 1: Title
            worksheet.Cells[currentRow, 1].Value = "Sales Order: " + checklist.SoNumber;
            worksheet.Cells[currentRow, 2].Value = "Sales Order Item: " + checklist.SalesOrderItem;
            worksheet.Cells[currentRow, 3].Value = "Customer Code: " + checklist.CustomerCode;
            worksheet.Cells[currentRow, 4].Value = "Customer Name: " + checklist.Customer;
            FormatHeaderRow(worksheet, currentRow);
            currentRow++;

            // Row 2: Material Info
            worksheet.Cells[currentRow, 1].Value = "Production Order: " + checklist.ProductionOrder;
            worksheet.Cells[currentRow, 2].Value = "Material Code (FG Code): " + checklist.FgCode;
            worksheet.Cells[currentRow, 3].Value = "Material Name: " + checklist.ItemName;
            worksheet.Cells[currentRow, 4].Value = "Size: " + checklist.Size;
            FormatHeaderRow(worksheet, currentRow);
            currentRow++;

            // Row 3: Details
            
            worksheet.Cells[currentRow, 1].Value = "Film Type: " + checklist.TypeOfFilm;
            worksheet.Cells[currentRow, 2].Value = "Plant: " + checklist.Plant;
            worksheet.Cells[currentRow, 3].Value = "Zone: " + checklist.MachineZone;
            //worksheet.Cells[currentRow, 4].Value = "Process: " + checklist.MachineProcess;
            worksheet.Cells[currentRow, 4].Value = "Machine: " + (checklist.MachineName ?? "N/A");
            FormatHeaderRow(worksheet, currentRow);
            currentRow++;

            // ============================================
            // Empty Row
            // ============================================
            currentRow++;

            // ============================================
            // Table Header (Row 5)
            // ============================================
            int headerRow = currentRow;
            int col = 1;

            // Column: กะ
            worksheet.Cells[headerRow, col].Value = "กะ";
            MergeCells(worksheet, headerRow, headerRow + 1, col, col);
            col++;

            // Column: เวลา
            worksheet.Cells[headerRow, col].Value = "เวลา";
            MergeCells(worksheet, headerRow, headerRow + 1, col, col);
            col++;

            // Column: lot
            worksheet.Cells[headerRow, col].Value = "lot";
            MergeCells(worksheet, headerRow, headerRow + 1, col, col);
            col++;

            // Group materials by DocDataType
            var groupedMaterials = materials.GroupBy(m => m.DocDataType ?? "อื่นๆ").ToList();

            foreach (var group in groupedMaterials)
            {
                int startCol = col;
                var groupItems = group.ToList();

                // Header: DocDataType (ตัด/เป่า/พิมพ์)
                worksheet.Cells[headerRow, startCol].Value = group.Key;
                MergeCells(worksheet, headerRow, headerRow, startCol, startCol + groupItems.Count - 1);
                ApplyHeaderStyle(worksheet, headerRow, startCol, startCol + groupItems.Count - 1);

                // Sub-headers: Parameter names
                for (int i = 0; i < groupItems.Count; i++)
                {
                    var item = groupItems[i];
                    int subCol = startCol + i;

                    worksheet.Cells[headerRow + 1, subCol].Value = item.DocInspection;
                    worksheet.Cells[headerRow + 2, subCol].Value = $"({item.DocUnit})";

                    if (item.DocMin > 0 || item.DocMax > 0)
                    {
                        worksheet.Cells[headerRow + 3, subCol].Value = $"min: {item.DocMin} | Max: {item.DocMax}";
                    }

                    ApplySubHeaderStyle(worksheet, headerRow + 1, subCol);
                }

                col += groupItems.Count;
            }

            // Apply header styles
            ApplyTableHeaderStyle(worksheet, headerRow, 1, col - 1);

            currentRow = headerRow + 4; // Skip to data rows

            // ============================================
            // Data Rows
            // ============================================
            foreach (var record in records)
            {
                col = 1;

                // กะ
                worksheet.Cells[currentRow, col++].Value = record.Shift;

                // เวลา
                worksheet.Cells[currentRow, col++].Value = record.Time;

                // lot
                worksheet.Cells[currentRow, col++].Value = record.Note;

                // Measurements
                foreach (var group in groupedMaterials)
                {
                    foreach (var material in group)
                    {
                        var measurement = record.Measurements
                            .FirstOrDefault(m => m.ParameterId == "param_" + material.DocId);

                        if (measurement != null)
                        {
                            worksheet.Cells[currentRow, col].Value = measurement.Value ?? "";

                            // Apply color based on pass/fail
                            if (measurement.IsFail)
                            {
                                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 215, 218));
                            }
                            else if (measurement.IsPass)
                            {
                                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(209, 231, 221));
                            }
                        }

                        col++;
                    }
                }

                ApplyDataRowStyle(worksheet, currentRow, 1, col - 1);
                currentRow++;
            }

            // ============================================
            // Auto-fit columns
            // ============================================
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Set minimum column width
            for (int i = 1; i <= col - 1; i++)
            {
                if (worksheet.Column(i).Width < 10)
                    worksheet.Column(i).Width = 10;
            }

            return package.GetAsByteArray();
        }

        // ============================================
        // Helper Methods
        // ============================================

        private void FormatHeaderRow(ExcelWorksheet worksheet, int row)
        {
            worksheet.Row(row).Height = 20;
            var range = worksheet.Cells[row, 1, row, 15];
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 11;
        }

        private void MergeCells(ExcelWorksheet worksheet, int fromRow, int toRow, int fromCol, int toCol)
        {
            worksheet.Cells[fromRow, fromCol, toRow, toCol].Merge = true;
        }

        private void ApplyHeaderStyle(ExcelWorksheet worksheet, int row, int startCol, int endCol)
        {
            var range = worksheet.Cells[row, startCol, row, endCol];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 217, 217));
            range.Style.Font.Bold = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
        }

        private void ApplySubHeaderStyle(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
        }

        private void ApplyTableHeaderStyle(ExcelWorksheet worksheet, int row, int startCol, int endCol)
        {
            var range = worksheet.Cells[row, startCol, row + 3, endCol];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private void ApplyDataRowStyle(ExcelWorksheet worksheet, int row, int startCol, int endCol)
        {
            var range = worksheet.Cells[row, startCol, row, endCol];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private DocumentInspection MapDocumentInspection(SqlDataReader reader)
        {
            return new DocumentInspection
            {
                DocId = reader.GetInt32("DocId"),
                DocInspection = reader.GetString("DocInspection"),
                DocUnit = reader.GetString("DocUnit"),
                DocMin = reader.GetDecimal("DocMin"),
                DocMax = reader.GetDecimal("DocMax"),
                DocStd = reader.GetDecimal("DocStd"),
                DocDataType = reader.IsDBNull("DocDataType") ? null : reader.GetString("DocDataType"),
                DocIsLr = reader.GetBoolean("DocIsLr"),
                DocIsMm = reader.GetBoolean("DocIsMm"),
                DocOrderSort = reader.GetInt32("DocOrderSort")
            };
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QcChapWai.Models;
using QcChapWai.Services;

namespace QcChapWai.Controllers
{
    [Authorize]
    public class ChecklistController : Controller
    {
        private readonly ChecklistService _checklistService;
        private readonly MaterialService _materialService;
        private readonly MaterialMasterLocalService _materialMasterLocalService;
        private readonly ProductionOrderService _productionOrderService;
        private readonly MachineService _machineService;
        private readonly UserService _userService;

        public ChecklistController(ChecklistService checklistService, 
            MaterialService materialService, 
            MaterialMasterLocalService materialMasterLocalService, 
            ProductionOrderService productionOrderService,
            MachineService machineService, UserService userService)
        {
            _checklistService = checklistService;
            _materialService = materialService;
            _materialMasterLocalService = materialMasterLocalService;
            _productionOrderService = productionOrderService;
            _machineService = machineService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var activeChecklists = await _checklistService.GetActiveChecklistsAsync();
            return View(activeChecklists);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int id)
        {
            try
            {
                // Get Checklist
                var checklist = await _checklistService.GetChecklistByIdAsync(id);
                if (checklist == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบข้อมูล Checklist";
                    return RedirectToAction(nameof(Index));
                }

                // Generate Excel
                var excelData = await _checklistService.ExportChecklistToExcelAsync(id);

                // Generate filename
                var fileName = $"QC_Inspection_{checklist.InspectCode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Return file
                return File(
                    excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาดในการ Export: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// ✅ API: ดึง Zones
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetZones()
        {
            try
            {
                var zones = await _machineService.GetActiveZonesAsync();
                return Json(new { success = true, data = zones });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ✅ API: ดึง Processes ตาม Zone
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProcessesByZone(string zone)
        {
            try
            {
                var processes = await _machineService.GetProcessesByZoneAsync(zone);
                return Json(new { success = true, data = processes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ✅ API: ดึง Machines ตาม Zone และ Process
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMachinesByZoneAndProcess(string zone, string process)
        {
            try
            {
                var machines = await _machineService.GetMachinesByZoneAndProcessAsync(zone, process);
                return Json(new { success = true, data = machines });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ✅ API: ค้นหา Machine (Auto-complete)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchMachines(string term, string? zone = null, string? process = null)
        {
            try
            {
                var machines = await _machineService.SearchMachinesAsync(term, zone, process);
                return Json(new { success = true, data = machines });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ✅ API: ตรวจสอบ Production Order พร้อม Validate MaterialMaster
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ValidateProductionOrder(string productionOrder)
        {
            try
            {
                if (string.IsNullOrEmpty(productionOrder))
                {
                    return Json(new
                    {
                        success = false,
                        message = "กรุณากรอก Production Order Number"
                    });
                }

                // ✅ ดึงข้อมูลและ Validate
                var result = await _productionOrderService.GetAndValidateProductionOrderAsync(
                    productionOrder,
                    _materialMasterLocalService);

                if (!result.Success)
                {
                    return Json(new
                    {
                        success = false,
                        hasMaterialMaster = result.HasMaterialMaster,
                        message = result.Message,
                        materialCode = result.MaterialCode,
                        data = result.ProductionOrder != null ? new
                        {
                            productionOrder = result.ProductionOrder.OrderNumber,
                            salesOrder = result.ProductionOrder.SalesOrder,
                            salesOrderItem = result.ProductionOrder.SalesOrderItem,
                            customerCode = result.ProductionOrder.CustomerNumber,
                            customerName = result.ProductionOrder.CustomerName,
                            materialCode = result.MaterialCode,
                            materialName = result.ProductionOrder.MaterialDescription,
                            size = result.ProductionOrder.Size,
                            filmType = result.ProductionOrder.MaterialGroupDesc,
                            plant = result.ProductionOrder.Plant
                        } : null
                    });
                }

                // ✅ Success - มี Material Master
                return Json(new
                {
                    success = true,
                    hasMaterialMaster = true,
                    message = result.Message,
                    materialCode = result.MaterialCode,
                    data = new
                    {
                        productionOrder = result.ProductionOrder!.OrderNumber,
                        salesOrder = result.ProductionOrder.SalesOrder,
                        salesOrderItem = result.ProductionOrder.SalesOrderItem,
                        customerCode = result.ProductionOrder.CustomerNumber,
                        customerName = result.ProductionOrder.CustomerName,
                        materialCode = result.MaterialCode,
                        materialName = result.ProductionOrder.MaterialDescription,
                        size = result.ProductionOrder.Size,
                        filmType = result.ProductionOrder.MaterialGroupDesc,
                        plant = result.ProductionOrder.Plant,
                        process = "" // ✅ ไม่มีก็ว่างไว้ตามที่บอก
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"เกิดข้อผิดพลาด: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create Checklist - เปลี่ยนเป็นไม่ต้องรับ orderNumber (User จะกรอกเอง)
        /// </summary>
        public async Task<IActionResult> Create()
        {
            var inspectorsTeamA = await _userService.GetInspectorsTeamAAsync();
            var inspectorsTeamB = await _userService.GetInspectorsTeamBAsync();
            var approvers = await _userService.GetApproversAsync();

            // สร้าง SelectList สำหรับ Dropdown
            ViewBag.InspectorsTeamA = new SelectList(
                inspectorsTeamA,
                "Id",
                "UserFullName"
            );

            ViewBag.InspectorsTeamB = new SelectList(
                inspectorsTeamB,
                "Id",
                "UserFullName"
            );

            ViewBag.Approvers = new SelectList(
                approvers,
                "Id",
                "UserFullName"
            );

            var model = new InspectionChecklist
            {
                Plant = "KB01", // ✅ Default Plant
                Process = "CP"  // ✅ Default Process (ไว้ว่างก็ได้ตามที่บอก)
            };

            return View(model);
        }

        // ============================================
        // Controllers/ChecklistController.cs
        // แก้ไข POST Create Action
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InspectionChecklist model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ แก้ไข: Convert string ID เป็น int ก่อน get User
                    int inspectorTeamAId = 0;
                    int inspectorTeamBId = 0;
                    int approverId = 0;

                    // Parse IDs
                    if (!int.TryParse(model.InspectorTeamAId.ToString(), out inspectorTeamAId))
                    {
                        ModelState.AddModelError(nameof(model.InspectorTeamAId), "รหัสผู้ตรวจสอบ กะ A ไม่ถูกต้อง");
                        await LoadDropdownsAsync();
                        return View(model);
                    }

                    if (!int.TryParse(model.InspectorTeamBId.ToString(), out inspectorTeamBId))
                    {
                        ModelState.AddModelError(nameof(model.InspectorTeamBId), "รหัสผู้ตรวจสอบ กะ B ไม่ถูกต้อง");
                        await LoadDropdownsAsync();
                        return View(model);
                    }

                    //if (model.ApproverId <= 0)
                    //{
                    //    ModelState.AddModelError(nameof(model.ApproverId), "รหัสผู้อนุมัติไม่ถูกต้อง");
                    //    await LoadDropdownsAsync();
                    //    return View(model);
                    //}

                    // ✅ ดึงชื่อเต็มจาก User ID
                    var inspectorTeamA = await _userService.GetUserByIdAsync(inspectorTeamAId);
                    var inspectorTeamB = await _userService.GetUserByIdAsync(inspectorTeamBId);
                    //var approver = await _userService.GetUserByIdAsync(model.ApproverId);

                    if (inspectorTeamA == null)
                    {
                        ModelState.AddModelError(nameof(model.InspectorTeamAId), "ไม่พบข้อมูลผู้ตรวจสอบ กะ A");
                        await LoadDropdownsAsync();
                        return View(model);
                    }

                    if (inspectorTeamB == null)
                    {
                        ModelState.AddModelError(nameof(model.InspectorTeamBId), "ไม่พบข้อมูลผู้ตรวจสอบ กะ B");
                        await LoadDropdownsAsync();
                        return View(model);
                    }


                    // ✅ Validate Production Order ก่อนบันทึก
                    var validation = await _productionOrderService.GetAndValidateProductionOrderAsync(
                        model.ProductionOrder ?? "",
                        _materialMasterLocalService);

                    if (!validation.Success || !validation.HasMaterialMaster)
                    {
                        ModelState.AddModelError(string.Empty, validation.Message);
                        await LoadDropdownsAsync();
                        return View(model);
                    }

                    // ✅ Set ชื่อเต็มให้กับ Model
                    model.InspectorTeamA = inspectorTeamA.UserFullName;
                    model.InspectorTeamB = inspectorTeamB.UserFullName;
                    //model.Approver = approver.UserFullName;

                    // Set metadata
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = User.Identity?.Name ?? "System";
                    model.Status = "Active";

                    // ✅ บันทึก
                    await _checklistService.CreateChecklistAsync(model);

                    TempData["SuccessMessage"] = "สร้างรายการตรวจสอบใหม่สำเร็จ";
                    return RedirectToAction(nameof(Inspect), new { id = model.ChecklistId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"เกิดข้อผิดพลาด: {ex.Message}");
                    await LoadDropdownsAsync();
                }
            }
            else
            {
                // ModelState ไม่ Valid
                await LoadDropdownsAsync();
            }

            return View(model);
        }

        private async Task LoadDropdownsAsync()
        {
            var inspectorsTeamA = await _userService.GetInspectorsTeamAAsync();
            var inspectorsTeamB = await _userService.GetInspectorsTeamBAsync();
            var approvers = await _userService.GetApproversAsync();

            ViewBag.InspectorsTeamA = new SelectList(inspectorsTeamA, "Id", "UserFullName");
            ViewBag.InspectorsTeamB = new SelectList(inspectorsTeamB, "Id", "UserFullName");
            ViewBag.Approvers = new SelectList(approvers, "Id", "UserFullName");
        }

        public async Task<IActionResult> Inspect(int id)
        {
            var checklist = await _checklistService.GetChecklistByIdAsync(id);
            if (checklist == null)
            {
                return NotFound();
            }

            // Get inspection parameters based on the FG Code
            var materials = await _materialService.GetAllMaterialsAsync();
            var relatedMaterials = materials
                .Where(m => m.DocFg == checklist.FgCode && !m.DocHide)
                .OrderBy(m => m.DocOrderSort)
                .ToList();

            ViewBag.InspectionParameters = relatedMaterials;
            ViewBag.ExistingRecords = await _checklistService.GetInspectionRecordsAsync(id);

            return View(checklist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInspectionData([FromBody] InspectionDataRequest request)
        {
            try
            {
                await _checklistService.SaveInspectionDataAsync(request);
                return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                await _checklistService.CompleteChecklistAsync(id, User.Identity?.Name ?? "System");
                TempData["SuccessMessage"] = "ปิดรายการตรวจสอบสำเร็จ";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาด: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _checklistService.DeleteChecklistAsync(id);
                TempData["SuccessMessage"] = "ลบรายการตรวจสอบสำเร็จ";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาด: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterialData(string fgCode)
        {
            try
            {
                // ✅ ดึงข้อมูลจาก MaterialMaster
                var material = await _materialMasterLocalService.GetMaterialMasterByCodeAsync(fgCode);

                if (material != null)
                {
                    // ✅ ดึง Customer และ Process จาก DocumentInspection (ถ้ามี)
                    string customer = "";
                    string process = "CP"; // default

                    var parameters = await _materialService.GetParametersByMaterialCodeAsync(material.MaterialCode);
                    if (parameters.Any())
                    {
                        customer = parameters.First().DocCustomer;
                        process = parameters.First().DocProcess;
                    }

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            fgCode = material.MaterialCode,
                            itemName = material.ProductName,
                            customer = customer,
                            plant = material.Plant,
                            process = process,
                            size = material.Size ?? "",
                            typeOfFilm = material.FilmType ?? ""
                        }
                    });
                }

                return Json(new { success = false, message = "ไม่พบข้อมูล FG Code นี้" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportInspectionData(int id)
        {
            try
            {
                var csvData = await _checklistService.ExportInspectionDataAsync(id);
                var fileName = $"QC_Inspection_{id}_{DateTime.Now:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csvData), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาดในการ Export: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    // Request model for saving inspection data
    public class InspectionDataRequest
    {
        public int ChecklistId { get; set; }
        public List<InspectionRowData> InspectionRows { get; set; } = new();
        public List<InspectionMeasurement> Measurements { get; set; } = new();
        public string? Remark { get; set; }
        public string? Inspector { get; set; }
        public string? Approver { get; set; }


    }

    public class InspectionRowData
    {
        public string Shift { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class InspectionMeasurement
    {
        public string Parameter { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public bool Failed { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? StandardValue { get; set; }
        public decimal? ActualValue { get; set; }
    }
}
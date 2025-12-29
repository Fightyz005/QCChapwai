using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QcChapWai.Models;
using QcChapWai.Services;

namespace QcChapWai.Controllers
{
    [Authorize]
    public class MaterialController : Controller
    {
        private readonly MaterialService _materialService;
        private readonly MaterialMasterService _materialMasterService;
        private readonly MaterialMasterLocalService _materialMasterLocalService;

        public MaterialController(
            MaterialService materialService,
            MaterialMasterService materialMasterService,
            MaterialMasterLocalService materialMasterLocalService)
        {
            _materialService = materialService;
            _materialMasterService = materialMasterService;
            _materialMasterLocalService = materialMasterLocalService;
        }

        /// <summary>
        /// ✅ Material Index - แสดงข้อมูลจาก MaterialMaster + Parameters จาก DocumentInspection
        /// </summary>
        public async Task<IActionResult> Index(string? searchTerm)
        {
            try
            {
                // ✅ ดึงข้อมูล Product จาก MaterialMaster
                var materialMasters = await _materialMasterLocalService.GetAllMaterialMasterAsync();

                // ✅ Filter ตาม searchTerm
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    materialMasters = materialMasters
                        .Where(m => m.MaterialCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   m.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // ✅ สำหรับแต่ละ Material ดึง Parameters จาก DocumentInspection
                var groupedMaterials = new List<MaterialGroupViewModel>();

                foreach (var material in materialMasters)
                {
                    var parameters = await _materialService.GetParametersByMaterialCodeAsync(material.MaterialCode);

                    groupedMaterials.Add(new MaterialGroupViewModel
                    {
                        MaterialMaster = material,
                        Parameters = parameters,
                        PassedCount = parameters.Count(p => p.DocPassed),
                        TotalCount = parameters.Count
                    });
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.GroupedMaterials = groupedMaterials;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาด: {ex.Message}";
                return View(new List<MaterialGroupViewModel>());
            }
        }

        // ========================================
        // CREATE - Show Form
        // ========================================
        public async Task<IActionResult> Create()
        {
            try
            {
                // ✅ ดึงข้อมูล Material จาก ZMATMASDW
                var materials = await _materialMasterService.GetAllMaterialsAsync();

                // ✅ ส่ง List<MaterialMasterViewModel> ไปให้ View
                ViewBag.Materials = materials;

                // ✅ สร้าง Empty Model
                var model = new MaterialMasterViewModel();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาด: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================================
        // ✅ API: บันทึก Material Master
        // ========================================
        [HttpPost]
        public async Task<IActionResult> SaveMaterialMaster([FromBody] SaveMaterialRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MaterialCode))
                {
                    return Json(new { success = false, message = "กรุณาระบุ Material Code" });
                }

                var result = await _materialMasterLocalService.SaveMaterialMasterAsync(
                    request.MaterialCode,
                    User.Identity?.Name ?? "System"
                );

                return Json(new
                {
                    success = result.success,
                    message = result.message,
                    redirectUrl = Url.Action("Index", "Material")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ========================================
        // ✅ API: เพิ่มพารามิเตอร์การตรวจสอบ
        // ========================================
        [HttpPost]
        public async Task<IActionResult> AddInspectionParameter([FromBody] AddParameterRequest request)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(request.DocInspection))
                {
                    return Json(new { success = false, message = "กรุณากรอกชื่อการตรวจสอบ" });
                }

                if (string.IsNullOrWhiteSpace(request.DocUnit))
                {
                    return Json(new { success = false, message = "กรุณากรอกหน่วย" });
                }

                // ถ้าเป็นแบบตัวเลข ต้องมี Min, Max, Std
                if (request.ParameterType == "number")
                {
                    if (!request.DocMin.HasValue || !request.DocMax.HasValue || !request.DocStd.HasValue)
                    {
                        return Json(new { success = false, message = "กรุณากรอกค่า Min, Max, Std สำหรับการตรวจแบบตัวเลข" });
                    }

                    if (request.DocMin >= request.DocMax)
                    {
                        return Json(new { success = false, message = "ค่า Min ต้องน้อยกว่า Max" });
                    }

                    if (request.DocStd < request.DocMin || request.DocStd > request.DocMax)
                    {
                        return Json(new { success = false, message = "ค่า Standard ต้องอยู่ระหว่าง Min และ Max" });
                    }
                }

                // Get next order sort
                var existingMaterials = await _materialService.GetAllMaterialsAsync();
                var maxOrderSort = existingMaterials
                    .Where(m => m.DocFg == request.DocFg)
                    .Select(m => m.DocOrderSort)
                    .DefaultIfEmpty(0)
                    .Max();

                // Create new parameter
                var newParameter = new DocumentInspection
                {
                    DocFg = request.DocFg,
                    DocFgItem = request.DocFgItem,
                    DocCustomer = request.DocCustomer,
                    DocPlant = request.DocPlant,
                    DocProcess = request.DocProcess,
                    DocSize = request.DocSize,
                    DocTypeOfFilm = request.DocTypeOfFilm,
                    DocSo = request.DocSo ?? "",
                    DocSoItem = request.DocSoItem ?? "",

                    // Parameter Details
                    DocInspection = request.DocInspection,
                    DocUnit = request.DocUnit,
                    DocMin = request.DocMin ?? 0,
                    DocMax = request.DocMax ?? 0,
                    DocStd = request.DocStd ?? 0,

                    // Type Flags
                    DocIsLr = request.ParameterType == "leftright",
                    DocIsMm = request.ParameterType == "passfail",

                    // Other Fields
                    DocPassed = false,
                    DocHide = false,
                    DocOrderSort = maxOrderSort + 1,
                    DocCreateDate = DateTime.Now,
                    DocRemark = request.DocRemark,
                    DocDataType = request.DocDataType
                };

                await _materialService.CreateMaterialAsync(newParameter);

                return Json(new
                {
                    success = true,
                    message = "เพิ่มพารามิเตอร์สำเร็จ",
                    parameterId = newParameter.DocId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ========================================
        // EDIT
        // ========================================
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _materialService.GetMaterialByIdAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentInspection model)
        {
            if (id != model.DocId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _materialService.UpdateMaterialAsync(model);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "อัปเดตข้อมูลสำเร็จ";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "ไม่พบข้อมูลที่ต้องการอัปเดต");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"เกิดข้อผิดพลาด: {ex.Message}");
                }
            }

            return View(model);
        }

        // ========================================
        // DETAILS
        // ========================================
        public async Task<IActionResult> Details(int id)
        {
            var material = await _materialService.GetMaterialByIdAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        // ========================================
        // DELETE
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _materialService.DeleteMaterialAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "ลบข้อมูลสำเร็จ";
                }
                else
                {
                    TempData["ErrorMessage"] = "ไม่พบข้อมูลที่ต้องการลบ";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"เกิดข้อผิดพลาดในการลบข้อมูล: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========================================
        // TOGGLE STATUS
        // ========================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var passed = await _materialService.ToggleStatusAsync(id);
                return Json(new { success = true, passed = passed });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // ========================================
    // REQUEST MODELS
    // ========================================

    /// <summary>
    /// Request Model สำหรับบันทึก Material Master
    /// </summary>
    public class SaveMaterialRequest
    {
        public string MaterialCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request Model สำหรับเพิ่มพารามิเตอร์
    /// </summary>
    public class AddParameterRequest
    {
        // Product Info
        public string DocFg { get; set; } = string.Empty;
        public string DocFgItem { get; set; } = string.Empty;
        public string DocCustomer { get; set; } = string.Empty;
        public string DocPlant { get; set; } = string.Empty;
        public string DocProcess { get; set; } = string.Empty;
        public string DocSize { get; set; } = string.Empty;
        public string DocTypeOfFilm { get; set; } = string.Empty;
        public string? DocSo { get; set; }
        public string? DocSoItem { get; set; }

        // Parameter Type: "number", "leftright", "passfail"
        public string ParameterType { get; set; } = "number";

        // Parameter Details
        public string DocInspection { get; set; } = string.Empty;
        public string DocUnit { get; set; } = string.Empty;
        public decimal? DocMin { get; set; }
        public decimal? DocMax { get; set; }
        public decimal? DocStd { get; set; }
        public string? DocRemark { get; set; }
        public string? DocDataType { get; set;} = string.Empty;
    }
}
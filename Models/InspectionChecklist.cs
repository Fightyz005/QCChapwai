using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QcChapWai.Models
{
    [Table("InspectionChecklist")]
    public class InspectionChecklist
    {
        [Key]
        public int ChecklistId { get; set; }

        [Required]
        [StringLength(50)]
        public string FgCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Customer { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SoNumber { get; set; }

        [Required]
        [StringLength(10)]
        public string Plant { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Process { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? TypeOfFilm { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Completed, Cancelled

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(100)]
        public string? CompletedBy { get; set; }

        [StringLength(1000)]
        public string? Remark { get; set; }

        [StringLength(100)]
        public string? Inspector { get; set; }

        [StringLength(100)]
        public string? Approver { get; set; }

        [StringLength(20)]
        public string? ProductionOrder { get; set; } = string.Empty;// AUFNR

        [StringLength(10)]
        public string? SalesOrderItem { get; set; } = string.Empty;// KDPOS

        [StringLength(10)]
        public string? CustomerCode { get; set; } = string.Empty;// KUNNR

        // ✅ เพิ่มฟิลด์ใหม่ใน InspectionChecklist Model

        [StringLength(10)]
        public string? MachineZone { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MachineProcess { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MachineName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MachineStorage { get; set; } = string.Empty;

        [StringLength(20)]
        public string? InspectCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string? DocDataType { get; set; } = string.Empty;

        // ✅ บันทึกลง Database (ชื่อเต็ม)
        [StringLength(100)]
        public string? InspectorTeamA { get; set; }

        [StringLength(100)]
        public string? InspectorTeamB { get; set; }

        // ✅ รับค่าจาก Form (ID)
        // ใช้สำหรับรับค่าจาก Form
        [NotMapped]
        [Required(ErrorMessage = "กรุณาเลือกผู้ตรวจสอบ กะ A")]
        public int InspectorTeamAId { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "กรุณาเลือกผู้ตรวจสอบ กะ B")]
        public int InspectorTeamBId { get; set; }


        // Navigation properties
        public virtual ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
    }

    [Table("InspectionRecord")]
    public class InspectionRecord
    {
        [Key]
        public int RecordId { get; set; }

        [Required]
        public int ChecklistId { get; set; }

        [Required]
        [StringLength(1)]
        public string Shift { get; set; } = string.Empty; // A, B, C

        [Required]
        public TimeSpan InspectionTime { get; set; }


        [StringLength(200)]
        public string? Note { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Foreign key
        [ForeignKey("ChecklistId")]
        public virtual InspectionChecklist InspectionChecklist { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<InspectionMeasurementData> Measurements { get; set; } = new List<InspectionMeasurementData>();
    }

    [Table("InspectionMeasurementData")]
    public class InspectionMeasurementData
    {
        [Key]
        public int MeasurementId { get; set; }

        [Required]
        public int RecordId { get; set; }

        [Required]
        [StringLength(100)]
        public string ParameterId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ParameterName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MeasurementType { get; set; } = string.Empty; // number, passfail

        [StringLength(100)]
        public string? MeasurementValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NumericValue { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        public bool? PassFailValue { get; set; }

        public bool IsPass { get; set; }

        public bool IsFail { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? StandardValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign key
        [ForeignKey("RecordId")]
        public virtual InspectionRecord InspectionRecord { get; set; } = null!;
    }

    [Table("InspectionParameter")]
    public class InspectionParameter
    {
        [Key]
        public int ParameterId { get; set; }

        [Required]
        [StringLength(100)]
        public string ParameterCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ParameterNameTH { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ParameterNameEN { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ParameterType { get; set; } = string.Empty; // number, passfail

        [StringLength(20)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? StandardValue { get; set; }

        [StringLength(500)]
        public string? Specification { get; set; }

        public bool HasSpecification { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }

    // View Models
    public class ChecklistSummaryViewModel
    {
        public int ChecklistId { get; set; }
        public string FgCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? SoNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public int TotalRecords { get; set; }
        public int PassedMeasurements { get; set; }
        public int FailedMeasurements { get; set; }
        public int TotalMeasurements { get; set; }
        public decimal PassRate { get; set; }
        public string? Inspector { get; set; }

        public string? Approver { get; set; }

        public string? ProductionOrder { get; set; } // AUFNR

        public string? SalesOrderItem { get; set; } // KDPOS

        public string? CustomerCode { get; set; } // KUNNR

        public string? MachineZone { get; set; }

        public string? MachineProcess { get; set; }

        public string? MachineName { get; set; }

        public string? MachineStorage { get; set; }
        public string? InspectCode { get; set; }
    }

    public class InspectionRecordViewModel
    {
        public int RecordId { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<MeasurementViewModel> Measurements { get; set; } = new();
    }

    public class MeasurementViewModel
    {
        public string ParameterId { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string MeasurementType { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Unit { get; set; }
        public bool IsPass { get; set; }
        public bool IsFail { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? StandardValue { get; set; }
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

        // เพิ่มใหม่
        public decimal? MinValue { get; set; }  
        public decimal? MaxValue { get; set; }
        public decimal? StandardValue { get; set; }
        public decimal? ActualValue { get; set; }
    }
}
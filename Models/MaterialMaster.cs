using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QcChapWai.Models
{
    /// <summary>
    /// Model สำหรับดึงข้อมูลจาก ZMATMASDW (Material Master)
    /// </summary>
    //[Table("ZMATMASDW")]
    //public class MaterialMaster
    //{
    //    [Key]
    //    [StringLength(18)]
    //    public string MATNR { get; set; } = string.Empty; // Material Number → DocFg

    //    [StringLength(40)]
    //    public string? MAKTX { get; set; } // Material Description → DocFgItem

    //    [StringLength(32)]
    //    public string? GROES { get; set; } // Size → DocSize

    //    [StringLength(4)]
    //    public string? MTART { get; set; } // Material Type (ZFG, ZSF, ZSK, ZSV)

    //    [StringLength(20)]
    //    public string? MTBEZ { get; set; } // Material Type Description

    //    [StringLength(3)]
    //    public string? MEINS { get; set; } // Base Unit of Measure → DocMaterialUnit

    //    [StringLength(40)]
    //    public string? WGBEZ { get; set; } // Material Group Description → DocTypeOfFilm

    //    [StringLength(1)]
    //    public string? LVORM { get; set; } // Deletion Flag (X = Deleted)

    //    [StringLength(3)]
    //    public string? MANDT { get; set; } // Client (910)

    [Table("MaterialMaster")]
    public class MaterialMaster
    {
        [Key]
        public int MaterialId { get; set; }

        [Required]
        [StringLength(50)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        [StringLength(10)]
        public string Plant { get; set; } = "KB01";

        [StringLength(100)]
        public string? FilmType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }


    /// <summary>
    /// ViewModel สำหรับแสดงใน Dropdown
    /// </summary>
    public class MaterialMasterViewModel
    {
        public string MaterialNumber { get; set; } = string.Empty; // MATNR
        public string MaterialDescription { get; set; } = string.Empty; // MAKTX
        public string Size { get; set; } = string.Empty; // GROES
        public string Unit { get; set; } = string.Empty; // MEINS
        public string MaterialGroup { get; set; } = string.Empty; // WGBEZ
        public string MaterialType { get; set; } = string.Empty;
        public string TypeOfFilm { get; set; } = string.Empty;

        public string DisplayText => $"{MaterialNumber} - {MaterialDescription}"; // For Dropdown

        public string SearchText => $"{MaterialNumber} - {MaterialDescription}";
    }

    /// <summary>
    /// ViewModel สำหรับ Material/Index
    /// รวม MaterialMaster + Parameters
    /// </summary>
    public class MaterialGroupViewModel
    {
        public MaterialMaster MaterialMaster { get; set; } = new();
        public List<DocumentInspection> Parameters { get; set; } = new();
        public int PassedCount { get; set; }
        public int TotalCount { get; set; }
        public bool OverallPassed => TotalCount > 0 && PassedCount == TotalCount;
    }

    public class ProductionOrderViewModel
    {
        public string OrderNumber { get; set; }        // AUFNR
        public string SalesOrder { get; set; }         // KDAUF
        public string SalesOrderItem { get; set; }     // KDPOS
        public string CustomerNumber { get; set; }     // KUNNR
        public string CustomerName { get; set; }       // NAME1
        public string Plant { get; set; }              // Fixed: KB01
        public string MaterialDescription { get; set; } // MAKTX
        public string Size { get; set; }               // GROES
        public string MaterialGroup { get; set; }      // MATKL
        public string MaterialGroupDesc { get; set; }  // WGBEZ

        public string DisplayText => $"{OrderNumber} - {MaterialDescription} ({CustomerName})";
    }
}
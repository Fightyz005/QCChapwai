using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QcChapWai.Models
{
    [Table("DocumentInspection")]
    public class DocumentInspection
    {
        [Key]
        public int DocId { get; set; }

        [Required]
        [StringLength(10)]
        public string DocPlant { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string DocProcess { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DocInspection { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string DocUnit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DocMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DocMax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DocStd { get; set; }

        public bool DocIsLr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DocLeft { get; set; }

        public bool DocIsMm { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DocMinPlus { get; set; }

        [StringLength(500)]
        public string? DocRemark { get; set; }

        public DateTime? DocCreateDate { get; set; }

        [StringLength(50)]
        public string? DocCustomer { get; set; } = string.Empty;

        [StringLength(20)]
        public string? DocSo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? DocSoItem { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocFg { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DocFgItem { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocSize { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocTypeOfFilm { get; set; } = string.Empty;

        [StringLength(20)]
        public string? DocMachineNo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocLotno { get; set; } = string.Empty;

        public bool DocPassed { get; set; }

        [StringLength(20)]
        public string? DocNcNo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocApproverA { get; set; }

        [StringLength(50)]
        public string? DocApproverB { get; set; }

        [StringLength(500)]
        public string? DocRemarkA { get; set; }

        [StringLength(500)]
        public string? DocRemarkB { get; set; }

        public int DocOrderSort { get; set; }

        public bool DocHide { get; set; }

        [StringLength(20)]
        public string? DocDataType { get; set; } = string.Empty;
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "User";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public string UserFullName { get; set; } = string.Empty;
        public bool UserHead { get; set; } = true;
        public bool CanEdit { get; set; } = true;
        public string UserTeam { get; set; } = string.Empty;

    }

    public class UserDropdownViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; }
        public string UserTeam { get; set; }
    }
}
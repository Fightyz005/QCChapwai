using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QcChapWai.Models
{
    [Table("MachineMaster")]
    public class MachineMaster
    {
        [Key]
        public int MachineId { get; set; }

        [Required]
        [StringLength(10)]
        public string Plant { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Zone { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Process { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Storage { get; set; }

        [Required]
        [StringLength(20)]
        public string Machine { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ViewModel สำหรับ Dropdown
    /// </summary>
    public class MachineSelectionViewModel
    {
        public string Zone { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;

        public string DisplayText => $"{Machine} ({Zone} - {Process})";
    }
}
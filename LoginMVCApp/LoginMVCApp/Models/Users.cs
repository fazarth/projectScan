using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Users
    {
        public long Id { get; set; }
        [Required]
        [Column("nama")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        [Column("phonenumber")]
        public string PhoneNumber { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("line_id")]
        public long? LineId { get; set; }
        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("createdby")]
        public string CreatedBy { get; set; } = string.Empty;

        public virtual Lines? Line { get; set; }
    }
}

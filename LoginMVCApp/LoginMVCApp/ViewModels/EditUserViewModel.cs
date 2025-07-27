using LoginMVCApp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.ViewModels
{
    public class EditUserViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // Tidak wajib, hanya jika ingin ubah password
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [NotMapped] // Tidak disimpan ke database
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role wajib dipilih")]
        public string Role { get; set; } = string.Empty;

        [Column("line_id")]
        public long? LineId { get; set; }

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

    }
}

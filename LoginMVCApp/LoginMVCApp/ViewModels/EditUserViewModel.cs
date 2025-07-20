using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Username wajib diisi")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        // Tidak wajib, hanya jika ingin ubah password
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [NotMapped] // Tidak disimpan ke database
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role wajib dipilih")]
        public string Role { get; set; }

        public bool IsActive { get; set; }
    }
}

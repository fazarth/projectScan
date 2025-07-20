using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginMVCApp.Models
{
    public class Users
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; }
    }
}

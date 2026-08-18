using System.ComponentModel.DataAnnotations;

namespace KutuphaneOtomasyonu.ViewModels
{
    public class SifreDegistirViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre")]
        public string MevcutSifre { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(
            6,
            ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string YeniSifre { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare(
            nameof(YeniSifre),
            ErrorMessage = "Yeni şifreler eşleşmiyor.")]
        public string YeniSifreTekrar { get; set; } = "";
    }
}
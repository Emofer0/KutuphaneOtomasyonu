using System.ComponentModel.DataAnnotations;

namespace KutuphaneOtomasyonu.ViewModels
{
    public class GirisViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Eposta { get; set; } = "";

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = "";

        [Display(Name = "Beni hatırla")]
        public bool BeniHatirla { get; set; }
    }
}
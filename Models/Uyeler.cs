using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Models;

public partial class Uyeler
{
    public int UyeId { get; set; }

    public string AdSoyad { get; set; } = null!;

    public string Eposta { get; set; } = null!;

    public string? Telefon { get; set; }

    public DateTime KayitTarihi { get; set; }

    public string Sifre { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public virtual ICollection<OduncIslemleri> OduncIslemleris { get; set; } = new List<OduncIslemleri>();
}

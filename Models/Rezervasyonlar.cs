using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Models;

public partial class Rezervasyonlar
{
    public int RezervasyonId { get; set; }

    public int KitapId { get; set; }

    public int UyeId { get; set; }

    public DateTime RezervasyonTarihi { get; set; }

    public string Durum { get; set; } = null!;

    public virtual Kitaplar Kitap { get; set; } = null!;

    public virtual Uyeler Uye { get; set; } = null!;
}
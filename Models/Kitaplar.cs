using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Models;

public partial class Kitaplar
{
    public int KitapId { get; set; }

    public string Isbn { get; set; } = null!;

    public string Baslik { get; set; } = null!;

    public int YazarId { get; set; }

    public int KategoriId { get; set; }

    public string? RafKonumu { get; set; }

    public int ToplamAdet { get; set; }

    public int MevcutAdet { get; set; }

    public bool AktifMi { get; set; }

public string? PasifeAlmaNedeni { get; set; }

public DateTime? PasifeAlmaTarihi { get; set; }

    public virtual Kategoriler Kategori { get; set; } = null!;

    public virtual ICollection<OduncIslemleri> OduncIslemleris { get; set; } = new List<OduncIslemleri>();

    public virtual Yazarlar Yazar { get; set; } = null!;
}

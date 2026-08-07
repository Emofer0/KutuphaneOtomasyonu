using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Models;

public partial class Kategoriler
{
    public int KategoriId { get; set; }

    public string Baslik { get; set; } = null!;

    public virtual ICollection<Kitaplar> Kitaplars { get; set; } = new List<Kitaplar>();
}

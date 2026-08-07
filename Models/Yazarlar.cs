using System;
using System.Collections.Generic;

namespace KutuphaneOtomasyonu.Models;

public partial class Yazarlar
{
    public int YazarId { get; set; }

    public string AdSoyad { get; set; } = null!;

    public virtual ICollection<Kitaplar> Kitaplars { get; set; } = new List<Kitaplar>();
}

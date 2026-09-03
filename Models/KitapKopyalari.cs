namespace KutuphaneOtomasyonu.Models;

public partial class KitapKopyalari
{
    public int KopyaId { get; set; }

    public int KitapId { get; set; }

    public string Barkod { get; set; } = null!;

    public string Durum { get; set; } = null!;

    public DateTime EklenmeTarihi { get; set; }

    public virtual Kitaplar Kitap
        { get; set; } = null!;

    public virtual ICollection<OduncIslemleri> OduncIslemleris
        { get; set; } = new List<OduncIslemleri>();
}
namespace KutuphaneOtomasyonu.Models;

public partial class OduncIslemleri
{
    public int OduncId { get; set; }

    public int KitapId { get; set; }

    public int UyeId { get; set; }

    public int? KopyaId { get; set; }

    public DateTime VerilisTarihi { get; set; }

    public DateTime SonTeslimTarihi { get; set; }

    public DateTime? IadeTarihi { get; set; }

    public decimal CezaTutari { get; set; }

    public bool TeslimEdildiMi { get; set; }

    public virtual Kitaplar Kitap
        { get; set; } = null!;

    public virtual Uyeler Uye
        { get; set; } = null!;

    public virtual KitapKopyalari? Kopya
        { get; set; }
}
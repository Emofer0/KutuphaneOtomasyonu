using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Data;

public partial class KutuphaneContext : DbContext
{
    public KutuphaneContext(
        DbContextOptions<KutuphaneContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Kategoriler> Kategorilers
        { get; set; }

    public virtual DbSet<Kitaplar> Kitaplars
        { get; set; }

    public virtual DbSet<KitapKopyalari> KitapKopyalaris
        { get; set; }

    public virtual DbSet<OduncIslemleri> OduncIslemleris
        { get; set; }

    public virtual DbSet<Rezervasyonlar> Rezervasyonlars
        { get; set; }

    public virtual DbSet<Uyeler> Uyelers
        { get; set; }

    public virtual DbSet<Yazarlar> Yazarlars
        { get; set; }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        // Kategoriler
        modelBuilder.Entity<Kategoriler>(entity =>
        {
            entity.HasKey(e => e.KategoriId);

            entity.ToTable("Kategoriler");
        });

        // Kitaplar
        modelBuilder.Entity<Kitaplar>(entity =>
        {
            entity.HasKey(e => e.KitapId);

            entity.ToTable("Kitaplar");

            entity.HasIndex(
                e => e.KategoriId,
                "IX_Kitaplar_KategoriId");

            entity.HasIndex(
                e => e.YazarId,
                "IX_Kitaplar_YazarId");

            entity.Property(e => e.Isbn)
                .HasColumnName("ISBN");

            entity.Property(e => e.AktifMi)
                .HasDefaultValue(true);

            entity.Property(e => e.PasifeAlmaNedeni)
                .HasMaxLength(100);

            entity.Property(e => e.PasifeAlmaTarihi);

            entity.HasOne(d => d.Kategori)
                .WithMany(p => p.Kitaplars)
                .HasForeignKey(d => d.KategoriId);

            entity.HasOne(d => d.Yazar)
                .WithMany(p => p.Kitaplars)
                .HasForeignKey(d => d.YazarId);
        });

        // Fiziksel kitap kopyaları ve barkodlar
        modelBuilder.Entity<KitapKopyalari>(entity =>
        {
            entity.HasKey(e => e.KopyaId);

            entity.ToTable("KitapKopyalari");

            entity.HasIndex(
                    e => e.KitapId,
                    "IX_KitapKopyalari_KitapId");

            entity.HasIndex(
                    e => e.Barkod,
                    "UX_KitapKopyalari_Barkod")
                .IsUnique();

            entity.Property(e => e.Barkod)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Durum)
                .HasMaxLength(20)
                .HasDefaultValue("Rafta")
                .IsRequired();

            entity.Property(e => e.EklenmeTarihi)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Kitap)
                .WithMany(p => p.KitapKopyalaris)
                .HasForeignKey(d => d.KitapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName(
                    "FK_KitapKopyalari_Kitaplar");
        });

        // Ödünç işlemleri
        modelBuilder.Entity<OduncIslemleri>(entity =>
        {
            entity.HasKey(e => e.OduncId);

            entity.ToTable("OduncIslemleri");

            entity.HasIndex(
                e => e.KitapId,
                "IX_OduncIslemleri_KitapId");

            entity.HasIndex(
                e => e.UyeId,
                "IX_OduncIslemleri_UyeId");

            entity.HasIndex(
                    e => e.KopyaId,
                    "UX_OduncIslemleri_AktifKopya")
                .IsUnique()
                .HasFilter(
                    "[TeslimEdildiMi] = 0 AND [KopyaId] IS NOT NULL");

            entity.Property(e => e.CezaTutari)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Kitap)
                .WithMany(p => p.OduncIslemleris)
                .HasForeignKey(d => d.KitapId);

            entity.HasOne(d => d.Uye)
                .WithMany(p => p.OduncIslemleris)
                .HasForeignKey(d => d.UyeId);

            entity.HasOne(d => d.Kopya)
                .WithMany(p => p.OduncIslemleris)
                .HasForeignKey(d => d.KopyaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName(
                    "FK_OduncIslemleri_KitapKopyalari");
        });

        // Rezervasyonlar
        modelBuilder.Entity<Rezervasyonlar>(entity =>
        {
            entity.HasKey(e => e.RezervasyonId);

            entity.ToTable("Rezervasyonlar");

            entity.HasIndex(
                e => e.KitapId,
                "IX_Rezervasyonlar_KitapId");

            entity.HasIndex(
                e => e.UyeId,
                "IX_Rezervasyonlar_UyeId");

            entity.Property(e => e.Durum)
                .HasMaxLength(20)
                .HasDefaultValue("Bekliyor");

            entity.Property(e => e.RezervasyonTarihi)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Kitap)
                .WithMany()
                .HasForeignKey(d => d.KitapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName(
                    "FK_Rezervasyonlar_Kitaplar");

            entity.HasOne(d => d.Uye)
                .WithMany()
                .HasForeignKey(d => d.UyeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName(
                    "FK_Rezervasyonlar_Uyeler");
        });

        // Üyeler
        modelBuilder.Entity<Uyeler>(entity =>
        {
            entity.HasKey(e => e.UyeId);

            entity.ToTable("Uyeler");

            entity.Property(e => e.Rol)
                .HasDefaultValue("");

            entity.Property(e => e.Sifre)
                .HasDefaultValue("");

            entity.Property(e => e.AktifMi)
                .HasDefaultValue(true);
        });

        // Yazarlar
        modelBuilder.Entity<Yazarlar>(entity =>
        {
            entity.HasKey(e => e.YazarId);

            entity.ToTable("Yazarlar");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(
        ModelBuilder modelBuilder);
}
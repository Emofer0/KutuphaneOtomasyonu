using System;
using System.Collections.Generic;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Data;

public partial class KutuphaneContext : DbContext
{
    public KutuphaneContext(DbContextOptions<KutuphaneContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Kategoriler> Kategorilers { get; set; }

    public virtual DbSet<Kitaplar> Kitaplars { get; set; }

    public virtual DbSet<OduncIslemleri> OduncIslemleris { get; set; }

    public virtual DbSet<Uyeler> Uyelers { get; set; }

    public virtual DbSet<Yazarlar> Yazarlars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kategoriler>(entity =>
        {
            entity.HasKey(e => e.KategoriId);

            entity.ToTable("Kategoriler");
        });

        modelBuilder.Entity<Kitaplar>(entity =>
        {
            entity.HasKey(e => e.KitapId);

            entity.ToTable("Kitaplar");

            entity.HasIndex(e => e.KategoriId, "IX_Kitaplar_KategoriId");

            entity.HasIndex(e => e.YazarId, "IX_Kitaplar_YazarId");

            entity.Property(e => e.Isbn).HasColumnName("ISBN");

            entity.HasOne(d => d.Kategori).WithMany(p => p.Kitaplars).HasForeignKey(d => d.KategoriId);

            entity.HasOne(d => d.Yazar).WithMany(p => p.Kitaplars).HasForeignKey(d => d.YazarId);
        });

        modelBuilder.Entity<OduncIslemleri>(entity =>
        {
            entity.HasKey(e => e.OduncId);

            entity.ToTable("OduncIslemleri");

            entity.HasIndex(e => e.KitapId, "IX_OduncIslemleri_KitapId");

            entity.HasIndex(e => e.UyeId, "IX_OduncIslemleri_UyeId");

            entity.Property(e => e.CezaTutari).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Kitap).WithMany(p => p.OduncIslemleris).HasForeignKey(d => d.KitapId);

            entity.HasOne(d => d.Uye).WithMany(p => p.OduncIslemleris).HasForeignKey(d => d.UyeId);
        });

        modelBuilder.Entity<Uyeler>(entity =>
        {
            entity.HasKey(e => e.UyeId);

            entity.ToTable("Uyeler");

            entity.Property(e => e.Rol).HasDefaultValue("");
            entity.Property(e => e.Sifre).HasDefaultValue("");
        });

        modelBuilder.Entity<Yazarlar>(entity =>
        {
            entity.HasKey(e => e.YazarId);

            entity.ToTable("Yazarlar");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

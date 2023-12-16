using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data;

public partial class DataBaseNameContext : DbContext
{
    public DataBaseNameContext()
    {
    }

    public DataBaseNameContext(DbContextOptions<DataBaseNameContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Weather> Weathers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Weather>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("weather_pk");

            entity.ToTable("weather");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

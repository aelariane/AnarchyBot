using AnarchyBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnarchyBot
{
    public class AnarchyBotContext : DbContext
    {
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Art> Arts { get; set; }
        public DbSet<ArtTag> ArtTags { get; set; }
        public DbSet<ArtistSocial> Socials { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArtServiceInformation> ServiceInformations { get; set; }

        public AnarchyBotContext() : base()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if(BotMain.BotConfiguration.Database == "SQL Server") 
            { 
               optionsBuilder.UseSqlServer(BotMain.BotConfiguration.ConnectionString);
            }
            //Put a check for wanted database connection here
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuring database relations
            EntityTypeBuilder<Artist> bldArtist = modelBuilder.Entity<Artist>();
            bldArtist.HasKey(x => x.Id);
            bldArtist
                .HasMany(x => x.Profiles)
                .WithOne(x => x.Artist)
                .HasForeignKey(x => x.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
            bldArtist
                .HasMany(x => x.Arts)
                .WithOne(x => x.Author)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
            bldArtist
                .Property(x => x.NickName)
                .IsUnicode(true)
                .HasMaxLength(32);

            EntityTypeBuilder<Art> bldArt = modelBuilder.Entity<Art>();
            bldArt.HasKey(x => x.Id);
            bldArt
                .Property(x => x.Source)
                .IsUnicode(true)
                .IsRequired();
            bldArt
                .HasMany(x => x.Tags)
                .WithOne(x => x.Art)
                .HasForeignKey(x => x.ArtId)
                .OnDelete(DeleteBehavior.Cascade);

            EntityTypeBuilder<ArtistSocial> bldSocial = modelBuilder.Entity<ArtistSocial>();
            bldSocial.HasKey(x => x.Id);
            bldSocial
                .Property(x => x.Link)
                .IsUnicode(true)
                .HasMaxLength(128);

            EntityTypeBuilder<Tag> bldTag = modelBuilder.Entity<Tag>();
            bldTag.HasKey(x => x.Id);
            bldTag
                .HasMany(x => x.Arts)
                .WithOne(x => x.Tag)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);
            bldTag
                .Property(x => x.Name)
                .HasMaxLength(32)
                .IsUnicode(true);

            EntityTypeBuilder<ArtTag> bldArtTag = modelBuilder.Entity<ArtTag>();
            bldArtTag.HasKey(x => new { x.ArtId, x.TagId });

            EntityTypeBuilder<ArtServiceInformation> bldInfo = modelBuilder.Entity<ArtServiceInformation>();
            bldInfo.ToTable("ServiceInformation");
            bldInfo.HasKey(x => x.GuildId);
            bldInfo.Property(x => x.GuildId).IsRequired();
        }
    }
}
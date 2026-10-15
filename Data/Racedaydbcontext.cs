using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Table and column definitions mirror the Part 1 SQL script (PROG_POE_P1.sql).

        b.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsUnicode(false).HasDefaultValue(UserRole.Participant);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        b.Entity<Venue>(e =>
        {
            e.ToTable("Venues");
            e.HasKey(x => x.VenueId);
            e.Property(x => x.Name).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.Address).HasMaxLength(200).IsUnicode(false).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Capacity).HasDefaultValue(0);
            e.HasData(
                new Venue { VenueId = 1, Name = "Nelson Mandela Bay Stadium", Address = "1 Prince Alfred Rd", City = "Gqeberha", Capacity = 5000 },
                new Venue { VenueId = 2, Name = "Kings Beach Promenade", Address = "Marine Dr", City = "Gqeberha", Capacity = 2000 },
                new Venue { VenueId = 3, Name = "Rhodes Park", Address = "Park Dr", City = "Makhanda", Capacity = 800 });
        });

        b.Entity<Event>(e =>
        {
            // EventType is the one column added in Part 2 (the Part 2 brief requires run/walk/cycle).
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));
            e.HasKey(x => x.EventId);
            e.Property(x => x.Name).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(20).IsUnicode(false).HasDefaultValue(EventType.Run);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venue).WithMany(v => v.Events).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Name).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed','Cancelled')"));
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).HasDefaultValue(EnrolmentStatus.Confirmed);
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments).HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments_Participant_Category");
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished','DNF','DQ')"));
            e.HasKey(x => x.ResultId);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).HasDefaultValue(ResultStatus.Finished);
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result).HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
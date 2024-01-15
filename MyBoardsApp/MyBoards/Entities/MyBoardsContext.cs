using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities;

public class MyBoardsContext(DbContextOptions<MyBoardsContext> options) : DbContext(options)
{
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Address> Addresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItem>(eb =>
        {
            eb.Property(wi => wi.State).IsRequired();
            eb.Property(wi => wi.Area).HasMaxLength(200);
            eb.Property(wi => wi.Effort).HasPrecision(5, 2);
            eb.Property(wi => wi.EndDate).HasPrecision(3);
            eb.Property(wi => wi.Activity).HasMaxLength(200);
            eb.Property(wi => wi.RemainingWork).HasPrecision(14, 2);
            eb.Property(wi => wi.Priority).HasDefaultValue(1);
            eb.HasMany(wi => wi.Comments).WithOne(c => c.WorkItem).HasForeignKey(c => c.WorkItemId);
            eb.HasOne(wi => wi.Author).WithMany(u => u.WorkItems).HasForeignKey(wi => wi.AuthorId);
        });

        modelBuilder.Entity<Comment>(eb =>
        {
            eb.Property(wi => wi.CreatedDate).HasDefaultValueSql("getutcdate()");
            eb.Property(wi => wi.UpdatedDate).ValueGeneratedOnUpdate();
        });

        modelBuilder
            .Entity<User>()
            .HasOne(u => u.Address)
            .WithOne(a => a.User)
            .HasForeignKey<Address>(a => a.UserId);
    }
}

using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities;

public class MyBoardsContext(DbContextOptions<MyBoardsContext> options) : DbContext(options)
{
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Epic> Epics { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<WorkItemState> WorkItemStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Epic>().Property(wi => wi.EndDate).HasPrecision(3);

        modelBuilder.Entity<Issue>().Property(wi => wi.Effort).HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Task>(eb =>
        {
            eb.Property(wi => wi.Activity).HasMaxLength(200);
            eb.Property(wi => wi.RemainingWork).HasPrecision(14, 2);
        });

        modelBuilder.Entity<WorkItem>(eb =>
        {
            eb.Property(wi => wi.Area).HasMaxLength(200);
            eb.Property(wi => wi.Priority).HasDefaultValue(1);
            eb.HasOne(wi => wi.WorkItemState).WithMany().HasForeignKey(wi => wi.WorkItemStateId);
            eb.HasMany(wi => wi.Comments).WithOne(c => c.WorkItem).HasForeignKey(c => c.WorkItemId);
            eb.HasOne(wi => wi.Author).WithMany(u => u.WorkItems).HasForeignKey(wi => wi.AuthorId);
            eb.HasMany(wi => wi.Tags)
                .WithMany(t => t.WorkItems)
                .UsingEntity<WorkItemTag>(
                    w => w.HasOne(wit => wit.Tag).WithMany().HasForeignKey(wit => wit.TagId),
                    w =>
                        w.HasOne(wit => wit.WorkItem)
                            .WithMany()
                            .HasForeignKey(wit => wit.WorkItemId),
                    wit =>
                    {
                        wit.HasKey(x => new { x.TagId, x.WorkItemId });
                        wit.Property(x => x.PublicationDate).HasDefaultValueSql("getutcdate()");
                    }
                );
        });

        modelBuilder.Entity<WorkItemState>().Property(s => s.Value).IsRequired().HasMaxLength(50);

        modelBuilder.Entity<Comment>(eb =>
        {
            eb.Property(wi => wi.CreatedDate).HasDefaultValueSql("getutcdate()");
            eb.Property(wi => wi.UpdatedDate).ValueGeneratedOnUpdate();
            eb.HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder
            .Entity<User>()
            .HasOne(u => u.Address)
            .WithOne(a => a.User)
            .HasForeignKey<Address>(a => a.UserId);
    }
}

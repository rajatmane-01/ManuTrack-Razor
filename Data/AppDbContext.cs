using ManuTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ManuTrackAPI.Data;

using Component = ManuTrackAPI.Models.Component; // ← add this


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<BOM> BOMs => Set<BOM>();
    public DbSet<Component> Components => Set<Component>(); // ← add

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();           // ← add
    public DbSet<WorkOrderTask> WorkOrderTasks => Set<WorkOrderTask>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>()
            .HasKey(a => a.AuditLogID);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BOM>()
            .HasKey(b => b.BOMID);

        modelBuilder.Entity<BOM>()
            .Property(b => b.Quantity)
            .HasPrecision(18, 2);



        modelBuilder.Entity<BOM>()
            .HasOne(b => b.Product)
            .WithMany(p => p.BOMs)
            .HasForeignKey(b => b.ProductID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BOM>()
            .HasOne(b => b.Component)
            .WithMany(c => c.BOMs)
            .HasForeignKey(b => b.ComponentID)
            .OnDelete(DeleteBehavior.Restrict);


        // ── WorkOrder ──────────────────────────────────────────
        modelBuilder.Entity<WorkOrder>()
            .HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.ProductID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkOrderTask>()
        .HasKey(t => t.TaskID);

        // ── WorkOrderTask ──────────────────────────────────────
        modelBuilder.Entity<WorkOrderTask>()
            .HasOne(t => t.WorkOrder)
            .WithMany(w => w.Tasks)
            .HasForeignKey(t => t.WorkOrderID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrderTask>()
            .HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.Restrict);



    }
}
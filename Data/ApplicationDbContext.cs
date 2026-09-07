using Leave_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Leave_Management_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {


        }

       
        public DbSet<Users> Users { get; set; }
        public DbSet<Leave> Leave { get; set; }

        public DbSet<RoleMaster> RoleMaster { get; set; }
        public DbSet<Team_Project> Team_Project { get; set; }

        public DbSet<LeaveMaster> LeaveMaster { get; set; }
        public DbSet<LeaveMatrix> LeaveMatrix { get; set; }

        public DbSet<LeaveObservationDesiredFlow> LeaveObservationDesiredFlow { get; set; }

        public DbSet<LeaveObservationFlow> LeaveObservationFlow { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LeaveObservationFlow>()
                .HasOne(l => l.Leave)
                .WithMany()
                .HasForeignKey(l => l.LeaveId)
                .OnDelete(DeleteBehavior.NoAction); // 🔥 important

            modelBuilder.Entity<LeaveObservationFlow>()
                .HasOne(l => l.LeaveObservationDesiredFlow)
                .WithMany()
                .HasForeignKey(l => l.LeaveObservationDesiredFlowId)
                .OnDelete(DeleteBehavior.NoAction); // 🔥 important

            // Configure LeaveMatrix relationships to prevent cascade delete conflicts
            modelBuilder.Entity<LeaveMatrix>()
                .HasOne(l => l.RoleMasterParent)
                .WithMany(r => r.LeaveMatrices)
                .HasForeignKey(l => l.RoleMasterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LeaveMatrix>()
                .HasOne(l => l.RoleMaster)
                .WithMany()
                .HasForeignKey(l => l.ToRole)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using NycJobFilings.Data.Models;

namespace NycJobFilings.Data
{
    public class JobFilingsDbContext : DbContext
    {
        public JobFilingsDbContext(DbContextOptions<JobFilingsDbContext> options) : base(options)
        {
        }

        public DbSet<JobFiling> JobFilings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the JobFiling entity
            modelBuilder.Entity<JobFiling>(entity =>
            {
                entity.ToTable("JobFilings");

                // Set up indexes based on the requirements
                entity.HasIndex(e => e.LatestActionDate);
                entity.HasIndex(e => e.Borough);
                entity.HasIndex(e => new { e.JobType, e.JobStatus });
                entity.HasIndex(e => e.InitialCost);
                entity.HasIndex(e => e.ProposedDwellingUnits);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

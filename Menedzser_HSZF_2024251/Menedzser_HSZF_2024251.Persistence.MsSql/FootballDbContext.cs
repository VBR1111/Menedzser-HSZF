using Menedzser_HSZF_2024251.Model;
using Microsoft.EntityFrameworkCore;

namespace Menedzser_HSZF_2024251.Persistence.MsSql
{
    public class FootballDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamTask> Tasks { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<PlayerTeamTask> PlayerTeamTasks { get; set; }
        public DbSet<TransferOffer> TransferOffers { get; set; }
        public DbSet<Season> Seasons { get; set; }


        public FootballDbContext()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connStr = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=footballdb;Integrated Security=True;MultipleActiveResultSets=true";
            optionsBuilder.UseSqlServer(connStr);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<TransferOffer>()
                .HasOne(to => to.FromTeam)
                .WithMany()
                .HasForeignKey(to => to.FromTeamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TransferOffer>()
                .HasOne(to => to.ToTeam)
                .WithMany()
                .HasForeignKey(to => to.ToTeamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PlayerTeamTask>()
                .HasOne(pt => pt.Player)
                .WithMany(p => p.PlayerTasks)
                .HasForeignKey(pt => pt.PlayerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PlayerTeamTask>()
                .HasOne(pt => pt.Task)
                .WithMany(t => t.PlayerTasks)
                .HasForeignKey(pt => pt.TaskId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }

    }
}

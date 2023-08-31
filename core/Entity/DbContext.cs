using Microsoft.EntityFrameworkCore;

namespace supper_searchbot.Entity
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ExecutorSetting> ExecutorSettings { get; set; }
        public DbSet<History> Histories { get; set; }
    }
}

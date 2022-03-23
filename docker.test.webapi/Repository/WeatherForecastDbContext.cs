using Microsoft.EntityFrameworkCore;

namespace docker.test.webapi.Repository;

public class WeatherForecastDbContext: DbContext
{
    public DbSet<WeatherForecast> WeatherForecasts { get; set; }

    public WeatherForecastDbContext(DbContextOptions<WeatherForecastDbContext> options)
        :base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath($"{Directory.GetCurrentDirectory()}")
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("AppDb");
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherForecast>().HasNoKey();
    }
}
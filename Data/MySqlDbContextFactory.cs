using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMvcApp.Data;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<MySqlDbContext>
{
    public MySqlDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<MySqlDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();
        var options = new DbContextOptionsBuilder<MySqlDbContext>()
            .UseMySQL(configuration.GetConnectionString("MySqlConnection")
                ?? throw new InvalidOperationException("MySqlConnection is not configured."))
            .Options;
        return new MySqlDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using MyMvcApp.Entities;

namespace MyMvcApp.Data;

 
public class MySqlDbContext(DbContextOptions<MySqlDbContext> options) : MyDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(user => user.UserName).HasMaxLength(100);
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<CartDB>().Property(product => product.Price).HasPrecision(18, 2);
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using MyMvcApp.Entities;

namespace MyMvcApp.Data;

public class MyDbContext:DbContext
{
     public MyDbContext(DbContextOptions<MyDbContext> options):base (options){}
     
     protected MyDbContext(DbContextOptions options):base(options){}

     public DbSet<User> users{set;get;}
     public DbSet<Product> products{set;get;}
     public DbSet<CartDB> cartDBs{set;get;}
}







using System;
using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Entities;

public class Product
{

    [Key]
  public Guid ID{set;get;}
  public string ProductName { get; set; }=string.Empty;
  public decimal Price { get; set; }
  public string Desc { get; set; }=string.Empty;

  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }

  public Guid UserId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Models;

public class AddToCartDto
{
    [Required]
    public Guid ProductId { get; set; }

    // [Range(1, int.MaxValue)]
    // public int Quantity { get; set; } = 1;
}

public class CartDto
{
    public Guid ID { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Desc { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice => Price * Quantity;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

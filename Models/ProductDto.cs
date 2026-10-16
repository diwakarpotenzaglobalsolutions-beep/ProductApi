using System;
using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Models;

public class ProductDto
{
    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Desc { get; set; } = string.Empty;
    public Guid ID { get; internal set; }

      public DateTime CreatedAt { get; set; }
      public DateTime? UpdatedAt { get; set; }

      public Guid UserId { get; set; }
}




public class AddProductDto
{
    [Required]
    public string ProductName { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }
    public string Desc { get; set; } = string.Empty;
}


public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}


public class UpdateProductDto
{
    public string? ProductName { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? Price { get; set; }
    public string? Desc { get; set; }
}


using Microsoft.EntityFrameworkCore;
using MyMvcApp.Data;
using MyMvcApp.Entities;
using MyMvcApp.Models;

namespace MyMvcApp.Services;

public interface ICartServices
{
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto addToCartDto);
    Task<List<CartDto>> GetAllCartAsync(Guid userId);
    Task<bool> DeleteCartAsync(AddToCartDto addToCartDto );
}

public class CartServices : ICartServices
{
    private readonly MyDbContext _context;

    public CartServices(MyDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> AddToCartAsync(
        Guid userId,
        AddToCartDto addToCartDto

        )
    {


        var product = await _context.products.AsNoTracking().FirstOrDefaultAsync(p => p.ID == addToCartDto.ProductId);

        if (product == null)
            return null;

        var cartData = new CartDB
        {
            ID = Guid.NewGuid(),
            ProductName = product.ProductName,
            Price = product.Price,
            Desc = product.Desc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            UserId = userId
        };

        _context.cartDBs.Add(cartData);

        await _context.SaveChangesAsync();

        return new CartDto
        {
            ID = cartData.ID,
            ProductName = cartData.ProductName,
            Price = cartData.Price,
            Desc = cartData.Desc,
            CreatedAt = cartData.CreatedAt,
            UpdatedAt = cartData.UpdatedAt,
            UserId = cartData.UserId
        };
    }


    public async Task<List<CartDto>> GetAllCartAsync(Guid userId)
    {
        var cartList = await _context.cartDBs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CartDto
            {
                ID = x.ID,
                ProductName = x.ProductName,
                Price = x.Price,
                Desc = x.Desc,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToListAsync();

        return cartList;
    }

        public async Task<bool> DeleteCartAsync(AddToCartDto addToCartDto)
    {
        
        var product = await _context.cartDBs.FirstOrDefaultAsync(p => p.ID == addToCartDto.ProductId);

        if (product is null)
            return false;

        _context.cartDBs.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}

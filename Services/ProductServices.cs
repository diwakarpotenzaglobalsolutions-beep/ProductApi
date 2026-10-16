
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyMvcApp.Data;
using MyMvcApp.Entities;
using MyMvcApp.Models;

namespace MyMvcApp.Services;



public interface IProductServices
{
    Task<ProductDto> AddProduct(AddProductDto productDto, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid productID, CancellationToken cancellationToken = default);
    Task<PagedResponse<ProductDto>> GetAllProduct( int page, int pageSize, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    //Task<ProductDto?> GetProductByIdAsync(Guid productId, Guid userId, bool isAdmin = false);
    Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(Guid productID, UpdateProductDto request, CancellationToken cancellationToken = default);
}

public class ProductServices : IProductServices
{
    private readonly MyDbContext _context;


    public ProductServices(MyDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> AddProduct(AddProductDto productDto, Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = new Product
        {
            ID = Guid.NewGuid(),
            ProductName = productDto.ProductName,
            Price = productDto.Price,
            Desc = productDto.Desc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            UserId = userId
        };

        _context.products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {

            ID = product.ID,
            ProductName = product.ProductName,
            Price = product.Price,
            Desc = product.Desc,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            UserId = product.UserId
        };
    }

    public async Task<PagedResponse<ProductDto>> GetAllProduct( int page, int pageSize, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = _context.products.AsQueryable();
        //var query = _context.products.AsNoTracking().Where(product => isAdmin || product.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(product =>
                product.ProductName.ToLower().Contains(searchTerm) ||
                product.Desc.ToLower().Contains(searchTerm));
        }

        // Apply optional price filters before counting and paginating the results.
        if (minPrice.HasValue)
            query = query.Where(product => product.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(product => product.Price <= maxPrice.Value);

        var totalRecords = await query.CountAsync(cancellationToken);

        var products = await query.OrderByDescending(product => product.CreatedAt)
        .ThenBy(product => product.ID)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(product => new ProductDto
        {
            ID = product.ID,
            ProductName = product.ProductName,
            Price = product.Price,
            Desc = product.Desc,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            UserId = product.UserId

        }).ToListAsync(cancellationToken);



        return new PagedResponse<ProductDto>
        {
            Items = products,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
        };
    }


    // public async Task<ProductDto?> GetProductByIdAsync(Guid productId, Guid userId, bool isAdmin = false)
    // {
    //     return await _context.products.AsNoTracking().Where(p => p.ID == productId && (isAdmin || p.UserId == userId)).Select(p => new ProductDto
    //     {
    //         ID = p.ID,
    //         ProductName = p.ProductName,
    //         Price = p.Price,
    //         Desc = p.Desc,
    //         UserId = p.UserId,
    //         CreatedAt = p.CreatedAt,
    //         UpdatedAt = p.UpdatedAt
    //     }).FirstOrDefaultAsync();
    // }

        public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _context.products.AsNoTracking().Where(p => p.ID == productId ).Select(p => new ProductDto
        {
            ID = p.ID,
            ProductName = p.ProductName,
            Price = p.Price,
            Desc = p.Desc,
            UserId = p.UserId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).FirstOrDefaultAsync(cancellationToken);
    }



    public async Task<ProductDto?> UpdateProductAsync(Guid productID, UpdateProductDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var products = await _context.products.FirstOrDefaultAsync(p => p.ID == productID, cancellationToken);
        if (products is null)
            return null;

        if (request.ProductName is not null)
            products.ProductName = request.ProductName;
        if (request.Price is not null)
            products.Price = request.Price.Value;
        if (request.Desc is not null)
            products.Desc = request.Desc;
        products.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            ID = products.ID,
            ProductName = products.ProductName,
            Price = products.Price,
            Desc = products.Desc,
            CreatedAt = products.CreatedAt,
            UpdatedAt = products.UpdatedAt,
            UserId = products.UserId
        };
    }


    public async Task<bool> DeleteProductAsync(Guid productID, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = await _context.products.FirstOrDefaultAsync(p => p.ID == productID, cancellationToken);

        if (product is null)
            return false;

        _context.products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

   
}

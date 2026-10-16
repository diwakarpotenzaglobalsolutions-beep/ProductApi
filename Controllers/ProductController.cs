
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using MyMvcApp.Services;

namespace MyMvcApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class ProductController : ControllerBase
    {
        private readonly ProductServices _productServices;

        public ProductController(ProductServices productServices)
        {
            _productServices = productServices;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Add-Product")]
        public async Task<IActionResult> AddProduct([FromBody] AddProductDto productDto, CancellationToken cancellationToken = default)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var result = await _productServices.AddProduct(productDto, userId, cancellationToken);


            return Ok(new
            {
                success = true,
                message = "Product added successfully.",
                data = result
            });

            // return OK( new
            // {
            //     success = true,
            //     message = "Product added successfully.",
            //     data = result,
            // });
        }

        [Authorize]
        [HttpGet("GetAll-Product")]

        public async Task<IActionResult> GetAllProductList(int page = 1, int pageSize = 10, [FromQuery] string? search = null,
            [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null, CancellationToken cancellationToken = default)
        {


            if (page < 1)
            {
                return BadRequest(ApiErrorResponse.Create(400, "Page must be at least 1."));
            }
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(ApiErrorResponse.Create(400, "Page size must be between 1 and 100."));
            }

            if (minPrice < 0 || maxPrice < 0)
                return BadRequest(ApiErrorResponse.Create(400, "Price filters must be zero or greater."));

            if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
                return BadRequest(ApiErrorResponse.Create(400, "Minimum price cannot be greater than maximum price."));

            // var userIdValue = User.FindFirstValue(ClaimTypes.Authentication);
            // if (!Guid.TryParse(userIdValue, out var userId))
            //     return Unauthorized(ApiErrorResponse.Create(401));

            // var result = await _productServices.GetAllProduct(userId,page,pageSize,search,minPrice,maxPrice, true);
            var result = await _productServices.GetAllProduct(page, pageSize, search, minPrice, maxPrice, true, cancellationToken);
            return Ok(new
            {
                success = true,
                messsage = "Products retrieved successfully.",
                data = result
            });
        }
        [Authorize(Roles = "Admin,User")]
        [HttpGet("GetProductById/{id:guid}")]
        public async Task<IActionResult> GetProductByID(Guid id, CancellationToken cancellationToken = default)
        {
            // var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (!Guid.TryParse(userIdValue, out var userId))
            //     return Unauthorized(ApiErrorResponse.Create(401));

            var product = await _productServices.GetProductByIdAsync(id, cancellationToken);

            if (product is null)
                return NotFound(ApiErrorResponse.Create(404, "Product not found."));

            return Ok(new { success = true, data = product });
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("Update-ProductById/{id:guid}")]

        public async Task<IActionResult> UpdateProductByID(Guid id, [FromBody] UpdateProductDto request, CancellationToken cancellationToken = default)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var product = await _productServices.UpdateProductAsync(id, request, cancellationToken);
            if (product is null)
                return NotFound(ApiErrorResponse.Create(404, "Product not found."));

            return Ok(new
            {
                success = true,
                message = "Product updated successfully.",
                data = product
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteProduct/{id:guid}")]

        public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken = default)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));


            var product = await _productServices.DeleteProductAsync(id, cancellationToken);
            if (!product)
            {
                return NotFound(ApiErrorResponse.Create(404, "Product not found"));
            }
            return Ok(new
            {
                success = true,
                message = "Product deleted successully"
            });
        }


    }
}



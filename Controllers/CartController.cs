using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using MyMvcApp.Services;

namespace MyMvcApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartServices _cartServices;

        public CartController(ICartServices cartServices)
        {
            _cartServices = cartServices;
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("Add-To-Cart")]

        public async Task<IActionResult> AddToCartProduct([FromBody] AddToCartDto productID)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var result = await _cartServices.AddToCartAsync(userId, productID);

            if (result is null)
                return NotFound(ApiErrorResponse.Create(404, "Product not found"));
            return Ok(new
            {
                success = true,
                message = "Product added to cart successfully.",
                data = result
            });

        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("Get-All-Cart-Data")]
        public async Task<IActionResult> GetAllCartData()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));

            var result = await _cartServices.GetAllCartAsync(userId);
            if (result is null)
            {
                return Ok(new
                {
                    success = false,
                    message = "Data is not found",
                    data = result
                });
            }

            return Ok(new
            {
                success = true,
                message = "Data is found",
                data = result
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpDelete("Remove-Cart-Data")]
        public async Task<IActionResult> DeleteCartData(AddToCartDto addToCartDto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(ApiErrorResponse.Create(401));


            var product = await _cartServices.DeleteCartAsync(addToCartDto);
            if (!product)
            {
                return NotFound(ApiErrorResponse.Create(404, "Product not found"));
            }
            return Ok(new
            {
                success = true,
                message = "Cart deleted successully"
            });
        }



    }
}

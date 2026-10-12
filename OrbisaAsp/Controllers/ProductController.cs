using CloudinaryDotNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrbisaAsp.Data.DTO;
//using OrbisaAsp.Data.DTOs.Products;
using OrbisaAsp.Data.Models;
using OrbisaAsp.Data.Services;

namespace OrbisaAsp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        // GET: api/Product
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _productService.GetProducts();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error al obtener los productos.",
                        error = ex.Message
                    }
                );
            }
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(string id)
        {
            try
            {
                var product = await _productService.GetProductById(id);

                if (product == null)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error al obtener el producto.",
                        error = ex.Message
                    }
                );
            }
        }

        // POST: api/Product
        [HttpPost]
        [Authorize(Policy = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct(
            [FromForm] CreateProductRequest request)
        {
            try
            {
                var product = new Product
                {
                    id = request.id,
                    name = request.name,
                    size = request.size,
                    color = request.color,
                    gender = request.gender,
                    description = request.description,
                    tag = request.tag,
                    price = request.price,
                    isAbled = request.isAbled
                };

                var createdProduct =
                    await _productService.CreateProduct(
                        product,
                        request.images
                    );

                return CreatedAtAction(
                    nameof(GetProduct),
                    new { id = createdProduct.id },
                    createdProduct
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error al crear el producto.",
                        error = ex.Message
                    }
                );
            }
        }

        // PUT: api/Product/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(
            string id,
            [FromForm] UpdateProductRequest request)
        {
            try
            {
                var product = new Product
                {
                    id = id,
                    name = request.name,
                    size = request.size,
                    color = request.color,
                    gender = request.gender,
                    description = request.description,
                    tag = request.tag,
                    price = request.price,
                    isAbled = request.isAbled
                };

                var updatedProduct =
                    await _productService.UpdateProduct(
                        product,
                        request.images
                    );

                if (updatedProduct == null)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                return Ok(updatedProduct);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error al actualizar el producto.",
                        error = ex.Message
                    }
                );
            }
        }

        // DELETE: api/Product/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                var deleted =
                    await _productService.DeleteProduct(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                return Ok(new
                {
                    message = "Producto eliminado correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error al eliminar el producto.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}
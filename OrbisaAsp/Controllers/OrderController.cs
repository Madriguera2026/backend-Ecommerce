using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrbisaAsp.Data.Models;
using OrbisaAsp.Data.Services;
using static OrbisaAsp.Data.Models.Order;

namespace OrbisaAsp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: api/Order
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<List<Order>>> GetAll()
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }

        // GET: api/Order/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetById(string id)
        {
            var order = await _orderService.GetByIdAsync(id);

            if (order == null)
                return NotFound(new { message = "Orden no encontrada." });

            // TODO: comprobar que el usuario autenticado sea el propietario
            // de esta orden o tenga permisos de administrador.

            return Ok(order);
        }

        // GET: api/Order/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<List<Order>>> GetByUserId(
            string userId)
        {
            var orders = await _orderService.GetByUserIdAsync(userId);
            return Ok(orders);
        }

        // POST: api/Order
        [HttpPost]
        public async Task<ActionResult<Order>> Create([FromBody] Order order)
        {
            if (order == null)
                return BadRequest(new { message = "La orden es obligatoria." });

            try
            {
                var created = await _orderService.CreateAsync(order);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = created.id },
                    created);
            }
            catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
            {
                return Conflict(new
                {
                    message = "Ya existe una orden con ese identificador."
                });
            }
        }

        // PUT: api/Order/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Update(
            string id,
            [FromBody] Order order)
        {
            if (order == null || id != order.id)
            {
                return BadRequest(new
                {
                    message = "El ID de la ruta debe coincidir con el de la orden."
                });
            }

            var updated = await _orderService.UpdateAsync(order);

            if (!updated)
                return NotFound(new { message = "Orden no encontrada." });

            return NoContent();
        }

        // DELETE: api/Order/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> Delete(string id)
        {
            var deleted = await _orderService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Orden no encontrada." });

            return NoContent();
        }

        // PUT: api/Order/{id}
        [HttpPut("{id}/{status}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> UpdateStatus(string id, int status)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order== null) { return NotFound(new { message = "Orden no encontrada." }); }
            order.state = (StateType)status;

            var updated = await _orderService.UpdateAsync(order);

            if (!updated)
                return NotFound(new { message = "Orden no encontrada." });

            return NoContent();
        }
    }
}

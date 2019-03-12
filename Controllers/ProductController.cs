using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ng_Core_Auth.Data;
using Ng_Core_Auth.Models;

namespace Ng_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Product
        [HttpGet]
        [Authorize(Policy = "RequiredLoggenIn")]
        public async Task<IActionResult> GetProductsAsync()
        {
            return Ok( await _context.Products.ToListAsync());
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        [Authorize(Policy = "RequiredAdministratorRole")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        [Authorize(Policy = "RequiredAdministratorRole")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id,[FromBody] ProductModel formData)
        {
            if (id != formData.ProductId)
            {
                return BadRequest();
            }

            _context.Entry(formData).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok( new JsonResult("The Product with id " + id + " was updated"));
        }

        // POST: api/Product
        [HttpPost]
        [Authorize(Policy = "RequiredAdministratorRole")]
        public async Task<IActionResult> AddProduct([FromBody]ProductModel formData)
        {
            _context.Products.Add(formData);
            await _context.SaveChangesAsync();

            return CreatedAtAction("AddProduct", new { id = formData.ProductId }, formData);
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequiredAdministratorRole")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new JsonResult("The Product with id " + id + " was deleted"));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}

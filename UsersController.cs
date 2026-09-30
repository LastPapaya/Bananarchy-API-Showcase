using Microsoft.AspNetCore.Mvc;
using BananarchyAPI.Data;
using BananarchyAPI.Models;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BananarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            
            var resources = string.IsNullOrEmpty(user.ResourcesJson)
                ? new Dictionary<string, int> { { "bananas", 100 } } 
                : JsonSerializer.Deserialize<Dictionary<string, int>>(user.ResourcesJson);

            
            if (string.IsNullOrEmpty(user.ResourcesJson))
            {
                user.ResourcesJson = JsonSerializer.Serialize(resources);
                await _context.SaveChangesAsync();
            }
            

            return Ok(new
            {
                user.Id,
                user.Username,
                Resources = resources,
                user.PositionX,
                user.PositionY,
                user.ArmyCount
            });
        }
// POST api/users/{id}/train-monkey
[HttpPost("{id}/train-monkey")]
public async Task<IActionResult> TrainMonkey(int id)
{
    var user = await _context.Users.FindAsync(id);
    if (user == null)
        return NotFound(new { message = "User not found" });

    var resources = string.IsNullOrEmpty(user.ResourcesJson)
        ? new Dictionary<string, int>()
        : JsonSerializer.Deserialize<Dictionary<string, int>>(user.ResourcesJson) 
          ?? new Dictionary<string, int>();

    resources.TryGetValue("bananas", out int bananas);

    const int cost = 30;
    if (bananas < cost)
        return BadRequest(new { message = "Not enough bananas", bananas, cost });

    resources["bananas"] = bananas - cost;
    user.ResourcesJson = JsonSerializer.Serialize(resources);

    user.ArmyCount += 1;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Monkey trained",
        Army = user.ArmyCount,
        Resources = resources
    });
}

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
             var users = await _context.Users
            .Select(u => new
            {
                 u.Id,
                 u.Username,
                 u.PositionX,
                 u.PositionY,
                 u.ArmyCount
             })
                .ToListAsync();

            return Ok(users);
        }
        
        
        // POST api/users/{id}/resources
        [HttpPost("{id}/resources")]
        public async Task<IActionResult> UpdateResources(int id, [FromBody] UpdateResourcesRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (request == null || request.resources == null)
                return BadRequest(new { message = "Invalid resource data" });

            // Перетворюємо словник назад у JSON
            user.ResourcesJson = JsonSerializer.Serialize(request.resources);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Resources updated successfully", request.resources });
        }
    }

    

    // DTO
    public class UpdateResourcesRequest
    {
        public Dictionary<string, int> resources { get; set; }
    }
}

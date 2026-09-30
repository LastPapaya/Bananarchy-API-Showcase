using Microsoft.AspNetCore.Mvc;
using BananarchyAPI.Data;
using BananarchyAPI.Models;
using Microsoft.EntityFrameworkCore;
using BananarchyAPI.Helpers;

namespace BananarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerBuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlayerBuildingsController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetBuildings(int userId)
        {
            var buildings = await _context.PlayerBuilding
                .Where(b => b.UserId == userId)
                .ToListAsync();

            if (buildings == null || buildings.Count == 0)
                return Ok(new List<PlayerBuilding>()); 

            return Ok(buildings);
        }

        
        [HttpPost]
        public async Task<IActionResult> AddBuilding([FromBody] PlayerBuildingRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid building data" });

            
            var buildingType = await _context.Buildings
                .FirstOrDefaultAsync(b => b.Name == request.Type);

            if (buildingType == null)
                return NotFound(new { message = $"Building type '{request.Type}' not found" });

           
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
                return NotFound(new { message = $"User with id {request.UserId} not found" });
            
            bool isTileOccupied = await _context.PlayerBuilding
            .AnyAsync(b => b.UserId == request.UserId && b.PosX == request.PosX && b.PosY == request.PosY);

            if (isTileOccupied)
            {
            
                return BadRequest(new { message = "Ця клітинка вже зайнята іншою будівлею!" });
            }
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound("User not found");


            
            
            var playerBuilding = new PlayerBuilding
            {
                UserId = request.UserId,
                BuildingId = buildingType.Id,
                PosX = request.PosX,
                PosY = request.PosY,
                Level = request.Level
            };

            PlayerResources resources;
            try
            {
                resources = System.Text.Json.JsonSerializer.Deserialize<PlayerResources>(user.ResourcesJson) 
                            ?? new PlayerResources();
            }
            catch
            {
                
                resources = new PlayerResources();
            }

            
            if (resources.Bananas < buildingType.BuildCost) 
            {
                return BadRequest(new { message = "Недостатньо бананів для будівництва!" });
            }

            
            resources.Bananas -= buildingType.BuildCost;

            
            user.ResourcesJson = System.Text.Json.JsonSerializer.Serialize(resources);

            _context.PlayerBuilding.Add(playerBuilding);
            await _context.SaveChangesAsync();

           return Ok(new { 
                message = "Building added successfully", 
                playerBuilding,
                newBalance = resources.Bananas
                
            });
        }
    }

    
    public class PlayerBuildingRequest
    {
        public int UserId { get; set; }
        public string Type { get; set; } = "";
        public float PosX { get; set; }
        public float PosY { get; set; }
        public int Level { get; set; }
    }
}

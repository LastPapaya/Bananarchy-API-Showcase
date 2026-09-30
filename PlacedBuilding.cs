namespace BananarchyAPI.Models
{
    public class PlacedBuilding
    {
        public int Id { get; set; }
        
        
        public int UserId { get; set; } 
        
        
        public int BuildingId { get; set; }
        public Building? Building { get; set; }

        
        public int TileX { get; set; }
        public int TileY { get; set; }

        
        public int CurrentLevel { get; set; } = 1;
        
        
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    }
}
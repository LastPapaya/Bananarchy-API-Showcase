namespace BananarchyAPI.Models
{
    public class PlayerBuilding
    {
        public int Id { get; set; }

        
        public int UserId { get; set; }

        
        public int BuildingId { get; set; }

       
        public float PosX { get; set; }
        public float PosY { get; set; }

        
        public int Level { get; set; }

        
        public User? User { get; set; }
        public Building? Building { get; set; }
    }
}

namespace FCG.Games.Domain.Entities
{
    public class Game : Base
    {       
        public string Name { get; set; } = null!;        
        public string Description { get; set; } = null!;
        public DateTime DateRelease { get; set; } 
        public DateTime DateUpdate { get; set; } = DateTime.Now!;
    }
}

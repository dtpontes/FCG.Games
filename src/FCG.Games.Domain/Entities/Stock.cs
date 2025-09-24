namespace FCG.Games.Domain.Entities
{
    public class Stock : Base
    {
        public long GameId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Game Game { get; set; } = null!;
    }
}
namespace CartService.DAL.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public bool ForBonusPoints { get; set; }
        public int Amount { get; set; }
    }
}
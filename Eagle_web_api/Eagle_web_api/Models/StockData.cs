namespace Eagle_web_api.Models
{
    public class StockData
    {
        public Int32 Id { get; set; }

        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public Int32 FavoriteTickers_id { get; set; }
    }
}

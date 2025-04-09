namespace Eagle_web_api.Models.Tables
{
    public class StockData
    {
        public int Id { get; set; }

        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public int FavoriteTickers_id { get; set; }
    }
}

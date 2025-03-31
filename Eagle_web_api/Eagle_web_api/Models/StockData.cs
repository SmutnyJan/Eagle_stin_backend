namespace Eagle_web_api.Models
{
    public class StockData
    {
        public Int32 id { get; set; }

        public decimal price { get; set; }
        public DateTime date { get; set; }
        public Int32 FavoriteTickers_id { get; set; }

    }
}

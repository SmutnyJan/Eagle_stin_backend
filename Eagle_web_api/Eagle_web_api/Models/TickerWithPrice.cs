namespace Eagle_web_api.Models
{
    public class TickerWithPrice
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public decimal? LatestPrice { get; set; }
        public DateTime? LatestDate { get; set; }
    }
}

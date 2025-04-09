namespace Eagle_web_api.Models
{
    public class TickerWithRecommendation
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public Recommendation Recommendation { get; set; }
    }

    public enum Recommendation
    {
        Buy,
        Sell
    }
}

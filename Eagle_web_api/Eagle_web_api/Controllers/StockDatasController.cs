using Eagle_web_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eagle_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockDatasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockDatasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StockDatas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockData>>> GetStockDatas()
        {
            return await _context.StockDatas.ToListAsync();
        }

        // GET: api/StockDatas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockData>> GetStockData(int id)
        {
            StockData? stockData = await _context.StockDatas.FindAsync(id);

            if (stockData == null)
            {
                return NotFound();
            }

            return stockData;
        }



        // DELETE: api/StockDatas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockData(int id)
        {
            StockData? stockData = await _context.StockDatas.FindAsync(id);
            if (stockData == null)
            {
                return NotFound();
            }

            _context.StockDatas.Remove(stockData);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost("UpdateCurrentPrices")]
        public async Task<IActionResult> UpdateCurrentPrices()
        {

            List<FavoriteTicker> favoriteTickers = await _context.FavoriteTickers.ToListAsync();

            HttpClient client = new();

            foreach (FavoriteTicker ticker in favoriteTickers)
            {
                string url = $"https://finnhub.io/api/v1/quote?symbol={ticker.Ticker}&token=" + AppDbContext.API_KEY;
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode) continue;

                string json = await response.Content.ReadAsStringAsync();
                QuoteResponse? quote = JsonSerializer.Deserialize<QuoteResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quote == null || quote.C == 0) continue;

                StockData newStockData = new()
                {
                    Price = quote.C,
                    Date = DateTime.Now,
                    FavoriteTickers_id = ticker.Id
                };

                _context.StockDatas.Add(newStockData);
            }

            await _context.SaveChangesAsync();

            return Ok("Prices updated.");
        }
    }
}

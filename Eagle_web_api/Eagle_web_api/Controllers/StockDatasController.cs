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
        private const string API_KEY = "cvi0sn9r01qks9q7hi0gcvi0sn9r01qks9q7hi10";

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
            var stockData = await _context.StockDatas.FindAsync(id);

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
            var stockData = await _context.StockDatas.FindAsync(id);
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
            const int maxRetries = 10;
            int retryCount = 0;
            List<FavoriteTicker> favoriteTickers = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    favoriteTickers = await _context.FavoriteTickers.ToListAsync();

                    if (favoriteTickers != null && favoriteTickers.Count > 0)
                        break;
                }
                catch (Exception ex)
                {
                }

                retryCount++;
                await Task.Delay(2000);
            }

            if (favoriteTickers == null || favoriteTickers.Count == 0)
            {
                return StatusCode(500, "Database not ready or empty after " + maxRetries + " attempts.");
            }

            var client = new HttpClient();

            foreach (var ticker in favoriteTickers)
            {
                var url = $"https://finnhub.io/api/v1/quote?symbol={ticker.ticker}&token=" + API_KEY;
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var quote = JsonSerializer.Deserialize<QuoteResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quote == null || quote.c == 0) continue;

                var newStockData = new StockData
                {
                    price = quote.c,
                    date = DateTime.Now,
                    FavoriteTickers_id = ticker.id
                };

                _context.StockDatas.Add(newStockData);
            }

            await _context.SaveChangesAsync();

            return Ok("Prices updated.");
        }


    }
}

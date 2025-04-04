using Eagle_web_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eagle_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteTickersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoriteTickersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/FavoriteTickers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteTicker>>> GetFavoriteTickers()
        {
            return await _context.FavoriteTickers.ToListAsync();
        }

        // GET: api/FavoriteTickers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FavoriteTicker>> GetFavoriteTicker(int id)
        {
            var favoriteTicker = await _context.FavoriteTickers.FindAsync(id);

            if (favoriteTicker == null)
            {
                return NotFound();
            }

            return favoriteTicker;
        }

        // DELETE: api/FavoriteTickers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavoriteTicker(int id)
        {
            var favoriteTicker = await _context.FavoriteTickers.FindAsync(id);
            if (favoriteTicker == null)
            {
                return NotFound();
            }

            var relatedStockData = await _context.StockDatas
                .Where(sd => sd.FavoriteTickers_id == id)
                .ToListAsync();

            if (relatedStockData.Any())
            {
                _context.StockDatas.RemoveRange(relatedStockData);
                await _context.SaveChangesAsync();
            }

            _context.FavoriteTickers.Remove(favoriteTicker);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost]
        public async Task<ActionResult<FavoriteTicker>> PostFavoriteTicker([FromBody] string favoriteTicker)
        {
            if (string.IsNullOrWhiteSpace(favoriteTicker))
            {
                return BadRequest("Ticker is empty.");
            }

            var client = new HttpClient();
            var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={favoriteTicker}&token=" + AppDbContext.API_KEY;

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error communicating with Finnhub API.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<ProfileResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return BadRequest("Ticker " + favoriteTicker + " not found.");
            }

            var exists = await _context.FavoriteTickers.AnyAsync(x => x.ticker == favoriteTicker.ToUpper());
            if (exists)
            {
                return Conflict("Ticker is already in favorites.");
            }

            var newFavorite = new FavoriteTicker
            {
                ticker = favoriteTicker.ToUpper(),
                name = profile.Name,
                logo = profile.Logo
            };

            _context.FavoriteTickers.Add(newFavorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFavoriteTicker", new { id = newFavorite.id }, newFavorite);
        }


        [HttpGet("FilteredPrices")]
        public async Task<ActionResult<IEnumerable<TickerWithPrice>>> GetFilteredPrices([FromQuery] int filterId = 1)
        {
            
            var tickers = await _context.FavoriteTickers.ToListAsync();
            var result = new List<TickerWithPrice>();

            switch (filterId)
            {
                case 1:
                    foreach (var ft in tickers)
                    {
                        var latestPrice = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.id)
                            .OrderByDescending(sd => sd.date)
                            .FirstOrDefaultAsync();

                        result.Add(new TickerWithPrice
                        {
                            Ticker = ft.ticker,
                            Name = ft.name,
                            Logo = ft.logo,
                            LatestPrice = latestPrice?.price,
                            LatestDate = latestPrice?.date
                        });
                    }
                    break;

                case 2:
                    var today = DateTime.UtcNow.Date;
                    var threeDaysAgo = today.AddDays(-3);

                    foreach (var ft in tickers)
                    {
                        var prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.id && sd.date >= threeDaysAgo)
                            .ToListAsync();

                        // seskup podle dne a vezmi vždy nejpozdější záznam pro ten den
                        var grouped = prices
                            .GroupBy(p => p.date.Date)
                            .Select(g => g.OrderByDescending(p => p.date).First())
                            .OrderByDescending(p => p.date)
                            .Take(3)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        if (grouped[0].price >= grouped[1].price && grouped[1].price >= grouped[2].price)
                        {
                            // poslední 3 dny cena neklesala
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.ticker,
                                Name = ft.name,
                                Logo = ft.logo,
                                LatestPrice = grouped[0].price,
                                LatestDate = grouped[0].date
                            });
                        }
                    }
                    break;

                case 3:
                    var now = DateTime.UtcNow;
                    var fiveDaysAgo = now.Date.AddDays(-5);

                    foreach (var ft in tickers)
                    {
                        var prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.id && sd.date >= fiveDaysAgo)
                            .ToListAsync();

                        var grouped = prices
                            .GroupBy(p => p.date.Date)
                            .Select(g => g.OrderByDescending(p => p.date).First())
                            .OrderBy(p => p.date)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        int declineCount = 0;
                        for (int i = 1; i < grouped.Count; i++)
                        {
                            if (grouped[i].price < grouped[i - 1].price)
                                declineCount++;
                        }

                        if (declineCount <= 2)
                        {
                            var latest = grouped.Last();
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.ticker,
                                Name = ft.name,
                                Logo = ft.logo,
                                LatestPrice = latest.price,
                                LatestDate = latest.date
                            });
                        }
                    }
                    break;

                default:
                    return BadRequest("Neznámý filtr.");
            }

            return Ok(result);
        }
    }
}

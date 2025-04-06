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
            FavoriteTicker? favoriteTicker = await _context.FavoriteTickers.FindAsync(id);

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
            FavoriteTicker? favoriteTicker = await _context.FavoriteTickers.FindAsync(id);
            if (favoriteTicker == null)
            {
                return NotFound();
            }

            List<StockData> relatedStockData = await _context.StockDatas
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

            HttpClient client = new();
            string profileUrl = $"https://finnhub.io/api/v1/stock/profile2?symbol={favoriteTicker}&token=" + AppDbContext.API_KEY;

            HttpResponseMessage profileResponse = await client.GetAsync(profileUrl);
            if (!profileResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)profileResponse.StatusCode, "Error communicating with Finnhub API.");
            }

            string profileJson = await profileResponse.Content.ReadAsStringAsync();
            ProfileResponse? profile = JsonSerializer.Deserialize<ProfileResponse>(profileJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return BadRequest("Ticker " + favoriteTicker + " not found.");
            }

            bool exists = await _context.FavoriteTickers.AnyAsync(x => x.Ticker == favoriteTicker.ToUpper());
            if (exists)
            {
                return Conflict("Ticker is already in favorites.");
            }

            FavoriteTicker newFavorite = new()
            {
                Ticker = favoriteTicker.ToUpper(),
                Name = profile.Name,
                Logo = profile.Logo
            };

            _context.FavoriteTickers.Add(newFavorite);
            await _context.SaveChangesAsync();

            string quoteUrl = $"https://finnhub.io/api/v1/quote?symbol={newFavorite.Ticker}&token=" + AppDbContext.API_KEY;
            HttpResponseMessage quoteResponse = await client.GetAsync(quoteUrl);

            if (quoteResponse.IsSuccessStatusCode)
            {
                string quoteJson = await quoteResponse.Content.ReadAsStringAsync();
                QuoteResponse? quote = JsonSerializer.Deserialize<QuoteResponse>(quoteJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quote != null && quote.C != 0)
                {
                    StockData newStockData = new()
                    {
                        Price = quote.C,
                        Date = DateTime.Now,
                        FavoriteTickers_id = newFavorite.Id
                    };

                    _context.StockDatas.Add(newStockData);
                    await _context.SaveChangesAsync();
                }
            }

            return CreatedAtAction("GetFavoriteTicker", new { id = newFavorite.Id }, newFavorite);
        }


        [HttpGet("FilteredPrices")]
        public async Task<ActionResult<IEnumerable<TickerWithPrice>>> GetFilteredPrices([FromQuery] int filterId = 1)
        {

            List<FavoriteTicker> tickers = await _context.FavoriteTickers.ToListAsync();
            List<TickerWithPrice> result = new();
            Console.WriteLine("Filter ID: " + filterId);

            switch (filterId)
            {
                case 1:
                    foreach (FavoriteTicker? ft in tickers)
                    {
                        StockData? latestPrice = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id)
                            .OrderByDescending(sd => sd.Date)
                            .FirstOrDefaultAsync();

                        result.Add(new TickerWithPrice
                        {
                            Ticker = ft.Ticker,
                            Name = ft.Name,
                            Logo = ft.Logo,
                            LatestPrice = latestPrice?.Price,
                            LatestDate = latestPrice?.Date
                        });
                    }
                    break;

                case 2:
                    DateTime today = DateTime.UtcNow.Date;
                    DateTime threeDaysAgo = today.AddDays(-3);

                    foreach (FavoriteTicker? ft in tickers)
                    {
                        List<StockData> prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id && sd.Date >= threeDaysAgo)
                            .ToListAsync();

                        // seskup podle dne a vezmi vždy nejpozdější záznam pro ten den
                        List<StockData> grouped = prices
                            .GroupBy(p => p.Date.Date)
                            .Select(g => g.OrderByDescending(p => p.Date).First())
                            .OrderByDescending(p => p.Date)
                            .Take(3)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        if (grouped[0].Price >= grouped[1].Price && grouped[1].Price >= grouped[2].Price)
                        {
                            // poslední 3 dny cena neklesala
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.Ticker,
                                Name = ft.Name,
                                Logo = ft.Logo,
                                LatestPrice = grouped[0].Price,
                                LatestDate = grouped[0].Date
                            });
                        }
                    }
                    break;

                case 3:
                    DateTime now = DateTime.UtcNow;
                    DateTime fiveDaysAgo = now.Date.AddDays(-5);

                    foreach (FavoriteTicker? ft in tickers)
                    {
                        List<StockData> prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id && sd.Date >= fiveDaysAgo)
                            .ToListAsync();

                        List<StockData> grouped = prices
                            .GroupBy(p => p.Date.Date)
                            .Select(g => g.OrderByDescending(p => p.Date).First())
                            .OrderBy(p => p.Date)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        int declineCount = 0;
                        for (int i = 1; i < grouped.Count; i++)
                        {
                            if (grouped[i].Price < grouped[i - 1].Price)
                                declineCount++;
                        }

                        if (declineCount <= 2)
                        {
                            StockData latest = grouped.Last();
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.Ticker,
                                Name = ft.Name,
                                Logo = ft.Logo,
                                LatestPrice = latest.Price,
                                LatestDate = latest.Date
                            });
                        }
                    }
                    break;

                default:
                    return BadRequest("Neznámý filtr.");
            }

            return Ok(result);
        }


        [HttpPost("Rating")]
        public async Task<ActionResult<List<TickerRating>>> GetRatings([FromBody] List<string> tickers)
        {
            /*if (tickers == null || !tickers.Any())
                return BadRequest("Ticker seznam je prázdný.");

            var requestBody = new { tickers = tickers };

            using HttpClient client = new();

            string requestUrl = "http://localhost:5000/evaluateStocks"; // změň na produkční adresu podle potřeby

            StringContent content = new(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Chyba při komunikaci s hodnotícím API.");

            string json = await response.Content.ReadAsStringAsync();


            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

            Wrapper? parsed = JsonSerializer.Deserialize<Wrapper>(json, options);

            if (parsed?.Stocks == null)
                return StatusCode(500, "Chybná odpověď z API.");


            List<TickerRating> result = parsed.Stocks.Select(s => new TickerRating
            {
                Ticker = s.Symbol,
                Rating = int.TryParse(s.Rating, out int r) ? r : 0
            }).ToList();

            return Ok(result);*/

            Random random = new();

            IQueryable<FavoriteTicker> favoriteTickers = _context.FavoriteTickers.Where(x => tickers.Contains(x.Ticker));

            List<TickerRating> tickerRatings = favoriteTickers.Select(x => new TickerRating
            {

                Ticker = x.Ticker,
                Name = x.Name,
                Logo = x.Logo,
                Rating = random.Next(-10, 11)
            }).ToList();

            return Ok(tickerRatings);
        }


        [HttpPost("ProcessTickers")]
        public async Task<ActionResult<List<ProfileResponse>>> ProcessTickers([FromBody] List<TickerRating> tickers, [FromQuery] int tickerLimit)
        {
            if (tickers == null || !tickers.Any())
                return BadRequest("Seznam tickerů je prázdný.");

            List<ProfileResponse> filteredProfiles = tickers
                .Where(t => t.Rating > tickerLimit)
                .Select(t => new ProfileResponse
                {
                    Ticker = t.Ticker,
                    Name = t.Name,
                    Logo = t.Logo
                })
                .ToList();

            // volat api od Matěje

            return Ok(filteredProfiles);
        }
    }
}

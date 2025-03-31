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
        private const string API_KEY = "cvi0sn9r01qks9q7hi0gcvi0sn9r01qks9q7hi10";

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

            _context.FavoriteTickers.Remove(favoriteTicker);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<FavoriteTicker>> PostFavoriteTicker(string favoriteTicker)
        {
            if (string.IsNullOrWhiteSpace(favoriteTicker))
            {
                return BadRequest("Ticker is empty.");
            }

            var client = new HttpClient();
            var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={favoriteTicker}&token=" + API_KEY;

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
                name = profile.Name
            };

            _context.FavoriteTickers.Add(newFavorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFavoriteTicker", new { id = newFavorite.id }, newFavorite);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IMarketplaceSearchServices _marketplaceSearchServices;
        private readonly MarketplaceSettings _settings;

        public ProductController(
            IMarketplaceSearchServices marketplaceSearchServices,
            IOptions<MarketplaceSettings> settings)
        {
            _marketplaceSearchServices = marketplaceSearchServices;
            _settings = settings.Value;
        }

        /// <summary>
        /// Pencarian produk marketplace yang dipicu dari frontend.
        /// Contoh: GET /api/product/search?q=iphone&page=1&limit=20
        /// Kategori device (HP/tablet/laptop) dideteksi otomatis; override
        /// dengan ?category=24 (Handphone), 276 (Tablet), 3844 (Laptop),
        /// atau ?category=0 untuk mematikan filter kategori.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? q,
            [FromQuery] string? keyword,
            [FromQuery] string marketplace = "tokopedia",
            [FromQuery] int page = 1,
            [FromQuery] int? limit = null,
            [FromQuery] int? category = null,
            [FromQuery] bool refresh = false,
            CancellationToken cancellationToken = default)
        {
            // Dukung dua nama parameter agar fleksibel untuk frontend:
            // ?q=... maupun ?keyword=...
            var searchTerm = string.IsNullOrWhiteSpace(q) ? keyword : q;

            var query = new MarketplaceSearchQuery
            {
                Keyword = searchTerm ?? string.Empty,
                Marketplace = marketplace,
                Page = page,
                Limit = limit ?? _settings.DefaultPageSize,
                CategoryId = category,
                Refresh = refresh
            };

            var result = await _marketplaceSearchServices.SearchAsync(query, cancellationToken);

            var message = result.Stale
                ? "Marketplace sedang tidak dapat diakses. Menampilkan data cache terakhir."
                : result.Cached
                    ? "Produk diambil dari cache."
                    : "Produk berhasil diambil dari marketplace.";

            return Ok(new GlobalJsonResponse<MarketplaceSearchResult>(message, result));
        }

        [HttpGet("marketplaces")]
        public IActionResult GetMarketplaces()
        {
            return Ok(new GlobalJsonResponse<List<string>>(
                "Daftar marketplace yang didukung.",
                _marketplaceSearchServices.SupportedMarketplaces.ToList()));
        }
    }
}

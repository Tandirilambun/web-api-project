using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    /// <summary>
    /// Kontrak provider scraping marketplace. Tambahkan implementasi baru
    /// (mis. Shopee/Lazada) lalu daftarkan di DI untuk mendukung marketplace lain.
    /// </summary>
    public interface IMarketplaceScraper
    {
        /// <summary>Nama marketplace, dipakai sebagai nilai parameter "marketplace".</summary>
        string Marketplace { get; }

        Task<MarketplaceScrapeResult> SearchAsync(
            string keyword,
            int page,
            int limit,
            int? categoryId = null,
            CancellationToken cancellationToken = default);
    }
}

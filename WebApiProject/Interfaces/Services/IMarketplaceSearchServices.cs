using WebApiProject.Models;

namespace WebApiProject.Interfaces
{
    public interface IMarketplaceSearchServices
    {
        /// <summary>Daftar marketplace yang punya provider scraper terdaftar.</summary>
        IReadOnlyCollection<string> SupportedMarketplaces { get; }

        /// <summary>
        /// Cari produk di marketplace. Hasil disimpan di cache sehingga request
        /// dengan kata kunci + halaman yang sama tidak menembak web marketplace lagi.
        /// </summary>
        Task<MarketplaceSearchResult> SearchAsync(
            MarketplaceSearchQuery query,
            CancellationToken cancellationToken = default);
    }
}

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WebApiProject.Exceptions;
using WebApiProject.Interfaces;
using WebApiProject.Models;
using WebApiProject.Services.Marketplace;

namespace WebApiProject.Services
{
    /// <summary>
    /// Orkestrator pencarian marketplace: validasi input, panggil scraper,
    /// lalu simpan hasilnya di IMemoryCache.
    ///
    /// Tujuan cache:
    /// - Request dengan kata kunci + marketplace + halaman + limit yang sama
    ///   tidak menembak website marketplace lagi sampai TTL habis.
    /// - Lock per cache key mencegah "cache stampede" (banyak request serentak
    ///   untuk keyword yang sama tetap hanya melakukan satu kali scraping).
    /// - Data lama (stale) disimpan lebih lama dan dipakai sebagai fallback
    ///   ketika marketplace sedang error/diblokir.
    /// </summary>
    public class MarketplaceSearchServices : IMarketplaceSearchServices
    {
        private const int MinKeywordLength = 2;
        private const int MaxKeywordLength = 100;

        // Static agar lock tetap berlaku antar-request (service ini scoped).
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SearchLocks = new();

        private readonly IReadOnlyDictionary<string, IMarketplaceScraper> _scrapers;
        private readonly IReadOnlyCollection<string> _supportedMarketplaces;
        private readonly IMemoryCache _cache;
        private readonly MarketplaceSettings _settings;
        private readonly ILogger<MarketplaceSearchServices> _logger;

        public MarketplaceSearchServices(
            IEnumerable<IMarketplaceScraper> scrapers,
            IMemoryCache cache,
            IOptions<MarketplaceSettings> options,
            ILogger<MarketplaceSearchServices> logger)
        {
            _scrapers = scrapers.ToDictionary(
                scraper => scraper.Marketplace,
                StringComparer.OrdinalIgnoreCase);

            _supportedMarketplaces = _scrapers.Keys.ToList();
            _cache = cache;
            _settings = options.Value;
            _logger = logger;
        }

        public IReadOnlyCollection<string> SupportedMarketplaces => _supportedMarketplaces;

        public async Task<MarketplaceSearchResult> SearchAsync(
            MarketplaceSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            var keyword = ValidateAndNormalize(query);
            var marketplace = query.Marketplace.Trim().ToLowerInvariant();

            if (!_scrapers.TryGetValue(marketplace, out var scraper))
            {
                throw new ValidationExceptions(
                    $"Marketplace '{marketplace}' tidak didukung. Pilihan: {string.Join(", ", _supportedMarketplaces)}.");
            }

            var cacheKey = BuildCacheKey(
                marketplace, keyword, query.Page, query.Limit, query.CategoryId);
            var staleKey = cacheKey + ":stale";

            // Ambil kandidat lebih banyak dari limit yang diminta, supaya
            // setelah aksesoris dibuang masih cukup produk untuk diisi.
            var candidateLimit = CalculateCandidateLimit(query.Limit);

            var cacheTtl = TimeSpan.FromMinutes(Math.Max(1, _settings.CacheDurationMinutes));
            var staleTtl = TimeSpan.FromMinutes(
                Math.Max(_settings.CacheDurationMinutes, _settings.StaleDurationMinutes));

            if (!query.Refresh &&
                _cache.TryGetValue(cacheKey, out MarketplaceSearchResult? cachedResult) &&
                cachedResult != null)
            {
                _logger.LogInformation(
                    "Marketplace cache hit. Marketplace={Marketplace} Keyword={Keyword} Page={Page}",
                    marketplace, keyword, query.Page);

                return Clone(cachedResult, cached: true, stale: false, cacheTtl);
            }

            var searchLock = SearchLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await searchLock.WaitAsync(cancellationToken);

            try
            {
                // Cek ulang setelah mendapat lock: mungkin request lain baru saja
                // menyelesaikan scraping untuk keyword yang sama.
                if (!query.Refresh &&
                    _cache.TryGetValue(cacheKey, out cachedResult) &&
                    cachedResult != null)
                {
                    return Clone(cachedResult, cached: true, stale: false, cacheTtl);
                }

                MarketplaceScrapeResult scraped;

                try
                {
                    scraped = await scraper.SearchAsync(
                        keyword, query.Page, candidateLimit, query.CategoryId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Gagal scraping marketplace. Marketplace={Marketplace} Keyword={Keyword} Page={Page}",
                        marketplace, keyword, query.Page);

                    // Fallback: tampilkan data terakhir yang masih tersimpan
                    // daripada mengembalikan error ke frontend.
                    if (_cache.TryGetValue(staleKey, out MarketplaceSearchResult? staleResult) &&
                        staleResult != null)
                    {
                        _logger.LogWarning(
                            "Mengembalikan data stale. Marketplace={Marketplace} Keyword={Keyword} Page={Page}",
                            marketplace, keyword, query.Page);

                        return Clone(staleResult, cached: true, stale: true, TimeSpan.Zero);
                    }

                    if (ex is AppExceptions)
                    {
                        throw;
                    }

                    throw new AppExceptions(
                        "Gagal mengambil data dari marketplace. Silakan coba lagi.", 502);
                }

                var products = MarketplaceRelevance.FilterAndRank(
                    scraped.Products, keyword, query.Limit);

                var result = new MarketplaceSearchResult
                {
                    Keyword = keyword,
                    Marketplace = marketplace,
                    Page = query.Page,
                    Limit = query.Limit,
                    Total = scraped.Total,
                    CategoryId = scraped.CategoryId,
                    CategoryName = scraped.CategoryName,
                    HasMore = scraped.Total > 0
                        ? (long)query.Page * candidateLimit < scraped.Total
                        : products.Count >= query.Limit,
                    FetchedAt = DateTimeOffset.UtcNow,
                    Cached = false,
                    Stale = false,
                    CacheTtlSeconds = (int)cacheTtl.TotalSeconds,
                    Products = products
                };

                // Hasil kosong tetap di-cache sebentar supaya keyword aneh
                // tidak membanjiri marketplace dengan request berulang.
                var freshTtl = result.Products.Count == 0
                    ? TimeSpan.FromSeconds(Math.Max(5, _settings.NegativeCacheDurationSeconds))
                    : cacheTtl;

                result.CacheTtlSeconds = (int)freshTtl.TotalSeconds;

                _cache.Set(cacheKey, result, freshTtl);
                _cache.Set(staleKey, result, staleTtl);

                _logger.LogInformation(
                    "Marketplace scraping sukses. Marketplace={Marketplace} Keyword={Keyword} Page={Page} Candidates={Candidates} Count={Count} Total={Total}",
                    marketplace, keyword, query.Page, scraped.Products.Count, result.Products.Count, result.Total);

                return result;
            }
            finally
            {
                searchLock.Release();

                // Hapus lock yang sudah tidak dipakai agar dictionary tidak tumbuh.
                if (searchLock.CurrentCount == 1)
                {
                    SearchLocks.TryRemove(
                        new KeyValuePair<string, SemaphoreSlim>(cacheKey, searchLock));
                }
            }
        }

        private string ValidateAndNormalize(MarketplaceSearchQuery query)
        {
            if (query == null)
            {
                throw new ValidationExceptions("Body/query pencarian tidak valid.");
            }

            if (string.IsNullOrWhiteSpace(query.Keyword))
            {
                throw new ValidationExceptions("Parameter pencarian 'q' wajib diisi.");
            }

            var keyword = Regex.Replace(query.Keyword.Trim(), @"\s+", " ");

            if (keyword.Length < MinKeywordLength)
            {
                throw new ValidationExceptions(
                    $"Kata kunci minimal {MinKeywordLength} karakter.");
            }

            if (keyword.Length > MaxKeywordLength)
            {
                throw new ValidationExceptions(
                    $"Kata kunci maksimal {MaxKeywordLength} karakter.");
            }

            if (query.Page < 1)
            {
                throw new ValidationExceptions("Parameter 'page' minimal 1.");
            }

            var maxPageSize = Math.Max(1, _settings.MaxPageSize);
            if (query.Limit < 1 || query.Limit > maxPageSize)
            {
                throw new ValidationExceptions(
                    $"Parameter 'limit' harus di antara 1 dan {maxPageSize}.");
            }

            if (string.IsNullOrWhiteSpace(query.Marketplace))
            {
                query.Marketplace = "tokopedia";
            }

            return keyword;
        }

        private int CalculateCandidateLimit(int requestedLimit)
        {
            var multiplier = Math.Max(1, _settings.CandidateMultiplier);
            var minCandidates = Math.Max(requestedLimit, _settings.MinCandidateProducts);
            var maxCandidates = Math.Max(requestedLimit, _settings.MaxCandidateProducts);

            var target = (long)requestedLimit * multiplier;
            target = Math.Clamp(target, minCandidates, maxCandidates);

            return (int)Math.Min(int.MaxValue, target);
        }

        private static string BuildCacheKey(
            string marketplace,
            string keyword,
            int page,
            int limit,
            int? categoryId)
        {
            // Keyword di-lowercase hanya untuk key cache, bukan untuk request
            // ke marketplace, supaya "iPhone" dan "iphone" berbagi cache.
            var normalizedKeyword = keyword.ToLowerInvariant();
            var normalizedCategory = categoryId?.ToString() ?? "auto";
            return $"marketplace-search:{marketplace}:p{page}:l{limit}:c{normalizedCategory}:{normalizedKeyword}";
        }

        private static MarketplaceSearchResult Clone(
            MarketplaceSearchResult source,
            bool cached,
            bool stale,
            TimeSpan ttl)
        {
            return new MarketplaceSearchResult
            {
                Keyword = source.Keyword,
                Marketplace = source.Marketplace,
                Page = source.Page,
                Limit = source.Limit,
                Total = source.Total,
                CategoryId = source.CategoryId,
                CategoryName = source.CategoryName,
                HasMore = source.HasMore,
                FetchedAt = source.FetchedAt,
                Cached = cached,
                Stale = stale,
                CacheTtlSeconds = (int)ttl.TotalSeconds,
                Products = new List<MarketplaceProduct>(source.Products)
            };
        }
    }
}

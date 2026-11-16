using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WebApiProject.Exceptions;
using WebApiProject.Interfaces;
using WebApiProject.Models;

namespace WebApiProject.Services.Marketplace
{
    /// <summary>
    /// Scraper pencarian produk Tokopedia memakai endpoint GraphQL publik
    /// yang dipakai website tokopedia.com saat melakukan search.
    /// </summary>
    public class TokopediaScraper : IMarketplaceScraper
    {
        public const string MarketplaceName = "tokopedia";

        // Tokopedia menerima GraphQL di root domain gql.tokopedia.com (/),
        // bukan di /graphql/... — path tersebut akan menggantung/timeout.
        private const string SearchPath = "/";

        private const int HandphoneCategoryId = 24;   // Tokopedia: Handphone
        private const int TabletCategoryId = 276;     // Tokopedia: Tablet
        private const int LaptopCategoryId = 3844;    // Tokopedia: Laptop
        private const int WearableCategoryId = 3081;  // Tokopedia: Wearable Devices

        private static readonly Regex WearableCategoryPattern = new(
            @"\b(garmin|forerunner|fenix|venu|instinct|apple\s*watch|smartwatch|smart\s*watch|wearable|fitbit|amazfit|mi\s*band|galaxy\s*watch|huawei\s*watch|suunto|polar)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PhoneCategoryPattern = new(
            @"\b(iphone|samsung|galaxy|xiaomi|redmi|poco|oppo|vivo|realme|infinix|tecno|itel|oneplus|pixel|huawei|honor|nokia|xperia|smartphone|handphone|ponsel)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TabletCategoryPattern = new(
            @"\b(ipad|tablet)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TvCategoryPattern = new(
            @"\b(tv|televisi|smart\s*tv)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LaptopCategoryPattern = new(
            @"\b(laptop|notebook|macbook|ultrabook|chromebook|thinkpad|ideapad|vivobook|zenbook|legion|zephyrus)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Query komponen/spec (SSD, RAM, monitor, dsb.) jangan dipaksa
        // masuk kategori device seperti Handphone/Laptop.
        private static readonly Regex ComponentPattern = new(
            @"\b(ssd|hdd|nvme|ram|memory|processor|cpu|gpu|vga|motherboard|psu|monitor|printer|keyboard|mouse|router|modem|hard\s*disk|hardisk)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string SearchQuery = """
            query SearchProductQueryV4($params: String!) {
              ace_search_product_v4(params: $params) {
                header {
                  totalData
                  totalDataText
                  responseCode
                  errorMessage
                  keywordProcess
                }
                data {
                  products {
                    id
                    name
                    price
                    priceRange
                    originalPrice
                    discountPercentage
                    imageUrl
                    url
                    rating
                    countReview
                    categoryName
                    categoryBreadcrumb
                    labelGroups {
                      title
                      type
                      position
                    }
                    badges {
                      title
                      show
                    }
                    shop {
                      id
                      name
                      url
                      city
                      isOfficial
                      isPowerBadge
                    }
                  }
                }
              }
            }
            """;

        private readonly HttpClient _httpClient;
        private readonly ILogger<TokopediaScraper> _logger;
        private readonly MarketplaceSettings _settings;

        public string Marketplace => MarketplaceName;

        public TokopediaScraper(
            HttpClient httpClient,
            IOptions<MarketplaceSettings> options,
            ILogger<TokopediaScraper> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<MarketplaceScrapeResult> SearchAsync(
            string keyword,
            int page,
            int limit,
            int? categoryId = null,
            CancellationToken cancellationToken = default)
        {
            var offset = ((long)page - 1) * limit;
            categoryId = ResolveCategoryId(keyword, categoryId);

            // Parameter yang sama dengan yang dikirim website Tokopedia saat search.
            var queryParameters = new List<string>
            {
                "device=desktop",
                "scheme=https",
                "source=search",
                "st=product",
                "ob=23", // paling relevan
                "safe_search=false",
                $"q={Uri.EscapeDataString(keyword)}",
                $"rows={limit}",
                $"start={offset}"
            };

            // sc = filter kategori. Tanpa ini, "iphone 17" didominasi casing,
            // tempered glass, dan aksesoris lain karena relevansi Tokopedia.
            if (categoryId.HasValue)
            {
                queryParameters.Add($"sc={categoryId.Value}");
            }

            var parameters = string.Join("&", queryParameters);

            var payload = new[]
            {
                new
                {
                    operationName = "SearchProductQueryV4",
                    variables = new { @params = parameters },
                    query = SearchQuery
                }
            };

            const int maxAttempts = 2;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var response = await _httpClient.PostAsJsonAsync(
                        SearchPath, payload, cancellationToken);
                    var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var status = (int)response.StatusCode;

                        // Retry sekali untuk error server yang bersifat sementara.
                        if (status >= 500 && attempt < maxAttempts)
                        {
                            _logger.LogWarning(
                                "Tokopedia HTTP {Status}, retry attempt {Attempt}. Keyword={Keyword}",
                                status, attempt + 1, keyword);

                            await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken);
                            continue;
                        }

                        _logger.LogWarning(
                            "Tokopedia search gagal. Status={Status} Keyword={Keyword} Body={Body}",
                            status, keyword, Truncate(raw, 300));

                        throw new AppExceptions(
                            $"Marketplace Tokopedia menolak permintaan (HTTP {status}).", 502);
                    }

                    var result = ParseResponse(raw, keyword);
                    return ApplyCategory(result, categoryId);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (AppExceptions)
                {
                    throw;
                }
                catch (Exception ex) when (
                    attempt < maxAttempts &&
                    ex is HttpRequestException or TaskCanceledException or IOException)
                {
                    _logger.LogWarning(ex,
                        "Koneksi ke Tokopedia gagal, retry attempt {Attempt}. Keyword={Keyword}",
                        attempt + 1, keyword);

                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken);
                }
            }

            throw new AppExceptions("Gagal menghubungi marketplace Tokopedia.", 502);
        }

        private int? ResolveCategoryId(string keyword, int? requestedCategoryId)
        {
            // category=0 dari frontend berarti: jangan pakai filter kategori.
            if (requestedCategoryId.HasValue)
            {
                return requestedCategoryId.Value > 0 ? requestedCategoryId.Value : null;
            }

            if (!_settings.AutoDetectDeviceCategory ||
                MarketplaceRelevance.IsAccessoryQuery(keyword) ||
                ComponentPattern.IsMatch(keyword))
            {
                return null;
            }

            // "Samsung TV" jangan difilter ke kategori Handphone.
            if (TvCategoryPattern.IsMatch(keyword))
            {
                return null;
            }

            if (WearableCategoryPattern.IsMatch(keyword))
            {
                return WearableCategoryId;
            }

            if (TabletCategoryPattern.IsMatch(keyword))
            {
                return TabletCategoryId;
            }

            if (PhoneCategoryPattern.IsMatch(keyword))
            {
                return HandphoneCategoryId;
            }

            if (LaptopCategoryPattern.IsMatch(keyword))
            {
                return LaptopCategoryId;
            }

            return null;
        }

        private static MarketplaceScrapeResult ApplyCategory(
            MarketplaceScrapeResult result,
            int? categoryId)
        {
            result.CategoryId = categoryId;
            result.CategoryName = categoryId switch
            {
                HandphoneCategoryId => "Handphone",
                TabletCategoryId => "Tablet",
                LaptopCategoryId => "Laptop",
                WearableCategoryId => "Wearable Devices",
                _ => null
            };

            return result;
        }

        private MarketplaceScrapeResult ParseResponse(string raw, string keyword)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(raw);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Respons Tokopedia bukan JSON valid. Keyword={Keyword}", keyword);
                throw new AppExceptions("Respons marketplace tidak dapat dibaca.", 502);
            }

            using (document)
            {
                var root = document.RootElement;
                var node = root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0
                    ? root[0]
                    : root;

                if (node.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array &&
                    errors.GetArrayLength() > 0)
                {
                    var messages = errors.EnumerateArray()
                        .Select(error => GetString(error, "message"))
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Select(message => message!)
                        .ToList();

                    _logger.LogWarning("Tokopedia GraphQL error. Keyword={Keyword} Errors={Errors}",
                        keyword, string.Join(" | ", messages));

                    throw new AppExceptions("Marketplace Tokopedia mengembalikan error.", 502, messages);
                }

                if (!node.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("ace_search_product_v4", out var search))
                {
                    throw new AppExceptions("Struktur respons marketplace tidak dikenali.", 502);
                }

                var header = search.TryGetProperty("header", out var headerElement)
                    ? headerElement
                    : default;

                var responseCode = GetInt(header, "responseCode") ?? 0;
                if (responseCode != 0)
                {
                    var message = GetString(header, "errorMessage") ?? "Unknown error";
                    throw new AppExceptions($"Marketplace Tokopedia error: {message}", 502);
                }

                var total = GetInt(header, "totalData") ?? 0;
                var products = new List<MarketplaceProduct>();

                if (search.TryGetProperty("data", out var inner) &&
                    inner.TryGetProperty("products", out var productsElement) &&
                    productsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in productsElement.EnumerateArray())
                    {
                        var product = MapProduct(item);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                    }
                }

                return new MarketplaceScrapeResult
                {
                    Total = total,
                    Products = products
                };
            }
        }

        private static MarketplaceProduct? MapProduct(JsonElement item)
        {
            var id = GetString(item, "id");
            var name = GetString(item, "name");

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var priceText = GetString(item, "price");
            var originalPriceText = GetString(item, "originalPrice");

            var labels = new List<string>();
            if (item.TryGetProperty("labelGroups", out var labelGroups) &&
                labelGroups.ValueKind == JsonValueKind.Array)
            {
                foreach (var label in labelGroups.EnumerateArray())
                {
                    var title = GetString(label, "title");
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        labels.Add(title!);
                    }
                }
            }

            MarketplaceShop? shop = null;
            if (item.TryGetProperty("shop", out var shopElement) &&
                shopElement.ValueKind == JsonValueKind.Object)
            {
                shop = new MarketplaceShop
                {
                    Id = GetString(shopElement, "id"),
                    Name = GetString(shopElement, "name"),
                    Url = NormalizeUrl(GetString(shopElement, "url")),
                    City = GetString(shopElement, "city"),
                    IsOfficial = GetBool(shopElement, "isOfficial"),
                    IsPowerBadge = GetBool(shopElement, "isPowerBadge")
                };
            }

            return new MarketplaceProduct
            {
                Id = id!,
                Name = name!,
                Url = NormalizeUrl(GetString(item, "url")),
                ImageUrl = GetString(item, "imageUrl"),
                Price = ParseRupiah(priceText),
                PriceText = string.IsNullOrWhiteSpace(priceText) ? null : priceText,
                OriginalPrice = ParseRupiah(originalPriceText),
                OriginalPriceText = string.IsNullOrWhiteSpace(originalPriceText) ? null : originalPriceText,
                DiscountPercentage = GetInt(item, "discountPercentage"),
                Rating = GetDouble(item, "rating"),
                ReviewCount = GetInt(item, "countReview"),
                Category = GetString(item, "categoryName"),
                CategoryBreadcrumb = GetString(item, "categoryBreadcrumb"),
                Marketplace = MarketplaceName,
                Shop = shop,
                Labels = labels
            };
        }

        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (url.StartsWith("//"))
            {
                return "https:" + url;
            }

            return "https://www.tokopedia.com" + (url.StartsWith('/') ? url : "/" + url);
        }

        /// <summary>Mengubah "Rp1.234.567" menjadi 1234567.</summary>
        private static decimal? ParseRupiah(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return decimal.TryParse(digits, out var result) ? result : null;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                _ => null
            };
        }

        private static int? GetInt(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            return value.ValueKind == JsonValueKind.String &&
                   int.TryParse(value.GetString(), out var parsed)
                ? parsed
                : null;
        }

        private static double? GetDouble(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            return value.ValueKind == JsonValueKind.String &&
                   double.TryParse(value.GetString(), out var parsed)
                ? parsed
                : null;
        }

        private static bool? GetBool(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }
    }
}

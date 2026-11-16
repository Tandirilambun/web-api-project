namespace WebApiProject.Models
{
    public class MarketplaceSettings
    {
        public int CacheDurationMinutes { get; set; } = 15;
        public int StaleDurationMinutes { get; set; } = 180;
        public int NegativeCacheDurationSeconds { get; set; } = 30;
        public int DefaultPageSize { get; set; } = 20;
        public int MaxPageSize { get; set; } = 60;
        public int HttpTimeoutSeconds { get; set; } = 20;
        public bool AutoDetectDeviceCategory { get; set; } = true;
        public int CandidateMultiplier { get; set; } = 3;
        public int MinCandidateProducts { get; set; } = 30;
        public int MaxCandidateProducts { get; set; } = 60;
        public string UserAgent { get; set; } =
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    }

    public class MarketplaceSearchQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string Marketplace { get; set; } = "tokopedia";
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
        public bool Refresh { get; set; } = false;
        /// <summary>
        /// ID kategori marketplace (Tokopedia: sc). 24 = Handphone, 3844 = Laptop.
        /// Isi 0 untuk mematikan auto-detect kategori.
        /// </summary>
        public int? CategoryId { get; set; }
    }

    public class MarketplaceProduct
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? PriceText { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? OriginalPriceText { get; set; }
        public int? DiscountPercentage { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public string? Category { get; set; }
        public string? CategoryBreadcrumb { get; set; }
        public string Marketplace { get; set; } = string.Empty;
        public MarketplaceShop? Shop { get; set; }
        public List<string> Labels { get; set; } = new();
    }

    public class MarketplaceShop
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? City { get; set; }
        public bool? IsOfficial { get; set; }
        public bool? IsPowerBadge { get; set; }
    }

    public class MarketplaceSearchResult
    {
        public string Keyword { get; set; } = string.Empty;
        public string Marketplace { get; set; } = string.Empty;
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public bool HasMore { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
        public bool Cached { get; set; }
        public bool Stale { get; set; }
        public int CacheTtlSeconds { get; set; }
        public List<MarketplaceProduct> Products { get; set; } = new();
    }

    public class MarketplaceScrapeResult
    {
        public int Total { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<MarketplaceProduct> Products { get; set; } = new();
    }
}

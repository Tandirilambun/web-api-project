using System.Text.RegularExpressions;
using WebApiProject.Models;

namespace WebApiProject.Services.Marketplace
{
    /// <summary>
    /// Lapisan relevance di atas hasil pencarian marketplace.
    ///
    /// Marketplace (Tokopedia) meranking berdasarkan keyword, sehingga query
    /// seperti "garmin forerunner 165" bisa didominasi strap, charger, dan
    /// tempered glass. Di sini hasil mentah difilter dari aksesoris (kecuali
    /// user memang mencari aksesoris) lalu diurutkan berdasarkan kedekatan
    /// judul produk dengan kata kunci.
    /// </summary>
    public static class MarketplaceRelevance
    {
        private static readonly Regex TokenPattern = new(
            "[a-z0-9]+",
            RegexOptions.Compiled);

        private static readonly Regex AccessoryPattern = new(
            @"\b(case|casing|cover|tempered\s*glass|anti\s*gores|kabel|cable|charger|adaptor|adapter|power\s*bank|powerbank|lens\s*protector|camera\s*lens\s*protector|pelindung|stiker|skin|holder|strap|watch\s*band|magsafe|bumper|pouch|sleeve|screen\s*guard|screen\s*protector|hydrogel|earphone|headset|tws|sarung|aksesoris|accessories|spare\s*part|mount|tripod|gimbal|tali|kaos\s*kaki|socks|insole|shoelace|tali\s*sepatu|cleaning|pembersih|perawatan|refill|spray|hanger|gantungan)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "for", "with", "dan", "untuk", "the", "and", "of", "di", "ke",
            "yang", "ori", "original", "new", "baru", "resmi", "garansi",
            "promo", "murah", "termurah", "terbaru"
        };

        /// <summary>Apakah user memang sedang mencari aksesoris?</summary>
        public static bool IsAccessoryQuery(string keyword)
        {
            return AccessoryPattern.IsMatch(keyword);
        }

        /// <summary>
        /// Buang aksesoris yang tidak diminta, lalu ambil produk paling relevan.
        /// </summary>
        public static List<MarketplaceProduct> FilterAndRank(
            IEnumerable<MarketplaceProduct> products,
            string keyword,
            int limit)
        {
            var queryTokens = Tokenize(keyword);
            if (queryTokens.Count == 0)
            {
                return products.Take(limit).ToList();
            }

            var queryWantsAccessory = AccessoryPattern.IsMatch(keyword);
            var phrase = Normalize(keyword);
            var scored = new List<(MarketplaceProduct Product, int Score, int Order)>();

            var order = 0;
            foreach (var product in products)
            {
                order++;

                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    continue;
                }

                var title = Normalize(product.Name);

                // Kalau user tidak mencari aksesoris, buang produk aksesoris.
                if (!queryWantsAccessory && AccessoryPattern.IsMatch(title))
                {
                    continue;
                }

                scored.Add((product, Score(title, phrase, queryTokens), order));
            }

            return scored
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Order)
                .Take(limit)
                .Select(item => item.Product)
                .ToList();
        }

        private static int Score(
            string title,
            string phrase,
            IReadOnlyList<string> queryTokens)
        {
            var score = 0;

            // Exact phrase adalah sinyal paling kuat.
            if (title.Contains(phrase, StringComparison.Ordinal))
            {
                score += 100;
            }

            var matched = queryTokens.Count(token =>
                title.Contains(token, StringComparison.Ordinal));

            score += matched * 15;
            score += (int)Math.Round(20.0 * matched / queryTokens.Count);

            if (matched == queryTokens.Count)
            {
                score += 40;
            }

            // Token angka/model (165, 97, 256gb, dsb.) wajib ada.
            foreach (var token in queryTokens.Where(token => token.Any(char.IsDigit)))
            {
                if (!title.Contains(token, StringComparison.Ordinal))
                {
                    score -= 100;
                }
            }

            // Judul yang jauh lebih panjang dari query biasanya keyword stuffing.
            score -= Math.Max(0, title.Length - phrase.Length - 40) / 10;

            return score;
        }

        private static List<string> Tokenize(string value)
        {
            return TokenPattern.Matches(value.ToLowerInvariant())
                .Select(match => match.Value)
                .Where(token => token.Length > 1 && !StopWords.Contains(token))
                .Distinct()
                .ToList();
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
        }
    }
}

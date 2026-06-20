using System.Text;
using System.Text.RegularExpressions;

namespace apiContact.Utilities
{
    public static class SlugHelper
    {
        /// <summary>
        /// Converts a display name into a URL-safe slug.
        /// "Engineering Team Chat" → "engineering-team-chat"
        /// </summary>
        public static string Generate(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Normalize Unicode (removes accents)
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var slug = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();

            // Replace spaces and underscores with hyphens
            slug = Regex.Replace(slug, @"[\s_]+", "-");

            // Remove anything that isn't a letter, digit, or hyphen
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

            // Collapse multiple hyphens
            slug = Regex.Replace(slug, @"-{2,}", "-");

            return slug.Trim('-');
        }

        /// <summary>
        /// Ensures the slug is unique among a set of existing slugs.
        /// If "engineering" is taken it returns "engineering-2", then "engineering-3", etc.
        /// </summary>
        public static string Uniquify(string slug, IEnumerable<string> existing)
        {
            if (!existing.Contains(slug)) return slug;

            int counter = 2;
            string candidate;
            do { candidate = $"{slug}-{counter++}"; }
            while (existing.Contains(candidate));
            return candidate;
        }
    }
}

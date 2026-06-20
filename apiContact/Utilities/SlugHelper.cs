using System.Text;
using System.Text.RegularExpressions;

namespace apiContact.Utilities
{
    /// <summary>
    /// Converts display names into URL-safe slugs and guarantees uniqueness within a set.
    /// </summary>
    public static class SlugHelper
    {
        private const int DefaultMaxLength = 80;

        // ── Core generation ───────────────────────────────────────────────────

        /// <summary>
        /// Converts a display name into a URL-safe slug.
        /// "Engineering Team Chat" → "engineering-team-chat"
        /// Handles Unicode, special characters, and consecutive hyphens.
        /// Returns an empty string for null / whitespace input.
        /// </summary>
        public static string Generate(string? input, int maxLength = DefaultMaxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // 1. Unicode normalization — strips combining accent marks
            var normalized = input.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // 2. Replace whitespace, underscores, dots, and forward-slashes with hyphens
            slug = Regex.Replace(slug, @"[\s_.\/]+", "-");

            // 3. Remove anything that is not a letter, digit, or hyphen
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

            // 4. Collapse runs of hyphens into one
            slug = Regex.Replace(slug, @"-{2,}", "-");

            // 5. Strip leading / trailing hyphens
            slug = slug.Trim('-');

            // 6. Enforce max length (cut at a hyphen boundary when possible)
            return Truncate(slug, maxLength);
        }

        /// <summary>
        /// Generates a slug for a Direct-Message (DM) room from two user identifiers.
        /// The result is order-independent so the same pair always produces the same slug.
        /// Example: ("alice", "bob") → "dm-alice-bob"
        /// </summary>
        public static string GenerateDm(string userA, string userB)
        {
            var a = Generate(userA);
            var b = Generate(userB);

            // Canonical order — alphabetical so it's commutative
            var pair = string.Compare(a, b, StringComparison.Ordinal) <= 0
                ? $"{a}-{b}"
                : $"{b}-{a}";

            return Truncate($"dm-{pair}", DefaultMaxLength);
        }

        // ── Uniqueness ────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures the slug is unique within <paramref name="existing"/>.
        /// If "engineering" is taken it returns "engineering-2", then "engineering-3", etc.
        /// </summary>
        public static string Uniquify(string slug, IEnumerable<string> existing)
        {
            if (string.IsNullOrEmpty(slug)) return Guid.NewGuid().ToString("N")[..8];

            var set = existing as ISet<string> ?? new HashSet<string>(existing, StringComparer.Ordinal);
            if (!set.Contains(slug)) return slug;

            int counter = 2;
            string candidate;
            do { candidate = $"{slug}-{counter++}"; }
            while (set.Contains(candidate));

            return candidate;
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="slug"/> is a valid slug
        /// (only lowercase letters, digits, and hyphens; no leading/trailing hyphens;
        ///  length between 1 and <paramref name="maxLength"/>).
        /// </summary>
        public static bool IsValid(string? slug, int maxLength = DefaultMaxLength)
        {
            if (string.IsNullOrWhiteSpace(slug))         return false;
            if (slug.Length > maxLength)                  return false;
            if (slug.StartsWith('-') || slug.EndsWith('-')) return false;
            return Regex.IsMatch(slug, @"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Truncates <paramref name="slug"/> to at most <paramref name="maxLength"/> characters,
        /// preferring to cut at a hyphen boundary to avoid broken words.
        /// </summary>
        public static string Truncate(string slug, int maxLength = DefaultMaxLength)
        {
            if (slug.Length <= maxLength) return slug;

            var truncated = slug[..maxLength];
            var lastHyphen = truncated.LastIndexOf('-');

            // Only snap to hyphen boundary if it leaves a reasonable length
            return lastHyphen > maxLength / 2
                ? truncated[..lastHyphen].TrimEnd('-')
                : truncated.TrimEnd('-');
        }
    }
}

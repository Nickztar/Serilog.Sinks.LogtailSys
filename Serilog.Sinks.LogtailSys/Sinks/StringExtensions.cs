using System;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.Logtail
{
    public static class StringExtensions
    {
        private static readonly Regex printableAsciiRegex = new("[^\\u0021-\\u007E]", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Truncates a string so that it is no longer than the specified number of characters.
        /// If the truncated string ends with a space, it will be removed
        /// </summary>
        /// <param name="source">String to be truncated</param>
        /// <param name="maxLength">Maximum string length before truncation will occur</param>
        /// <returns>Original string, or a truncated to the specified length if too long</returns>
        public static string WithMaxLength(this string source, int maxLength)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return source.Length > maxLength
                ? source[..maxLength].TrimEnd()
                : source;
        }

        public static string AsPrintableAscii(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return printableAsciiRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove any surrounding quotes, and unescape all others
        /// </summary>
        /// <param name="source">String to be processed</param>
        /// <returns>The string, with surrounding quotes removed and all others unescapes</returns>
        public static string TrimAndUnescapeQuotes(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return source
                .Trim('"')
                .Replace(@"\""", @"""");
        }

        public static int ToInt(this string source)
            => Convert.ToInt32(source);
    }
}

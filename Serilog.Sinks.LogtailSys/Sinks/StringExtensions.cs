using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.Logtail
{
    public static partial class StringExtensions
    {
#if !NET7_0
        private static readonly Regex printableAsciiRegex = new(@"[^\u0021-\u007E]", RegexOptions.Compiled | RegexOptions.Singleline);
#endif

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
            #if NETSTANDARD2_0
            return source.Length > maxLength
                ? source.Substring(0, maxLength).TrimEnd()
                : source;
            #else
            return source.Length > maxLength
                ? source[..maxLength].TrimEnd()
                : source;
            #endif
        }

        public static string AsPrintableAscii(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
#if NET7_0
            return PrintableAsciiRx().Replace(source, string.Empty);
#else
            return printableAsciiRegex.Replace(source, string.Empty);
#endif
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

#if NET7_0
        [GeneratedRegex(@"[^\u0021-\u007E]", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex PrintableAsciiRx();
#endif
    }
}

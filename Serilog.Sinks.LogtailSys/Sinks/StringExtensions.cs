using System;

namespace Serilog.Sinks.Logtail;

public static class StringExtensions
{
    /// <summary>
    /// 
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
}
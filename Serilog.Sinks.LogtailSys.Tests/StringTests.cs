using System.Text.RegularExpressions;
using FluentAssertions;
using Serilog.Sinks.Logtail;

namespace Serilog.Sinks.LogtailSys.Tests;

public partial class StringTests
{
    [Theory]
    [InlineData("hello world", '"', "hello world")]
    [InlineData("\"hello world", '"', "hello world")]
    [InlineData("hello world\"", '"', "hello world")]
    [InlineData("\"hello world\"", '"', "hello world")]
    [InlineData("\"hell\"o\" world\"", '"', "hell\"o\" world")]
    [InlineData("\"hell\\\"o\\\" world\"", '"', "hell\"o\" world")]
    [InlineData("", '"', "")]
    [InlineData("\"", '"', "")]
    [InlineData("\"\"", '"', "")]
    public void CanTrimString(string source, char trimChar, string expected)
    {
        var cleaned = new StringCleaner(source)
            .WithTrimed(trimChar)
            .WithUnescapeQuotes()
            .Build();
        cleaned.Should().Be(expected);
        var previousCleaned = source.TrimAndUnescapeQuotes(); 
        cleaned.Should().Be(previousCleaned, because: "Should match previous");
    }
    
    [Theory]
    [InlineData("Hello world", "Hello world", '"', '\\', ']')]
    [InlineData("[]Hello world", @"[\]Hello world", '"', '\\', ']')]
    [InlineData("[]H\"e\"llo world", "[\\]H\\\"e\\\"llo world", '"', '\\', ']')]
    public void CanEscapeString(string source, string expected, params char[] charsToReplace)
    {
        var cleaned = new StringCleaner(source)
            .WithEscapedChars(charsToReplace)
            .Build();
        cleaned.Should().Be(expected);
        var previousCleaned = PropertyValueRx().Replace(source, match => $@"\{match}");
        cleaned.Should().Be(previousCleaned, because: "Should match previous");
    }
    
    [Fact]
    public void CanGetMaxLength()
    {
        for (var i = 0; i < 100; i++)
        {
            var str = new string('*', i) + " ";
            var strCopy = new string('*', i) + " ";
            var cleaned = new StringCleaner(str)
                .WithMaxLength(32)
                .Build();
            var previous = strCopy.WithMaxLength(32);

            var expectedLen = str.Length switch
            {
                <= 32 => str.Length,
                _ => 32
            };
            cleaned.Should().Be(previous, because: "Should match previous");
            cleaned.Should().HaveLength(expectedLen);
        }
    }
    
    [Theory]
    [InlineData("", "")]
    [InlineData("Hej!", "Hej!")]
    [InlineData("Hej!^", "Hej!^")]
    [InlineData("Hej!^~", "Hej!^~")]
    [InlineData("Hej!^~-", "Hej!^~-")]
    [InlineData("Hej!^@", "Hej!^@")]
    [InlineData("Hej 💦", "Hej")]
    [InlineData("Hej ", "Hej")]
    public void AsPrintableAscii(string source, string expected)
    {
        var cleaned = new StringCleaner(source)
            .WithAsciiPrintable()
            .Build();
        var previousClean = source.AsPrintableAscii();
        
        cleaned.Should().Be(expected);   
        cleaned.Should().Be(previousClean, because: "Should match previous");   
    }
    
    [GeneratedRegex("[\\]\\\\\"]")]
    private static partial Regex PropertyValueRx();
}

public static partial class StringExtensions
{
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
        return PrintableAsciiRx().Replace(source, string.Empty);
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

    [GeneratedRegex(@"[^\u0021-\u007E]", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex PrintableAsciiRx();
    
    
    [GeneratedRegex("[=\\\"\\]]")]
    private static partial Regex PropertyKeyRx();
}
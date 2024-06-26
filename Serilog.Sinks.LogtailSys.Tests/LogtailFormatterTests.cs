using System.Text.RegularExpressions;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Logtail;
using Xunit.Abstractions;

namespace Serilog.Sinks.LogtailSys.Tests;
public class LogtailFormatterTests
    {
        private const string NILVALUE = "-";
        private const string LogtailSD = "[Logtail=\"SOURCE_TOKEN\"]";
        const string APP_NAME = "TestApp";
        const string SOURCE_CONTEXT = "TestCtx";
        private static readonly string Host = Environment.MachineName.WithMaxLength(255);

        private readonly ITestOutputHelper output;
        private readonly LogtailFormatter formatter = new(
           "Logtail", "SOURCE_TOKEN", "Parameters", Facility.User, APP_NAME);
        private readonly DateTimeOffset timestamp;
        private readonly Regex regex;

        public LogtailFormatterTests(ITestOutputHelper output)
        {
            this.output = output;

            // Prepare a regex object that can be used to check the output format
            // NOTE: The regex is in a text file instead of as a variable - it's a but large, and all the escaping required to
            // have it as a variable just makes it hard to grok
            this.regex = new Regex(File.ReadAllText("LogtailRegex.txt"), RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            // Timestamp used in tests
            var instant = new DateTime(2013, 12, 19, 4, 1, 2, 357) + TimeSpan.FromTicks(8523);
            this.timestamp = new DateTimeOffset(instant);
        }

        [Fact]
        public void Should_format_message_without_structured_data()
        {
            var template = new MessageTemplateParser().Parse("This is a test message");
            var infoEvent = new LogEvent(this.timestamp, LogEventLevel.Information, null, template, Enumerable.Empty<LogEventProperty>());

            var formatted = this.formatter.FormatMessage(infoEvent);
            this.output.WriteLine($"RFC5424 without structured data: {formatted}");

            var match = this.regex.Match(formatted);
            match.Success.Should().BeTrue();

            match.Groups["pri"].Value.Should().Be("<14>");
            match.Groups["ver"].Value.Should().Be("1");
            match.Groups["timestamp"].Value.Should().Be($"2013-12-19T04:01:02.357852{this.timestamp:zzz}");
            match.Groups["app"].Value.Should().Be(APP_NAME);
            match.Groups["host"].Value.Should().Be(Host);
            match.Groups["proc"].Value.ToInt().Should().BeGreaterThan(0);
            match.Groups["msgid"].Value.Should().Be(NILVALUE);
            match.Groups["ltail"].Value.Should().Be(LogtailSD); // Since logtail is present, this is fine.
            match.Groups["sd"].Value.Should().Be(""); // Since logtail is present, this is fine.
            match.Groups["msg"].Value.Should().Be("This is a test message");

            this.output.WriteLine($"FORMATTED: {formatted}");
        }

        [Fact]
        public void Should_format_message_with_structured_data()
        {
            const string testVal = "A Value";

            var properties = new List<LogEventProperty> {
                new LogEventProperty("AProperty", new ScalarValue(testVal)),
                new LogEventProperty("AnotherProperty", new ScalarValue("AnotherValue")),
                new LogEventProperty("SourceContext", new ScalarValue(SOURCE_CONTEXT))
            };

            var template = new MessageTemplateParser().Parse("This is a test message with val {AProperty}");
            var ex = new ArgumentException("Test");
            var warnEvent = new LogEvent(this.timestamp, LogEventLevel.Warning, ex, template, properties);

            var formatted = this.formatter.FormatMessage(warnEvent);
            this.output.WriteLine($"RFC5424 with structured data: {formatted}");

            var match = this.regex.Match(formatted);
            match.Success.Should().BeTrue();

            match.Groups["pri"].Value.Should().Be("<12>");
            match.Groups["ver"].Value.Should().Be("1");
            match.Groups["timestamp"].Value.Should().Be($"2013-12-19T04:01:02.357852{this.timestamp:zzz}");
            match.Groups["app"].Value.Should().Be(APP_NAME);
            match.Groups["host"].Value.Should().Be(Host);
            match.Groups["proc"].Value.ToInt().Should().BeGreaterThan(0);
            match.Groups["msgid"].Value.Should().Be(SOURCE_CONTEXT);
            match.Groups["sd"].Value.Should().NotBe(NILVALUE);
            match.Groups["ltail"].Value.Should().Be(LogtailSD); // Since logtail is present, this is fine.
            match.Groups["msg"].Value.Should().Be($"This is a test message with val \"{testVal}\"");
        }

        [Fact]
        public void Should_choose_another_msgId_provider()
        {
            const string testProperty = "AProperty";
            const string testVal = "AValue";
            const string msgIdPropertyName = testProperty;
            var customFormatter = new LogtailFormatter("Logtail", "SOURCE_TOKEN", "Parameters", Facility.User, APP_NAME, null, msgIdPropertyName);

            var properties = new List<LogEventProperty>
            {
                new LogEventProperty(testProperty, new ScalarValue(testVal)),
                new LogEventProperty("AnotherProperty", new ScalarValue("AnotherValue")),
                new LogEventProperty("SourceContext", new ScalarValue(SOURCE_CONTEXT))
            };

            var template = new MessageTemplateParser().Parse("This is a test message with val {AProperty}");
            var ex = new ArgumentException("Test");
            var warnEvent = new LogEvent(this.timestamp, LogEventLevel.Warning, ex, template, properties);

            var formatted = customFormatter.FormatMessage(warnEvent);
            this.output.WriteLine($"RFC5424 with structured data: {formatted}");

            var match = this.regex.Match(formatted);
            match.Success.Should().BeTrue();

            match.Groups["pri"].Value.Should().Be("<12>");
            match.Groups["ver"].Value.Should().Be("1");
            match.Groups["timestamp"].Value.Should().Be($"2013-12-19T04:01:02.357852{this.timestamp:zzz}");
            match.Groups["app"].Value.Should().Be(APP_NAME);
            match.Groups["host"].Value.Should().Be(Host);
            match.Groups["proc"].Value.ToInt().Should().BeGreaterThan(0);
            match.Groups["msgid"].Value.Should().Be(testVal);
            match.Groups["sd"].Value.Should().NotBe(NILVALUE);
            match.Groups["ltail"].Value.Should().Be(LogtailSD); // Since logtail is present, this is fine.
            match.Groups["msg"].Value.Should().Be($"This is a test message with val \"{testVal}\"");
        }

        /// <summary>
        /// RFC5424 rules:
        /// - Property names must be >= 1 and &lt;= 32 characters and may only contain printable ASCII
        ///   characters as defined by PRINTUSASCII
        ///
        /// - Property values must escape the characters '"', '\' and ']' with a backslash '\'
        ///
        /// - MSGID (source context) must be >= 1 and &lt;= 32 characters and may only contain printable ASCII
        ///   characters as defined by PRINTUSASCII
        /// </summary>
        [Fact]
        public void Should_clean_invalid_strings()
        {
            var properties = new List<LogEventProperty> {
                new LogEventProperty("安森Test", new ScalarValue(@"test")),
                new LogEventProperty("APropertyNameThatIsLongerThan32Characters", new ScalarValue(@"A value \contain]ing ""quotes"" to test")),
                new LogEventProperty("SourceContext", new ScalarValue("安森 A string that is longer than 32 characters"))
            };

            var template = new MessageTemplateParser().Parse("This is a test message");
            var infoEvent = new LogEvent(this.timestamp, LogEventLevel.Information, null, template, properties);

            var formatted = this.formatter.FormatMessage(infoEvent);
            this.output.WriteLine($"RFC5424: {formatted}");

            var match = this.regex.Match(formatted);
            match.Success.Should().BeTrue();

            match.Groups["msgid"].Value.Length.Should().Be(32);

            // Spaces and anything other than printable ASCII should have been removed
            match.Groups["msgid"].Value.Should().StartWith("Astringthatis");
            match.Groups["ltail"].Value.Should().Be(LogtailSD); // Since logtail is present, this is fine.

            // '"', '\' and ']' in property values should have been escaped with a backslash '\'
            Regex.IsMatch(match.Groups["sd"].Value, @"\\\\contain\\]ing\s\\""quotes\\""").Should().BeTrue();

            // Property names have had spaces and anything other than printable ASCII should have been removed,
            // and should have been truncated to 32 chars
            Regex.IsMatch(match.Groups["sd"].Value, @"APropertyNameThatIsLongerThan32C="".*""\s").Should().BeTrue();
        }

        [Fact]
        public void Should_override_log_host_name()
        {
            var template = new MessageTemplateParser().Parse("This is a test message");
            var infoEvent = new LogEvent(this.timestamp, LogEventLevel.Information, null, template, Enumerable.Empty<LogEventProperty>());

            const string hostname = "NewHostName";
            var localFormatter = new LogtailFormatter("Logtail", "SOURCE_TOKEN", "Parameters", Facility.User, APP_NAME, null, "SourceContext", hostname);
            var formatted = localFormatter.FormatMessage(infoEvent);

            var match = this.regex.Match(formatted);
            match.Success.Should().BeTrue();

            match.Groups["host"].Value.Should().Be(hostname);
        }
    }

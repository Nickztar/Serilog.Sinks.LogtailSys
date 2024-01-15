using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Logtail
{
    /// <inheritdoc />
    /// <summary>
    /// Formats messages that comply with syslog RFC5424 & Logtail
    /// https://tools.ietf.org/html/rfc5424
    /// </summary>
    public partial class LogtailFormatter : LogtailFormatterBase
    {
        /// <summary>
        /// Used in place of data that cannot be obtained or is unavailable
        /// </summary>
        private const string NILVALUE = "-";

        private const string DATE_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffzzz";

        private readonly string applicationName;
        private readonly string messageIdPropertyName;
        private readonly string tokenKey;
        private readonly string token;
        private readonly string dataName;

        internal const string DefaultMessageIdPropertyName = "SourceContext";

        /// <summary>
        /// Initialize a new instance of <see cref="LogtailFormatter"/> class allowing you to specify values for
        /// the facility, application name, template formatter, and message Id property name.
        /// </summary>
        /// <param name="facility"><inheritdoc cref="Facility" path="/summary"/></param>
        /// <param name="applicationName">A user supplied value representing the application name that will appear in the syslog event. Must be all printable ASCII characters. Max length 48. Defaults to the current process name.</param>
        /// <param name="templateFormatter"><inheritdoc cref="LogtailFormatterBase.templateFormatter" path="/summary"/></param>
        /// <param name="messageIdPropertyName">Where the Id number of the message will be derived from. Defaults to the "SourceContext" property of the syslog event. Property name and value must be all printable ASCII characters with max length of 32.</param>
        /// <param name="sourceHost"><inheritdoc cref="LogtailFormatterBase.Host" path="/summary"/></param>
        /// <param name="severityMapping"><inheritdoc cref="LogtailFormatterBase" path="/param[@name='severityMapping']"/></param>
        /// <param name="tokenKey">The key of Logtail token, something like logtail@11111 source_token</param>
        /// <param name="token">Your source token from rsys logtail</param>
        /// <param name="dataName">A name for the structured data, defaults to "Parameters"</param>
        public LogtailFormatter(
            string tokenKey,
            string token,
            string dataName,
            Facility facility = Facility.Local0, 
            string? applicationName = null,
            ITextFormatter? templateFormatter = null,
            string? messageIdPropertyName = DefaultMessageIdPropertyName,
            string? sourceHost = null,
            Func<LogEventLevel, Severity>? severityMapping = null)
            : base(facility, templateFormatter, sourceHost, severityMapping)
        {
            this.applicationName = applicationName ?? ProcessName;

            // Conform to the RFC
            this.applicationName = this.applicationName
                .AsPrintableAscii()
                .WithMaxLength(48);

            // Conform to the RFC
            this.messageIdPropertyName = (messageIdPropertyName ?? DefaultMessageIdPropertyName)
                .AsPrintableAscii()
                .WithMaxLength(32);

             this.tokenKey = tokenKey;
             this.token = token;
             this.dataName = dataName;
        }

        public override string FormatMessage(LogEvent logEvent)
        {
            var priority = CalculatePriority(logEvent.Level);
            var messageId = GetMessageId(logEvent);

            var timestamp = logEvent.Timestamp.ToString(DATE_FORMAT);
            var sd = RenderStructuredData(logEvent);
            var msg = RenderMessage(logEvent);

            return $"<{priority}>1 {timestamp} {Host} {applicationName} {ProcessId} {messageId} {sd} {msg}";
        }

        /// <summary>
        /// Get the LogEvent's SourceContext in a format suitable for use as the MSGID field of a syslog message
        /// </summary>
        /// <param name="logEvent">The LogEvent to extract the context from</param>
        /// <returns>The processed SourceContext, or NILVALUE '-' if not set</returns>
        private string GetMessageId(LogEvent logEvent)
        {
            var hasMsgId = logEvent.Properties.TryGetValue(messageIdPropertyName, out var propertyValue);

            if (!hasMsgId)
                return NILVALUE;

            var result = propertyValue?
                .ToString()
                .TrimAndUnescapeQuotes();

            // Conform to the RFC's restrictions
            result = result?
                .AsPrintableAscii()
                .WithMaxLength(32);

            return result is { Length: >= 1 }
                ? result
                : NILVALUE;
        }

        private string RenderStructuredData(LogEvent logEvent)
        {
            var tokenPart = $"{tokenKey}=\"{token}\"";
            var structuredDataKvps = string.Join(" ", logEvent.Properties.Select(t => $"""
                {RenderPropertyKey(t.Key)}="{RenderPropertyValue(t.Value)}"
                """));
            var structuredData = string.IsNullOrEmpty(structuredDataKvps) ? $"[{tokenPart}]" : $"[{tokenPart}][{dataName} {structuredDataKvps}]";

            return structuredData;
        }

        private static string RenderPropertyKey(string propertyKey)
        {
            // Conform to the RFC's restrictions
            var result = propertyKey.AsPrintableAscii();

            // Also remove any '=', ']', and '"", as these are also not permitted in structured data parameter names
            // Unescaped regex pattern: [=\"\]]
            
#if NET7_0
            result = PropertyKeyRx().Replace(result, string.Empty);
#else
            result = Regex.Replace(result, "[=\\\"\\]]", string.Empty);
#endif

            return result.WithMaxLength(32);
        }

        /// <summary>
        /// All Serilog property values are quoted, which is unnecessary, as we are going to encase them in
        /// quotes anyway, to conform to the specification for syslog structured data values - so this
        /// removes them and also unescapes any others
        /// </summary>
        private static string RenderPropertyValue(LogEventPropertyValue propertyValue)
        {
            // Trim surrounding quotes, and unescape all others
            var result = propertyValue
                .ToString()
                .TrimAndUnescapeQuotes();

            // Use a backslash to escape backslashes, double quotes and closing square brackets
#if NET7_0
            return PropertyValueRx().Replace(result, match => $@"\{match}");
#else
            return Regex.Replace(result, @"[\]\\""]", match => $@"\{match}");
#endif
        }
        
#if NET7_0
        [GeneratedRegex("[\\]\\\\\"]")]
        private static partial Regex PropertyValueRx();
        [GeneratedRegex("[=\\\"\\]]")]
        private static partial Regex PropertyKeyRx();
#endif
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.Logtail;

namespace Serilog
{
    /// <summary>
    /// Extends Serilog configuration to write events to a logtail
    /// </summary>
    public static class LogtailLoggerConfigurationExtensions
    {
        private static readonly BatchingOptions DefaultBatchOptions = new BatchingOptions
        {
            BatchSizeLimit = 1000,
            BufferingTimeLimit = TimeSpan.FromSeconds(2),
            QueueLimit = 100_000,
            EagerlyEmitFirstEvent = true
        };

        /// <summary>
        /// Adds a sink that writes log events to a UDP syslog server
        /// </summary>
        /// <param name="loggerSinkConfig">The logger configuration</param>
        /// <param name="host">Hostname of the syslog server</param>
        /// <param name="tokenKey">The key of Logtail token, leave empty if not overriding</param>
        /// <param name="token">Your source token from (rsyslog) logtail</param>
        /// <param name="port">Port the syslog server is listening on</param>
        /// <param name="dataName">A name for the structured data, defaults to "Parameters"</param>
        /// <param name="appName">The name of the application. Must be all printable ASCII characters. Max length 32. Defaults to the current process name</param>
        /// <param name="facility"><inheritdoc cref="Facility" path="/summary"/> Defaults to <see cref="Facility.Local0"/>.</param>
        /// <param name="batchConfig">Batching configuration</param>
        /// <param name="outputTemplate">A message template describing the output messages</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="messageIdPropertyName">Where the Id number of the message will be derived from. Defaults to the "SourceContext" property of the syslog event. Property name and value must be all printable ASCII characters with max length of 32.</param>
        /// <param name="sourceHost"><inheritdoc cref="LogtailFormatterBase.Host" path="/summary"/></param>
        /// <param name="severityMapping">Provide your own method to override the default mapping logic of a Serilog <see cref="LogEventLevel"/></param>
        /// <param name="formatter">The message formatter</param>
        /// <see cref="!:https://github.com/serilog/serilog/wiki/Formatting-Output"/>
        public static LoggerConfiguration Logtail(
            this LoggerSinkConfiguration loggerSinkConfig,
            string token,
            string tokenKey = "logtail@11993 source_token",
            string host = "in.logs.betterstack.com", 
            int port = 6517, 
            string dataName = "Parameters", 
            string? appName = null,
            Facility facility = Facility.Local0, 
            BatchingOptions? batchConfig = null, 
            string? outputTemplate = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? levelSwitch = null,
            string messageIdPropertyName = LogtailFormatter.DefaultMessageIdPropertyName,
            string? sourceHost = null,
            Func<LogEventLevel, Severity>? severityMapping = null, 
            ITextFormatter? formatter = null)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException(nameof(host));

            batchConfig ??= DefaultBatchOptions;
            var messageFormatter = GetFormatter(tokenKey, token, dataName, appName, facility, outputTemplate, messageIdPropertyName, sourceHost, severityMapping, formatter);
            var endpoint = ResolveIP(host, port);

            var logtailSink = new LogtailSink(endpoint, messageFormatter);
            return loggerSinkConfig.Sink(logtailSink, batchConfig, restrictedToMinimumLevel, levelSwitch);
        }


        /// <summary>An alternative mapping function that can be specified in any of the 'severityMapping' parameters.
        /// This mapping takes a numerical approach, as opposed to a name approach when converting a Serilog
        /// <see cref="LogEventLevel"/> to a syslog <see cref="Severity"/>. However, since syslog has more possible
        /// values, <see cref="Severity.Critical"/> and <see cref="Severity.Alert"/> are skipped and the mapping
        /// ends with <see cref="LogEventLevel.Fatal"/> being assigned to <see cref="Severity.Emergency"/>.</summary>
        /// <param name="logEventLevel">A Serilog <see cref="LogEventLevel"/>.</param>
        /// <returns>A syslog <see cref="Severity"/>.</returns>
        public static Severity ValueBasedLogLevelToSeverityMap(LogEventLevel logEventLevel)
            => logEventLevel switch
            {
                LogEventLevel.Verbose     => Severity.Debug,
                LogEventLevel.Debug       => Severity.Informational,
                LogEventLevel.Information => Severity.Notice,
                LogEventLevel.Warning     => Severity.Warning,
                LogEventLevel.Error       => Severity.Error,
                LogEventLevel.Fatal       => Severity.Emergency,
                _ => throw new ArgumentOutOfRangeException(nameof(logEventLevel), $"The value {logEventLevel} is not a valid LogEventLevel.")
            };

        private static ILogtailFormatter GetFormatter(
            string tokenKey,
            string token,
            string dataName,
            string? appName, 
            Facility facility,
            string? outputTemplate,
            string? messageIdPropertyName,
            string? sourceHost,
            Func<LogEventLevel, Severity>? severityMapping = null, 
            ITextFormatter? formatter = null)
        {
            ITextFormatter? templateFormatter;

            if (formatter == null)
            {
                templateFormatter = string.IsNullOrWhiteSpace(outputTemplate)
                    ? null
                    : new MessageTemplateTextFormatter(outputTemplate!);
            }
            else
            {
                templateFormatter = formatter;
            }

            return new LogtailFormatter(
                tokenKey,
                token,
                dataName,
                facility,
                appName,
                templateFormatter,
                messageIdPropertyName,
                sourceHost,
                severityMapping
            );
        }

        private static IPEndPoint ResolveIP(string host, int port)
        {
            var addr = Dns.GetHostAddresses(host)
                .First(x => x.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
            
            return new IPEndPoint(addr, port);
        }
    }
}

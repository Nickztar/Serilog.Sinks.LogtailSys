using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Logtail
{
    /// <summary>
    /// Sink that writes events to a remote syslog service using UDP
    /// </summary>
    public class LogtailSink : IBatchedLogEventSink, IDisposable
    {
        private readonly ILogtailFormatter formatter;
        private readonly UdpClient client;
        private readonly IPEndPoint endpoint;

        public LogtailSink(IPEndPoint endpoint, ILogtailFormatter formatter)
        {
            this.formatter = formatter;
            this.endpoint = endpoint;
            client = new UdpClient(endpoint.AddressFamily);
        }

        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to send to the syslog service</param>
        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events)
            {
                var message = formatter.FormatMessage(logEvent);
                var data = Encoding.UTF8.GetBytes(message);

                try
                {
                    await client.SendAsync(data, data.Length, endpoint).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    SelfLog.WriteLine($"[{nameof(LogtailSink)}] error while sending log event to syslog {endpoint.Address}:{endpoint.Port} - {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public Task OnEmptyBatchAsync()
            => Task.CompletedTask;

        public void Dispose()
        {
            client.Close();
            client.Dispose();
        }
    }
}

using Serilog.Events;

namespace Serilog.Sinks.Logtail
{
    public interface ILogtailFormatter
    {
        string FormatMessage(LogEvent logEvent);
        int CalculatePriority(LogEventLevel level);
    }
}

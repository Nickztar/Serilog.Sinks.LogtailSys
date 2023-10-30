// See https://aka.ms/new-console-template for more information


using Serilog;
using Serilog.Debugging;

var logConfig = new LoggerConfiguration()
    .WriteTo.Console();
const string outputTemplate = "UDP: {Message}";
var log = logConfig
    .MinimumLevel.Debug()
    .WriteTo.Logtail(
        token: "$SOURCE_TOKEN",
        appName: "ExampleLogtailProject",
        outputTemplate: outputTemplate
    )
    .Enrich.FromLogContext()
    .CreateLogger();
    
SelfLog.Enable(Console.Error);

var name = "logtail";
log.Information("Hello World! {Name}", name);
//Not required.
await log.DisposeAsync();
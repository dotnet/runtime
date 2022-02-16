using Microsoft.DotNet.XHarness.CLI.Commands.Wasm;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "[HH:mm:ss] ";
        })
        .AddFilter(null, LogLevel.Trace);
});
var logger = loggerFactory.CreateLogger("symbolicator");
logger.LogInformation("hello");

var sym = new Symbolicator(args[0], args[1], true, logger);

foreach (var line in File.ReadAllLines(args[2]))
{
    var newLine = sym.Symbolicate(line);
    Console.WriteLine (newLine);
}

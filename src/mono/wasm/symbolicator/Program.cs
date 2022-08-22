// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Internal;

#pragma warning disable CA1852 // Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible

if (args.Length < 1)
{
    ShowUsage();
    return 1;
}

string symbolsFile = args[0];
if (!File.Exists(symbolsFile))
{
    Console.WriteLine($"error: cannot find symbols file {symbolsFile}");
    return 1;
}

string patternsFile = Path.Combine(AppContext.BaseDirectory, "wasm-symbol-patterns.txt");
if (!File.Exists(patternsFile))
{
    Console.WriteLine($"Internal error: cannot find patterns file {patternsFile}");
    return 1;
}

if (args.Length < 2)
{
    ShowUsage();
    return 1;
}

StreamReader tracesReader;
if (args[1] == "-")
{
    tracesReader = new StreamReader(Console.OpenStandardInput());
}
else
{
    string tracesFile = args[1];
    if (!File.Exists(tracesFile))
    {
        Console.WriteLine($"error: cannot find the traces file {tracesFile}");
        return 1;
    }
    tracesReader = new StreamReader(File.OpenRead(tracesFile));
}

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "[HH:mm:ss] ";
        })
        .AddFilter(null, LogLevel.Trace);
});
ILogger logger = loggerFactory.CreateLogger("symbolicator");

var sym = new WasmSymbolicator(symbolsFile, patternsFile, throwOnMissing: true, logger);
while (true)
{
    string? line = await tracesReader.ReadLineAsync().ConfigureAwait(false);
    if (line is null)
        break;

    string newLine = sym.Symbolicate(line);
    Console.WriteLine(newLine);
}

return 0;

static void ShowUsage() => Console.WriteLine($"Usage: symbolicator <path/to/dotnet.js.symbols> [</path/to/patterns-file>] [-|<file-with-traces>]");

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

class Program
{
    /// <summary>
    /// Tests that using Logging.Console Json formatter options coming from the Configuration works correctly with trimming
    /// </summary>
    static async Task<int> Main()
    {
        FakeConsoleWriter consoleWriter = new FakeConsoleWriter();
        Console.SetOut(consoleWriter);

        IServiceCollection services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new[]
                {
                    new KeyValuePair<string, string>("Logging:Console:FormatterName", "json"),
                    new KeyValuePair<string, string>("Logging:Console:FormatterOptions:TimestampFormat", "dd/MM/yy"),
                    new KeyValuePair<string, string>("Logging:Console:FormatterOptions:JsonWriterOptions:Indented", "true"),
                })
            .Build();

        services.AddLogging(logging =>
        {
            logging.AddConfiguration(config.GetSection("Logging"));
            logging.AddConsole();
        });

        ServiceProvider provider = services.BuildServiceProvider();
        ILogger logger = provider.GetRequiredService<ILogger<Program>>();

        logger.LogError("Hello");

        try
        {
            await consoleWriter.FinishedWriting.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // timing out means that FakeConsoleWriter isn't overriding the right 'Write' method
            return -99;
        }

        string consoleOutput = consoleWriter.GetOutput();
        
        // ensure the output contains whitespace between the property and the value
        if (!consoleOutput.Contains("""
            "Message": "Hello",
            """))
        {
            return -1;
        }

        // ensure the output contains new lines since JsonWriterOptions.Indented is true
        if (!consoleOutput.Contains('\n'))
        {
            return -2;
        }

        // ensure the output contains the right Timestamp format
        int timestampIndex = consoleOutput.IndexOf("\"Timestamp\": \"");
        if (timestampIndex == -1)
        {
            return -3;
        }

        if (!IsTimestampFormat(consoleOutput.AsSpan(timestampIndex + 14, 8)))
        {
            return -4;
        }

        return 100;
    }

    private static bool IsTimestampFormat(ReadOnlySpan<char> timestamp) =>
        timestamp.Length == 8
        && char.IsDigit(timestamp[0])
        && char.IsDigit(timestamp[1])
        && timestamp[2] == '/'
        && char.IsDigit(timestamp[3])
        && char.IsDigit(timestamp[4])
        && timestamp[5] == '/'
        && char.IsDigit(timestamp[6])
        && char.IsDigit(timestamp[7]);

    private sealed class FakeConsoleWriter : TextWriter
    {
        private readonly StringBuilder _sb;
        private readonly TaskCompletionSource _onFinishedWriting;

        public FakeConsoleWriter()
        {
            _sb = new StringBuilder();
            _onFinishedWriting = new TaskCompletionSource();
        }

        public override Encoding Encoding => Encoding.Unicode;

        public Task FinishedWriting => _onFinishedWriting.Task;

        public override void Write(char[] buffer, int index, int count)
        {
            _sb.Append(buffer, index, count);
            _onFinishedWriting.SetResult();
        }

        public string GetOutput() => _sb.ToString();
    }
}

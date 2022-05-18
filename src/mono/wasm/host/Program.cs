// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Mono.Options;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.WebAssembly.AppHost;

public class WasmAppHost
{
    internal delegate Task<int> HostHandler(CommonConfiguration commonArgs,
                                            ILoggerFactory loggerFactory,
                                            ILogger logger,
                                            CancellationToken token);

    private static readonly Dictionary<WasmHost, HostHandler> s_hostHandlers = new();

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"WasmAppHost {string.Join(' ', args)}");
        RegisterHostHandler(WasmHost.Browser, BrowserHost.InvokeAsync);
        RegisterHostHandler(WasmHost.V8, JSEngineHost.InvokeAsync);
        RegisterHostHandler(WasmHost.NodeJS, JSEngineHost.InvokeAsync);

        using CancellationTokenSource cts = new();
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[HH:mm:ss] ";
            })
            .AddFilter("DevToolsProxy", LogLevel.Information)
            .AddFilter("FirefoxMonoProxy", LogLevel.Information)
            .AddFilter("host", LogLevel.Trace)
            .AddFilter(null, LogLevel.Warning));

        ILogger logger = loggerFactory.CreateLogger("host");
        try
        {
            CommonConfiguration commonConfig = CommonConfiguration.FromCommandLineArguments(args);
            return !s_hostHandlers.TryGetValue(commonConfig.Host, out HostHandler? handler)
                ? throw new CommandLineException($"Cannot find any handler for host type {commonConfig.Host}")
                : await handler(commonConfig, loggerFactory, logger, cts.Token);
        }
        catch (CommandLineException cle)
        {
            Console.WriteLine($"Error: {cle.Message}");
            return -1;
        }
        catch (OptionException oe)
        {
            Console.WriteLine($"Error: {oe.Message}");
            return -1;
        }
    }

    private static void RegisterHostHandler(WasmHost host, HostHandler handler)
        => s_hostHandlers[host] = handler;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public static class DebugProxyHost
{
    public static Task RunDebugProxyAsync(ProxyOptions options, string[] args, ILoggerFactory loggerFactory, CancellationToken token)
    {
        return Task.WhenAny(
            RunFirefoxServerLoopAsync(options, args, loggerFactory, token),
            RunDevToolsProxyAsync(options, args, loggerFactory, token)
        )
        .ContinueWith(t => Console.WriteLine($"Debug proxy server failed with {t.Exception}"),
                            token,
                            TaskContinuationOptions.OnlyOnFaulted,
                            TaskScheduler.Default);
    }

    public static Task RunFirefoxServerLoopAsync(ProxyOptions options, string[] args, ILoggerFactory loggerFactory, CancellationToken token)
        => FirefoxDebuggerProxy.RunServerLoopAsync(browserPort: options.FirefoxDebugPort,
                                                   proxyPort: options.FirefoxProxyPort,
                                                   loggerFactory,
                                                   loggerFactory.CreateLogger("FirefoxMonoProxy"),
                                                   token,
                                                   autoSetBreakpointOnEntryPoint: options.AutoSetBreakpointOnEntryPoint);

    public static async Task RunDevToolsProxyAsync(ProxyOptions options, string[] args, ILoggerFactory loggerFactory, CancellationToken token)
    {
        string proxyUrl = $"http://127.0.0.1:{options.DevToolsProxyPort}";
        IWebHost host = new WebHostBuilder()
            .UseSetting("UseIISIntegration", false.ToString())
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>()
            .ConfigureServices(services =>
            {
                services.AddSingleton(loggerFactory);
                services.AddLogging(configure => configure.AddSimpleConsole().AddFilter(null, LogLevel.Information));
                services.AddSingleton(Options.Create(options));
                services.AddRouting();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddCommandLine(args);
            })
            .UseUrls(proxyUrl)
            .Build();

        token.Register(async () => { Console.WriteLine($"-- token got cancelled, stopping host"); await host.StopAsync(); });
        await host.RunAsync(token);
    }
}

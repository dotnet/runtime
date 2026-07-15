// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WebAssembly.AppHost;

namespace Microsoft.WebAssembly.AppHost.DevServer;

/// <summary>
/// Launches the shared Blazor Gateway (<c>Microsoft.AspNetCore.Components.Gateway</c>) as a subprocess to serve
/// the application's static web assets. This unifies the browser dev server with the Blazor Gateway
/// (https://github.com/dotnet/runtime/issues/122144).
///
/// The Gateway serves static web assets (including the SPA fallback when the app is built with
/// StaticWebAssetSpaFallbackEnabled) from the generated endpoints manifest. Runtime/testing specific behaviors
/// that the in-process dev server used to provide - browser console forwarding (<c>/console</c>), cross-origin
/// isolation headers (COOP/COEP) and the <c>DEVSERVER_UPLOAD_PATH</c> upload endpoint - are not yet provided by
/// the Gateway and are expected to be added upstream.
/// </summary>
internal static class GatewayServer
{
    internal static async Task<(ServerURLs, IHost)> StartAsync(DevServerOptions options, ILogger logger, CancellationToken token)
    {
        TaskCompletionSource<ServerURLs> realUrlsAvailableTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        IHost host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(logger);
                services.AddSingleton(Options.Create(options));
                services.AddSingleton(realUrlsAvailableTcs);
                services.AddHostedService<GatewayProcessService>();
            })
            .Build();

        await host.StartAsync(token);

        ServerURLs serverUrls = await realUrlsAvailableTcs.Task;
        return (serverUrls, host);
    }

    internal static string LocateGatewayDll()
    {
        // The Gateway payload is copied next to WasmAppHost under a BlazorGateway subfolder (see WasmAppHost.csproj).
        string baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDirectory, "BlazorGateway", "blazor-gateway.dll"),
            Path.Combine(baseDirectory, "blazor-gateway.dll"),
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "Cannot start the Blazor Gateway because 'blazor-gateway.dll' was not found. Ensure the " +
            "Microsoft.AspNetCore.Components.Gateway package is referenced by WasmAppHost.csproj and its payload " +
            $"is copied to the output directory. Searched: {string.Join(", ", candidates)}");
    }
}

internal sealed class GatewayProcessService : IHostedService, IAsyncDisposable
{
    private static readonly Regex NowListeningRegex = new(@"^\s*Now listening on: (?<url>.*)$", RegexOptions.Compiled, TimeSpan.FromSeconds(10));
    private static readonly Regex ApplicationStartedRegex = new(@"^\s*Application started\.", RegexOptions.Compiled, TimeSpan.FromSeconds(10));
    private static readonly string[] MessageSuppressionPrefixes =
    {
        "Now listening on:",
        "Application started.",
        "Hosting environment:",
        "Content root path:",
    };

    private readonly DevServerOptions _options;
    private readonly ILogger _logger;
    private readonly TaskCompletionSource<ServerURLs> _realUrlsAvailableTcs;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly List<string> _listeningUrls = new();
    private Process? _process;

    public GatewayProcessService(
        IOptions<DevServerOptions> options,
        ILogger logger,
        TaskCompletionSource<ServerURLs> realUrlsAvailableTcs,
        IHostApplicationLifetime lifetime)
    {
        _options = options.Value;
        _logger = logger;
        _realUrlsAvailableTcs = realUrlsAvailableTcs;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string gatewayDll = GatewayServer.LocateGatewayDll();

        var processStartInfo = new ProcessStartInfo
        {
            // "dotnet" is resolved via PATH on all platforms (Windows CreateProcess appends the .exe extension).
            FileName = "dotnet",
            Arguments = BuildArguments(gatewayDll, _options),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = Process.Start(processStartInfo);
        if (_process is null)
        {
            _realUrlsAvailableTcs.TrySetException(new InvalidOperationException("Unable to start the Blazor Gateway process."));
            return Task.CompletedTask;
        }

        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
    }

    private static string BuildArguments(string gatewayDll, DevServerOptions options)
    {
        var arguments = new List<string>
        {
            "exec",
            Quote(gatewayDll),
            "--environment", "Development",
            // The dev server is a local development/testing host, so the Gateway's production oriented behaviors
            // (telemetry export, health checks and HTTP->HTTPS redirection) are turned off. HTTPS redirection in
            // particular must stay off because the app is commonly served and tested over the plain HTTP url.
            "--Gateway:Telemetry:Enabled=false",
            "--Gateway:HealthChecks:Enabled=false",
            "--Gateway:HttpsRedirection:Enabled=false",
        };

        if (!string.IsNullOrEmpty(options.StaticWebAssetsPath))
        {
            arguments.Add("--staticWebAssets");
            arguments.Add(Quote(options.StaticWebAssetsPath));
        }

        if (!string.IsNullOrEmpty(options.StaticWebAssetsEndpointsPath))
        {
            arguments.Add("--ClientApps:app:EndpointsManifest");
            arguments.Add(Quote(options.StaticWebAssetsEndpointsPath));
            arguments.Add("--ClientApps:app:PathPrefix");
            arguments.Add("\"\"");
        }

        if (options.Urls is { Length: > 0 })
        {
            arguments.Add("--urls");
            arguments.Add(Quote(string.Join(';', options.Urls)));
        }

        return string.Join(' ', arguments);
    }

    private static string Quote(string value) => $"\"{value}\"";

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        string? line = eventArgs.Data;
        if (line is null)
            return;

        Match listeningMatch = NowListeningRegex.Match(line);
        if (listeningMatch.Success)
        {
            lock (_listeningUrls)
            {
                _listeningUrls.Add(listeningMatch.Groups["url"].Value.Trim());
            }
        }
        else if (ApplicationStartedRegex.IsMatch(line))
        {
            ResolveServerUrls();
        }

        // Suppress the gateway's own status messages so they are not confused with the application's output.
        foreach (string prefix in MessageSuppressionPrefixes)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return;
        }

        Console.WriteLine(line);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
            _logger.LogError("[BlazorGateway] {Message}", eventArgs.Data);
    }

    private void ResolveServerUrls()
    {
        string? http;
        string? https;
        lock (_listeningUrls)
        {
            http = GetServerAddress(_listeningUrls, secure: false);
            https = GetServerAddress(_listeningUrls, secure: true);
        }

        if (http is null)
            _realUrlsAvailableTcs.TrySetException(new InvalidOperationException("Failed to determine the Blazor Gateway's HTTP address or port."));
        else
            _realUrlsAvailableTcs.TrySetResult(new ServerURLs(http, https));
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _realUrlsAvailableTcs.TrySetException(new InvalidOperationException("The Blazor Gateway process exited before it started listening."));
        _lifetime.StopApplication();
    }

    private static string? GetServerAddress(IEnumerable<string> addresses, bool secure) => addresses
        .Where(a => a.StartsWith(secure ? "https:" : "http:", StringComparison.InvariantCultureIgnoreCase))
        .Select(a => new Uri(a).ToString())
        .FirstOrDefault();

    public async ValueTask DisposeAsync()
    {
        Process? process = _process;
        _process = null;
        if (process is null)
            return;

        process.Exited -= OnProcessExited;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to stop the Blazor Gateway process: {Message}", ex.Message);
        }
        finally
        {
            process.Dispose();
        }
    }
}

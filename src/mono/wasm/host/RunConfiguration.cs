// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.WebAssembly.Diagnostics;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class RunConfiguration
{
    public WasmHost Host { get; init; }
    public WasmHostProperties HostProperties { get; init; }
    public HostConfig HostConfig { get; init; }
    public string AppPath { get; init; }

    public RunConfiguration(string runtimeConfigPath, string? hostArg)
    {
        if (string.IsNullOrEmpty(runtimeConfigPath) || !File.Exists(runtimeConfigPath))
            throw new Exception($"Cannot find runtime config at {runtimeConfigPath}");

        AppPath = Path.GetDirectoryName(runtimeConfigPath) ?? ".";

        RuntimeConfig? rconfig = JsonSerializer.Deserialize<RuntimeConfig>(
                                                File.ReadAllText(runtimeConfigPath),
                                                CommonConfiguration.JsonOptions);
        if (rconfig == null)
            throw new Exception($"Failed to deserialize {runtimeConfigPath}");

        if (rconfig.RuntimeOptions == null)
            throw new Exception($"Failed to deserialize {runtimeConfigPath} - rconfig.RuntimeOptions");

        HostProperties = rconfig.RuntimeOptions.WasmHostProperties;
        if (HostProperties == null)
            throw new Exception($"Could not find any {nameof(RuntimeOptions.WasmHostProperties)} in {runtimeConfigPath}");

        if (HostProperties.HostConfigs is null || HostProperties.HostConfigs.Count == 0)
            throw new Exception($"no perHostConfigs found");

        // read only if it wasn't overridden by command line option
        string desiredConfig = hostArg ?? HostProperties.DefaultConfig;
        HostConfig? foundConfig = HostProperties.HostConfigs
                                    .Where(hc => string.Equals(hc.Name, desiredConfig, StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();

        HostConfig = foundConfig ?? HostProperties.HostConfigs.First();
        if (HostConfig == null)
            throw new Exception("no host config found");

        // FIXME: validate hostconfig
        if (!Enum.TryParse(HostConfig.HostString, ignoreCase: true, out WasmHost wasmHost))
            throw new Exception($"Unknown host {HostConfig.HostString} in config named {HostConfig.Name}");
        Host = wasmHost;
    }

    public ProxyOptions ToProxyOptions()
    {
        ProxyOptions options = new();
        if (HostProperties.ChromeProxyPort is not null)
            options.DevToolsProxyPort = HostProperties.ChromeProxyPort.Value;
        if (HostProperties.ChromeDebuggingPort is not null)
            options.DevToolsDebugPort = HostProperties.ChromeDebuggingPort.Value;
        if (HostProperties.FirefoxProxyPort is not null)
            options.FirefoxProxyPort = HostProperties.FirefoxProxyPort.Value;
        if (HostProperties.FirefoxDebuggingPort is not null)
            options.FirefoxDebugPort = HostProperties.FirefoxDebuggingPort.Value;
        options.LogPath = ".";
        return options;
    }
}

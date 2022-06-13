// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Mono.Options;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.WebAssembly.Diagnostics;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class CommonConfiguration
{
    public bool Debugging { get; set; }
    public string AppPath { get; init; }
    public string[] RuntimeArguments => HostProperties.RuntimeArguments ?? Array.Empty<string>();
    public IEnumerable<string> RemainingArgs { get; init; }
    public WasmHost Host { get; init; }
    public HostConfig HostConfig { get; init; }
    public WasmHostProperties HostProperties { get; init; }

    private string? hostArg;
    private string? _runtimeConfigPath;

    public static CommonConfiguration FromCommandLineArguments(string[] args) => new CommonConfiguration(args);

    private CommonConfiguration(string[] args)
    {
        var options = new OptionSet
        {
            { "debug|d", "Start debug server", _ => Debugging = true },
            { "host|h=", "Host config name", v => hostArg = v },
            { "runtime-config|r=", "runtimeconfig.json path for the app", v => _runtimeConfigPath = v }
        };

        RemainingArgs = options.Parse(args);
        if (string.IsNullOrEmpty(_runtimeConfigPath))
        {
            string[] configs = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.runtimeconfig.json").ToArray();
            if (configs.Length == 0)
                throw new CommandLineException($"Could not find any runtimeconfig.json in {Environment.CurrentDirectory}. Use --runtime-config= to specify the path");

            if (configs.Length > 1)
                throw new CommandLineException($"Found multiple runtimeconfig.json files: {string.Join(", ", configs)}. Use --runtime-config= to specify one");

            _runtimeConfigPath = Path.GetFullPath(configs[0]);
        }

        AppPath = Path.GetDirectoryName(_runtimeConfigPath) ?? ".";

        if (string.IsNullOrEmpty(_runtimeConfigPath) || !File.Exists(_runtimeConfigPath))
            throw new CommandLineException($"Cannot find runtime config at {_runtimeConfigPath}");

        RuntimeConfig? rconfig = JsonSerializer.Deserialize<RuntimeConfig>(
                                                File.ReadAllText(_runtimeConfigPath),
                                                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                                                {
                                                    AllowTrailingCommas = true,
                                                    ReadCommentHandling = JsonCommentHandling.Skip,
                                                    PropertyNameCaseInsensitive = true
                                                });
        if (rconfig == null)
            throw new CommandLineException($"Failed to deserialize {_runtimeConfigPath}");

        if (rconfig.RuntimeOptions == null)
            throw new CommandLineException($"Failed to deserialize {_runtimeConfigPath} - rconfig.RuntimeOptions");

        HostProperties = rconfig.RuntimeOptions.WasmHostProperties;
        if (HostProperties == null)
            throw new CommandLineException($"Failed to deserialize {_runtimeConfigPath} - config");

        if (HostProperties.HostConfigs is null || HostProperties.HostConfigs.Count == 0)
            throw new CommandLineException($"no perHostConfigs found");

        // read only if it wasn't overridden by command line option
        string desiredConfig = hostArg ?? HostProperties.DefaultConfig;
        HostConfig? foundConfig = HostProperties.HostConfigs
                                    .Where(hc => string.Equals(hc.Name, desiredConfig, StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();
        if (foundConfig is null && !string.IsNullOrEmpty(hostArg))
        {
            string validHosts = string.Join(", ", HostProperties.HostConfigs.Select(hc => hc.Name));
            throw new CommandLineException($"Unknown host '{hostArg}'. Valid options: {validHosts}");
        }

        HostConfig = foundConfig ?? HostProperties.HostConfigs.First();
        if (HostConfig == null)
            throw new CommandLineException("no host config found");

        // FIXME: validate hostconfig
        if (!Enum.TryParse(HostConfig.HostString, ignoreCase: true, out WasmHost wasmHost))
            throw new CommandLineException($"Unknown host {HostConfig.HostString} in config named {HostConfig.Name}");
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

    public static void CheckPathOrInAppPath(string appPath, string? path, string argName)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException($"Missing value for {argName}");

        if (Path.IsPathRooted(path))
        {
            if (!File.Exists(path))
                throw new ArgumentException($"Cannot find {argName}: {path}");
        }
        else
        {
            string fullPath = Path.Combine(appPath, path);
            if (!File.Exists(fullPath))
                throw new ArgumentException($"Cannot find {argName} {path} in app directory {appPath}");
        }
    }
}

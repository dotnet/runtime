// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public IEnumerable<string> HostArguments { get; init; }
    public bool Silent { get; private set; } = true;
    public bool UseStaticWebAssets { get; private set; }
    public string? RuntimeConfigPath { get; private set; }

    private string? hostArg;
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true
    };

    public static JsonSerializerOptions JsonOptions => s_jsonOptions;
    public static CommonConfiguration FromCommandLineArguments(string[] args) => new CommonConfiguration(args);

    private CommonConfiguration(string[] args)
    {
        List<string> hostArgsList = new();
        var options = new OptionSet
        {
            { "debug|d", "Start debug server", _ => Debugging = true },
            { "host|h=", "Host config name", v => hostArg = v },
            { "runtime-config|r=", "runtimeconfig.json path for the app", v => RuntimeConfigPath = v },
            { "extra-host-arg=", "Extra argument to be passed to the host", hostArgsList.Add },
            { "no-silent", "Verbose output from WasmAppHost", _ => Silent = false },
            { "use-staticwebassets", "Use static web assets, needed for projects targeting WebAssembly SDK", _ => UseStaticWebAssets = true }
        };

        RemainingArgs = options.Parse(args);
        if (string.IsNullOrEmpty(RuntimeConfigPath))
        {
            string[] configs = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.runtimeconfig.json").ToArray();
            if (configs.Length == 0)
                throw new CommandLineException($"Could not find any runtimeconfig.json in {Environment.CurrentDirectory}. Use --runtime-config= to specify the path");

            if (configs.Length > 1)
                throw new CommandLineException($"Found multiple runtimeconfig.json files: {string.Join(", ", configs)}. Use --runtime-config= to specify one");

            RuntimeConfigPath = Path.GetFullPath(configs[0]);
        }

        AppPath = Path.GetDirectoryName(RuntimeConfigPath) ?? ".";

        if (string.IsNullOrEmpty(RuntimeConfigPath) || !File.Exists(RuntimeConfigPath))
            throw new CommandLineException($"Cannot find runtime config at {RuntimeConfigPath}");

        RuntimeConfig? rconfig = JsonSerializer.Deserialize<RuntimeConfig>(
                                                File.ReadAllText(RuntimeConfigPath),
                                                JsonOptions);
        if (rconfig == null)
            throw new CommandLineException($"Failed to deserialize {RuntimeConfigPath}");

        if (rconfig.RuntimeOptions == null)
            throw new CommandLineException($"Failed to deserialize {RuntimeConfigPath} - rconfig.RuntimeOptions");

        HostProperties = rconfig.RuntimeOptions.WasmHostProperties;
        if (HostProperties == null)
            throw new CommandLineException($"Could not find any {nameof(RuntimeOptions.WasmHostProperties)} in {RuntimeConfigPath}");

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

        hostArgsList.AddRange(HostConfig.HostArguments);
        HostArguments = hostArgsList;
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
            throw new CommandLineException($"Missing value for {argName}");

        if (Path.IsPathRooted(path))
        {
            if (!File.Exists(path))
                throw new CommandLineException($"Cannot find {argName}: {path}");
        }
        else
        {
            string fullPath = Path.Combine(appPath, path);
            if (!File.Exists(fullPath))
                throw new CommandLineException($"Cannot find {argName} {path} in app directory {appPath}");
        }
    }
}

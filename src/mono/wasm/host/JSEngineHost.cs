// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class JSEngineHost
{
    private readonly JSEngineArguments _args;
    private readonly ILogger _logger;

    public JSEngineHost(JSEngineArguments args, ILogger logger)
    {
        _args = args;
        _logger = logger;
    }

    public static async Task<int> InvokeAsync(CommonConfiguration commonArgs,
                                              ILoggerFactory loggerFactory,
                                              ILogger logger,
                                              CancellationToken token)
    {
        var args = new JSEngineArguments(commonArgs);
        args.Validate();
        return await new JSEngineHost(args, logger).RunAsync();
    }

    private async Task<int> RunAsync()
    {
        string[] engineArgs = Array.Empty<string>();

        string engineBinary = _args.Host switch
        {
            WasmHost.V8 => "v8",
            WasmHost.JavaScriptCore => "jsc",
            WasmHost.SpiderMonkey => "sm",
            WasmHost.NodeJS => "node",
            _ => throw new CommandLineException($"Unsupported engine {_args.Host}")
        };

        string? engineBinaryPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (engineBinary.Equals("node"))
                engineBinaryPath = FindEngineInPath(engineBinary + ".exe"); // NodeJS ships as .exe rather than .cmd
            else
                engineBinaryPath = FindEngineInPath(engineBinary + ".cmd");
        }
        else
        {
            engineBinaryPath = FindEngineInPath(engineBinary);
        }

        if (engineBinaryPath is null)
            throw new CommandLineException($"Cannot find host {engineBinary} in PATH");

        if (_args.CommonConfig.Debugging)
            throw new CommandLineException($"Debugging not supported with {_args.Host}");

        var runArgsJson = new RunArgumentsJson(applicationArguments: Array.Empty<string>(),
                                               runtimeArguments: _args.CommonConfig.RuntimeArguments);
        runArgsJson.Save(Path.Combine(_args.CommonConfig.AppPath, "runArgs.json"));

        var args = new List<string>();

        if (_args.Host == WasmHost.V8)
        {
            // v8 needs this flag to enable WASM support
            args.Add("--expose_wasm");
        }

        args.Add(_args.JSPath!);

        args.AddRange(engineArgs);
        if (_args.Host is WasmHost.V8 or WasmHost.JavaScriptCore)
        {
            // v8/jsc want arguments to the script separated by "--", others don't
            args.Add("--");
        }

        args.AddRange(_args.AppArgs);

        ProcessStartInfo psi = new()
        {
            FileName = engineBinary,
            WorkingDirectory = _args.CommonConfig.AppPath
        };

        foreach (string? arg in args)
            psi.ArgumentList.Add(arg!);

        int exitCode = await Utils.TryRunProcess(psi,
                                    _logger,
                                    msg => { if (msg != null) _logger.LogInformation(msg); },
                                    msg => { if (msg != null) _logger.LogInformation(msg); });

        return exitCode;
    }

    private static string? FindEngineInPath(string engineBinary)
    {
        if (File.Exists(engineBinary) || Path.IsPathRooted(engineBinary))
            return engineBinary;

        var path = Environment.GetEnvironmentVariable("PATH");

        if (path == null)
            return engineBinary;

        foreach (var folder in path.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(folder, engineBinary);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}

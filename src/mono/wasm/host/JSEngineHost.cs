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

    public static Task<int> InvokeAsync(CommonConfiguration commonArgs,
                                              ILoggerFactory _,
                                              ILogger logger,
                                              CancellationToken _1)
    {
        var args = new JSEngineArguments(commonArgs);
        args.Validate();
        return new JSEngineHost(args, logger).RunAsync();
    }

    private async Task<int> RunAsync()
    {
        string engineBinary = _args.Host switch
        {
            WasmHost.V8 => "v8",
            WasmHost.JavaScriptCore => "jsc",
            WasmHost.SpiderMonkey => "sm",
            WasmHost.NodeJS => "node",
            _ => throw new CommandLineException($"Unsupported engine {_args.Host}")
        };

        if (!FileUtils.TryFindExecutableInPATH(engineBinary, out string? engineBinaryPath, out string? errorMessage))
            throw new CommandLineException($"Cannot find host {engineBinary}: {errorMessage}");

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

        args.AddRange(_args.CommonConfig.HostArguments);

        args.Add(_args.JSPath!);

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
                                    msg => { if (msg != null) _logger.LogInformation(msg); },
                                    silent: _args.CommonConfig.Silent);

        if (!_args.CommonConfig.Silent)
            Console.WriteLine($"{_args.Host} exited with {exitCode}");
        return exitCode;
    }
}

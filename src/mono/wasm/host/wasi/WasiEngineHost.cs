// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WasiEngineHost
{
    private readonly WasiEngineArguments _args;
    private readonly ILogger _logger;

    public WasiEngineHost(WasiEngineArguments args, ILogger logger)
    {
        _args = args;
        _logger = logger;
    }

    public static async Task<int> InvokeAsync(CommonConfiguration commonArgs,
                                              ILoggerFactory _,
                                              ILogger logger,
                                              CancellationToken _1)
    {
        var args = new WasiEngineArguments(commonArgs);
        args.Validate();
        return await new WasiEngineHost(args, logger).RunAsync();
    }

    private async Task<int> RunAsync()
    {
        string[] engineArgs = Array.Empty<string>();

        string engineBinary = _args.Host switch
        {
            WasmHost.Wasmtime => "wasmtime",
            _ => throw new CommandLineException($"Unsupported engine {_args.Host}")
        };

        if (!FileUtils.TryFindExecutableInPATH(engineBinary, out string? engineBinaryPath, out string? errorMessage))
            throw new CommandLineException($"Cannot find host {engineBinary}: {errorMessage}");

        if (_args.CommonConfig.Debugging)
            throw new CommandLineException($"Debugging not supported with {_args.Host}");

        // var runArgsJson = new RunArgumentsJson(applicationArguments: Array.Empty<string>(),
        //                                        runtimeArguments: _args.CommonConfig.RuntimeArguments);
        // runArgsJson.Save(Path.Combine(_args.CommonConfig.AppPath, "runArgs.json"));

        List<string> args = new() { "run" };

        if (!_args.IsSingleFileBundle)
        {
            args.AddRange(["--dir", "."]);
        };

        args.AddRange(engineArgs);
        args.Add("--");

        if (_args.IsSingleFileBundle)
        {
            args.Add($"{Path.GetFileNameWithoutExtension(_args.CommonConfig.HostProperties.MainAssembly)}.wasm");
        }
        else
        {
            // FIXME: maybe move the assembly name to a config file
            args.Add("dotnet.wasm");
            args.Add(Path.GetFileNameWithoutExtension(_args.CommonConfig.HostProperties.MainAssembly));
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
}

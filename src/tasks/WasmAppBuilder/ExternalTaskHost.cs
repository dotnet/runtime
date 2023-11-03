// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace WasmAppBuilder;

public static class Program {
    public static int Main (string[] args) {
        if (args.Length != 1)
            throw new ArgumentException("Expected 'wasmappbuilder [response file]'");
        if (!File.Exists(args[0]))
            throw new FileNotFoundException("Response file not found: " + args[0]);
        var rspText = File.ReadAllText(args[0]);
        var rsp = JsonSerializer.Deserialize<Microsoft.WebAssembly.Build.Tasks.ManagedToNativeGenerator.ExecuteArguments>(rspText);
        var logAdapter = new LogAdapter();

        try {
            Microsoft.WebAssembly.Build.Tasks.ManagedToNativeGenerator.ExecuteForAssemblies(
                logAdapter,
                rsp.managedAssemblies,
                rsp.runtimeIcallTableFile,
                rsp.pInvokeModules,
                rsp.pInvokeOutputPath,
                rsp.icallOutputPath,
                rsp.interpToNativeOutputPath,
                rsp.cacheFilePath
            );
        } catch (LogAsErrorException e) {
            logAdapter.Error(e.Message);
            return 2;
        }

        if (logAdapter.HasLoggedErrors)
            return 1;
        else
            return 0;
    }
}

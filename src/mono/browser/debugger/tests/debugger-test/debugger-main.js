// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './_framework/dotnet.js'

try {
    const runtime = await dotnet
        .withEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES", "debug")
        .withConfig({ maxParallelDownloads: 10 })
        // For custom logging patch the functions below
        //.withDiagnosticTracing(true)
        //.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        //.withEnvironmentVariable("MONO_LOG_MASK", "all")
        .create();
    /*runtime.INTERNAL.logging = {
        trace: (domain, log_level, message, isFatal, dataPtr) => console.log({ domain, log_level, message, isFatal, dataPtr }),
        debugger: (level, message) => console.log({ level, message }),
    };*/
    App.runtime = runtime;

    // this is fake implementation of legacy `bind_static_method`
    // so that we don't have to rewrite all the tests which use it via `invoke_static_method`
    App.bind_static_method = (method_name) => {
        const methodInfo = App.exports.DebuggerTests.BindStaticMethod.GetMethodInfo(method_name);
        const signature = App.exports.DebuggerTests.BindStaticMethod.GetSignature(methodInfo);
        const invoker = App.exports.DebuggerTests.BindStaticMethod[signature];
        if (!invoker) {
            const message = `bind_static_method: Could not find invoker for ${method_name} with signature ${signature}`;
            console.error(message);
            throw new Error(message);
        }
        return function () {
            return invoker(methodInfo, ...arguments);
        }
    }

    // this is fake implementation of legacy `bind_static_method` which uses `mono_wasm_invoke_method_raw`
    // We have unit tests that stop on unhandled managed exceptions.
    // as opposed to [JSExport], the `mono_wasm_invoke_method_raw` doesn't handle managed exceptions.
    // Same way as old `bind_static_method` didn't
    App.bind_static_method_native = (method_name) => {
        try {
            const monoMethodPtr = App.exports.DebuggerTests.BindStaticMethod.GetMonoMethodPtr(method_name);
            // this is only implemented for void methods with no arguments
            const invoker = runtime.Module.cwrap("mono_wasm_invoke_method_raw", "number", ["number", "number"]);
            return function () {
                try {
                    return invoker(monoMethodPtr);
                }
                catch (err) {
                    console.error(err);
                    throw err;
                }
            }
        }
        catch (err) {
            console.error(err);
            throw err;
        }
    }

    await App.init();
}
catch (err) {
    exit(2, err);
}
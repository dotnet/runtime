// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WasiEngineArguments
{
    public WasmHost Host => CommonConfig.Host;
    public CommonConfiguration CommonConfig { get; init; }

    public string[] AppArgs { get; init; }

    public WasiEngineArguments(CommonConfiguration commonConfig)
    {
        CommonConfig = commonConfig;
        AppArgs = commonConfig.RemainingArgs.ToArray();
    }

    public void Validate()
    {
        if (CommonConfig.Host is not WasmHost.Wasmtime)
            throw new ArgumentException($"Internal error: host {CommonConfig.Host} not supported as a jsengine");
    }
}

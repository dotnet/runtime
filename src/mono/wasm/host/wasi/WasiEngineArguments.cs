// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WasiEngineArguments
{
    public WasmHost Host => CommonConfig.Host;
    public CommonConfiguration CommonConfig { get; init; }

    public IEnumerable<string> AppArgs => CommonConfig.RemainingArgs;

    public bool IsSingleFileBundle =>
        CommonConfig.HostProperties.Extra?.TryGetValue("singleFileBundle", out JsonElement singleFileValue) == true&&
        singleFileValue.GetBoolean();

    public WasiEngineArguments(CommonConfiguration commonConfig)
    {
        CommonConfig = commonConfig;
    }

    public void Validate()
    {
        if (CommonConfig.Host is not WasmHost.Wasmtime)
            throw new ArgumentException($"Internal error: host {CommonConfig.Host} not supported as a jsengine");
    }
}

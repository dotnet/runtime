// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Mono.Options;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WasiEngineArguments
{
    public WasmHost Host => CommonConfig.Host;
    public CommonConfiguration CommonConfig { get; init; }

    public bool ForwardExitCode { get; private set; }
    public string[] AppArgs { get; init; }

    public bool IsSingleFileBundle =>
        CommonConfig.HostProperties.Extra?.TryGetValue("singleFileBundle", out JsonElement singleFileValue) == true&&
        singleFileValue.GetBoolean();

    public WasiEngineArguments(CommonConfiguration commonConfig)
    {
        CommonConfig = commonConfig;
        AppArgs = GetOptions().Parse(commonConfig.RemainingArgs).ToArray();
        ParseJsonProperties(CommonConfig.HostConfig.Properties);
    }

    private OptionSet GetOptions() => new OptionSet
    {
        { "forward-exit-code", "Forward process exit code via stderr", v => ForwardExitCode = true }
    };

    public void ParseJsonProperties(IDictionary<string, JsonElement>? properties)
    {
        if (properties?.TryGetValue("forward-exit-code", out JsonElement forwardElement) == true)
            ForwardExitCode = forwardElement.GetBoolean();
    }

    public void Validate()
    {
        if (CommonConfig.Host is not WasmHost.Wasmtime)
            throw new ArgumentException($"Internal error: host {CommonConfig.Host} not supported as a jsengine");
    }
}

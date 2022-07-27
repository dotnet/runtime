// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class JSEngineArguments
{
    public string? JSPath { get; set; }
    public WasmHost Host => CommonConfig.Host;
    public CommonConfiguration CommonConfig { get; init; }

    // no js specific options
    public IEnumerable<string> AppArgs => CommonConfig.RemainingArgs;

    public JSEngineArguments(CommonConfiguration commonConfig)
    {
        CommonConfig = commonConfig;

        if (CommonConfig.Host is not (WasmHost.JavaScriptCore or WasmHost.NodeJS or WasmHost.SpiderMonkey or WasmHost.V8))
            throw new ArgumentException($"Internal error: host {CommonConfig.Host} not supported as a jsengine");

        ParseJsonProperties(CommonConfig.HostConfig.Properties);
    }

    private void ParseJsonProperties(IDictionary<string, JsonElement>? properties)
    {
        if (properties?.TryGetValue("js-path", out JsonElement jsPathElement) == true &&
            jsPathElement.GetString() is string parsedPath)
            JSPath = parsedPath;
    }

    public void Validate()
    {
        CommonConfiguration.CheckPathOrInAppPath(CommonConfig.AppPath, JSPath, "js-path");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed record RuntimeConfig(RuntimeOptions RuntimeOptions);

internal sealed record RuntimeOptions(WasmHostProperties WasmHostProperties);

internal sealed record WasmHostProperties(
     string DefaultConfig,
     [property: JsonPropertyName("perHostConfig")] List<HostConfig> HostConfigs,
     string MainAssembly,
     string[] RuntimeArguments,
     IDictionary<string, string>? EnvironmentVariables,
     int? FirefoxProxyPort,
     int? FirefoxDebuggingPort,
     int? ChromeProxyPort,
     int? ChromeDebuggingPort,
     int WebServerPort = 0)
{
    // using an explicit property because the deserializer doesn't like
    // extension data in the record constructor
    [property: JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

internal sealed record HostConfig(string? Name, [property: JsonPropertyName("host")] string? HostString)
{
    [property: JsonPropertyName("host-args")] public string[] HostArguments { get; set; } = Array.Empty<string>();
    // using an explicit property because the deserializer doesn't like
    // extension data in the record constructor
    [property: JsonExtensionData] public Dictionary<string, JsonElement>? Properties { get; set; }
}

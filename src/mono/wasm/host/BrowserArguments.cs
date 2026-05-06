// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Mono.Options;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class BrowserArguments
{
    public string? HTMLPath { get; private set; }
    public bool? ForwardConsoleOutput { get; private set; }
    public string[] AppArgs { get; init; }
    public CommonConfiguration CommonConfig { get; init; }

    public BrowserArguments(CommonConfiguration commonConfig)
    {
        CommonConfig = commonConfig;
        AppArgs = GetOptions().Parse(commonConfig.RemainingArgs).ToArray();

        ParseJsonProperties(CommonConfig.HostConfig.Properties);
    }

    private OptionSet GetOptions() => new OptionSet
    {
        { "forward-console", "Forward JS console output", v => ForwardConsoleOutput = true }
    };

    public void ParseJsonProperties(IDictionary<string, JsonElement>? properties)
    {
        if (properties?.TryGetValue("html-path", out JsonElement htmlPathElement) == true)
            HTMLPath = htmlPathElement.GetString();
        if (properties?.TryGetValue("forward-console", out JsonElement forwardConsoleElement) == true)
            ForwardConsoleOutput = forwardConsoleElement.GetBoolean();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needs to validate instance members")]
    public void Validate()
    {
    }
}

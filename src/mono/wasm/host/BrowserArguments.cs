// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using Mono.Options;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class BrowserArguments
{
    public string? HTMLPath { get; private set; }
    public bool? ForwardConsoleOutput { get; private set; }
    public bool UseQueryStringToPassArguments { get; private set; }
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
        { "forward-console", "Forward JS console output", v => ForwardConsoleOutput = true },
        { "use-query-string-for-args", "Use query string to pass arguments (Default: false)", v => UseQueryStringToPassArguments = true }
    };

    public void ParseJsonProperties(IDictionary<string, JsonElement>? properties)
    {
        if (properties?.TryGetValue("html-path", out JsonElement htmlPathElement) == true)
            HTMLPath = htmlPathElement.GetString();
        if (properties?.TryGetValue("forward-console", out JsonElement forwardConsoleElement) == true)
            ForwardConsoleOutput = forwardConsoleElement.GetBoolean();
        if (properties?.TryGetValue("use-query-string-for-args", out JsonElement useQueryElement) == true)
            UseQueryStringToPassArguments = useQueryElement.GetBoolean();
    }

    public void Validate()
    {
        CommonConfiguration.CheckPathOrInAppPath(CommonConfig.AppPath, HTMLPath, "html-path");
    }
}

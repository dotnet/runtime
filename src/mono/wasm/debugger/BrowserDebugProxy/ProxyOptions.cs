// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public class ProxyOptions
{
    public Uri DevToolsUrl { get; set; } = new Uri($"http://localhost:9222");
    public int? OwnerPid { get; set; }
    public int FirefoxProxyPort { get; set; }
    public int FirefoxDebugPort { get; set; } = 6000;
    public int DevToolsProxyPort { get; set; }
    public int DevToolsDebugPort
    {
        get => DevToolsUrl.Port;
        set
        {
            var builder = new UriBuilder(DevToolsUrl)
            {
                Port = value
            };
            DevToolsUrl = builder.Uri;
        }
    }
    public string? LogPath { get; set; }
    public bool RunningForBlazor { get; set; }
    public bool IgnoreProxyForLocalAddress { get; set; }
    public bool IsFirefoxDebugging { get; set; }
    public bool JustMyCode { get; set; }
}

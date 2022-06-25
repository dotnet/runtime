// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.WebAssembly.AppHost;

internal enum WasmHost
{
    /// <summary>
    /// V8
    /// </summary>
    V8,
    /// <summary>
    /// JavaScriptCore
    /// </summary>
    JavaScriptCore,
    /// <summary>
    /// SpiderMonkey
    /// </summary>
    SpiderMonkey,
    /// <summary>
    /// NodeJS
    /// </summary>
    NodeJS,
    /// <summary>
    /// Browser
    /// </summary>
    Browser
}

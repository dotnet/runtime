// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging.Console;

namespace Microsoft.WebAssembly.AppHost;
internal sealed class PassThroughConsoleFormatterOptions : ConsoleFormatterOptions
{
    public string Prefix = string.Empty;
}

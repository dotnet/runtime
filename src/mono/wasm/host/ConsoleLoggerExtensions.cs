// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.AppHost;

internal static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddPassThroughConsole(this ILoggingBuilder builder) =>
            builder
                .AddConsole(options => options.FormatterName = nameof(PassThroughConsoleFormatter))
                .AddConsoleFormatter<PassThroughConsoleFormatter, PassThroughConsoleFormatterOptions>();

    public static ILoggingBuilder AddPassThroughConsole(
        this ILoggingBuilder builder, Action<PassThroughConsoleFormatterOptions> configure) =>
            builder
                .AddConsole(options => options.FormatterName = nameof(PassThroughConsoleFormatter))
                .AddConsoleFormatter<PassThroughConsoleFormatter, PassThroughConsoleFormatterOptions>(configure);
}

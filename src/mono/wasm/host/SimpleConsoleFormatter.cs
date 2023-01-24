// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class PassThroughConsoleFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;
    private PassThroughConsoleFormatterOptions? _formatterOptions;

    public PassThroughConsoleFormatter(IOptionsMonitor<PassThroughConsoleFormatterOptions> options)
        : base("PassThroughConsoleFormatter") =>
        (_optionsReloadToken, _formatterOptions) =
            (options.OnChange((options, _) => ReloadLoggerOptions(options)), options.CurrentValue);

    private void ReloadLoggerOptions(PassThroughConsoleFormatterOptions options) => _formatterOptions = options;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_formatterOptions!.Prefix))
            textWriter.Write(_formatterOptions!.Prefix);
        textWriter.WriteLine(message);
    }

    public void Dispose() => _optionsReloadToken?.Dispose();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WasmTestMessagesProcessor
{
    private readonly ILogger _logger;

    public WasmTestMessagesProcessor(ILogger logger)
    {
        _logger = logger;
    }

    public void Invoke(string message)
    {
        WasmLogMessage? logMessage = null;
        string line;

        if (message.Length > 0 && message[0] == '{')
        {
            try
            {
                logMessage = JsonSerializer.Deserialize<WasmLogMessage>(message);
                line = logMessage?.payload ?? message.TrimEnd();
            }
            catch (JsonException)
            {
                line = message.TrimEnd();
            }
        }
        else
        {
            line = message.TrimEnd();
        }

        switch (logMessage?.method?.ToLowerInvariant())
        {
            case "console.debug": _logger.LogDebug(line); break;
            case "console.error": _logger.LogError(line); break;
            case "console.warn": _logger.LogWarning(line); break;
            case "console.trace": _logger.LogTrace(line); break;

            case "console.log":
            default: _logger.LogInformation(line); break;
        }
    }
}

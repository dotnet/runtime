// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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
                if (logMessage != null)
                {
                    line = logMessage.payload + " " + string.Join(" ", logMessage.arguments ?? Enumerable.Empty<object>());
                }
                else
                {
                    line = message;
                }
            }
            catch (JsonException)
            {
                line = message;
            }
        }
        else
        {
            line = message;
        }

        line = line.TrimEnd();
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

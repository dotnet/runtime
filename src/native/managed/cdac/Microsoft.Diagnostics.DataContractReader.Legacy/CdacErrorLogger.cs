// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class CdacErrorLogger
{
    private static readonly object s_lock = new();
    private static readonly string? s_logPath =
        Environment.GetEnvironmentVariable("DOTNET_CDAC_ERROR_LOG");

    internal static void Log(string message)
    {
        if (string.IsNullOrEmpty(s_logPath))
            return;

        string entry = $"[{DateTime.UtcNow:O}] {message}";
        try
        {
            Console.Error.WriteLine($"[cDAC] {entry}");
            lock (s_lock)
            {
                File.AppendAllText(s_logPath, entry + Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics must not affect cDAC behavior.
        }
    }

    internal static void LogException(string operation, Exception exception) =>
        Log($"{operation} failed:{Environment.NewLine}{exception}");
}

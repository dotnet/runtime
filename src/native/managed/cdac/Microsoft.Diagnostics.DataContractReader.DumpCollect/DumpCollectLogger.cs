// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal static class DumpCollectLogger
{
    private static readonly object s_lock = new();
    private static readonly string? s_logPath =
        Environment.GetEnvironmentVariable("DOTNET_CDAC_DUMP_LOG");

    internal static void Log(string message)
    {
        string entry = $"[{DateTime.UtcNow:O}] {message}";
        TryWriteToStandardError(entry);

        if (string.IsNullOrEmpty(s_logPath))
            return;

        try
        {
            lock (s_lock)
            {
                File.AppendAllText(s_logPath, entry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            TryWriteToStandardError(
                $"Failed to write cDAC dump diagnostics to '{s_logPath}': {ex}");
        }
    }

    internal static void LogException(string phase, Exception exception) =>
        Log($"{phase} failed:{Environment.NewLine}{exception}");

    private static void TryWriteToStandardError(string message)
    {
        try
        {
            Console.Error.WriteLine($"[cdac-dump] {message}");
        }
        catch
        {
            // Diagnostics must not affect dump collection.
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Controls whether delegation-only APIs can fall back to the legacy DAC implementation.
/// When <c>CDAC_NO_FALLBACK=1</c> is set, only explicitly allowlisted methods may delegate.
/// Blocked calls are tracked and written to a log file on process exit.
/// </summary>
internal static class LegacyFallbackHelper
{
    private static readonly bool s_noFallback =
        Environment.GetEnvironmentVariable("CDAC_NO_FALLBACK") == "1";

    // Methods that are allowed to fall back even in no-fallback mode.
    // Use the method name as it appears via [CallerMemberName].
    private static readonly HashSet<string> s_allowlist = new(StringComparer.Ordinal)
    {
        // Dump creation — the cDAC does not implement memory enumeration.
        nameof(ICLRDataEnumMemoryRegions.EnumMemoryRegions),
    };

    // Tracks APIs that attempted fallback but were blocked in no-fallback mode.
    private static readonly ConcurrentDictionary<string, int> s_blockedCalls = new();

    static LegacyFallbackHelper()
    {
        if (s_noFallback)
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => FlushBlockedCallLog();
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the calling method is allowed to delegate to the legacy DAC.
    /// In normal mode (no <c>CDAC_NO_FALLBACK</c>), always returns <c>true</c>.
    /// In no-fallback mode, returns <c>true</c> only for allowlisted methods.
    /// Blocked calls are recorded for diagnostics.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CanFallback([CallerMemberName] string name = "")
    {
        if (!s_noFallback)
            return true;

        if (s_allowlist.Contains(name))
            return true;

        s_blockedCalls.AddOrUpdate(name, 1, (_, count) => count + 1);
        return false;
    }

    private static void FlushBlockedCallLog()
    {
        if (s_blockedCalls.IsEmpty)
            return;

        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "cdac_blocked_fallbacks.log");

            using StreamWriter writer = new(logPath, append: false);
            writer.WriteLine("== cDAC Blocked Legacy Fallback Calls ==");
            writer.WriteLine($"Timestamp: {DateTime.UtcNow:O}");
            writer.WriteLine();
            writer.WriteLine($"{"API",-60} {"Hits",8}");
            writer.WriteLine(new string('-', 70));

            foreach (KeyValuePair<string, int> entry in s_blockedCalls)
            {
                writer.WriteLine($"{entry.Key,-60} {entry.Value,8}");
            }

            writer.WriteLine();
            writer.WriteLine($"Total: {s_blockedCalls.Count} unique APIs blocked");
        }
        catch
        {
            // Best-effort logging — don't crash the process on exit.
        }
    }
}

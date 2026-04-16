// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Controls whether delegation-only APIs can fall back to the legacy DAC implementation.
/// When <c>CDAC_NO_FALLBACK=1</c> is set, only explicitly allowlisted methods may delegate.
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
        "EnumMemoryRegion",
        "EnumMemoryRegionsWrapper",
    };

    /// <summary>
    /// Returns <c>true</c> if the calling method is allowed to delegate to the legacy DAC.
    /// In normal mode (no <c>CDAC_NO_FALLBACK</c>), always returns <c>true</c>.
    /// In no-fallback mode, returns <c>true</c> only for allowlisted methods.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CanFallback([CallerMemberName] string name = "")
    {
        if (!s_noFallback)
            return true;

        return s_allowlist.Contains(name);
    }
}

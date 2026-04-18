// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Controls whether delegation-only APIs can fall back to the legacy DAC implementation.
/// When <c>CDAC_NO_FALLBACK=1</c> is set, only explicitly allowlisted methods may delegate.
/// All fallback attempts are logged to stderr for capture by the test infrastructure.
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

        // IMetaDataImport QI — needed until managed MetadataReader wrapper lands (PR #127028).
        nameof(ICustomQueryInterface.GetInterface),

        // GC heap analysis — not yet implemented in the cDAC (PR #125895).
        nameof(ISOSDacInterface11.IsTrackedType),

        // Loader heap traversal — not yet implemented in the cDAC (PR #125129).
        nameof(ISOSDacInterface.TraverseLoaderHeap),

        // IXCLRDataMethodDefinition — not yet implemented in the cDAC.
        nameof(IXCLRDataMethodDefinition.StartEnumInstances),
        nameof(IXCLRDataMethodDefinition.GetName),
        nameof(IXCLRDataMethodDefinition.SetCodeNotification),
        nameof(IXCLRDataMethodDefinition.HasClassOrMethodInstantiation),
    };

    // Files whose methods are all allowed to fall back.
    // The entire DBI interface is deferred — the cDAC does not implement ICorDebug data access yet.
    private static readonly HashSet<string> s_fileAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DacDbiImpl.cs",
    };

    /// <summary>
    /// Returns <c>true</c> if the calling method is allowed to delegate to the legacy DAC.
    /// In normal mode (no <c>CDAC_NO_FALLBACK</c>), always returns <c>true</c>.
    /// In no-fallback mode, returns <c>true</c> only for allowlisted methods.
    /// All fallback attempts (allowed and blocked) are logged to stderr.
    /// </summary>
    internal static bool CanFallback(
        [CallerMemberName] string name = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (!s_noFallback)
            return true;

        if (s_allowlist.Contains(name) || s_fileAllowlist.Contains(Path.GetFileName(file)))
        {
            Console.Error.WriteLine($"[cDAC] Allowed fallback: {name} at {Path.GetFileName(file)}:{line}");
            return true;
        }

        Console.Error.WriteLine($"[cDAC] Blocked fallback: {name} at {Path.GetFileName(file)}:{line}");
        return false;
    }
}

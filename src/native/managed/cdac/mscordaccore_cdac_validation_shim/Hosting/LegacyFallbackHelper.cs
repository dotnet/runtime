// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Controls whether delegation-only APIs can fall back to the legacy DAC implementation.
/// In <see cref="ValidationMode.Strict"/> only explicitly allowlisted methods may delegate.
/// All fallback attempts are logged to stderr for capture by the test infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This preserves the pre-refactor <c>LegacyFallbackHelper</c> behavior exactly, including the
/// method-name allowlist matching by simple name (so any method named <c>EnumMemoryRegions</c>
/// collides into the allowlist regardless of which interface declares it) and the whole-file
/// allowance for <c>DacDbiImpl.cs</c> (which, being an exact file-name match, does <b>not</b>
/// cover <c>DacDbiImpl.NativeCodeInfo.cs</c>).
/// </para>
/// <para>
/// The proxies pass the <em>production</em> method name and file name rather than relying on
/// <see cref="CallerMemberNameAttribute"/>/<see cref="CallerFilePathAttribute"/>, because the shim's
/// own file layout differs from the production cDAC's. The effective manifest is therefore
/// identical to the one that existed before the production decoupling.
/// </para>
/// </remarks>
internal static class LegacyFallbackHelper
{
    private static readonly ValidationMode s_mode = ShimEnvironment.Mode;

    // Methods that are allowed to fall back even in strict mode.
    // Use the method name as it appeared via [CallerMemberName] in the production cDAC.
    private static readonly HashSet<string> s_allowlist = new(StringComparer.Ordinal)
    {
        // Dump creation — the cDAC does not implement memory enumeration.
        nameof(ICLRDataEnumMemoryRegions.EnumMemoryRegions),
    };

    // Files whose methods are all allowed to fall back.
    // The entire DBI interface is deferred — the cDAC does not implement ICorDebug data access yet.
    private static readonly HashSet<string> s_fileAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "DacDbiImpl.cs",
    };

    /// <summary>
    /// Returns <c>true</c> if the named production method is allowed to delegate to the legacy DAC.
    /// In <see cref="ValidationMode.Fallback"/> always returns <c>true</c>.
    /// In <see cref="ValidationMode.Strict"/> returns <c>true</c> only for allowlisted methods.
    /// All fallback attempts (allowed and blocked) are logged to stderr.
    /// </summary>
    /// <param name="name">The production method name (as <c>[CallerMemberName]</c> would have reported it).</param>
    /// <param name="file">The production file the method was defined in.</param>
    /// <param name="line">The production line number, when known.</param>
    internal static bool CanFallback(string name, string file, int line = 0)
    {
        if (s_mode != ValidationMode.Strict)
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

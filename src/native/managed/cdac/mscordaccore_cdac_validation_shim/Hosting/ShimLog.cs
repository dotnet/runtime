// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Which legacy-DAC behavior the validation shim applies. Selected by
/// <c>DOTNET_CDAC_VALIDATION_MODE</c>.
/// </summary>
internal enum ValidationMode
{
    /// <summary>
    /// Unimplemented cDAC APIs delegate to the legacy DAC. Mirrors the pre-refactor default
    /// (no <c>CDAC_NO_FALLBACK</c>).
    /// </summary>
    Fallback,

    /// <summary>
    /// Only the allowlisted APIs may delegate to the legacy DAC. Mirrors the pre-refactor
    /// <c>CDAC_NO_FALLBACK=1</c> behavior.
    /// </summary>
    Strict,
}

/// <summary>
/// Diagnostic output for the validation shim. Everything is written to stderr so the
/// dotnet/diagnostics test infrastructure captures it alongside the debugger output.
/// </summary>
internal static class ShimLog
{
    private const string Prefix = "[cDAC]";

    internal static void Info(string message) => Console.Error.WriteLine($"{Prefix} {message}");

    internal static void Mismatch(string message) => Console.Error.WriteLine($"{Prefix} Validation mismatch: {message}");

    internal static void Error(string message) => Console.Error.WriteLine($"{Prefix} Error: {message}");
}

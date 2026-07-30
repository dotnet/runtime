// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Private duplicate of the production cDAC <c>HResultValidationMode</c>. The recovered comparison
/// blocks reference these modes by name, so the values must stay in sync with
/// <c>Microsoft.Diagnostics.DataContractReader.Legacy.HResultValidationMode</c>.
/// </summary>
internal enum HResultValidationMode
{
    /// <summary>
    /// HRESULTs must match exactly.
    /// </summary>
    Exact,

    /// <summary>
    /// Success HRESULTs must match exactly, but any two failing HRESULTs (negative values) are considered equivalent.
    /// This is the recommended default because the cDAC and native DAC may use different exception types for the
    /// same invalid input (e.g., InvalidOperationException vs E_INVALIDARG), producing different failing HRESULTs.
    /// </summary>
    AllowDivergentFailures,

    /// <summary>
    /// Allows divergent success HRESULTs, but failing HRESULTs must match exactly.
    /// </summary>
    AllowDivergentSuccess,

    /// <summary>
    /// Like <see cref="AllowDivergentFailures"/>, but also allows the cDAC to succeed when the native DAC fails.
    /// </summary>
    AllowCdacSuccess,
}

/// <summary>
/// Stands in for <see cref="System.Diagnostics.Debug"/> inside the validation shim.
/// </summary>
/// <remarks>
/// <para>
/// The comparison blocks in this assembly are recovered verbatim from the pre-refactor cDAC
/// implementations, where they called <c>Debug.Assert</c> / <c>Debug.ValidateHResult</c>. Because
/// this type lives in the same namespace as those blocks, it binds in preference to
/// <see cref="System.Diagnostics.Debug"/> without editing the recovered source.
/// </para>
/// <para>
/// A failed assertion is first recorded and logged (so the SOS test run always gets a
/// <c>[cDAC] Validation mismatch</c> line), and then forwarded to
/// <see cref="System.Diagnostics.Debug"/> so it behaves exactly as the pre-refactor cDAC did.
/// The production cDAC result stays authoritative in every case: when assertion execution
/// continues, the caller still receives the cDAC's answer.
/// </para>
/// </remarks>
internal static class Debug
{
    private static int s_failureCount;

    /// <summary>
    /// Number of validation failures observed so far in this process.
    /// </summary>
    internal static int FailureCount => Volatile.Read(ref s_failureCount);

    internal static void Assert(
        bool condition,
        string? message = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Fail(message ?? "Assertion failed", filePath, lineNumber);
        }
    }

    internal static void Assert(
        bool condition,
        string? message,
        string? detailMessage,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Fail(
                detailMessage is null ? message ?? "Assertion failed" : $"{message} {detailMessage}",
                filePath,
                lineNumber);
        }
    }

    internal static void Fail(
        string? message,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        Interlocked.Increment(ref s_failureCount);
        ShimLog.Mismatch($"{message} ({Path.GetFileName(filePath)}:{lineNumber})");

        // Per the user's decision, after recording and logging the mismatch, invoke the real
        // System.Diagnostics.Debug.Fail so the assertion behaves exactly as it did in the
        // pre-refactor cDAC. The production cDAC result remains authoritative: if assertion
        // execution continues (e.g. no breaking trace listener is installed), the shim still
        // returns the cDAC's answer to the caller.
        global::System.Diagnostics.Debug.Fail(message ?? "Assertion failed", $"{Path.GetFileName(filePath)}:{lineNumber}");
    }

    /// <summary>
    /// Informational trace used by the recovered comparison blocks for best-effort (non-failing)
    /// divergence notes. It is logged and forwarded to <see cref="System.Diagnostics.Debug"/>, but
    /// never counts as a validation failure.
    /// </summary>
    internal static void WriteLine(string? message)
    {
        ShimLog.Info(message ?? string.Empty);
        global::System.Diagnostics.Debug.WriteLine(message);
    }

    internal static void ValidateHResult(
        int cdacHr,
        int dacHr,
        HResultValidationMode mode = HResultValidationMode.AllowDivergentFailures,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        bool match = mode switch
        {
            HResultValidationMode.Exact => cdacHr == dacHr,
            HResultValidationMode.AllowDivergentFailures => cdacHr == dacHr || (cdacHr < 0 && dacHr < 0),
            HResultValidationMode.AllowDivergentSuccess => cdacHr == dacHr || (cdacHr >= 0 && dacHr >= 0),
            HResultValidationMode.AllowCdacSuccess => cdacHr == dacHr || (cdacHr < 0 && dacHr < 0) || (cdacHr >= 0 && dacHr < 0),
            _ => cdacHr == dacHr,
        };

        if (!match)
        {
            Fail(
                $"HResult mismatch - cDAC: 0x{unchecked((uint)cdacHr):X8}, DAC: 0x{unchecked((uint)dacHr):X8}",
                filePath,
                lineNumber);
        }
    }
}

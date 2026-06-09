// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

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
    /// Like <see cref="AllowDivergentFailures"/>, but also allows the cDAC to succeed when the native DAC fails.
    /// The native DAC's MetaSig constructor traverses MethodDesc -> Module -> MDImport -> signature blob via
    /// DAC host pointers, and any intermediate read can throw under EX_TRY on certain frames (e.g., EH dispatch).
    /// The cDAC reads the same metadata through contracts (EcmaMetadata -> PEImage -> metadata blob) which uses
    /// a different pointer traversal path that doesn't hit the same failure.
    /// </summary>
    AllowCdacSuccess,
}

internal static class DebugExtensions
{
#if DEBUG
    private const int LastExceptionRingSize = 4;

    [ThreadStatic]
    private static Exception?[]? _debugLastExceptions;

    [ThreadStatic]
    private static int _debugLastExceptionsNextIndex;

    static DebugExtensions()
    {
        AppDomain.CurrentDomain.FirstChanceException += static (_, e) =>
        {
            Exception?[] ring = _debugLastExceptions ??= new Exception?[LastExceptionRingSize];
            ring[_debugLastExceptionsNextIndex] = e.Exception;
            _debugLastExceptionsNextIndex = (_debugLastExceptionsNextIndex + 1) % LastExceptionRingSize;
        };
    }

    private static Exception? FindMatchingException(int hr)
    {
        Exception?[]? ring = _debugLastExceptions;
        if (ring is null)
            return null;

        // Walk from newest to oldest.
        int next = _debugLastExceptionsNextIndex;
        for (int i = 0; i < LastExceptionRingSize; i++)
        {
            int idx = (next - 1 - i + LastExceptionRingSize) % LastExceptionRingSize;
            Exception? ex = ring[idx];
            if (ex is not null && ex.HResult == hr)
                return ex;
        }

        return null;
    }

    private static void ClearLastExceptions()
    {
        Exception?[]? ring = _debugLastExceptions;
        if (ring is not null)
            Array.Clear(ring);
        _debugLastExceptionsNextIndex = 0;
    }
#endif

    extension(Debug)
    {
        [Conditional("DEBUG")]
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
                HResultValidationMode.AllowCdacSuccess => cdacHr == dacHr || (cdacHr < 0 && dacHr < 0) || (cdacHr >= 0 && dacHr < 0),
                _ => cdacHr == dacHr,
            };

            if (!match)
            {
                string message = $"HResult mismatch - cDAC: 0x{unchecked((uint)cdacHr):X8}, DAC: 0x{unchecked((uint)dacHr):X8} ({Path.GetFileName(filePath)}:{lineNumber})";
#if DEBUG
                if (cdacHr < 0)
                {
                    Exception? ex = FindMatchingException(cdacHr);
                    if (ex is not null)
                    {
                        message += $"{Environment.NewLine}---- cDAC exception ----{Environment.NewLine}{ex}{Environment.NewLine}---- end cDAC exception ----";
                    }
                }
#endif
                Debug.Assert(false, message);
            }

#if DEBUG
            ClearLastExceptions();
#endif
        }
    }
}

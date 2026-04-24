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
            Debug.Assert(match, $"HResult mismatch - cDAC: 0x{unchecked((uint)cdacHr):X8}, DAC: 0x{unchecked((uint)dacHr):X8} ({Path.GetFileName(filePath)}:{lineNumber})");
        }

        [Conditional("DEBUG")]
        internal static unsafe void ValidateOutputStringBuffer(
            char* cdacBuffer,
            uint* cdacNeeded,
            char[] dacBuffer,
            uint dacNeeded,
            uint count,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            string location = $"{Path.GetFileName(filePath)}:{lineNumber}";

            if (cdacNeeded is not null && *cdacNeeded != dacNeeded)
            {
                int cdacLen = (int)Math.Min(*cdacNeeded, count);
                string cdacStr = cdacBuffer is not null && cdacLen > 0 ? new string(cdacBuffer, 0, cdacLen - 1) : "<null>";
                string dacStr = dacNeeded > 0 ? new string(dacBuffer, 0, (int)dacNeeded - 1) : "<empty>";
                Trace.TraceWarning($"Output buffer pNeeded mismatch ({location}) - cDAC: {*cdacNeeded} (\"{cdacStr}\"), DAC: {dacNeeded} (\"{dacStr}\")");
            }
            else if (cdacBuffer is not null && dacNeeded > 0 && count >= dacNeeded)
            {
                var cdacSpan = new ReadOnlySpan<char>(cdacBuffer, (int)dacNeeded - 1);
                var dacSpan = new ReadOnlySpan<char>(dacBuffer, 0, (int)dacNeeded - 1);
                if (!cdacSpan.SequenceEqual(dacSpan))
                {
                    Trace.TraceWarning($"Output buffer content mismatch ({location}) - cDAC: \"{new string(cdacBuffer, 0, (int)dacNeeded - 1)}\", DAC: \"{new string(dacBuffer, 0, (int)dacNeeded - 1)}\"");
                }
            }
        }
    }
}

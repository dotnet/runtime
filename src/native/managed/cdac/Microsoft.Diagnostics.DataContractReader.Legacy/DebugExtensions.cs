// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}

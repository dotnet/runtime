// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64;

internal static class LookupValues
{
    /// <summary>
    /// This table describes the size of each unwind code, in bytes, for unwind codes
    /// in the range 0xE0-0xFF.
    /// </summary>
    public static ReadOnlySpan<byte> UnwindCodeSizeTable =>
    [
        4, 1, 2, 1, 1, 1, 1, 3,
        1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1,
        2, 3, 4, 5, 1, 1, 1, 1,
    ];

    /// <summary>
    // This table describes the number of instructions represented by each unwind
    // code in the range 0xE0-0xFF.
    /// </summary>
    public static ReadOnlySpan<byte> UnwindCodeInstructionCountTable =>
    [
        1, 1, 1, 1, 1, 1, 1, 1,    // 0xE0-0xE7
        0,                         // 0xE8 - MSFT_OP_TRAP_FRAME
        0,                         // 0xE9 - MSFT_OP_MACHINE_FRAME
        0,                         // 0xEA - MSFT_OP_CONTEXT
        0,                         // 0xEB - MSFT_OP_EC_CONTEXT / MSFT_OP_RET_TO_GUEST (unused)
        0,                         // 0xEC - MSFT_OP_CLEAR_UNWOUND_TO_CALL
        0,                         // 0XED - MSFT_OP_RET_TO_GUEST_LEAF (unused)
        0, 0,                      // 0xEE-0xEF
        0, 0, 0, 0, 0, 0, 0, 0,    // 0xF0-0xF7
        1, 1, 1, 1, 1, 1, 1, 1,    // 0xF8-0xFF
    ];
}

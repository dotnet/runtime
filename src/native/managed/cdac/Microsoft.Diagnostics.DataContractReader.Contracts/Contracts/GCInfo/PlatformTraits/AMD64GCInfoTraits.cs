// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal class AMD64GCInfoTraits : IGCInfoTraits
{
    public static uint DenormalizeStackBaseRegister(uint reg) => reg ^ 0x5u;
    public static uint DenormalizeCodeLength(uint len) => len;
    public static uint NormalizeCodeLength(uint len) => len;
    public static uint DenormalizeCodeOffset(uint offset) => offset;
    public static uint NormalizeCodeOffset(uint offset) => offset;
    public static int DenormalizeStackSlot(int x) => x << 3;
    public static uint DenormalizeSizeOfStackArea(uint size) => size << 3;

    public static int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE => 6;

    public static int GS_COOKIE_STACK_SLOT_ENCBASE => 6;
    public static int CODE_LENGTH_ENCBASE => 8;

    public static int STACK_BASE_REGISTER_ENCBASE => 3;
    public static int SIZE_OF_STACK_AREA_ENCBASE => 3;
    public static int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE => 4;
    public static int SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE => 0;
    public static int REVERSE_PINVOKE_FRAME_ENCBASE => 6;
    public static int NUM_REGISTERS_ENCBASE => 2;
    public static int NUM_STACK_SLOTS_ENCBASE => 2;
    public static int NUM_UNTRACKED_SLOTS_ENCBASE => 1;
    public static int NORM_PROLOG_SIZE_ENCBASE => 5;
    public static int NORM_EPILOG_SIZE_ENCBASE => 3;
    public static int INTERRUPTIBLE_RANGE_DELTA1_ENCBASE => 6;
    public static int INTERRUPTIBLE_RANGE_DELTA2_ENCBASE => 6;
    public static int REGISTER_ENCBASE => 3;
    public static int REGISTER_DELTA_ENCBASE => 2;
    public static int STACK_SLOT_ENCBASE => 6;
    public static int STACK_SLOT_DELTA_ENCBASE => 4;
    public static int NUM_SAFE_POINTS_ENCBASE => 2;
    public static int NUM_INTERRUPTIBLE_RANGES_ENCBASE => 1;

    public static bool HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA => true;

    // Preserved (non-scratch): rbx(3), rbp(5), rsi(6), rdi(7), r12(12)-r15(15)
    // On Unix ABI, rsi(6) and rdi(7) are scratch, but the GCInfo encoder
    // uses the Windows ABI register numbering for all platforms.
    public static bool IsScratchRegister(uint regNum)
    {
        const uint preservedMask =
            (1u << 3)   // rbx
            | (1u << 5) // rbp
            | (1u << 6) // rsi (Windows ABI)
            | (1u << 7) // rdi (Windows ABI)
            | (1u << 12) // r12
            | (1u << 13) // r13
            | (1u << 14) // r14
            | (1u << 15); // r15
        return (preservedMask & (1u << (int)regNum)) == 0;
    }

    // AMD64 has a fixed stack parameter scratch area (shadow space + outgoing args).
    // Stack slots with GC_SP_REL base and offset in [0, scratchAreaSize) are scratch slots.
    // This matches the native IsScratchStackSlot which computes GetStackSlot and checks
    // pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea.
    public static bool IsScratchStackSlot(int spOffset, uint spBase, uint fixedStackParameterScratchArea)
    {
        // GC_SP_REL = 1
        return spBase == 1
            && spOffset >= 0
            && (uint)spOffset < fixedStackParameterScratchArea;
    }
}

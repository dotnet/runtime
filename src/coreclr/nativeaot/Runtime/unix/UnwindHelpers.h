// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "common.h"

struct REGDISPLAY;

// The effect of the unwind codes of the frame bodies that share unwind information, which StepFrame computes for the
// first of them on arm64 and x64: the locations of the saved integer registers (x19-x30, or rax-r15) and of d8-d15
// from FirstFloat (in Offsets, in 8-byte units) and the caller stack pointer, relative to the frame register or the
// stack pointer of the frame. State also has the size of the prolog and the frame register.
struct BodyUnwindSummary
{
    enum : uint32_t { Computed = 1, Usable = 2, FpBased = 4, Unsupported = 8, PrologSizeShift = 8, PrologSizeMask = 0xFFFF, BaseRegisterShift = 24, FirstFloat = 16 };

    uint32_t State;
    uint32_t SavedMask;
    int32_t CfaOffset;
    int8_t Offsets[FirstFloat + 8];
};

class UnwindHelpers
{
public:
    // Returns the size of the Windows unwind information at pUnwindInfo, which the LSDA follows
    static uint32_t GetUnwindInfoSize(PTR_uint8_t pUnwindInfo);

    // Unwinds the frame of the code from frameStart to frameEnd that the Windows unwind information at
    // pUnwindInfo describes, or with packed the packed arm64 unwind data there, with the portable Windows unwinder.
    // pEpilogStarts is the LSDA of the frame when the epilog start offsets of the unwind information can precede it.
    // pSummary, when not NULL, caches the effect of the unwind information on arm64 and x64. On arm64 pSpForPacSign
    // receives the stack pointer that signed the return address, or 0 when the frame doesn't sign it.
    static bool StepFrame(REGDISPLAY *regs, TADDR frameStart, TADDR frameEnd, PTR_uint8_t pUnwindInfo, bool packed,
        PTR_uint8_t pEpilogStarts, BodyUnwindSummary *pSummary, uintptr_t *pSpForPacSign = NULL);
};

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This file is included also during native runtime compilation by the C++ compiler for verification purposes.
// The static asserts at the end are compiled only when this file is included in native build by c++ compiler. They
// provide compile time verification that all the sizes and offsets match between the managed and native code.

#if !__cplusplus
internal static
#endif
class AsmOffsets
{

    // Offsets / sizes that are different in Release / Debug builds
#if DEBUG
    // Debug build offsets
#if TARGET_AMD64
#if TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x1b90;
    public const int OFFSETOF__REGDISPLAY__SP = 0x1b78;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x1b80;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0xbf0;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbd8;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbe0;
#endif // TARGET_UNIX
#elif TARGET_ARM64
#if TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x9e0;
    public const int OFFSETOF__REGDISPLAY__SP = 0x938;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x940;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x940;
    public const int OFFSETOF__REGDISPLAY__SP = 0x898;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x8a0;
#endif // TARGET_UNIX
#elif TARGET_ARM
    public const int SIZEOF__REGDISPLAY = 0x410;
    public const int OFFSETOF__REGDISPLAY__SP = 0x3ec;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x3f0;
#elif TARGET_X86
    public const int SIZEOF__REGDISPLAY = 0x5fc;
    public const int OFFSETOF__REGDISPLAY__SP = 0x5f0;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x5f4;
#elif TARGET_RISCV64
    public const int SIZEOF__REGDISPLAY = 0x6C0;
    public const int OFFSETOF__REGDISPLAY__SP = 0x628;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x630;
#elif TARGET_LOONGARCH64
    public const int SIZEOF__REGDISPLAY = 0xc60;
    public const int OFFSETOF__REGDISPLAY__SP = 0xba8;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbb0;
#elif TARGET_WASM
    public const int SIZEOF__REGDISPLAY = 0x38;
    public const int OFFSETOF__REGDISPLAY__SP = 0x30;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x34;
#endif

#if TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x8;
#if FEATURE_INTERPRETER
    public const int SIZEOF__StackFrameIterator = 0x170;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x168;
#else
    public const int SIZEOF__StackFrameIterator = 0x150;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x148;
#endif
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x132;
#elif TARGET_X86
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
    public const int SIZEOF__StackFrameIterator = 0x3d0;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x3c2;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x3cc;
#else // TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
#if FEATURE_INTERPRETER
    public const int SIZEOF__StackFrameIterator = 0xd8;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0xd4;
#else
    public const int SIZEOF__StackFrameIterator = 0xc8;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0xc4;
#endif
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0xba;
#endif // TARGET_64BIT

#else // DEBUG
    // Release build offsets
#if TARGET_AMD64
#if TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x1b80;
    public const int OFFSETOF__REGDISPLAY__SP = 0x1b70;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x1b78;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0xbf0;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbd0;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbd8;
#endif // TARGET_UNIX
#elif TARGET_ARM64
#if TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x9d0;
    public const int OFFSETOF__REGDISPLAY__SP = 0x930;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x938;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x930;
    public const int OFFSETOF__REGDISPLAY__SP = 0x890;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x898;
#endif // TARGET_UNIX
#elif TARGET_ARM
    public const int SIZEOF__REGDISPLAY = 0x408;
    public const int OFFSETOF__REGDISPLAY__SP = 0x3e8;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x3ec;
#elif TARGET_X86
    public const int SIZEOF__REGDISPLAY = 0x5f8;
    public const int OFFSETOF__REGDISPLAY__SP = 0x5ec;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x5f0;
#elif TARGET_RISCV64
    public const int SIZEOF__REGDISPLAY = 0x6B0;
    public const int OFFSETOF__REGDISPLAY__SP = 0x620;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x628;
#elif TARGET_LOONGARCH64
    public const int SIZEOF__REGDISPLAY = 0xc50;
    public const int OFFSETOF__REGDISPLAY__SP = 0xba0;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xba8;
#elif TARGET_WASM
    public const int SIZEOF__REGDISPLAY = 0x34;
    public const int OFFSETOF__REGDISPLAY__SP = 0x2c;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x30;
#endif

#if TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x8;
#if FEATURE_INTERPRETER
    public const int SIZEOF__StackFrameIterator = 0x168;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x160;
#else
    public const int SIZEOF__StackFrameIterator = 0x148;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x140;
#endif
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x12a;
#elif TARGET_X86
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
    public const int SIZEOF__StackFrameIterator = 0x3c8;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x3ba;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x3c4;
#else // TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
#if FEATURE_INTERPRETER
    public const int SIZEOF__StackFrameIterator = 0xd0;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0xcc;
#else
    public const int SIZEOF__StackFrameIterator = 0xc0;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0xbc;
#endif
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0xb2;
#endif // TARGET_64BIT

#endif // DEBUG

#if TARGET_AMD64
#if TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0xca0;
#else // TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x4d0;
#endif // TARGET_UNIX
#elif TARGET_ARM64
#if TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x3e0;
#else // TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x390;
#endif // TARGET_UNIX
#elif TARGET_ARM
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x1a0;
#elif TARGET_X86
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x2cc;
#elif TARGET_RISCV64
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x220;
#elif TARGET_LOONGARCH64
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x520;
#elif TARGET_WASM
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x04;
#endif

#if TARGET_AMD64
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0xf8;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0xa0;
#elif TARGET_ARM64
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0x108;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0xf0;
#elif TARGET_ARM
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0x40;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0x30;
#elif TARGET_X86
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0xb8;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0xb4;
#elif TARGET_RISCV64
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0x108;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0x48;
#elif TARGET_LOONGARCH64
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0x108;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0xb8;
#elif TARGET_WASM
    // offset to dummy field
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__IP = 0x04;
    public const int OFFSETOF__PAL_LIMITED_CONTEXT__FP = 0x04;
#endif

    // Offsets / sizes that are different in 64 / 32 bit mode

#if TARGET_64BIT
    public const int SIZEOF__EHEnum = 0x20;
    public const int OFFSETOF__StackFrameIterator__m_pRegDisplay = 0x20;
    public const int OFFSETOF__ExInfo__m_pPrevExInfo = 0;
    public const int OFFSETOF__ExInfo__m_pExContext = 0xa8;
    public const int OFFSETOF__ExInfo__m_exception = 0xb0;
    public const int OFFSETOF__ExInfo__m_kind = 0xb8;
    public const int OFFSETOF__ExInfo__m_passNumber = 0xb9;
    public const int OFFSETOF__ExInfo__m_idxCurClause = 0xbc;
    public const int OFFSETOF__ExInfo__m_frameIter = 0xc0;
    public const int OFFSETOF__ExInfo__m_notifyDebuggerSP = OFFSETOF__ExInfo__m_frameIter + SIZEOF__StackFrameIterator;
#else // TARGET_64BIT
    public const int SIZEOF__EHEnum = 0x10;
    public const int OFFSETOF__StackFrameIterator__m_pRegDisplay = 0x14;
    public const int OFFSETOF__ExInfo__m_pPrevExInfo = 0;
    public const int OFFSETOF__ExInfo__m_pExContext = 0x5c;
    public const int OFFSETOF__ExInfo__m_exception = 0x60;
    public const int OFFSETOF__ExInfo__m_kind = 0x64;
    public const int OFFSETOF__ExInfo__m_passNumber = 0x65;
    public const int OFFSETOF__ExInfo__m_idxCurClause = 0x68;
    public const int OFFSETOF__ExInfo__m_frameIter = 0x6c;
    public const int OFFSETOF__ExInfo__m_notifyDebuggerSP = OFFSETOF__ExInfo__m_frameIter + SIZEOF__StackFrameIterator;
#endif // TARGET_64BIT

#if __cplusplus
    static_assert(sizeof(CONTEXT) == AsmOffsets::SIZEOF__PAL_LIMITED_CONTEXT);
#if TARGET_AMD64
    static_assert(offsetof(CONTEXT, Rip) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, Rbp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_ARM64
    static_assert(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_ARM
    static_assert(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, R11) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_X86
    static_assert(offsetof(CONTEXT, Eip) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, Ebp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_RISCV64
    static_assert(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_LOONGARCH64
    static_assert(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#endif
    static_assert(sizeof(REGDISPLAY) == AsmOffsets::SIZEOF__REGDISPLAY);
    static_assert(offsetof(REGDISPLAY, SP) == AsmOffsets::OFFSETOF__REGDISPLAY__SP);
    static_assert(offsetof(REGDISPLAY, ControlPC) == AsmOffsets::OFFSETOF__REGDISPLAY__ControlPC);
    static_assert(offsetof(REGDISPLAY, pCurrentContext) == AsmOffsets::OFFSETOF__REGDISPLAY__m_pCurrentContext);
    static_assert(sizeof(StackFrameIterator) == AsmOffsets::SIZEOF__StackFrameIterator);
    static_assert(offsetof(StackFrameIterator, m_crawl) + offsetof(CrawlFrame, pRD) == OFFSETOF__StackFrameIterator__m_pRegDisplay);
    static_assert(offsetof(StackFrameIterator, m_isRuntimeWrappedExceptions) == OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions);
    static_assert(offsetof(StackFrameIterator, m_AdjustedControlPC) == OFFSETOF__StackFrameIterator__m_AdjustedControlPC);
    static_assert(sizeof(ExtendedEHClauseEnumerator) == AsmOffsets::SIZEOF__EHEnum);
    static_assert(offsetof(ExInfo, m_pPrevNestedInfo) == OFFSETOF__ExInfo__m_pPrevExInfo);
    static_assert(offsetof(ExInfo, m_pExContext) == OFFSETOF__ExInfo__m_pExContext);
    static_assert(offsetof(ExInfo, m_exception) == OFFSETOF__ExInfo__m_exception);
    static_assert(offsetof(ExInfo, m_kind) == OFFSETOF__ExInfo__m_kind);
    static_assert(offsetof(ExInfo, m_passNumber) == OFFSETOF__ExInfo__m_passNumber);
    static_assert(offsetof(ExInfo, m_idxCurClause) == OFFSETOF__ExInfo__m_idxCurClause);
    static_assert(offsetof(ExInfo, m_frameIter) == OFFSETOF__ExInfo__m_frameIter);
    static_assert(offsetof(ExInfo, m_notifyDebuggerSP) == OFFSETOF__ExInfo__m_notifyDebuggerSP);
#endif

}
#if __cplusplus
;
#endif

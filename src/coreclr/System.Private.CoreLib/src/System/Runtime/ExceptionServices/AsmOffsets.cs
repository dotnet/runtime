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
    public const int SIZEOF__REGDISPLAY = 0x1a90;
    public const int OFFSETOF__REGDISPLAY__SP = 0x1a78;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x1a80;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0xbf0;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbd8;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbe0;
#endif // TARGET_UNIX
#elif TARGET_ARM64
    public const int SIZEOF__REGDISPLAY = 0x940;
    public const int OFFSETOF__REGDISPLAY__SP = 0x898;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x8a0;
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
    public const int SIZEOF__REGDISPLAY = 0xc70;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbb8;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbc0;
#endif

#if TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x8;
    public const int SIZEOF__StackFrameIterator = 0x370;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x352;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x368;
#else // TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
    public const int SIZEOF__StackFrameIterator = 0x2d8;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x2c2;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x2d0;
#endif // TARGET_64BIT

#else // DEBUG
    // Release build offsets
#if TARGET_AMD64
#if TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0x1a80;
    public const int OFFSETOF__REGDISPLAY__SP = 0x1a70;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x1a78;
#else // TARGET_UNIX
    public const int SIZEOF__REGDISPLAY = 0xbe0;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbd0;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbd8;
#endif // TARGET_UNIX
#elif TARGET_ARM64
    public const int SIZEOF__REGDISPLAY = 0x930;
    public const int OFFSETOF__REGDISPLAY__SP = 0x890;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0x898;
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
    public const int SIZEOF__REGDISPLAY = 0xc60;
    public const int OFFSETOF__REGDISPLAY__SP = 0xbb0;
    public const int OFFSETOF__REGDISPLAY__ControlPC = 0xbb8;
#endif

#if TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x8;
    public const int SIZEOF__StackFrameIterator = 0x370;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x34a;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x360;
#else // TARGET_64BIT
    public const int OFFSETOF__REGDISPLAY__m_pCurrentContext = 0x4;
    public const int SIZEOF__StackFrameIterator = 0x2d0;
    public const int OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions = 0x2ba;
    public const int OFFSETOF__StackFrameIterator__m_AdjustedControlPC = 0x2c8;
#endif // TARGET_64BIT

#endif // DEBUG

#if TARGET_AMD64
#if TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0xc20;
#else // TARGET_UNIX
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x4d0;
#endif // TARGET_UNIx
#elif TARGET_ARM64
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x390;
#elif TARGET_ARM
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x1a0;
#elif TARGET_X86
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x2cc;
#elif TARGET_RISCV64
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x220;
#elif TARGET_LOONGARCH64
    public const int SIZEOF__PAL_LIMITED_CONTEXT = 0x520;
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
#endif

    // Offsets / sizes that are different in 64 / 32 bit mode

#if TARGET_64BIT
    public const int SIZEOF__EHEnum = 0x20;
    public const int OFFSETOF__StackFrameIterator__m_pRegDisplay = 0x228;
    public const int OFFSETOF__ExInfo__m_pPrevExInfo = 0;
    public const int OFFSETOF__ExInfo__m_pExContext = 0xc0;
    public const int OFFSETOF__ExInfo__m_exception = 0xc8;
    public const int OFFSETOF__ExInfo__m_kind = 0xd0;
    public const int OFFSETOF__ExInfo__m_passNumber = 0xd1;
    public const int OFFSETOF__ExInfo__m_idxCurClause = 0xd4;
    public const int OFFSETOF__ExInfo__m_frameIter = 0xe0;
    public const int OFFSETOF__ExInfo__m_notifyDebuggerSP = OFFSETOF__ExInfo__m_frameIter + SIZEOF__StackFrameIterator;
#else // TARGET_64BIT
    public const int SIZEOF__EHEnum = 0x10;
    public const int OFFSETOF__StackFrameIterator__m_pRegDisplay = 0x218;
    public const int OFFSETOF__ExInfo__m_pPrevExInfo = 0;
    public const int OFFSETOF__ExInfo__m_pExContext = 0x70;
    public const int OFFSETOF__ExInfo__m_exception = 0x74;
    public const int OFFSETOF__ExInfo__m_kind = 0x78;
    public const int OFFSETOF__ExInfo__m_passNumber = 0x79;
    public const int OFFSETOF__ExInfo__m_idxCurClause = 0x7C;
    public const int OFFSETOF__ExInfo__m_frameIter = 0x80;
    public const int OFFSETOF__ExInfo__m_notifyDebuggerSP = OFFSETOF__ExInfo__m_frameIter + SIZEOF__StackFrameIterator;
#endif // TARGET_64BIT

#if __cplusplus
    static_assert_no_msg(sizeof(CONTEXT) == AsmOffsets::SIZEOF__PAL_LIMITED_CONTEXT);
#if TARGET_AMD64
    static_assert_no_msg(offsetof(CONTEXT, Rip) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, Rbp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_ARM64
    static_assert_no_msg(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_ARM
    static_assert_no_msg(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, R11) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_X86
    static_assert_no_msg(offsetof(CONTEXT, Eip) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, Ebp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_RISCV64
    static_assert_no_msg(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#elif TARGET_LOONGARCH64
    static_assert_no_msg(offsetof(CONTEXT, Pc) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__IP);
    static_assert_no_msg(offsetof(CONTEXT, Fp) == AsmOffsets::OFFSETOF__PAL_LIMITED_CONTEXT__FP);
#endif
    static_assert_no_msg(sizeof(REGDISPLAY) == AsmOffsets::SIZEOF__REGDISPLAY);
    static_assert_no_msg(offsetof(REGDISPLAY, SP) == AsmOffsets::OFFSETOF__REGDISPLAY__SP);
    static_assert_no_msg(offsetof(REGDISPLAY, ControlPC) == AsmOffsets::OFFSETOF__REGDISPLAY__ControlPC);
    static_assert_no_msg(offsetof(REGDISPLAY, pCurrentContext) == AsmOffsets::OFFSETOF__REGDISPLAY__m_pCurrentContext);
    static_assert_no_msg(sizeof(StackFrameIterator) == AsmOffsets::SIZEOF__StackFrameIterator);
    static_assert_no_msg(offsetof(StackFrameIterator, m_crawl) + offsetof(CrawlFrame, pRD) == OFFSETOF__StackFrameIterator__m_pRegDisplay);
    static_assert_no_msg(offsetof(StackFrameIterator, m_isRuntimeWrappedExceptions) == OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions);
    static_assert_no_msg(offsetof(StackFrameIterator, m_AdjustedControlPC) == OFFSETOF__StackFrameIterator__m_AdjustedControlPC);
    static_assert_no_msg(sizeof(ExtendedEHClauseEnumerator) == AsmOffsets::SIZEOF__EHEnum);
    static_assert_no_msg(offsetof(ExInfo, m_pPrevNestedInfo) == OFFSETOF__ExInfo__m_pPrevExInfo);
    static_assert_no_msg(offsetof(ExInfo, m_pExContext) == OFFSETOF__ExInfo__m_pExContext);
    static_assert_no_msg(offsetof(ExInfo, m_exception) == OFFSETOF__ExInfo__m_exception);
    static_assert_no_msg(offsetof(ExInfo, m_kind) == OFFSETOF__ExInfo__m_kind);
    static_assert_no_msg(offsetof(ExInfo, m_passNumber) == OFFSETOF__ExInfo__m_passNumber);
    static_assert_no_msg(offsetof(ExInfo, m_idxCurClause) == OFFSETOF__ExInfo__m_idxCurClause);
    static_assert_no_msg(offsetof(ExInfo, m_frameIter) == OFFSETOF__ExInfo__m_frameIter);
    static_assert_no_msg(offsetof(ExInfo, m_notifyDebuggerSP) == OFFSETOF__ExInfo__m_notifyDebuggerSP);
#endif    

}
#if __cplusplus
;
#endif

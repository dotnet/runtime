// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DBG_TARGET_CONTEXT_INCLUDED
#define __DBG_TARGET_CONTEXT_INCLUDED

#include <dbgportable.h>
#include <stddef.h>
#include "crosscomp.h"

//
// The right side of the debugger can be built to target multiple platforms. This means it is not
// safe to use the CONTEXT structure directly: the context of the platform we're building for might not match
// that of the one the debugger is targeting. So all right side code will use the DT_CONTEXT abstraction
// instead. When the debugger target is the local platform this will just resolve back into CONTEXT, but cross
// platform we'll provide a hand-rolled version.
//

//
// For cross platform cases we also need to provide a helper function for byte-swapping a context structure
// should the endian-ness of the debugger and debuggee platforms differ. This is called ByteSwapContext and is
// obviously a no-op for those cases where the left and right sides agree on storage format.
//
// NOTE: Any changes to the field layout of DT_CONTEXT must be tracked in the associated definition of
// ByteSwapContext.
//

//
// **** NOTE: Keep these in sync with pal/inc/pal.h ****
//

// This odd define pattern is needed because in DBI we set _TARGET_ to match the host and
// DBG_TARGET to control our targeting. In x-plat DBI DBG_TARGET won't match _TARGET_ and
// DBG_TARGET needs to take precedence
#if defined(TARGET_X86)
#define DTCONTEXT_IS_X86
#elif defined (TARGET_AMD64)
#define DTCONTEXT_IS_AMD64
#elif defined (TARGET_ARM)
#define DTCONTEXT_IS_ARM
#elif defined (TARGET_ARM64)
#define DTCONTEXT_IS_ARM64
#elif defined (TARGET_X86)
#define DTCONTEXT_IS_X86
#elif defined (TARGET_AMD64)
#define DTCONTEXT_IS_AMD64
#elif defined (TARGET_ARM)
#define DTCONTEXT_IS_ARM
#elif defined (TARGET_ARM64)
#define DTCONTEXT_IS_ARM64
#elif defined (TARGET_LOONGARCH64)
#define DTCONTEXT_IS_LOONGARCH64
#endif

#if defined(DTCONTEXT_IS_X86)

#define DT_SIZE_OF_80387_REGISTERS      80

#define DT_CONTEXT_i386            0x00010000
#define DT_CONTEXT_CONTROL         (DT_CONTEXT_i386 | 0x00000001L) // SS:SP, CS:IP, FLAGS, BP
#define DT_CONTEXT_INTEGER         (DT_CONTEXT_i386 | 0x00000002L) // AX, BX, CX, DX, SI, DI
#define DT_CONTEXT_SEGMENTS        (DT_CONTEXT_i386 | 0x00000004L)
#define DT_CONTEXT_FLOATING_POINT  (DT_CONTEXT_i386 | 0x00000008L) // 387 state
#define DT_CONTEXT_DEBUG_REGISTERS (DT_CONTEXT_i386 | 0x00000010L)
#define DT_CONTEXT_EXTENDED_REGISTERS  (DT_CONTEXT_i386 | 0x00000020L)

#define DT_CONTEXT_FULL     (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_SEGMENTS)
#define DT_CONTEXT_ALL      (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_SEGMENTS | DT_CONTEXT_FLOATING_POINT | DT_CONTEXT_DEBUG_REGISTERS | DT_CONTEXT_EXTENDED_REGISTERS)

#define DT_MAXIMUM_SUPPORTED_EXTENSION     512

typedef struct {
    DWORD   ControlWord;
    DWORD   StatusWord;
    DWORD   TagWord;
    DWORD   ErrorOffset;
    DWORD   ErrorSelector;
    DWORD   DataOffset;
    DWORD   DataSelector;
    BYTE    RegisterArea[DT_SIZE_OF_80387_REGISTERS];
    DWORD   Cr0NpxState;
} DT_FLOATING_SAVE_AREA;

typedef struct {
    ULONG ContextFlags;

    ULONG   Dr0;
    ULONG   Dr1;
    ULONG   Dr2;
    ULONG   Dr3;
    ULONG   Dr6;
    ULONG   Dr7;

    DT_FLOATING_SAVE_AREA FloatSave;

    ULONG   SegGs;
    ULONG   SegFs;
    ULONG   SegEs;
    ULONG   SegDs;

    ULONG   Edi;
    ULONG   Esi;
    ULONG   Ebx;
    ULONG   Edx;
    ULONG   Ecx;
    ULONG   Eax;

    ULONG   Ebp;
    ULONG   Eip;
    ULONG   SegCs;
    ULONG   EFlags;
    ULONG   Esp;
    ULONG   SegSs;

    UCHAR   ExtendedRegisters[DT_MAXIMUM_SUPPORTED_EXTENSION];

} DT_CONTEXT;

// Since the target is little endian in this case we only have to provide a real implementation of
// ByteSwapContext if the platform we're building on is big-endian.
#ifdef BIGENDIAN
inline void ByteSwapContext(DT_CONTEXT *pContext)
{
    // Our job is simplified since the context has large contiguous ranges with fields of the same size. Keep
    // the following logic in sync with the definition of DT_CONTEXT above.
    BYTE *pbContext = (BYTE*)pContext;

    // The first span consists of 4 byte fields.
    DWORD cbFields = (offsetof(DT_CONTEXT, FloatSave) + offsetof(DT_FLOATING_SAVE_AREA, RegisterArea)) / 4;
    for (DWORD i = 0; i < cbFields; i++)
    {
        ByteSwapPrimitive(pbContext, pbContext, 4);
        pbContext += 4;
    }

    // Then there's a float save area containing 8 byte fields.
    cbFields = sizeof(pContext->FloatSave.RegisterArea);
    for (DWORD i = 0; i < cbFields; i++)
    {
        ByteSwapPrimitive(pbContext, pbContext, 8);
        pbContext += 8;
    }

    // Back to 4 byte fields.
    cbFields = (offsetof(DT_CONTEXT, ExtendedRegisters) - offsetof(DT_CONTEXT, SegGs)) / 4;
    for (DWORD i = 0; i < cbFields; i++)
    {
        ByteSwapPrimitive(pbContext, pbContext, 4);
        pbContext += 4;
    }

    // We don't know the formatting of the extended register area, but the debugger doesn't access this data
    // on the left side, so just leave it in left-side format for now.

    // Validate that we converted up to where we think we did as a hedge against DT_CONTEXT layout changes.
    _PASSERT((pbContext - ((BYTE*)pContext)) == (sizeof(DT_CONTEXT) - sizeof(pContext->ExtendedRegisters)));
}
#else // BIGENDIAN
inline void ByteSwapContext(DT_CONTEXT *pContext)
{
}
#endif // BIGENDIAN

#elif defined(DTCONTEXT_IS_AMD64)

#define DT_CONTEXT_AMD64            0x00100000L

#define DT_CONTEXT_CONTROL          (DT_CONTEXT_AMD64 | 0x00000001L)
#define DT_CONTEXT_INTEGER          (DT_CONTEXT_AMD64 | 0x00000002L)
#define DT_CONTEXT_SEGMENTS         (DT_CONTEXT_AMD64 | 0x00000004L)
#define DT_CONTEXT_FLOATING_POINT   (DT_CONTEXT_AMD64 | 0x00000008L)
#define DT_CONTEXT_DEBUG_REGISTERS  (DT_CONTEXT_AMD64 | 0x00000010L)

#define DT_CONTEXT_FULL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT)
#define DT_CONTEXT_ALL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_SEGMENTS | DT_CONTEXT_FLOATING_POINT | DT_CONTEXT_DEBUG_REGISTERS)

typedef struct  {
    ULONGLONG Low;
    LONGLONG High;
} DT_M128A;

typedef struct  {
    WORD   ControlWord;
    WORD   StatusWord;
    BYTE  TagWord;
    BYTE  Reserved1;
    WORD   ErrorOpcode;
    DWORD ErrorOffset;
    WORD   ErrorSelector;
    WORD   Reserved2;
    DWORD DataOffset;
    WORD   DataSelector;
    WORD   Reserved3;
    DWORD MxCsr;
    DWORD MxCsr_Mask;
    DT_M128A FloatRegisters[8];
    DT_M128A XmmRegisters[16];
    BYTE  Reserved4[96];
} DT_XMM_SAVE_AREA32;

typedef struct DECLSPEC_ALIGN(16) {

    DWORD64 P1Home;
    DWORD64 P2Home;
    DWORD64 P3Home;
    DWORD64 P4Home;
    DWORD64 P5Home;
    DWORD64 P6Home;

    DWORD ContextFlags;
    DWORD MxCsr;

    WORD   SegCs;
    WORD   SegDs;
    WORD   SegEs;
    WORD   SegFs;
    WORD   SegGs;
    WORD   SegSs;
    DWORD EFlags;

    DWORD64 Dr0;
    DWORD64 Dr1;
    DWORD64 Dr2;
    DWORD64 Dr3;
    DWORD64 Dr6;
    DWORD64 Dr7;

    DWORD64 Rax;
    DWORD64 Rcx;
    DWORD64 Rdx;
    DWORD64 Rbx;
    DWORD64 Rsp;
    DWORD64 Rbp;
    DWORD64 Rsi;
    DWORD64 Rdi;
    DWORD64 R8;
    DWORD64 R9;
    DWORD64 R10;
    DWORD64 R11;
    DWORD64 R12;
    DWORD64 R13;
    DWORD64 R14;
    DWORD64 R15;

    DWORD64 Rip;

    union {
        DT_XMM_SAVE_AREA32 FltSave;
        struct {
            DT_M128A Header[2];
            DT_M128A Legacy[8];
            DT_M128A Xmm0;
            DT_M128A Xmm1;
            DT_M128A Xmm2;
            DT_M128A Xmm3;
            DT_M128A Xmm4;
            DT_M128A Xmm5;
            DT_M128A Xmm6;
            DT_M128A Xmm7;
            DT_M128A Xmm8;
            DT_M128A Xmm9;
            DT_M128A Xmm10;
            DT_M128A Xmm11;
            DT_M128A Xmm12;
            DT_M128A Xmm13;
            DT_M128A Xmm14;
            DT_M128A Xmm15;
        };
    };

    DT_M128A VectorRegister[26];
    DWORD64 VectorControl;

    DWORD64 DebugControl;
    DWORD64 LastBranchToRip;
    DWORD64 LastBranchFromRip;
    DWORD64 LastExceptionToRip;
    DWORD64 LastExceptionFromRip;
} DT_CONTEXT;

#elif defined(DTCONTEXT_IS_ARM)

#define DT_CONTEXT_ARM 0x00200000L

#define DT_CONTEXT_CONTROL         (DT_CONTEXT_ARM | 0x1L)
#define DT_CONTEXT_INTEGER         (DT_CONTEXT_ARM | 0x2L)
#define DT_CONTEXT_FLOATING_POINT  (DT_CONTEXT_ARM | 0x4L)
#define DT_CONTEXT_DEBUG_REGISTERS (DT_CONTEXT_ARM | 0x8L)

#define DT_CONTEXT_FULL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT)
#define DT_CONTEXT_ALL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT | DT_CONTEXT_DEBUG_REGISTERS)

#define DT_ARM_MAX_BREAKPOINTS     8
#define DT_ARM_MAX_WATCHPOINTS     1

typedef struct {
    ULONGLONG Low;
    LONGLONG High;
} DT_NEON128;

typedef DECLSPEC_ALIGN(8) struct {

    //
    // Control flags.
    //

    DWORD ContextFlags;

    //
    // Integer registers
    //

    DWORD R0;
    DWORD R1;
    DWORD R2;
    DWORD R3;
    DWORD R4;
    DWORD R5;
    DWORD R6;
    DWORD R7;
    DWORD R8;
    DWORD R9;
    DWORD R10;
    DWORD R11;
    DWORD R12;

    //
    // Control Registers
    //

    DWORD Sp;
    DWORD Lr;
    DWORD Pc;
    DWORD Cpsr;

    //
    // Floating Point/NEON Registers
    //

    DWORD Fpscr;
    DWORD Padding;
    union {
        DT_NEON128 Q[16];
        ULONGLONG D[32];
        DWORD S[32];
    };

    //
    // Debug registers
    //

    DWORD Bvr[DT_ARM_MAX_BREAKPOINTS];
    DWORD Bcr[DT_ARM_MAX_BREAKPOINTS];
    DWORD Wvr[DT_ARM_MAX_WATCHPOINTS];
    DWORD Wcr[DT_ARM_MAX_WATCHPOINTS];

    DWORD Padding2[2];

} DT_CONTEXT;

#elif defined(DTCONTEXT_IS_ARM64)

#define DT_CONTEXT_ARM64 0x00400000L

#define DT_CONTEXT_CONTROL         (DT_CONTEXT_ARM64 | 0x1L)
#define DT_CONTEXT_INTEGER         (DT_CONTEXT_ARM64 | 0x2L)
#define DT_CONTEXT_FLOATING_POINT  (DT_CONTEXT_ARM64 | 0x4L)
#define DT_CONTEXT_DEBUG_REGISTERS (DT_CONTEXT_ARM64 | 0x8L)

#define DT_CONTEXT_FULL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT)
#define DT_CONTEXT_ALL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT | DT_CONTEXT_DEBUG_REGISTERS)

#define DT_ARM64_MAX_BREAKPOINTS     8
#define DT_ARM64_MAX_WATCHPOINTS     2

typedef struct {
    ULONGLONG Low;
    LONGLONG High;
} DT_NEON128;

typedef DECLSPEC_ALIGN(16) struct {
    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //

    /* +0x004 */ DWORD Cpsr;       // NZVF + DAIF + CurrentEL + SPSel
    /* +0x008 */ union {
                    struct {
                        DWORD64 X0;
                        DWORD64 X1;
                        DWORD64 X2;
                        DWORD64 X3;
                        DWORD64 X4;
                        DWORD64 X5;
                        DWORD64 X6;
                        DWORD64 X7;
                        DWORD64 X8;
                        DWORD64 X9;
                        DWORD64 X10;
                        DWORD64 X11;
                        DWORD64 X12;
                        DWORD64 X13;
                        DWORD64 X14;
                        DWORD64 X15;
                        DWORD64 X16;
                        DWORD64 X17;
                        DWORD64 X18;
                        DWORD64 X19;
                        DWORD64 X20;
                        DWORD64 X21;
                        DWORD64 X22;
                        DWORD64 X23;
                        DWORD64 X24;
                        DWORD64 X25;
                        DWORD64 X26;
                        DWORD64 X27;
                        DWORD64 X28;
                    };
                    DWORD64 X[29];
                 };
    /* +0x0f0 */ DWORD64 Fp;
    /* +0x0f8 */ DWORD64 Lr;
    /* +0x100 */ DWORD64 Sp;
    /* +0x108 */ DWORD64 Pc;

    //
    // Floating Point/NEON Registers
    //

    /* +0x110 */ DT_NEON128 V[32];
    /* +0x310 */ DWORD Fpcr;
    /* +0x314 */ DWORD Fpsr;

    //
    // Debug registers
    //

    /* +0x318 */ DWORD Bcr[DT_ARM64_MAX_BREAKPOINTS];
    /* +0x338 */ DWORD64 Bvr[DT_ARM64_MAX_BREAKPOINTS];
    /* +0x378 */ DWORD Wcr[DT_ARM64_MAX_WATCHPOINTS];
    /* +0x380 */ DWORD64 Wvr[DT_ARM64_MAX_WATCHPOINTS];
    /* +0x390 */

} DT_CONTEXT;

#elif defined(DTCONTEXT_IS_LOONGARCH64)
#define DT_CONTEXT_LOONGARCH64 0x00800000L

#define DT_CONTEXT_CONTROL         (DT_CONTEXT_LOONGARCH64 | 0x1L)
#define DT_CONTEXT_INTEGER         (DT_CONTEXT_LOONGARCH64 | 0x2L)
#define DT_CONTEXT_FLOATING_POINT  (DT_CONTEXT_LOONGARCH64 | 0x4L)
#define DT_CONTEXT_DEBUG_REGISTERS (DT_CONTEXT_LOONGARCH64 | 0x8L)

#define DT_CONTEXT_FULL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT)
#define DT_CONTEXT_ALL (DT_CONTEXT_CONTROL | DT_CONTEXT_INTEGER | DT_CONTEXT_FLOATING_POINT | DT_CONTEXT_DEBUG_REGISTERS)

#define DT_LOONGARCH64_MAX_BREAKPOINTS     8
#define DT_LOONGARCH64_MAX_WATCHPOINTS     2

typedef DECLSPEC_ALIGN(16) struct {
    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //
    DWORD64 R0;
    DWORD64 RA;
    DWORD64 TP;
    DWORD64 SP;
    DWORD64 A0;
    DWORD64 A1;
    DWORD64 A2;
    DWORD64 A3;
    DWORD64 A4;
    DWORD64 A5;
    DWORD64 A6;
    DWORD64 A7;
    DWORD64 T0;
    DWORD64 T1;
    DWORD64 T2;
    DWORD64 T3;
    DWORD64 T4;
    DWORD64 T5;
    DWORD64 T6;
    DWORD64 T7;
    DWORD64 T8;
    DWORD64 X0;
    DWORD64 FP;
    DWORD64 S0;
    DWORD64 S1;
    DWORD64 S2;
    DWORD64 S3;
    DWORD64 S4;
    DWORD64 S5;
    DWORD64 S6;
    DWORD64 S7;
    DWORD64 S8;
    DWORD64 PC;

    //
    // Floating Point Registers
    //
    ULONGLONG F[32];
} DT_CONTEXT;

#else
#error Unsupported platform
#endif


#endif // __DBG_TARGET_CONTEXT_INCLUDED

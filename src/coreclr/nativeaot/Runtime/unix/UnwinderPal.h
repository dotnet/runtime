// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The Windows types and definitions that the portable Windows unwinders in src/coreclr/unwinder use, for
// NativeAOT on Unix, which neither has windows.h nor uses the CoreCLR PAL. The CONTEXT and context pointer
// records are the ones of the CoreCLR PAL.

#pragma once

#include "common.h"
#include "daccess.h"
#include "rhassert.h"
#include <sal.h>

typedef int BOOL;
typedef uint8_t BYTE;
typedef uint8_t BOOLEAN;
typedef unsigned char UCHAR;
typedef char CHAR;
typedef int16_t SHORT;
typedef unsigned short USHORT;
typedef uint16_t UINT16;
typedef uint16_t WORD;
typedef uint32_t UINT32;
typedef uint64_t UINT64;
typedef int32_t LONG;
typedef int32_t LONG32;
typedef int64_t LONGLONG;
typedef uint32_t DWORD;
typedef uint32_t ULONG;
// As on Windows, the 64-bit integers are the pointer sized ones on 64-bit targets
#if defined(TARGET_64BIT)
typedef uintptr_t DWORD64;
typedef uintptr_t ULONG64;
#else
typedef uint64_t DWORD64;
typedef uint64_t ULONG64;
#endif
typedef uint64_t ULONGLONG;
typedef uintptr_t ULONG_PTR;
typedef int32_t HRESULT;
typedef int32_t NTSTATUS;
typedef void VOID;
typedef void* PVOID;
typedef BYTE* PBYTE;
typedef DWORD* PDWORD;
typedef ULONG* PULONG;
typedef ULONG64* PULONG64;
typedef DWORD64* PDWORD64;
typedef ULONGLONG* PULONGLONG;
typedef ULONG_PTR* PULONG_PTR;
typedef DPTR(BYTE) PTR_BYTE;
typedef DPTR(DWORD) PTR_DWORD;
typedef DPTR(ULONG) PTR_ULONG;
typedef DPTR(UINT64) PTR_UINT64;
typedef DPTR(ULONG64) PTR_ULONG64;

#ifndef S_OK
#define S_OK 0x0
#endif
#ifndef E_FAIL
#define E_FAIL 0x80004005
#endif
#ifndef SUCCEEDED
#define SUCCEEDED(hr) ((HRESULT)(hr) >= 0)
#define FAILED(hr) ((HRESULT)(hr) < 0)
#endif
#ifndef TRUE
#define TRUE 1
#define FALSE 0
#endif
#define E_UNEXPECTED 0x8000FFFF
#define ERROR_READ_FAULT 30L
#define HRESULT_FROM_WIN32(x) ((HRESULT)(x) <= 0 ? (HRESULT)(x) : (HRESULT)(((x) & 0x0000FFFF) | (7 << 16) | 0x80000000))

#define STATUS_SUCCESS ((NTSTATUS)0x00000000L)
#define STATUS_UNSUCCESSFUL ((NTSTATUS)0xC0000001L)
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)

#define IN
#define OUT
#define OPTIONAL
#define NTAPI
#define NTSYSAPI
#ifndef EXTERN_C
#define EXTERN_C extern "C"
#endif
#ifndef FORCEINLINE
#define FORCEINLINE inline __attribute__((always_inline))
#endif
#define DECLSPEC_ALIGN(x) __attribute__((aligned(x)))
#ifndef UNREFERENCED_PARAMETER
#define UNREFERENCED_PARAMETER(P) (void)(P)
#endif
#ifndef __in
#define __in _In_
#define __out _Out_
#endif
#ifndef FIELD_OFFSET
#define FIELD_OFFSET(type, field) ((LONG)__builtin_offsetof(type, field))
#endif
#define ARGUMENT_PRESENT(ArgumentPointer) ((CHAR*)((ULONG_PTR)(ArgumentPointer)) != (CHAR*)NULL)

#define UNWINDER_ASSERT _ASSERTE

typedef void* PEXCEPTION_ROUTINE;

#if defined(TARGET_AMD64)

#include "../../../inc/win64unwind.h"

typedef DPTR(UNWIND_INFO) PTR_UNWIND_INFO;

struct M128A
{
    ULONG64 Low;
    int64_t High;
};
typedef M128A* PM128A;

// The integer registers are in the order of their numbers, which the unwind codes use as indices
struct CONTEXT
{
    ULONG64 Rax, Rcx, Rdx, Rbx, Rsp, Rbp, Rsi, Rdi;
    ULONG64 R8, R9, R10, R11, R12, R13, R14, R15;
    ULONG64 Rip;
    M128A Xmm0, Xmm1, Xmm2, Xmm3, Xmm4, Xmm5, Xmm6, Xmm7;
    M128A Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15;
};
typedef CONTEXT* PCONTEXT;

struct KNONVOLATILE_CONTEXT_POINTERS
{
    union
    {
        PM128A FloatingContext[16];
    };
    union
    {
        PULONG64 IntegerContext[16];
        struct
        {
            PULONG64 Rax, Rcx, Rdx, Rbx, Rsp, Rbp, Rsi, Rdi;
            PULONG64 R8, R9, R10, R11, R12, R13, R14, R15;
        };
    };
};
typedef KNONVOLATILE_CONTEXT_POINTERS* PKNONVOLATILE_CONTEXT_POINTERS;

struct _IMAGE_RUNTIME_FUNCTION_ENTRY
{
    DWORD BeginAddress;
    DWORD EndAddress;
    DWORD UnwindInfoAddress;
};
typedef _IMAGE_RUNTIME_FUNCTION_ENTRY* _PIMAGE_RUNTIME_FUNCTION_ENTRY;
typedef _IMAGE_RUNTIME_FUNCTION_ENTRY T_RUNTIME_FUNCTION;
typedef _IMAGE_RUNTIME_FUNCTION_ENTRY* PT_RUNTIME_FUNCTION;

#elif defined(TARGET_ARM)


#define CONTEXT_ARM   0x00200000L

// end_wx86

#define CONTEXT_CONTROL (CONTEXT_ARM | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM | 0x2L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_ARM | 0x4L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_ARM | 0x8L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000L
#define CONTEXT_SERVICE_ACTIVE 0x10000000L
#define CONTEXT_EXCEPTION_REQUEST 0x40000000L
#define CONTEXT_EXCEPTION_REPORTING 0x80000000L

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define ARM_MAX_BREAKPOINTS     8
#define ARM_MAX_WATCHPOINTS     1

typedef struct _NEON128 {
    ULONGLONG Low;
    LONGLONG High;
} NEON128, *PNEON128;

typedef struct DECLSPEC_ALIGN(8) _CONTEXT {

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
        NEON128 Q[16];
        ULONGLONG D[32];
        DWORD S[32];
    };

    //
    // Debug registers
    //

    DWORD Bvr[ARM_MAX_BREAKPOINTS];
    DWORD Bcr[ARM_MAX_BREAKPOINTS];
    DWORD Wvr[ARM_MAX_WATCHPOINTS];
    DWORD Wcr[ARM_MAX_WATCHPOINTS];

    DWORD Padding2[2];

} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD R4;
    PDWORD R5;
    PDWORD R6;
    PDWORD R7;
    PDWORD R8;
    PDWORD R9;
    PDWORD R10;
    PDWORD R11;
    PDWORD Lr;

    PULONGLONG D8;
    PULONGLONG D9;
    PULONGLONG D10;
    PULONGLONG D11;
    PULONGLONG D12;
    PULONGLONG D13;
    PULONGLONG D14;
    PULONGLONG D15;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

typedef struct _IMAGE_ARM_RUNTIME_FUNCTION_ENTRY {
    DWORD BeginAddress;
    DWORD EndAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD Ret : 2;
            DWORD H : 1;
            DWORD Reg : 3;
            DWORD R : 1;
            DWORD L : 1;
            DWORD C : 1;
            DWORD StackAdjust : 10;
        };
    };
} IMAGE_ARM_RUNTIME_FUNCTION_ENTRY, * PIMAGE_ARM_RUNTIME_FUNCTION_ENTRY;


typedef struct _RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD UnwindData;
} RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

FORCEINLINE ULONG RtlpGetFunctionEndAddress(PRUNTIME_FUNCTION FunctionEntry, TADDR ImageBase)
{
    ULONG FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0)
        FunctionLength = (FunctionLength >> 2) & 0x7ff;
    else
        FunctionLength = *(PTR_ULONG)(ImageBase + FunctionLength) & 0x3ffff;

    return FunctionEntry->BeginAddress + 2 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry) ((FunctionEntry)->BeginAddress & ~1)

EXTERN_C PEXCEPTION_ROUTINE RtlVirtualUnwind(ULONG HandlerType, ULONG ImageBase, ULONG ControlPc,
    PRUNTIME_FUNCTION FunctionEntry, PCONTEXT ContextRecord, PVOID* HandlerData, PULONG EstablisherFrame,
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);

#elif defined(TARGET_ARM64)


#define CONTEXT_ARM64   0x00400000L

#define CONTEXT_CONTROL (CONTEXT_ARM64 | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM64 | 0x2L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_ARM64 | 0x4L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_ARM64 | 0x8L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000L
#define CONTEXT_SERVICE_ACTIVE 0x10000000L
#define CONTEXT_EXCEPTION_REQUEST 0x40000000L
#define CONTEXT_EXCEPTION_REPORTING 0x80000000L

#define CONTEXT_ARM64_XSTATE (CONTEXT_ARM64 | 0x20L)
#define CONTEXT_XSTATE CONTEXT_ARM64_XSTATE

#define XSTATE_ARM64_SVE (2)
#define XSTATE_MASK_ARM64_SVE (1ULL << (XSTATE_ARM64_SVE))

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

//
// Define initial Cpsr/Fpscr value
//

// begin_ntoshvp

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define ARM64_MAX_BREAKPOINTS     8
#define ARM64_MAX_WATCHPOINTS     2

typedef struct _NEON128 {
    ULONGLONG Low;
    LONGLONG High;
} NEON128, *PNEON128;

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {

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

    /* +0x110 */ NEON128 V[32];
    /* +0x310 */ DWORD Fpcr;
    /* +0x314 */ DWORD Fpsr;

    //
    // Debug registers
    //

    /* +0x318 */ DWORD Bcr[ARM64_MAX_BREAKPOINTS];
    /* +0x338 */ DWORD64 Bvr[ARM64_MAX_BREAKPOINTS];
    /* +0x378 */ DWORD Wcr[ARM64_MAX_WATCHPOINTS];
    /* +0x380 */ DWORD64 Wvr[ARM64_MAX_WATCHPOINTS];

    /* +0x390 */ DWORD64 XStateFeaturesMask;
    /* +0x3a0 */

} CONTEXT, *PCONTEXT, *LPCONTEXT;

// The SVE header immediately follows CONTEXT, when CONTEXT_XSTATE is set and
// XStateFeaturesMask contains XSTATE_MASK_ARM64_SVE.
// If CONTEXT_SVE_STATE_HEADER.VectorLengthInBytes != 0, it is immediately
// followed by the serialized state of the vector/predicate register files,
// and the first-faulting register, padded to 16-byte alignment. The bottom
// 128 bits of each Z register are duplicated in CONTEXT::V in this design.
//
//  +------------------------------------------+
//  |                 CONTEXT                  |
//  +------------------------------------------+
//  | CONTEXT_SVE_STATE_HEADER (16 bytes)      |
//  +------------------------------------------+
//  | Z0..Z31 (32 * VL bytes)                  |
//  +------------------------------------------+
//  | P0..P15 (16 * VL/8 bytes)                |
//  +------------------------------------------+
//  | FFR (VL/8 bytes)                         |
//  +------------------------------------------+
//  | padding to 16-byte alignment             |
//  +------------------------------------------+
//
// TODO-SVE: We should update this layout to conform with Windows XSTATE.
typedef struct DECLSPEC_ALIGN(16) _CONTEXT_SVE_STATE_HEADER
{
    DWORD VectorLengthInBytes;
    DWORD Reserved[3];
} CONTEXT_SVE_STATE_HEADER, *PCONTEXT_SVE_STATE_HEADER;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 X19;
    PDWORD64 X20;
    PDWORD64 X21;
    PDWORD64 X22;
    PDWORD64 X23;
    PDWORD64 X24;
    PDWORD64 X25;
    PDWORD64 X26;
    PDWORD64 X27;
    PDWORD64 X28;
    PDWORD64 Fp;
    PDWORD64 Lr;

    PDWORD64 D8;
    PDWORD64 D9;
    PDWORD64 D10;
    PDWORD64 D11;
    PDWORD64 D12;
    PDWORD64 D13;
    PDWORD64 D14;
    PDWORD64 D15;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

typedef struct _IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY {
    DWORD BeginAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD RegF : 3;
            DWORD RegI : 4;
            DWORD H : 1;
            DWORD CR : 2;
            DWORD FrameSize : 9;
        };
    };
} IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY, * PIMAGE_ARM64_RUNTIME_FUNCTION_ENTRY;

typedef union IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA {
    ULONG HeaderData;
    struct {
        ULONG FunctionLength : 18;      // in words (2 bytes)
        ULONG Version : 2;
        ULONG ExceptionDataPresent : 1;
        ULONG EpilogInHeader : 1;
        ULONG EpilogCount : 5;          // number of epilogs or byte index of the first unwind code for the one only epilog
        ULONG CodeWords : 5;            // number of dwords with unwind codes
    };
} IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY_XDATA;


typedef IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

FORCEINLINE ULONG64 RtlpGetFunctionEndAddress(PRUNTIME_FUNCTION FunctionEntry, ULONG64 ImageBase)
{
    ULONG64 FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0)
        FunctionLength = (FunctionLength & 7) == 3 ? 3 : (FunctionLength >> 2) & 0x7ff;
    else
        FunctionLength = *(PTR_UINT64)(ImageBase + FunctionLength) & 0x3ffff;

    return FunctionEntry->BeginAddress + 4 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry) ((FunctionEntry)->BeginAddress)

EXTERN_C PEXCEPTION_ROUTINE RtlVirtualUnwindWithSpForPacSign(ULONG HandlerType, ULONG64 ImageBase, ULONG64 ControlPc,
    PRUNTIME_FUNCTION FunctionEntry, PCONTEXT ContextRecord, PVOID* HandlerData, PULONG64 EstablisherFrame,
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers, PULONG64 SpForPacSign);

#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#if defined(TARGET_LOONGARCH64)

// Please refer to src/coreclr/pal/src/arch/loongarch64/asmconstants.h
#define CONTEXT_LOONGARCH64   0x00800000

#define CONTEXT_CONTROL (CONTEXT_LOONGARCH64 | 0x1)
#define CONTEXT_INTEGER (CONTEXT_LOONGARCH64 | 0x2)
#define CONTEXT_FLOATING_POINT  (CONTEXT_LOONGARCH64 | 0x4)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_LOONGARCH64 | 0x8)
#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_LSX 0x10
#define CONTEXT_LASX 0x20

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000
#define CONTEXT_SERVICE_ACTIVE 0x10000000
#define CONTEXT_EXCEPTION_REQUEST 0x40000000
#define CONTEXT_EXCEPTION_REPORTING 0x80000000

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

// begin_ntoshvp

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, LOONGARCH64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers.
    //
    DWORD64 R0;
    DWORD64 Ra;
    DWORD64 Tp;
    DWORD64 Sp;
    DWORD64 A0;//DWORD64 V0;
    DWORD64 A1;//DWORD64 V1;
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
    DWORD64 Fp;
    DWORD64 S0;
    DWORD64 S1;
    DWORD64 S2;
    DWORD64 S3;
    DWORD64 S4;
    DWORD64 S5;
    DWORD64 S6;
    DWORD64 S7;
    DWORD64 S8;
    DWORD64 Pc;

    //
    // Floating Point Registers: FPR64/LSX/LASX.
    //
    ULONGLONG F[4*32];
    DWORD64 Fcc;
    DWORD Fcsr;
} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 S0;
    PDWORD64 S1;
    PDWORD64 S2;
    PDWORD64 S3;
    PDWORD64 S4;
    PDWORD64 S5;
    PDWORD64 S6;
    PDWORD64 S7;
    PDWORD64 S8;
    PDWORD64 Fp;
    PDWORD64 Ra;

    PDWORD64 F24;
    PDWORD64 F25;
    PDWORD64 F26;
    PDWORD64 F27;
    PDWORD64 F28;
    PDWORD64 F29;
    PDWORD64 F30;
    PDWORD64 F31;
} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

#else

// Please refer to src/coreclr/pal/src/arch/riscv64/asmconstants.h
#define CONTEXT_RISCV64 0x01000000L

#define CONTEXT_CONTROL (CONTEXT_RISCV64 | 0x1)
#define CONTEXT_INTEGER (CONTEXT_RISCV64 | 0x2)
#define CONTEXT_FLOATING_POINT  (CONTEXT_RISCV64 | 0x4)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_RISCV64 | 0x8)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000
#define CONTEXT_SERVICE_ACTIVE 0x10000000
#define CONTEXT_EXCEPTION_REQUEST 0x40000000
#define CONTEXT_EXCEPTION_REPORTING 0x80000000

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

// begin_ntoshvp

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, RISCV64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers.
    //
    DWORD64 R0;
    DWORD64 Ra;
    DWORD64 Sp;
    DWORD64 Gp;
    DWORD64 Tp;
    DWORD64 T0;
    DWORD64 T1;
    DWORD64 T2;
    DWORD64 Fp;
    DWORD64 S1;
    DWORD64 A0;
    DWORD64 A1;
    DWORD64 A2;
    DWORD64 A3;
    DWORD64 A4;
    DWORD64 A5;
    DWORD64 A6;
    DWORD64 A7;
    DWORD64 S2;
    DWORD64 S3;
    DWORD64 S4;
    DWORD64 S5;
    DWORD64 S6;
    DWORD64 S7;
    DWORD64 S8;
    DWORD64 S9;
    DWORD64 S10;
    DWORD64 S11;
    DWORD64 T3;
    DWORD64 T4;
    DWORD64 T5;
    DWORD64 T6;
    DWORD64 Pc;

    //
    // Floating Point Registers
    //
    // TODO-RISCV64: support the SIMD.
    ULONGLONG F[32];
    DWORD Fcsr;
} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 S1;
    PDWORD64 S2;
    PDWORD64 S3;
    PDWORD64 S4;
    PDWORD64 S5;
    PDWORD64 S6;
    PDWORD64 S7;
    PDWORD64 S8;
    PDWORD64 S9;
    PDWORD64 S10;
    PDWORD64 S11;
    PDWORD64 Fp;
    PDWORD64 Gp;
    PDWORD64 Tp;
    PDWORD64 Ra;

    PDWORD64 F8;
    PDWORD64 F9;
    PDWORD64 F18;
    PDWORD64 F19;
    PDWORD64 F20;
    PDWORD64 F21;
    PDWORD64 F22;
    PDWORD64 F23;
    PDWORD64 F24;
    PDWORD64 F25;
    PDWORD64 F26;
    PDWORD64 F27;
} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

#endif

typedef struct _RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD UnwindData;
} RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

FORCEINLINE ULONG64 RtlpGetFunctionEndAddress(PRUNTIME_FUNCTION FunctionEntry, ULONG64 ImageBase)
{
    ULONG64 FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0)
        FunctionLength = (FunctionLength >> 2) & 0x7ff;
    else
        FunctionLength = *(PTR_UINT64)(ImageBase + FunctionLength) & 0x3ffff;

#if defined(TARGET_LOONGARCH64)
    return FunctionEntry->BeginAddress + 4 * FunctionLength;
#else
    return FunctionEntry->BeginAddress + 2 * FunctionLength;
#endif
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry) ((FunctionEntry)->BeginAddress)

EXTERN_C PEXCEPTION_ROUTINE RtlVirtualUnwind(ULONG HandlerType, ULONG64 ImageBase, ULONG64 ControlPc,
    PRUNTIME_FUNCTION FunctionEntry, PCONTEXT ContextRecord, PVOID* HandlerData, PULONG64 EstablisherFrame,
    PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);

#else
#error "The Windows unwinder isn't ported to this architecture."
#endif

#if !defined(TARGET_AMD64)
typedef struct _UNWIND_INFO {
    // dummy
} UNWIND_INFO, *PUNWIND_INFO;

#define UNW_FLAG_NHANDLER 0x0
#define UNW_FLAG_EHANDLER 0x1
#define UNW_FLAG_UHANDLER 0x2

typedef CONTEXT T_CONTEXT;
typedef PCONTEXT PT_CONTEXT;
typedef KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS;
typedef PKNONVOLATILE_CONTEXT_POINTERS PT_KNONVOLATILE_CONTEXT_POINTERS;
typedef RUNTIME_FUNCTION T_RUNTIME_FUNCTION;
typedef PRUNTIME_FUNCTION PT_RUNTIME_FUNCTION;
typedef RUNTIME_FUNCTION _IMAGE_RUNTIME_FUNCTION_ENTRY;
typedef PRUNTIME_FUNCTION _PIMAGE_RUNTIME_FUNCTION_ENTRY;
#endif

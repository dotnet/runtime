// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

//
// This header contains binder-generated data structures that the runtime consumes.
//
#include "daccess.h" // DPTR
#include "TargetPtrs.h"

class MethodTable;

#ifdef TARGET_ARM
// Note for ARM: try and keep the flags in the low 16-bits, since they're not easy to load into a register in
// a single instruction within our stubs.
enum PInvokeTransitionFrameFlags
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\arm\AsmMacros.h

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_R4        = 0x00000001,
    PTFF_SAVE_R5        = 0x00000002,
    PTFF_SAVE_R6        = 0x00000004,
    PTFF_SAVE_R7        = 0x00000008,   // should never be used, we require FP frames for methods with
                                        // pinvoke and it is saved into the frame pointer field instead
    PTFF_SAVE_R8        = 0x00000010,
    PTFF_SAVE_R9        = 0x00000020,
    PTFF_SAVE_R10       = 0x00000040,
    PTFF_SAVE_SP        = 0x00000100,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                        // PInvokes are required to have a frame pointers, but methods which
                                        // call runtime helpers are not.  Therefore, methods that call runtime
                                        // helpers may need SP to seed the stackwalk.

    // scratch registers
    PTFF_SAVE_R0        = 0x00000200,
    PTFF_SAVE_R1        = 0x00000400,
    PTFF_SAVE_R2        = 0x00000800,
    PTFF_SAVE_R3        = 0x00001000,
    PTFF_SAVE_LR        = 0x00002000,   // this is useful for the case of loop hijacking where we need both
                                        // a return address pointing into the hijacked method and that method's
                                        // lr register, which may hold a gc pointer

    PTFF_THREAD_HIJACK  = 0x00004000,   // indicates that this is a frame for a hijacked call
};
#elif defined(TARGET_ARM64)
enum PInvokeTransitionFrameFlags : uint64_t
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\arm64\AsmMacros.h

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_X19       = 0x0000000000000001,
    PTFF_SAVE_X20       = 0x0000000000000002,
    PTFF_SAVE_X21       = 0x0000000000000004,
    PTFF_SAVE_X22       = 0x0000000000000008,
    PTFF_SAVE_X23       = 0x0000000000000010,
    PTFF_SAVE_X24       = 0x0000000000000020,
    PTFF_SAVE_X25       = 0x0000000000000040,
    PTFF_SAVE_X26       = 0x0000000000000080,
    PTFF_SAVE_X27       = 0x0000000000000100,
    PTFF_SAVE_X28       = 0x0000000000000200,

    PTFF_SAVE_SP        = 0x0000000000000400,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                                // PInvokes are required to have a frame pointers, but methods which
                                                // call runtime helpers are not.  Therefore, methods that call runtime
                                                // helpers may need SP to seed the stackwalk.

    // Scratch registers
    PTFF_SAVE_X0        = 0x0000000000000800,
    PTFF_SAVE_X1        = 0x0000000000001000,
    PTFF_SAVE_X2        = 0x0000000000002000,
    PTFF_SAVE_X3        = 0x0000000000004000,
    PTFF_SAVE_X4        = 0x0000000000008000,
    PTFF_SAVE_X5        = 0x0000000000010000,
    PTFF_SAVE_X6        = 0x0000000000020000,
    PTFF_SAVE_X7        = 0x0000000000040000,
    PTFF_SAVE_X8        = 0x0000000000080000,
    PTFF_SAVE_X9        = 0x0000000000100000,
    PTFF_SAVE_X10       = 0x0000000000200000,
    PTFF_SAVE_X11       = 0x0000000000400000,
    PTFF_SAVE_X12       = 0x0000000000800000,
    PTFF_SAVE_X13       = 0x0000000001000000,
    PTFF_SAVE_X14       = 0x0000000002000000,
    PTFF_SAVE_X15       = 0x0000000004000000,
    PTFF_SAVE_X16       = 0x0000000008000000,
    PTFF_SAVE_X17       = 0x0000000010000000,
    PTFF_SAVE_X18       = 0x0000000020000000,

    PTFF_SAVE_FP        = 0x0000000040000000,   // should never be used, we require FP frames for methods with
                                                // pinvoke and it is saved into the frame pointer field instead

    PTFF_SAVE_LR        = 0x0000000080000000,   // this is useful for the case of loop hijacking where we need both
                                                // a return address pointing into the hijacked method and that method's
                                                // lr register, which may hold a gc pointer

    PTFF_THREAD_HIJACK  = 0x0000000100000000,   // indicates that this is a frame for a hijacked call
};

#elif defined(TARGET_LOONGARCH64)
enum PInvokeTransitionFrameFlags : uint64_t
{
    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_R23       = 0x0000000000000001,
    PTFF_SAVE_R24       = 0x0000000000000002,
    PTFF_SAVE_R25       = 0x0000000000000004,
    PTFF_SAVE_R26       = 0x0000000000000008,
    PTFF_SAVE_R27       = 0x0000000000000010,
    PTFF_SAVE_R28       = 0x0000000000000020,
    PTFF_SAVE_R29       = 0x0000000000000040,
    PTFF_SAVE_R30       = 0x0000000000000080,
    PTFF_SAVE_R31       = 0x0000000000000100,

    PTFF_SAVE_SP        = 0x0000000000000200,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                                // PInvokes are required to have a frame pointers, but methods which
                                                // call runtime helpers are not.  Therefore, methods that call runtime
                                                // helpers may need SP to seed the stackwalk.

    // Scratch registers
    PTFF_SAVE_R0        = 0x0000000000000400,
    PTFF_SAVE_R4        = 0x0000000000000800,
    PTFF_SAVE_R5        = 0x0000000000001000,
    PTFF_SAVE_R6        = 0x0000000000002000,
    PTFF_SAVE_R7        = 0x0000000000004000,
    PTFF_SAVE_R8        = 0x0000000000008000,
    PTFF_SAVE_R9        = 0x0000000000010000,
    PTFF_SAVE_R10       = 0x0000000000020000,
    PTFF_SAVE_R11       = 0x0000000000040000,
    PTFF_SAVE_R12       = 0x0000000000080000,
    PTFF_SAVE_R13       = 0x0000000000100000,
    PTFF_SAVE_R14       = 0x0000000000200000,
    PTFF_SAVE_R15       = 0x0000000000400000,
    PTFF_SAVE_R16       = 0x0000000000800000,
    PTFF_SAVE_R17       = 0x0000000001000000,
    PTFF_SAVE_R18       = 0x0000000002000000,
    PTFF_SAVE_R19       = 0x0000000004000000,
    PTFF_SAVE_R20       = 0x0000000008000000,
    PTFF_SAVE_R21       = 0x0000000010000000,

    PTFF_SAVE_FP        = 0x0000000020000000,   // should never be used, we require FP frames for methods with
                                                // pinvoke and it is saved into the frame pointer field instead

    PTFF_SAVE_RA        = 0x0000000040000000,   // this is useful for the case of loop hijacking where we need both
                                                // a return address pointing into the hijacked method and that method's
                                                // ra register, which may hold a gc pointer

    PTFF_THREAD_HIJACK  = 0x0000000080000000,   // indicates that this is a frame for a hijacked call
};

#elif defined(TARGET_RISCV64)
enum PInvokeTransitionFrameFlags : uint64_t
{
    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp)

    // standard preserved registers
    PTFF_SAVE_S1        = 0x0000000000000001,
    PTFF_SAVE_S2        = 0x0000000000000002,
    PTFF_SAVE_S3        = 0x0000000000000004,
    PTFF_SAVE_S4        = 0x0000000000000008,
    PTFF_SAVE_S5        = 0x0000000000000010,
    PTFF_SAVE_S6        = 0x0000000000000020,
    PTFF_SAVE_S7        = 0x0000000000000040,
    PTFF_SAVE_S8        = 0x0000000000000080,
    PTFF_SAVE_S9        = 0x0000000000000100,
    PTFF_SAVE_S10       = 0x0000000000000200,
    PTFF_SAVE_S11       = 0x0000000000000400,

    PTFF_SAVE_SP        = 0x0000000000000800,

    // Scratch registers
    PTFF_SAVE_R0        = 0x0000000000001000,
    PTFF_SAVE_GP        = 0x0000000000002000,
    PTFF_SAVE_A0        = 0x0000000000004000,
    PTFF_SAVE_A1        = 0x0000000000008000,
    PTFF_SAVE_A2        = 0x0000000000010000,
    PTFF_SAVE_A3        = 0x0000000000020000,
    PTFF_SAVE_A4        = 0x0000000000040000,
    PTFF_SAVE_A5        = 0x0000000000080000,
    PTFF_SAVE_A6        = 0x0000000000100000,
    PTFF_SAVE_A7        = 0x0000000000200000,
    PTFF_SAVE_T0        = 0x0000000000400000,
    PTFF_SAVE_T1        = 0x0000000000800000,
    PTFF_SAVE_T2        = 0x0000000001000000,
    PTFF_SAVE_T3        = 0x0000000002000000,
    PTFF_SAVE_T4        = 0x0000000004000000,
    PTFF_SAVE_T5        = 0x0000000008000000,
    PTFF_SAVE_T6        = 0x0000000010000000,

    PTFF_SAVE_FP        = 0x0000000020000000,

    PTFF_SAVE_RA        = 0x0000000040000000,

    PTFF_THREAD_HIJACK  = 0x0000000080000000,   // indicates that this is a frame for a hijacked call
};

#else // TARGET_ARM
enum PInvokeTransitionFrameFlags
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\[amd64|i386]\AsmMacros.inc

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_RBX       = 0x00000001,
    PTFF_SAVE_RSI       = 0x00000002,
    PTFF_SAVE_RDI       = 0x00000004,
    PTFF_SAVE_RBP       = 0x00000008,   // should never be used, we require RBP frames for methods with
                                        // pinvoke and it is saved into the frame pointer field instead
    PTFF_SAVE_R12       = 0x00000010,
    PTFF_SAVE_R13       = 0x00000020,
    PTFF_SAVE_R14       = 0x00000040,
    PTFF_SAVE_R15       = 0x00000080,

    PTFF_SAVE_RSP       = 0x00008000,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                        // PInvokes are required to have a frame pointers, but methods which
                                        // call runtime helpers are not.  Therefore, methods that call runtime
                                        // helpers may need RSP to seed the stackwalk.
                                        //
                                        // NOTE: despite the fact that this flag's bit is out of order, it is
                                        // still expected to be saved here after the preserved registers and
                                        // before the scratch registers
    PTFF_SAVE_RAX       = 0x00000100,
    PTFF_SAVE_RCX       = 0x00000200,
    PTFF_SAVE_RDX       = 0x00000400,
    PTFF_SAVE_R8        = 0x00000800,
    PTFF_SAVE_R9        = 0x00001000,
    PTFF_SAVE_R10       = 0x00002000,
    PTFF_SAVE_R11       = 0x00004000,

#if defined(TARGET_X86)
    PTFF_RAX_IS_GCREF   = 0x00010000,   // used by hijack handler to report return value of hijacked method
    PTFF_RAX_IS_BYREF   = 0x00020000,
#endif

    PTFF_THREAD_HIJACK  = 0x00100000,   // indicates that this is a frame for a hijacked call
};
#endif // TARGET_ARM

#pragma warning(push)
#pragma warning(disable:4200) // nonstandard extension used: zero-sized array in struct/union
class Thread;
#if defined(FEATURE_PORTABLE_HELPERS)
//the members of this structure are currently unused except m_pThread and exist only to allow compilation
//of StackFrameIterator their values are not currently being filled in and will require significant rework
//in order to satisfy the runtime requirements of StackFrameIterator
struct PInvokeTransitionFrame
{
    void*       m_RIP;
    Thread*     m_pThread;  // unused by stack crawler, this is so GetThread is only called once per method
                            // can be an invalid pointer in universal transition cases (which never need to call GetThread)
    uint32_t    m_Flags;    // PInvokeTransitionFrameFlags
};
#else // FEATURE_PORTABLE_HELPERS
struct PInvokeTransitionFrame
{
#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // The FP and LR registers are pushed in different order when setting up frames
    TgtPTR_Void     m_FramePointer;
    TgtPTR_Void     m_RIP;
#else
    TgtPTR_Void     m_RIP;
    TgtPTR_Void     m_FramePointer;
#endif
    TgtPTR_Thread   m_pThread;  // unused by stack crawler, this is so GetThread is only called once per method
                                // can be an invalid pointer in universal transition cases (which never need to call GetThread)
#ifdef TARGET_ARM64
    uint64_t          m_Flags;  // PInvokeTransitionFrameFlags
#elif TARGET_LOONGARCH64 || TARGET_RISCV64
    uint64_t          m_Flags;  // PInvokeTransitionFrameFlags
#else
    uint32_t          m_Flags;  // PInvokeTransitionFrameFlags
#endif
    UIntTarget      m_PreservedRegs[];
};
#endif // FEATURE_PORTABLE_HELPERS
#pragma warning(pop)

#ifdef TARGET_AMD64
#define OFFSETOF__Thread__m_pTransitionFrame 0x48
#elif defined(TARGET_ARM64)
#define OFFSETOF__Thread__m_pTransitionFrame 0x48
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
#define OFFSETOF__Thread__m_pTransitionFrame 0x48
#elif defined(TARGET_X86)
#define OFFSETOF__Thread__m_pTransitionFrame 0x30
#elif defined(TARGET_ARM)
#define OFFSETOF__Thread__m_pTransitionFrame 0x30
#endif

typedef DPTR(MethodTable) PTR_EEType;
typedef DPTR(PTR_EEType) PTR_PTR_EEType;

// Blobs are opaque data passed from the compiler, through the binder and into the native image. At runtime we
// provide a simple API to retrieve these blobs (they're keyed by a simple integer ID). Blobs are passed to
// the binder from the compiler and stored in native images by the binder in a sequential stream, each blob
// having the following header.
struct BlobHeader
{
    uint32_t m_flags;  // Flags describing the blob (used by the binder only at the moment)
    uint32_t m_id;     // Unique identifier of the blob (used to access the blob at runtime)
                     // also used by BlobTypeFieldPreInit to identify (at bind time) which field to pre-init.
    uint32_t m_size;   // Size of the individual blob excluding this header (DWORD aligned)
};

enum RhEHClauseKind
{
    RH_EH_CLAUSE_TYPED              = 0,
    RH_EH_CLAUSE_FAULT              = 1,
    RH_EH_CLAUSE_FILTER             = 2,
    RH_EH_CLAUSE_UNUSED             = 3
};

#define RH_EH_CLAUSE_TYPED_INDIRECT RH_EH_CLAUSE_UNUSED


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef TARGET_H_
#define TARGET_H_

// Native Varargs are not supported on Unix (all architectures) and Windows ARM
#if defined(TARGET_WINDOWS) && !defined(TARGET_ARM)
#define FEATURE_VARARG 1
#else
#define FEATURE_VARARG 0
#endif

/*****************************************************************************/
// The following are human readable names for the target architectures
#if defined(TARGET_X86)
#define TARGET_READABLE_NAME "X86"
#elif defined(TARGET_AMD64)
#define TARGET_READABLE_NAME "AMD64"
#elif defined(TARGET_ARM)
#define TARGET_READABLE_NAME "ARM"
#elif defined(TARGET_ARM64)
#define TARGET_READABLE_NAME "ARM64"
#else
#error Unsupported or unset target architecture
#endif

/*****************************************************************************/
// The following are intended to capture only those #defines that cannot be replaced
// with static const members of Target
#if defined(TARGET_XARCH)
#define REGMASK_BITS 32
#define CSE_CONST_SHARED_LOW_BITS 16

#elif defined(TARGET_ARM)
#define REGMASK_BITS 64
#define CSE_CONST_SHARED_LOW_BITS 12

#elif defined(TARGET_ARM64)
#define REGMASK_BITS 64
#define CSE_CONST_SHARED_LOW_BITS 12

#else
#error Unsupported or unset target architecture
#endif

//------------------------------------------------------------------------
//
// Each register list in register.h must declare REG_STK as the last value.
// In the following enum declarations, the following REG_XXX are created beyond
// the "real" registers:
//    REG_STK          - Used to indicate something evaluated onto the stack.
//    ACTUAL_REG_COUNT - The number of physical registers. (same as REG_STK).
//    REG_COUNT        - The number of physical register + REG_STK. This is the count of values that may
//                       be assigned during register allocation.
//    REG_NA           - Used to indicate that a register is either not yet assigned or not required.
//
#if defined(TARGET_ARM)
enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned __int64
{
    RBM_NONE = 0,
#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_ARM64)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, xname, wname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned __int64
{
    RBM_NONE = 0,
#define REGDEF(name, rnum, mask, xname, wname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_AMD64)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned
{
    RBM_NONE = 0,

#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_X86)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned
{
    RBM_NONE = 0,

#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#else
#error Unsupported target architecture
#endif

/*****************************************************************************/

// TODO-Cleanup: The types defined below are mildly confusing: why are there both?
// regMaskSmall is large enough to represent the entire set of registers.
// If regMaskSmall is smaller than a "natural" integer type, regMaskTP is wider, based
// on a belief by the original authors of the JIT that in some situations it is more
// efficient to have the wider representation.  This belief should be tested, and if it
// is false, then we should coalesce these two types into one (the Small width, probably).
// In any case, we believe that is OK to freely cast between these types; no information will
// be lost.

#ifdef TARGET_ARMARCH
typedef unsigned __int64 regMaskTP;
#else
typedef unsigned       regMaskTP;
#endif

#if REGMASK_BITS == 8
typedef unsigned char regMaskSmall;
#define REG_MASK_INT_FMT "%02X"
#define REG_MASK_ALL_FMT "%02X"
#elif REGMASK_BITS == 16
typedef unsigned short regMaskSmall;
#define REG_MASK_INT_FMT "%04X"
#define REG_MASK_ALL_FMT "%04X"
#elif REGMASK_BITS == 32
typedef unsigned regMaskSmall;
#define REG_MASK_INT_FMT "%08X"
#define REG_MASK_ALL_FMT "%08X"
#else
typedef unsigned __int64 regMaskSmall;
#define REG_MASK_INT_FMT "%04llX"
#define REG_MASK_ALL_FMT "%016llX"
#endif

typedef _regNumber_enum regNumber;
typedef unsigned char   regNumberSmall;

/*****************************************************************************/

#define LEA_AVAILABLE 1
#define SCALED_ADDR_MODES 1

/*****************************************************************************/

#ifdef DEBUG
#define DSP_SRC_OPER_LEFT 0
#define DSP_SRC_OPER_RIGHT 1
#define DSP_DST_OPER_LEFT 1
#define DSP_DST_OPER_RIGHT 0
#endif

/*****************************************************************************/

// The pseudorandom nop insertion is not necessary for current scenarios
// #define PSEUDORANDOM_NOP_INSERTION

/*****************************************************************************/

// clang-format off
#if defined(TARGET_X86)

  #define CPU_LOAD_STORE_ARCH      0
  #define ROUND_FLOAT              1       // round intermed float expression results
  #define CPU_HAS_BYTE_REGS        1

  // TODO-CQ: Fine tune the following xxBlk threshold values:

  #define CPBLK_UNROLL_LIMIT       64      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     128     // Upper bound to let the code generator to loop unroll InitBlk.
  #define CPOBJ_NONGC_SLOTS_LIMIT  4       // For CpObj code generation, this is the the threshold of the number
                                           // of contiguous non-gc slots that trigger generating rep movsq instead of
                                           // sequences of movsq instructions

#ifdef FEATURE_SIMD
  #define ALIGN_SIMD_TYPES         1       // whether SIMD type locals are to be aligned
#endif // FEATURE_SIMD

  #define FEATURE_FIXED_OUT_ARGS   0       // X86 uses push instructions to pass args
  #define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  0  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL     0       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        0       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when
                                           // the flags need to be set
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (double).
  #define MAX_PASS_MULTIREG_BYTES       0  // No multireg arguments
  #define MAX_RET_MULTIREG_BYTES        8  // Maximum size of a struct that could be returned in more than one register

  #define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass an argument.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.

#ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS
  #define NOGC_WRITE_BARRIERS      1       // We have specialized WriteBarrier JIT Helpers that DO-NOT trash the
                                           // RBM_CALLEE_TRASH registers
#else
  #define NOGC_WRITE_BARRIERS      0       // Do not modify this -- modify the definition above.  (If we're not using
                                           // ASM barriers we definitely don't have NOGC barriers).
#endif
  #define USER_ARGS_COME_LAST      0
  #define EMIT_TRACK_STACK_DEPTH   1
  #define TARGET_POINTER_SIZE      4       // equal to sizeof(void*) and the managed pointer size in bytes for this
                                           // target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter,
                                           // filter-handler, fault) and directly execute 'finally' clauses.

  #define FEATURE_EH_CALLFINALLY_THUNKS 0  // Generate call-to-finally code in "thunks" in the enclosing EH region,
                                           // protected by "cloned finally" clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use EBP as a scratch register and must create EBP based
                                           // frames for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_EAX
  #define REG_INT_FIRST            REG_EAX
  #define REG_INT_LAST             REG_EDI
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  #define REG_FP_FIRST             REG_XMM0
  #define REG_FP_LAST              REG_XMM7
  #define FIRST_FP_ARGREG          REG_XMM0
  #define LAST_FP_ARGREG           REG_XMM3
  #define REG_FLTARG_0             REG_XMM0
  #define REG_FLTARG_1             REG_XMM1
  #define REG_FLTARG_2             REG_XMM2
  #define REG_FLTARG_3             REG_XMM3

  #define RBM_FLTARG_0             RBM_XMM0
  #define RBM_FLTARG_1             RBM_XMM1
  #define RBM_FLTARG_2             RBM_XMM2
  #define RBM_FLTARG_3             RBM_XMM3

  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3)

  #define RBM_ALLFLOAT            (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM3 | RBM_XMM4 | RBM_XMM5 | RBM_XMM6 | RBM_XMM7)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT

  // TODO-CQ: Currently we are following the x86 ABI for SSE2 registers.
  // This should be reconsidered.
  #define RBM_FLT_CALLEE_SAVED     RBM_NONE
  #define RBM_FLT_CALLEE_TRASH     RBM_ALLFLOAT
  #define REG_VAR_ORDER_FLT        REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5, REG_XMM6, REG_XMM7

  #define REG_FLT_CALLEE_SAVED_FIRST   REG_XMM6
  #define REG_FLT_CALLEE_SAVED_LAST    REG_XMM7

  #define XMM_REGSIZE_BYTES        16      // XMM register size in bytes
  #define YMM_REGSIZE_BYTES        32      // YMM register size in bytes

  #define REGNUM_BITS              6       // number of bits in a REG_*

  #define REGSIZE_BYTES            4       // number of bytes in one register
  #define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

  #define CODE_ALIGN               1       // code alignment requirement
#if !defined(UNIX_X86_ABI)
  #define STACK_ALIGN              4       // stack alignment requirement
  #define STACK_ALIGN_SHIFT        2       // Shift-right amount to convert size in bytes to size in STACK_ALIGN units == log2(STACK_ALIGN)
#else
  #define STACK_ALIGN              16      // stack alignment requirement
  #define STACK_ALIGN_SHIFT        4       // Shift-right amount to convert size in bytes to size in STACK_ALIGN units == log2(STACK_ALIGN)
#endif // !UNIX_X86_ABI

  #define RBM_INT_CALLEE_SAVED    (RBM_EBX|RBM_ESI|RBM_EDI)
  #define RBM_INT_CALLEE_TRASH    (RBM_EAX|RBM_ECX|RBM_EDX)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)

  #define REG_VAR_ORDER            REG_EAX,REG_EDX,REG_ECX,REG_ESI,REG_EDI,REG_EBX
  #define MAX_VAR_ORDER_SIZE       6

  // The order here is fixed: it must agree with an order assumed in eetwain...
  #define REG_CALLEE_SAVED_ORDER   REG_EDI,REG_ESI,REG_EBX,REG_EBP
  #define RBM_CALLEE_SAVED_ORDER   RBM_EDI,RBM_ESI,RBM_EBX,RBM_EBP

  #define CNT_CALLEE_SAVED        (4)
  #define CNT_CALLEE_TRASH        (3)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)

  #define CNT_CALLEE_SAVED_FLOAT  (0)
  #define CNT_CALLEE_TRASH_FLOAT  (6)

  #define CALLEE_SAVED_REG_MAXSZ  (CNT_CALLEE_SAVED*REGSIZE_BYTES)  // EBX,ESI,EDI,EBP

  #define REG_LNGARG_LO             REG_EAX
  #define RBM_LNGARG_LO             RBM_EAX
  #define REG_LNGARG_HI             REG_EDX
  #define RBM_LNGARG_HI             RBM_EDX
  // register to hold shift amount
  #define REG_SHIFT                REG_ECX
  #define RBM_SHIFT                RBM_ECX

  // register to hold shift amount when shifting 64-bit values
  #define REG_SHIFT_LNG            REG_ECX
  #define RBM_SHIFT_LNG            RBM_ECX

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_EAX

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_EAX
  #define RBM_EXCEPTION_OBJECT     RBM_EAX

  // Only used on ARM for GTF_CALL_M_VIRTSTUB_REL_INDIRECT
  #define REG_JUMP_THUNK_PARAM     REG_EAX
  #define RBM_JUMP_THUNK_PARAM     RBM_EAX

#if NOGC_WRITE_BARRIERS
  #define REG_WRITE_BARRIER        REG_EDX
  #define RBM_WRITE_BARRIER        RBM_EDX

  // We don't allow using ebp as a source register. Maybe we should only prevent this for ETW_EBP_FRAMED (but that is always set right now).
  #define RBM_WRITE_BARRIER_SRC    (RBM_EAX|RBM_ECX|RBM_EBX|RBM_ESI|RBM_EDI)

  #define RBM_CALLEE_TRASH_NOGC    RBM_EDX
#endif // NOGC_WRITE_BARRIERS

  // GenericPInvokeCalliHelper unmanaged target parameter
  #define REG_PINVOKE_TARGET_PARAM REG_EAX
  #define RBM_PINVOKE_TARGET_PARAM RBM_EAX

  // GenericPInvokeCalliHelper cookie parameter
  #define REG_PINVOKE_COOKIE_PARAM REG_STK

  // IL stub's secret parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM    REG_EAX
  #define RBM_SECRET_STUB_PARAM    RBM_EAX

  // VSD target address register
  #define REG_VIRTUAL_STUB_TARGET  REG_EAX
  #define RBM_VIRTUAL_STUB_TARGET  RBM_EAX

  // Registers used by PInvoke frame setup
  #define REG_PINVOKE_FRAME        REG_EDI      // EDI is p/invoke "Frame" pointer argument to CORINFO_HELP_INIT_PINVOKE_FRAME helper
  #define RBM_PINVOKE_FRAME        RBM_EDI
  #define REG_PINVOKE_TCB          REG_ESI      // ESI is set to Thread Control Block (TCB) on return from
                                                // CORINFO_HELP_INIT_PINVOKE_FRAME helper
  #define RBM_PINVOKE_TCB          RBM_ESI
  #define REG_PINVOKE_SCRATCH      REG_EAX      // EAX is trashed by CORINFO_HELP_INIT_PINVOKE_FRAME helper
  #define RBM_PINVOKE_SCRATCH      RBM_EAX

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_EAX
  #define REG_INT_FIRST            REG_EAX
  #define REG_INT_LAST             REG_EDI
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_EAX
  #define RBM_INTRET               RBM_EAX
  #define RBM_LNGRET              (RBM_EDX|RBM_EAX)
  #define REG_LNGRET_LO            REG_EAX
  #define RBM_LNGRET_LO            RBM_EAX
  #define REG_LNGRET_HI            REG_EDX
  #define RBM_LNGRET_HI            RBM_EDX

  #define REG_FLOATRET             REG_NA
  #define RBM_FLOATRET             RBM_NONE
  #define RBM_DOUBLERET            RBM_NONE

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper
  #define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper. On x86, this helper has a custom calling
  // convention that takes EDI as argument (but doesn't trash it), trashes EAX, and returns ESI.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  (RBM_PINVOKE_SCRATCH | RBM_PINVOKE_TCB)

  #define REG_FPBASE               REG_EBP
  #define RBM_FPBASE               RBM_EBP
  #define STR_FPBASE               "ebp"
  #define REG_SPBASE               REG_ESP
  #define RBM_SPBASE               RBM_ESP
  #define STR_SPBASE               "esp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved EBP and return address

  #define MAX_REG_ARG              2

  #define MAX_FLOAT_REG_ARG        0
  #define REG_ARG_FIRST            REG_ECX
  #define REG_ARG_LAST             REG_EDX
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_ECX
  #define REG_ARG_1                REG_EDX

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];

  #define RBM_ARG_0                RBM_ECX
  #define RBM_ARG_1                RBM_EDX

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1)

  // The registers trashed by profiler enter/leave/tailcall hook
  // See vm\i386\asmhelpers.asm for more details.
  #define RBM_PROFILER_ENTER_TRASH     RBM_NONE
  #define RBM_PROFILER_LEAVE_TRASH     RBM_NONE
  #define RBM_PROFILER_TAILCALL_TRASH  (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)

  // What sort of reloc do we use for [disp32] address mode
  #define IMAGE_REL_BASED_DISP32   IMAGE_REL_BASED_HIGHLOW

  // What sort of reloc to we use for 'moffset' address mode (for 'mov eax, moffset' or 'mov moffset, eax')
  #define IMAGE_REL_BASED_MOFFSET  IMAGE_REL_BASED_HIGHLOW

  // Pointer-sized string move instructions
  #define INS_movsp                INS_movsd
  #define INS_r_movsp              INS_r_movsd
  #define INS_stosp                INS_stosd
  #define INS_r_stosp              INS_r_stosd

  // Any stack pointer adjustments larger than this (in bytes) when setting up outgoing call arguments
  // requires a stack probe. Set it large enough so all normal stack arguments don't get a probe.
  #define ARG_STACK_PROBE_THRESHOLD_BYTES 1024

  // The number of bytes from the end the last probed page that must also be probed, to allow for some
  // small SP adjustments without probes. If zero, then the stack pointer can point to the last byte/word
  // on the stack guard page, and must be touched before any further "SUB SP".
  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES ARG_STACK_PROBE_THRESHOLD_BYTES

  #define REG_STACK_PROBE_HELPER_ARG   REG_EAX
  #define RBM_STACK_PROBE_HELPER_ARG   RBM_EAX

  #define RBM_STACK_PROBE_HELPER_TRASH RBM_NONE

#elif defined(TARGET_AMD64)
  // TODO-AMD64-CQ: Fine tune the following xxBlk threshold values:

  #define CPU_LOAD_STORE_ARCH      0
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

  #define CPBLK_UNROLL_LIMIT       64      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     128     // Upper bound to let the code generator to loop unroll InitBlk.
  #define CPOBJ_NONGC_SLOTS_LIMIT  4       // For CpObj code generation, this is the the threshold of the number
                                           // of contiguous non-gc slots that trigger generating rep movsq instead of
                                           // sequences of movsq instructions

#ifdef FEATURE_SIMD
  #define ALIGN_SIMD_TYPES         1       // whether SIMD type locals are to be aligned
#if defined(UNIX_AMD64_ABI)
  #define FEATURE_PARTIAL_SIMD_CALLEE_SAVE 0 // Whether SIMD registers are partially saved at calls
#else // !UNIX_AMD64_ABI
  #define FEATURE_PARTIAL_SIMD_CALLEE_SAVE 1 // Whether SIMD registers are partially saved at calls
#endif // !UNIX_AMD64_ABI
#endif
  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_FASTTAILCALL     1       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     1       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        0       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when the flags need to be set
  #define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (double).
#ifdef    UNIX_AMD64_ABI
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  1  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_STRUCT_CLASSIFIER     1  // Uses a classifier function to determine if structs are passed/returned in more than one register
  #define MAX_PASS_MULTIREG_BYTES      32  // Maximum size of a struct that could be passed in more than one register (Max is two SIMD16s)
  #define MAX_RET_MULTIREG_BYTES       32  // Maximum size of a struct that could be returned in more than one register  (Max is two SIMD16s)
  #define MAX_ARG_REG_COUNT             2  // Maximum registers used to pass a single argument in multiple registers.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.
#else // !UNIX_AMD64_ABI
  #define WINDOWS_AMD64_ABI                // Uses the Windows ABI for AMD64
  #define FEATURE_MULTIREG_ARGS_OR_RET  0  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          0  // Support for returning a single value in more than one register
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  0  // True when we want to promote fields of a multireg struct into registers
  #define MAX_PASS_MULTIREG_BYTES       0  // No multireg arguments
  #define MAX_RET_MULTIREG_BYTES        0  // No multireg return values
  #define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass a single argument (no arguments are passed using multiple registers)
  #define MAX_RET_REG_COUNT             1  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.
                                           // Note that this must be greater than 1 so that GenTreeLclVar can have an array of
                                           // MAX_MULTIREG_COUNT - 1.
#endif // !UNIX_AMD64_ABI

  #define NOGC_WRITE_BARRIERS      0       // We DO-NOT have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1
  #define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define FEATURE_EH_CALLFINALLY_THUNKS 1  // Generate call-to-finally code in "thunks" in the enclosing EH region, protected by "cloned finally" clauses.
#ifdef    UNIX_AMD64_ABI
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use EBP as a scratch register and must create EBP based frames for most methods
#else // !UNIX_AMD64_ABI
  #define ETW_EBP_FRAMED           0       // if 1 we cannot use EBP as a scratch register and must create EBP based frames for most methods
#endif // !UNIX_AMD64_ABI
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  #define RBM_ALLFLOAT            (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM3 | RBM_XMM4 | RBM_XMM5 | RBM_XMM6 | RBM_XMM7 | RBM_XMM8 | RBM_XMM9 | RBM_XMM10 | RBM_XMM11 | RBM_XMM12 | RBM_XMM13 | RBM_XMM14 | RBM_XMM15)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT
  #define REG_FP_FIRST             REG_XMM0
  #define REG_FP_LAST              REG_XMM15
  #define FIRST_FP_ARGREG          REG_XMM0

#ifdef    UNIX_AMD64_ABI
  #define LAST_FP_ARGREG        REG_XMM7
#else // !UNIX_AMD64_ABI
  #define LAST_FP_ARGREG        REG_XMM3
#endif // !UNIX_AMD64_ABI

  #define REGNUM_BITS              6       // number of bits in a REG_*
  #define REGMASK_BITS             32      // number of bits in a REGNUM_MASK
  #define REGSIZE_BYTES            8       // number of bytes in one register
  #define XMM_REGSIZE_BYTES        16      // XMM register size in bytes
  #define YMM_REGSIZE_BYTES        32      // YMM register size in bytes

  #define CODE_ALIGN               1       // code alignment requirement
  #define STACK_ALIGN              16      // stack alignment requirement
  #define STACK_ALIGN_SHIFT        4       // Shift-right amount to convert size in bytes to size in STACK_ALIGN units == log2(STACK_ALIGN)

#if ETW_EBP_FRAMED
  #define RBM_ETW_FRAMED_EBP        RBM_NONE
  #define RBM_ETW_FRAMED_EBP_LIST
  #define REG_ETW_FRAMED_EBP_LIST
  #define REG_ETW_FRAMED_EBP_COUNT  0
#else // !ETW_EBP_FRAMED
  #define RBM_ETW_FRAMED_EBP        RBM_EBP
  #define RBM_ETW_FRAMED_EBP_LIST   RBM_EBP,
  #define REG_ETW_FRAMED_EBP_LIST   REG_EBP,
  #define REG_ETW_FRAMED_EBP_COUNT  1
#endif // !ETW_EBP_FRAMED

#ifdef UNIX_AMD64_ABI
  #define MIN_ARG_AREA_FOR_CALL   0       // Minimum required outgoing argument space for a call.

  #define RBM_INT_CALLEE_SAVED    (RBM_EBX|RBM_ETW_FRAMED_EBP|RBM_R12|RBM_R13|RBM_R14|RBM_R15)
  #define RBM_INT_CALLEE_TRASH    (RBM_EAX|RBM_RDI|RBM_RSI|RBM_EDX|RBM_ECX|RBM_R8|RBM_R9|RBM_R10|RBM_R11)
  #define RBM_FLT_CALLEE_SAVED    (0)
  #define RBM_FLT_CALLEE_TRASH    (RBM_XMM0|RBM_XMM1|RBM_XMM2|RBM_XMM3|RBM_XMM4|RBM_XMM5|RBM_XMM6|RBM_XMM7| \
                                   RBM_XMM8|RBM_XMM9|RBM_XMM10|RBM_XMM11|RBM_XMM12|RBM_XMM13|RBM_XMM14|RBM_XMM15)
  #define REG_PROFILER_ENTER_ARG_0 REG_R14
  #define RBM_PROFILER_ENTER_ARG_0 RBM_R14
  #define REG_PROFILER_ENTER_ARG_1 REG_R15
  #define RBM_PROFILER_ENTER_ARG_1 RBM_R15

  #define REG_DEFAULT_PROFILER_CALL_TARGET REG_R11

#else // !UNIX_AMD64_ABI
#define MIN_ARG_AREA_FOR_CALL     (4 * REGSIZE_BYTES)       // Minimum required outgoing argument space for a call.

  #define RBM_INT_CALLEE_SAVED    (RBM_EBX|RBM_ESI|RBM_EDI|RBM_ETW_FRAMED_EBP|RBM_R12|RBM_R13|RBM_R14|RBM_R15)
  #define RBM_INT_CALLEE_TRASH    (RBM_EAX|RBM_ECX|RBM_EDX|RBM_R8|RBM_R9|RBM_R10|RBM_R11)
  #define RBM_FLT_CALLEE_SAVED    (RBM_XMM6|RBM_XMM7|RBM_XMM8|RBM_XMM9|RBM_XMM10|RBM_XMM11|RBM_XMM12|RBM_XMM13|RBM_XMM14|RBM_XMM15)
  #define RBM_FLT_CALLEE_TRASH    (RBM_XMM0|RBM_XMM1|RBM_XMM2|RBM_XMM3|RBM_XMM4|RBM_XMM5)
#endif // !UNIX_AMD64_ABI

  #define REG_FLT_CALLEE_SAVED_FIRST   REG_XMM6
  #define REG_FLT_CALLEE_SAVED_LAST    REG_XMM15

  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)
  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)

  #define RBM_CALLEE_TRASH_NOGC   RBM_CALLEE_TRASH

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)

#if 0
#define REG_VAR_ORDER            REG_EAX,REG_EDX,REG_ECX,REG_ESI,REG_EDI,REG_EBX,REG_ETW_FRAMED_EBP_LIST \
                                 REG_R8,REG_R9,REG_R10,REG_R11,REG_R14,REG_R15,REG_R12,REG_R13
#else
  // TEMPORARY ORDER TO AVOID CALLEE-SAVES
  // TODO-CQ: Review this and set appropriately
#ifdef UNIX_AMD64_ABI
  #define REG_VAR_ORDER          REG_EAX,REG_EDI,REG_ESI, \
                                 REG_EDX,REG_ECX,REG_R8,REG_R9, \
                                 REG_R10,REG_R11,REG_EBX,REG_ETW_FRAMED_EBP_LIST \
                                 REG_R14,REG_R15,REG_R12,REG_R13
#else // !UNIX_AMD64_ABI
  #define REG_VAR_ORDER          REG_EAX,REG_EDX,REG_ECX, \
                                 REG_R8,REG_R9,REG_R10,REG_R11, \
                                 REG_ESI,REG_EDI,REG_EBX,REG_ETW_FRAMED_EBP_LIST \
                                 REG_R14,REG_R15,REG_R12,REG_R13
#endif // !UNIX_AMD64_ABI
#endif

  #define REG_VAR_ORDER_FLT      REG_XMM0,REG_XMM1,REG_XMM2,REG_XMM3,REG_XMM4,REG_XMM5,REG_XMM6,REG_XMM7,REG_XMM8,REG_XMM9,REG_XMM10,REG_XMM11,REG_XMM12,REG_XMM13,REG_XMM14,REG_XMM15

#ifdef UNIX_AMD64_ABI
  #define CNT_CALLEE_SAVED         (5 + REG_ETW_FRAMED_EBP_COUNT)
  #define CNT_CALLEE_TRASH         (9)
  #define CNT_CALLEE_ENREG         (CNT_CALLEE_SAVED)

  #define CNT_CALLEE_SAVED_FLOAT   (0)
  #define CNT_CALLEE_TRASH_FLOAT   (16)

  #define REG_CALLEE_SAVED_ORDER   REG_EBX,REG_ETW_FRAMED_EBP_LIST REG_R12,REG_R13,REG_R14,REG_R15
  #define RBM_CALLEE_SAVED_ORDER   RBM_EBX,RBM_ETW_FRAMED_EBP_LIST RBM_R12,RBM_R13,RBM_R14,RBM_R15
#else // !UNIX_AMD64_ABI
  #define CNT_CALLEE_SAVED         (7 + REG_ETW_FRAMED_EBP_COUNT)
  #define CNT_CALLEE_TRASH         (7)
  #define CNT_CALLEE_ENREG         (CNT_CALLEE_SAVED)

  #define CNT_CALLEE_SAVED_FLOAT   (10)
  #define CNT_CALLEE_TRASH_FLOAT   (6)

  #define REG_CALLEE_SAVED_ORDER   REG_EBX,REG_ESI,REG_EDI,REG_ETW_FRAMED_EBP_LIST REG_R12,REG_R13,REG_R14,REG_R15
  #define RBM_CALLEE_SAVED_ORDER   RBM_EBX,RBM_ESI,RBM_EDI,RBM_ETW_FRAMED_EBP_LIST RBM_R12,RBM_R13,RBM_R14,RBM_R15
#endif // !UNIX_AMD64_ABI

  #define CALLEE_SAVED_REG_MAXSZ   (CNT_CALLEE_SAVED*REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ (CNT_CALLEE_SAVED_FLOAT*16)

  // register to hold shift amount
  #define REG_SHIFT                REG_ECX
  #define RBM_SHIFT                RBM_ECX

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_EAX

// Where is the exception object on entry to the handler block?
#ifdef UNIX_AMD64_ABI
  #define REG_EXCEPTION_OBJECT     REG_ESI
  #define RBM_EXCEPTION_OBJECT     RBM_ESI
#else // !UNIX_AMD64_ABI
  #define REG_EXCEPTION_OBJECT     REG_EDX
  #define RBM_EXCEPTION_OBJECT     RBM_EDX
#endif // !UNIX_AMD64_ABI

  #define REG_JUMP_THUNK_PARAM     REG_EAX
  #define RBM_JUMP_THUNK_PARAM     RBM_EAX

  // Register to be used for emitting helper calls whose call target is an indir of an
  // absolute memory address in case of Rel32 overflow i.e. a data address could not be
  // encoded as PC-relative 32-bit offset.
  //
  // Notes:
  // 1) that RAX is callee trash register that is not used for passing parameter and
  //    also results in smaller instruction encoding.
  // 2) Profiler Leave callback requires the return value to be preserved
  //    in some form.  We can use custom calling convention for Leave callback.
  //    For e.g return value could be preserved in rcx so that it is available for
  //    profiler.
  #define REG_DEFAULT_HELPER_CALL_TARGET    REG_RAX
  #define RBM_DEFAULT_HELPER_CALL_TARGET    RBM_RAX

  // GenericPInvokeCalliHelper VASigCookie Parameter
  #define REG_PINVOKE_COOKIE_PARAM          REG_R11
  #define RBM_PINVOKE_COOKIE_PARAM          RBM_R11

  // GenericPInvokeCalliHelper unmanaged target Parameter
  #define REG_PINVOKE_TARGET_PARAM          REG_R10
  #define RBM_PINVOKE_TARGET_PARAM          RBM_R10

  // IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM    REG_R10
  #define RBM_SECRET_STUB_PARAM    RBM_R10

  // Registers used by PInvoke frame setup
  #define REG_PINVOKE_FRAME        REG_EDI
  #define RBM_PINVOKE_FRAME        RBM_EDI
  #define REG_PINVOKE_TCB          REG_EAX
  #define RBM_PINVOKE_TCB          RBM_EAX
  #define REG_PINVOKE_SCRATCH      REG_EAX
  #define RBM_PINVOKE_SCRATCH      RBM_EAX

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_EAX
  #define REG_INT_FIRST            REG_EAX
  #define REG_INT_LAST             REG_R15
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_EAX
  #define RBM_INTRET               RBM_EAX

  #define RBM_LNGRET               RBM_EAX

#ifdef UNIX_AMD64_ABI
    #define REG_INTRET_1           REG_RDX
    #define RBM_INTRET_1           RBM_RDX

    #define REG_LNGRET_1           REG_RDX
    #define RBM_LNGRET_1           RBM_RDX
#endif // UNIX_AMD64_ABI


  #define REG_FLOATRET             REG_XMM0
  #define RBM_FLOATRET             RBM_XMM0
  #define REG_DOUBLERET            REG_XMM0
  #define RBM_DOUBLERET            RBM_XMM0

#ifdef UNIX_AMD64_ABI
#define REG_FLOATRET_1             REG_XMM1
#define RBM_FLOATRET_1             RBM_XMM1

#define REG_DOUBLERET_1            REG_XMM1
#define RBM_DOUBLERET_1            RBM_XMM1
#endif // UNIX_AMD64_ABI

  #define REG_FPBASE               REG_EBP
  #define RBM_FPBASE               RBM_EBP
  #define STR_FPBASE               "rbp"
  #define REG_SPBASE               REG_ESP
  #define RBM_SPBASE               RBM_ESP
  #define STR_SPBASE               "rsp"

  #define FIRST_ARG_STACK_OFFS     (REGSIZE_BYTES)   // return address

#ifdef UNIX_AMD64_ABI
  #define MAX_REG_ARG              6
  #define MAX_FLOAT_REG_ARG        8
  #define REG_ARG_FIRST            REG_EDI
  #define REG_ARG_LAST             REG_R9
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_EDI
  #define REG_ARG_1                REG_ESI
  #define REG_ARG_2                REG_EDX
  #define REG_ARG_3                REG_ECX
  #define REG_ARG_4                REG_R8
  #define REG_ARG_5                REG_R9

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];
  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define RBM_ARG_0                RBM_RDI
  #define RBM_ARG_1                RBM_RSI
  #define RBM_ARG_2                RBM_EDX
  #define RBM_ARG_3                RBM_ECX
  #define RBM_ARG_4                RBM_R8
  #define RBM_ARG_5                RBM_R9
#else // !UNIX_AMD64_ABI
  #define MAX_REG_ARG              4
  #define MAX_FLOAT_REG_ARG        4
  #define REG_ARG_FIRST            REG_ECX
  #define REG_ARG_LAST             REG_R9
  #define INIT_ARG_STACK_SLOT      4                  // 4 outgoing reserved stack slots

  #define REG_ARG_0                REG_ECX
  #define REG_ARG_1                REG_EDX
  #define REG_ARG_2                REG_R8
  #define REG_ARG_3                REG_R9

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];
  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define RBM_ARG_0                RBM_ECX
  #define RBM_ARG_1                RBM_EDX
  #define RBM_ARG_2                RBM_R8
  #define RBM_ARG_3                RBM_R9
#endif // !UNIX_AMD64_ABI

  #define REG_FLTARG_0             REG_XMM0
  #define REG_FLTARG_1             REG_XMM1
  #define REG_FLTARG_2             REG_XMM2
  #define REG_FLTARG_3             REG_XMM3

  #define RBM_FLTARG_0             RBM_XMM0
  #define RBM_FLTARG_1             RBM_XMM1
  #define RBM_FLTARG_2             RBM_XMM2
  #define RBM_FLTARG_3             RBM_XMM3

#ifdef UNIX_AMD64_ABI
  #define REG_FLTARG_4             REG_XMM4
  #define REG_FLTARG_5             REG_XMM5
  #define REG_FLTARG_6             REG_XMM6
  #define REG_FLTARG_7             REG_XMM7

  #define RBM_FLTARG_4             RBM_XMM4
  #define RBM_FLTARG_5             RBM_XMM5
  #define RBM_FLTARG_6             RBM_XMM6
  #define RBM_FLTARG_7             RBM_XMM7

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3|RBM_ARG_4|RBM_ARG_5)
  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3|RBM_FLTARG_4|RBM_FLTARG_5|RBM_FLTARG_6|RBM_FLTARG_7)
#else // !UNIX_AMD64_ABI
  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3)
  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3)
#endif // !UNIX_AMD64_ABI

  // The registers trashed by profiler enter/leave/tailcall hook
  // See vm\amd64\asmhelpers.asm for more details.
  #define RBM_PROFILER_ENTER_TRASH     RBM_CALLEE_TRASH
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper.
#ifdef UNIX_AMD64_ABI
  // See vm\amd64\unixasmhelpers.S for more details.
  //
  // On Unix a struct of size >=9 and <=16 bytes in size is returned in two return registers.
  // The return registers could be any two from the set { RAX, RDX, XMM0, XMM1 }.
  // STOP_FOR_GC helper preserves all the 4 possible return registers.
  #define RBM_STOP_FOR_GC_TRASH     (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET | RBM_FLOATRET_1 | RBM_INTRET_1))
  #define RBM_PROFILER_LEAVE_TRASH  (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET | RBM_FLOATRET_1 | RBM_INTRET_1))
#else
  // See vm\amd64\asmhelpers.asm for more details.
  #define RBM_STOP_FOR_GC_TRASH     (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET))
  #define RBM_PROFILER_LEAVE_TRASH  (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET))
#endif

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

  // What sort of reloc do we use for [disp32] address mode
  #define IMAGE_REL_BASED_DISP32   IMAGE_REL_BASED_REL32

  // What sort of reloc to we use for 'moffset' address mode (for 'mov eax, moffset' or 'mov moffset, eax')
  #define IMAGE_REL_BASED_MOFFSET  IMAGE_REL_BASED_DIR64

  // Pointer-sized string move instructions
  #define INS_movsp                INS_movsq
  #define INS_r_movsp              INS_r_movsq
  #define INS_stosp                INS_stosq
  #define INS_r_stosp              INS_r_stosq

  // AMD64 uses FEATURE_FIXED_OUT_ARGS so this can be zero.
  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

  #define REG_STACK_PROBE_HELPER_ARG   REG_R11
  #define RBM_STACK_PROBE_HELPER_ARG   RBM_R11

#ifdef TARGET_UNIX
  #define RBM_STACK_PROBE_HELPER_TRASH RBM_NONE
#else // !TARGET_UNIX
  #define RBM_STACK_PROBE_HELPER_TRASH RBM_RAX
#endif // !TARGET_UNIX

#elif defined(TARGET_ARM)

  // TODO-ARM-CQ: Use shift for division by power of 2
  // TODO-ARM-CQ: Check for sdiv/udiv at runtime and generate it if available
  #define USE_HELPERS_FOR_INT_DIV  1       // BeagleBoard (ARMv7A) doesn't support SDIV/UDIV
  #define CPU_LOAD_STORE_ARCH      1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

  #define CPBLK_UNROLL_LIMIT       32      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     16      // Upper bound to let the code generator to loop unroll InitBlk.

  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  0  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL     0       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        1       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when the flags need to be set
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register (including HFA support)
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register (including passing HFAs)
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register (including HFA returns)
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register
  #define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (double).
  #define MAX_PASS_MULTIREG_BYTES      32  // Maximum size of a struct that could be passed in more than one register (Max is an HFA of 4 doubles)
  #define MAX_RET_MULTIREG_BYTES       32  // Maximum size of a struct that could be returned in more than one register (Max is an HFA of 4 doubles)
  #define MAX_ARG_REG_COUNT             4  // Maximum registers used to pass a single argument in multiple registers. (max is 4 floats or doubles using an HFA)
  #define MAX_RET_REG_COUNT             4  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            4  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.

  #define NOGC_WRITE_BARRIERS      0       // We DO-NOT have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1       // This is something of a workaround.  For both ARM and AMD64, the frame size is fixed, so we don't really
                                           // need to track stack depth, but this is currently necessary to get GC information reported at call sites.
  #define TARGET_POINTER_SIZE      4       // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define FEATURE_EH_CALLFINALLY_THUNKS 0  // Generate call-to-finally code in "thunks" in the enclosing EH region, protected by "cloned finally" clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use REG_FP as a scratch register and must setup the frame pointer for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  #define REG_FP_FIRST             REG_F0
  #define REG_FP_LAST              REG_F31
  #define FIRST_FP_ARGREG          REG_F0
  #define LAST_FP_ARGREG           REG_F15

  #define REGNUM_BITS              6       // number of bits in a REG_*
  #define REGMASK_BITS             64      // number of bits in a REGNUM_MASK
  #define REGSIZE_BYTES            4       // number of bytes in one register
  #define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

  #define CODE_ALIGN               2       // code alignment requirement
  #define STACK_ALIGN              8       // stack alignment requirement

  #define RBM_INT_CALLEE_SAVED    (RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10)
  #define RBM_INT_CALLEE_TRASH    (RBM_R0|RBM_R1|RBM_R2|RBM_R3|RBM_R12|RBM_LR)
  #define RBM_FLT_CALLEE_SAVED    (RBM_F16|RBM_F17|RBM_F18|RBM_F19|RBM_F20|RBM_F21|RBM_F22|RBM_F23|RBM_F24|RBM_F25|RBM_F26|RBM_F27|RBM_F28|RBM_F29|RBM_F30|RBM_F31)
  #define RBM_FLT_CALLEE_TRASH    (RBM_F0|RBM_F1|RBM_F2|RBM_F3|RBM_F4|RBM_F5|RBM_F6|RBM_F7|RBM_F8|RBM_F9|RBM_F10|RBM_F11|RBM_F12|RBM_F13|RBM_F14|RBM_F15)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)

  #define REG_DEFAULT_HELPER_CALL_TARGET REG_R12
  #define RBM_DEFAULT_HELPER_CALL_TARGET RBM_R12

  #define REG_FASTTAILCALL_TARGET REG_R12   // Target register for fast tail call
  #define RBM_FASTTAILCALL_TARGET RBM_R12

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)
  #define RBM_ALLFLOAT            (RBM_FLT_CALLEE_SAVED | RBM_FLT_CALLEE_TRASH)
  #define RBM_ALLDOUBLE           (RBM_F0|RBM_F2|RBM_F4|RBM_F6|RBM_F8|RBM_F10|RBM_F12|RBM_F14|RBM_F16|RBM_F18|RBM_F20|RBM_F22|RBM_F24|RBM_F26|RBM_F28|RBM_F30)

  #define REG_VAR_ORDER            REG_R3,REG_R2,REG_R1,REG_R0,REG_R4,REG_LR,REG_R12,\
                                   REG_R5,REG_R6,REG_R7,REG_R8,REG_R9,REG_R10

  #define REG_VAR_ORDER_FLT        REG_F8,  REG_F9,  REG_F10, REG_F11, \
                                   REG_F12, REG_F13, REG_F14, REG_F15, \
                                   REG_F6,  REG_F7,  REG_F4,  REG_F5,  \
                                   REG_F2,  REG_F3,  REG_F0,  REG_F1,  \
                                   REG_F16, REG_F17, REG_F18, REG_F19, \
                                   REG_F20, REG_F21, REG_F22, REG_F23, \
                                   REG_F24, REG_F25, REG_F26, REG_F27, \
                                   REG_F28, REG_F29, REG_F30, REG_F31,

  #define RBM_LOW_REGS            (RBM_R0|RBM_R1|RBM_R2|RBM_R3|RBM_R4|RBM_R5|RBM_R6|RBM_R7)
  #define RBM_HIGH_REGS           (RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_R12|RBM_SP|RBM_LR|RBM_PC)

  #define REG_CALLEE_SAVED_ORDER   REG_R4,REG_R5,REG_R6,REG_R7,REG_R8,REG_R9,REG_R10,REG_R11
  #define RBM_CALLEE_SAVED_ORDER   RBM_R4,RBM_R5,RBM_R6,RBM_R7,RBM_R8,RBM_R9,RBM_R10,RBM_R11

  #define CNT_CALLEE_SAVED        (8)
  #define CNT_CALLEE_TRASH        (6)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)

  #define CNT_CALLEE_SAVED_FLOAT  (16)
  #define CNT_CALLEE_TRASH_FLOAT  (16)

  #define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED*REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT*sizeof(float))

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_R12
  #define REG_GSCOOKIE_TMP_1       REG_LR

  // register to hold shift amount; no special register is required on the ARM
  #define REG_SHIFT                REG_NA
  #define RBM_SHIFT                RBM_ALLINT

  // register to hold shift amount when shifting 64-bit values (this uses a helper call)
  #define REG_SHIFT_LNG            REG_R2            // REG_ARG_2
  #define RBM_SHIFT_LNG            RBM_R2            // RBM_ARG_2

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_LR

  // This is a general register that can be optionally reserved for other purposes during codegen
  #define REG_OPT_RSVD             REG_R10
  #define RBM_OPT_RSVD             RBM_R10

  // We reserve R9 to store SP on entry for stack unwinding when localloc is used
  // This needs to stay in sync with the ARM version of InlinedCallFrame::UpdateRegDisplay code.
  #define REG_SAVED_LOCALLOC_SP    REG_R9
  #define RBM_SAVED_LOCALLOC_SP    RBM_R9

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_R0
  #define RBM_EXCEPTION_OBJECT     RBM_R0

  #define REG_JUMP_THUNK_PARAM     REG_R12
  #define RBM_JUMP_THUNK_PARAM     RBM_R12

  // ARM write barrier ABI (see vm\arm\asmhelpers.asm, vm\arm\asmhelpers.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     On entry:
  //       r0: the destination address (LHS of the assignment)
  //       r1: the object reference (RHS of the assignment)
  //     On exit:
  //       r0: trashed
  //       r3: trashed
  // CORINFO_HELP_ASSIGN_BYREF (JIT_ByRefWriteBarrier):
  //     On entry:
  //       r0: the destination address (object reference written here)
  //       r1: the source address (points to object reference to write)
  //     On exit:
  //       r0: incremented by 4
  //       r1: incremented by 4
  //       r2: trashed
  //       r3: trashed

  #define REG_WRITE_BARRIER_DST_BYREF    REG_ARG_0
  #define RBM_WRITE_BARRIER_DST_BYREF    RBM_ARG_0

  #define REG_WRITE_BARRIER_SRC_BYREF    REG_ARG_1
  #define RBM_WRITE_BARRIER_SRC_BYREF    RBM_ARG_1

  #define RBM_CALLEE_TRASH_NOGC          (RBM_R2|RBM_R3|RBM_LR|RBM_DEFAULT_HELPER_CALL_TARGET)

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER         (RBM_R0|RBM_R3|RBM_LR|RBM_DEFAULT_HELPER_CALL_TARGET)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_WRITEBARRIER

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF | RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that r0 and r1 are still valid byref pointers after this helper call, despite their value being changed.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF RBM_CALLEE_TRASH_NOGC

  // GenericPInvokeCalliHelper VASigCookie Parameter
  #define REG_PINVOKE_COOKIE_PARAM          REG_R4
  #define RBM_PINVOKE_COOKIE_PARAM          RBM_R4

  // GenericPInvokeCalliHelper unmanaged target Parameter
  #define REG_PINVOKE_TARGET_PARAM          REG_R12
  #define RBM_PINVOKE_TARGET_PARAM          RBM_R12

  // IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM     REG_R12
  #define RBM_SECRET_STUB_PARAM     RBM_R12

  // R2R indirect call. Use the same registers as VSD
  #define REG_R2R_INDIRECT_PARAM          REG_R4
  #define RBM_R2R_INDIRECT_PARAM          RBM_R4

  // JMP Indirect call register
  #define REG_INDIRECT_CALL_TARGET_REG REG_R12

  // Registers used by PInvoke frame setup
  #define REG_PINVOKE_FRAME        REG_R4
  #define RBM_PINVOKE_FRAME        RBM_R4
  #define REG_PINVOKE_TCB          REG_R5
  #define RBM_PINVOKE_TCB          RBM_R5
  #define REG_PINVOKE_SCRATCH      REG_R6
  #define RBM_PINVOKE_SCRATCH      RBM_R6

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_LR
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // The following registers are used in emitting Enter/Leave/Tailcall profiler callbacks
  #define REG_PROFILER_ENTER_ARG           REG_R0
  #define RBM_PROFILER_ENTER_ARG           RBM_R0
  #define REG_PROFILER_RET_SCRATCH         REG_R2
  #define RBM_PROFILER_RET_SCRATCH         RBM_R2

  // The registers trashed by profiler enter/leave/tailcall hook
  // See vm\arm\asmhelpers.asm for more details.
  #define RBM_PROFILER_ENTER_TRASH     RBM_NONE
  // While REG_PROFILER_RET_SCRATCH is not trashed by the method, the register allocator must
  // consider it killed by the return.
  #define RBM_PROFILER_LEAVE_TRASH     RBM_PROFILER_RET_SCRATCH
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_NONE

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_R0
  #define RBM_INTRET               RBM_R0
  #define RBM_LNGRET              (RBM_R1|RBM_R0)
  #define REG_LNGRET_LO            REG_R0
  #define REG_LNGRET_HI            REG_R1
  #define RBM_LNGRET_LO            RBM_R0
  #define RBM_LNGRET_HI            RBM_R1

  #define REG_FLOATRET             REG_F0
  #define RBM_FLOATRET             RBM_F0
  #define RBM_DOUBLERET           (RBM_F0|RBM_F1)

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper (JIT_RareDisableHelper).
  // See vm\arm\amshelpers.asm for more details.
  #define RBM_STOP_FOR_GC_TRASH     (RBM_CALLEE_TRASH & ~(RBM_LNGRET|RBM_R7|RBM_R8|RBM_R11|RBM_DOUBLERET|RBM_F2|RBM_F3|RBM_F4|RBM_F5|RBM_F6|RBM_F7))

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH (RBM_CALLEE_TRASH | RBM_PINVOKE_TCB | RBM_PINVOKE_SCRATCH)

  #define REG_FPBASE               REG_R11
  #define RBM_FPBASE               RBM_R11
  #define STR_FPBASE               "r11"
  #define REG_SPBASE               REG_SP
  #define RBM_SPBASE               RBM_SP
  #define STR_SPBASE               "sp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved FP and return address

  #define MAX_REG_ARG              4
  #define MAX_FLOAT_REG_ARG        16
  #define MAX_HFA_RET_SLOTS        8

  #define REG_ARG_FIRST            REG_R0
  #define REG_ARG_LAST             REG_R3
  #define REG_ARG_FP_FIRST         REG_F0
  #define REG_ARG_FP_LAST          REG_F7
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_R0
  #define REG_ARG_1                REG_R1
  #define REG_ARG_2                REG_R2
  #define REG_ARG_3                REG_R3

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];

  #define RBM_ARG_0                RBM_R0
  #define RBM_ARG_1                RBM_R1
  #define RBM_ARG_2                RBM_R2
  #define RBM_ARG_3                RBM_R3

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3)
  #define RBM_FLTARG_REGS         (RBM_F0|RBM_F1|RBM_F2|RBM_F3|RBM_F4|RBM_F5|RBM_F6|RBM_F7|RBM_F8|RBM_F9|RBM_F10|RBM_F11|RBM_F12|RBM_F13|RBM_F14|RBM_F15)
  #define RBM_DBL_REGS            RBM_ALLDOUBLE

  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define LBL_DIST_SMALL_MAX_NEG  (0)
  #define LBL_DIST_SMALL_MAX_POS  (+1020)
  #define LBL_DIST_MED_MAX_NEG    (-4095)
  #define LBL_DIST_MED_MAX_POS    (+4096)

  #define JMP_DIST_SMALL_MAX_NEG  (-2048)
  #define JMP_DIST_SMALL_MAX_POS  (+2046)

  #define CALL_DIST_MAX_NEG (-16777216)
  #define CALL_DIST_MAX_POS (+16777214)

  #define JCC_DIST_SMALL_MAX_NEG  (-256)
  #define JCC_DIST_SMALL_MAX_POS  (+254)

  #define JCC_DIST_MEDIUM_MAX_NEG (-1048576)
  #define JCC_DIST_MEDIUM_MAX_POS (+1048574)

  #define LBL_SIZE_SMALL          (2)

  #define JMP_SIZE_SMALL          (2)
  #define JMP_SIZE_LARGE          (4)

  #define JCC_SIZE_SMALL          (2)
  #define JCC_SIZE_MEDIUM         (4)
  #define JCC_SIZE_LARGE          (6)

  // The first thing in an ARM32 prolog pushes LR to the stack, so this can be 0.
  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

  #define REG_STACK_PROBE_HELPER_ARG         REG_R4
  #define RBM_STACK_PROBE_HELPER_ARG         RBM_R4
  #define REG_STACK_PROBE_HELPER_CALL_TARGET REG_R5
  #define RBM_STACK_PROBE_HELPER_CALL_TARGET RBM_R5
  #define RBM_STACK_PROBE_HELPER_TRASH       (RBM_R5 | RBM_LR)

#elif defined(TARGET_ARM64)

  #define CPU_LOAD_STORE_ARCH      1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

  #define CPBLK_UNROLL_LIMIT       64      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     64      // Upper bound to let the code generator to loop unroll InitBlk.

#ifdef FEATURE_SIMD
  #define ALIGN_SIMD_TYPES         1       // whether SIMD type locals are to be aligned
  #define FEATURE_PARTIAL_SIMD_CALLEE_SAVE 1 // Whether SIMD registers are partially saved at calls
#endif // FEATURE_SIMD

  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE 1  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL     1       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     1       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        0       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when the flags need to be set
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register
  #define MAX_PASS_SINGLEREG_BYTES     16  // Maximum size of a struct passed in a single register (16-byte vector).
  #define MAX_PASS_MULTIREG_BYTES      64  // Maximum size of a struct that could be passed in more than one register (max is 4 16-byte vectors using an HVA)
  #define MAX_RET_MULTIREG_BYTES       64  // Maximum size of a struct that could be returned in more than one register (Max is an HVA of 4 16-byte vectors)
  #define MAX_ARG_REG_COUNT             4  // Maximum registers used to pass a single argument in multiple registers. (max is 4 128-bit vectors using an HVA)
  #define MAX_RET_REG_COUNT             4  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            4  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.

  #define NOGC_WRITE_BARRIERS      1       // We have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1       // This is something of a workaround.  For both ARM and AMD64, the frame size is fixed, so we don't really
                                           // need to track stack depth, but this is currently necessary to get GC information reported at call sites.
  #define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define FEATURE_EH_CALLFINALLY_THUNKS 1  // Generate call-to-finally code in "thunks" in the enclosing EH region, protected by "cloned finally" clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use REG_FP as a scratch register and must setup the frame pointer for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  #define REG_FP_FIRST             REG_V0
  #define REG_FP_LAST              REG_V31
  #define FIRST_FP_ARGREG          REG_V0
  #define LAST_FP_ARGREG           REG_V15

  #define REGNUM_BITS              6       // number of bits in a REG_*
  #define REGMASK_BITS             64      // number of bits in a REGNUM_MASK
  #define REGSIZE_BYTES            8       // number of bytes in one general purpose register
  #define FP_REGSIZE_BYTES         16      // number of bytes in one FP/SIMD register
  #define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP/SIMD register that are saved/restored, for callee-saved registers

  #define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

  #define CODE_ALIGN               4       // code alignment requirement
  #define STACK_ALIGN              16      // stack alignment requirement

  #define RBM_INT_CALLEE_SAVED    (RBM_R19|RBM_R20|RBM_R21|RBM_R22|RBM_R23|RBM_R24|RBM_R25|RBM_R26|RBM_R27|RBM_R28)
  #define RBM_INT_CALLEE_TRASH    (RBM_R0|RBM_R1|RBM_R2|RBM_R3|RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_R12|RBM_R13|RBM_R14|RBM_R15|RBM_IP0|RBM_IP1|RBM_LR)
  #define RBM_FLT_CALLEE_SAVED    (RBM_V8|RBM_V9|RBM_V10|RBM_V11|RBM_V12|RBM_V13|RBM_V14|RBM_V15)
  #define RBM_FLT_CALLEE_TRASH    (RBM_V0|RBM_V1|RBM_V2|RBM_V3|RBM_V4|RBM_V5|RBM_V6|RBM_V7|RBM_V16|RBM_V17|RBM_V18|RBM_V19|RBM_V20|RBM_V21|RBM_V22|RBM_V23|RBM_V24|RBM_V25|RBM_V26|RBM_V27|RBM_V28|RBM_V29|RBM_V30|RBM_V31)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)

  #define REG_DEFAULT_HELPER_CALL_TARGET REG_R12
  #define RBM_DEFAULT_HELPER_CALL_TARGET RBM_R12

  #define REG_FASTTAILCALL_TARGET REG_IP0   // Target register for fast tail call
  #define RBM_FASTTAILCALL_TARGET RBM_IP0

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)
  #define RBM_ALLFLOAT            (RBM_FLT_CALLEE_SAVED | RBM_FLT_CALLEE_TRASH)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT

  // REG_VAR_ORDER is: (CALLEE_TRASH & ~CALLEE_TRASH_NOGC), CALLEE_TRASH_NOGC, CALLEE_SAVED
  #define REG_VAR_ORDER            REG_R0, REG_R1, REG_R2, REG_R3, REG_R4, REG_R5, \
                                   REG_R6, REG_R7, REG_R8, REG_R9, REG_R10,        \
                                   REG_R11, REG_R13, REG_R14,                      \
                                   REG_R12, REG_R15, REG_IP0, REG_IP1,             \
                                   REG_CALLEE_SAVED_ORDER, REG_LR

  #define REG_VAR_ORDER_FLT        REG_V16, REG_V17, REG_V18, REG_V19, \
                                   REG_V20, REG_V21, REG_V22, REG_V23, \
                                   REG_V24, REG_V25, REG_V26, REG_V27, \
                                   REG_V28, REG_V29, REG_V30, REG_V31, \
                                   REG_V7,  REG_V6,  REG_V5,  REG_V4,  \
                                   REG_V8,  REG_V9,  REG_V10, REG_V11, \
                                   REG_V12, REG_V13, REG_V14, REG_V15, \
                                   REG_V3,  REG_V2, REG_V1,  REG_V0

  #define REG_CALLEE_SAVED_ORDER   REG_R19,REG_R20,REG_R21,REG_R22,REG_R23,REG_R24,REG_R25,REG_R26,REG_R27,REG_R28
  #define RBM_CALLEE_SAVED_ORDER   RBM_R19,RBM_R20,RBM_R21,RBM_R22,RBM_R23,RBM_R24,RBM_R25,RBM_R26,RBM_R27,RBM_R28

  #define CNT_CALLEE_SAVED        (11)
  #define CNT_CALLEE_TRASH        (17)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)

  #define CNT_CALLEE_SAVED_FLOAT  (8)
  #define CNT_CALLEE_TRASH_FLOAT  (24)

  #define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_R9
  #define REG_GSCOOKIE_TMP_1       REG_R10

  // register to hold shift amount; no special register is required on ARM64.
  #define REG_SHIFT                REG_NA
  #define RBM_SHIFT                RBM_ALLINT

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_R9

  // This is a general register that can be optionally reserved for other purposes during codegen
  #define REG_OPT_RSVD             REG_IP1
  #define RBM_OPT_RSVD             RBM_IP1

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_R0
  #define RBM_EXCEPTION_OBJECT     RBM_R0

  #define REG_JUMP_THUNK_PARAM     REG_R12
  #define RBM_JUMP_THUNK_PARAM     RBM_R12

  // ARM64 write barrier ABI (see vm\arm64\asmhelpers.asm, vm\arm64\asmhelpers.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     On entry:
  //       x14: the destination address (LHS of the assignment)
  //       x15: the object reference (RHS of the assignment)
  //     On exit:
  //       x12: trashed
  //       x14: incremented by 8
  //       x15: trashed
  //       x17: trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
  // CORINFO_HELP_ASSIGN_BYREF (JIT_ByRefWriteBarrier):
  //     On entry:
  //       x13: the source address (points to object reference to write)
  //       x14: the destination address (object reference written here)
  //     On exit:
  //       x12: trashed
  //       x13: incremented by 8
  //       x14: incremented by 8
  //       x15: trashed
  //       x17: trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
  //
  // Note that while x17 (ip1) is currently only trashed under FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP,
  // it is expected to be set in the future for R2R. Consider it trashed to avoid later breaking changes.

  #define REG_WRITE_BARRIER_DST          REG_R14
  #define RBM_WRITE_BARRIER_DST          RBM_R14

  #define REG_WRITE_BARRIER_SRC          REG_R15
  #define RBM_WRITE_BARRIER_SRC          RBM_R15

  #define REG_WRITE_BARRIER_DST_BYREF    REG_R14
  #define RBM_WRITE_BARRIER_DST_BYREF    RBM_R14

  #define REG_WRITE_BARRIER_SRC_BYREF    REG_R13
  #define RBM_WRITE_BARRIER_SRC_BYREF    RBM_R13

  #define RBM_CALLEE_TRASH_NOGC          (RBM_R12|RBM_R15|RBM_IP0|RBM_IP1|RBM_DEFAULT_HELPER_CALL_TARGET)

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER         (RBM_R14|RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF | RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that x13 and x14 are still valid byref pointers after this helper call, despite their value being changed.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF RBM_CALLEE_TRASH_NOGC

  // GenericPInvokeCalliHelper VASigCookie Parameter
  #define REG_PINVOKE_COOKIE_PARAM          REG_R15
  #define RBM_PINVOKE_COOKIE_PARAM          RBM_R15

  // GenericPInvokeCalliHelper unmanaged target Parameter
  #define REG_PINVOKE_TARGET_PARAM          REG_R12
  #define RBM_PINVOKE_TARGET_PARAM          RBM_R12

  // IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM     REG_R12
  #define RBM_SECRET_STUB_PARAM     RBM_R12

  // R2R indirect call. Use the same registers as VSD
  #define REG_R2R_INDIRECT_PARAM          REG_R11
  #define RBM_R2R_INDIRECT_PARAM          RBM_R11

  // JMP Indirect call register
  #define REG_INDIRECT_CALL_TARGET_REG    REG_IP0

  // Registers used by PInvoke frame setup
  #define REG_PINVOKE_FRAME        REG_R9
  #define RBM_PINVOKE_FRAME        RBM_R9
  #define REG_PINVOKE_TCB          REG_R10
  #define RBM_PINVOKE_TCB          RBM_R10
  #define REG_PINVOKE_SCRATCH      REG_R10
  #define RBM_PINVOKE_SCRATCH      RBM_R10

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_ZR
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // The following registers are used in emitting Enter/Leave/Tailcall profiler callbacks
  #define REG_PROFILER_ENTER_ARG_FUNC_ID    REG_R10
  #define RBM_PROFILER_ENTER_ARG_FUNC_ID    RBM_R10
  #define REG_PROFILER_ENTER_ARG_CALLER_SP  REG_R11
  #define RBM_PROFILER_ENTER_ARG_CALLER_SP  RBM_R11
  #define REG_PROFILER_LEAVE_ARG_FUNC_ID    REG_R10
  #define RBM_PROFILER_LEAVE_ARG_FUNC_ID    RBM_R10
  #define REG_PROFILER_LEAVE_ARG_CALLER_SP  REG_R11
  #define RBM_PROFILER_LEAVE_ARG_CALLER_SP  RBM_R11

  // The registers trashed by profiler enter/leave/tailcall hook
  #define RBM_PROFILER_ENTER_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_ARG_RET_BUFF|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_LEAVE_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_ARG_RET_BUFF|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_R0
  #define RBM_INTRET               RBM_R0
  #define RBM_LNGRET               RBM_R0
  // second return register for 16-byte structs
  #define REG_INTRET_1             REG_R1
  #define RBM_INTRET_1             RBM_R1

  #define REG_FLOATRET             REG_V0
  #define RBM_FLOATRET             RBM_V0
  #define RBM_DOUBLERET            RBM_V0

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper
  #define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

  #define REG_FPBASE               REG_FP
  #define RBM_FPBASE               RBM_FP
  #define STR_FPBASE               "fp"
  #define REG_SPBASE               REG_SP
  #define RBM_SPBASE               RBM_ZR     // reuse the RBM for REG_ZR
  #define STR_SPBASE               "sp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved FP and return address

  // On ARM64 the calling convention defines REG_R8 (x8) as an additional argument register.
  // It isn't allocated for the normal user arguments, so it isn't counted by MAX_REG_ARG.
  // Whether we use this register to pass the RetBuff is controlled by the function hasFixedRetBuffReg().
  // It is considered to be the next integer argnum, which is 8.
  //
  #define REG_ARG_RET_BUFF         REG_R8
  #define RBM_ARG_RET_BUFF         RBM_R8
  #define RET_BUFF_ARGNUM          8

  #define MAX_REG_ARG              8
  #define MAX_FLOAT_REG_ARG        8

  #define REG_ARG_FIRST            REG_R0
  #define REG_ARG_LAST             REG_R7
  #define REG_ARG_FP_FIRST         REG_V0
  #define REG_ARG_FP_LAST          REG_V7
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_R0
  #define REG_ARG_1                REG_R1
  #define REG_ARG_2                REG_R2
  #define REG_ARG_3                REG_R3
  #define REG_ARG_4                REG_R4
  #define REG_ARG_5                REG_R5
  #define REG_ARG_6                REG_R6
  #define REG_ARG_7                REG_R7

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];

  #define RBM_ARG_0                RBM_R0
  #define RBM_ARG_1                RBM_R1
  #define RBM_ARG_2                RBM_R2
  #define RBM_ARG_3                RBM_R3
  #define RBM_ARG_4                RBM_R4
  #define RBM_ARG_5                RBM_R5
  #define RBM_ARG_6                RBM_R6
  #define RBM_ARG_7                RBM_R7

  #define REG_FLTARG_0             REG_V0
  #define REG_FLTARG_1             REG_V1
  #define REG_FLTARG_2             REG_V2
  #define REG_FLTARG_3             REG_V3
  #define REG_FLTARG_4             REG_V4
  #define REG_FLTARG_5             REG_V5
  #define REG_FLTARG_6             REG_V6
  #define REG_FLTARG_7             REG_V7

  #define RBM_FLTARG_0             RBM_V0
  #define RBM_FLTARG_1             RBM_V1
  #define RBM_FLTARG_2             RBM_V2
  #define RBM_FLTARG_3             RBM_V3
  #define RBM_FLTARG_4             RBM_V4
  #define RBM_FLTARG_5             RBM_V5
  #define RBM_FLTARG_6             RBM_V6
  #define RBM_FLTARG_7             RBM_V7

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3|RBM_ARG_4|RBM_ARG_5|RBM_ARG_6|RBM_ARG_7)
  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3|RBM_FLTARG_4|RBM_FLTARG_5|RBM_FLTARG_6|RBM_FLTARG_7)

  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define LBL_DIST_SMALL_MAX_NEG  (-1048576)
  #define LBL_DIST_SMALL_MAX_POS  (+1048575)

  #define LBL_SIZE_SMALL          (4)

  #define JCC_DIST_SMALL_MAX_NEG  (-1048576)
  #define JCC_DIST_SMALL_MAX_POS  (+1048575)

  #define TB_DIST_SMALL_MAX_NEG   (-32768)
  #define TB_DIST_SMALL_MAX_POS   (+32767)

  #define JCC_SIZE_SMALL          (4)
  #define JCC_SIZE_LARGE          (8)

  #define LDC_DIST_SMALL_MAX_NEG  (-1048576)
  #define LDC_DIST_SMALL_MAX_POS  (+1048575)

  #define LDC_SIZE_SMALL          (4)

  #define JMP_SIZE_SMALL          (4)

  // The number of bytes from the end the last probed page that must also be probed, to allow for some
  // small SP adjustments without probes. If zero, then the stack pointer can point to the last byte/word
  // on the stack guard page, and must be touched before any further "SUB SP".
  // For arm64, this is the maximum prolog establishment pre-indexed (that is SP pre-decrement) offset.
  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 512

  // Some "Advanced SIMD scalar x indexed element" and "Advanced SIMD vector x indexed element" instructions (e.g. "MLA (by element)")
  // have encoding that restricts what registers that can be used for the indexed element when the element size is H (i.e. 2 bytes).
  #define RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS (RBM_V0|RBM_V1|RBM_V2|RBM_V3|RBM_V4|RBM_V5|RBM_V6|RBM_V7|RBM_V8|RBM_V9|RBM_V10|RBM_V11|RBM_V12|RBM_V13|RBM_V14|RBM_V15)

  #define REG_ZERO_INIT_FRAME_REG1 REG_R9
  #define REG_ZERO_INIT_FRAME_REG2 REG_R10
  #define REG_ZERO_INIT_FRAME_SIMD REG_V16

#else
  #error Unsupported or unset target architecture
#endif

#ifdef TARGET_XARCH

  #define JMP_DIST_SMALL_MAX_NEG  (-128)
  #define JMP_DIST_SMALL_MAX_POS  (+127)

  #define JCC_DIST_SMALL_MAX_NEG  (-128)
  #define JCC_DIST_SMALL_MAX_POS  (+127)

  #define JMP_SIZE_SMALL          (2)
  #define JMP_SIZE_LARGE          (5)

  #define JCC_SIZE_SMALL          (2)
  #define JCC_SIZE_LARGE          (6)

  #define PUSH_INST_SIZE          (5)
  #define CALL_INST_SIZE          (5)

#endif // TARGET_XARCH

C_ASSERT(REG_FIRST == 0);
C_ASSERT(REG_INT_FIRST < REG_INT_LAST);
C_ASSERT(REG_FP_FIRST  < REG_FP_LAST);

// Opportunistic tail call feature converts non-tail prefixed calls into
// tail calls where possible. It requires fast tail calling mechanism for
// performance. Otherwise, we are better off not converting non-tail prefixed
// calls into tail calls.
C_ASSERT((FEATURE_TAILCALL_OPT == 0) || (FEATURE_FASTTAILCALL == 1));

/*****************************************************************************/

#define BITS_PER_BYTE              8
#define REGNUM_MASK              ((1 << REGNUM_BITS) - 1)     // a n-bit mask use to encode multiple REGNUMs into a unsigned int
#define RBM_ALL(type) (varTypeUsesFloatReg(type) ? RBM_ALLFLOAT : RBM_ALLINT)

/*****************************************************************************/

#if CPU_HAS_BYTE_REGS
  #define RBM_BYTE_REGS           (RBM_EAX|RBM_ECX|RBM_EDX|RBM_EBX)
  #define BYTE_REG_COUNT          4
  #define RBM_NON_BYTE_REGS       (RBM_ESI|RBM_EDI)
#else
  #define RBM_BYTE_REGS            RBM_ALLINT
  #define RBM_NON_BYTE_REGS        RBM_NONE
#endif
// clang-format on

/*****************************************************************************/
class Target
{
public:
    static const char* g_tgtCPUName;
    static const char* g_tgtPlatformName;

    enum ArgOrder
    {
        ARG_ORDER_R2L,
        ARG_ORDER_L2R
    };
    static const enum ArgOrder g_tgtArgOrder;
    static const enum ArgOrder g_tgtUnmanagedArgOrder;
};

#if defined(DEBUG) || defined(LATE_DISASM) || DUMP_GC_TABLES
const char* getRegName(unsigned reg, bool isFloat = false); // this is for gcencode.cpp and disasm.cpp that don't use
                                                            // the regNumber type
const char* getRegName(regNumber reg, bool isFloat = false);
#endif // defined(DEBUG) || defined(LATE_DISASM) || DUMP_GC_TABLES

#ifdef DEBUG
const char* getRegNameFloat(regNumber reg, var_types type);
extern void dspRegMask(regMaskTP regMask, size_t minSiz = 0);
#endif

#if CPU_HAS_BYTE_REGS
inline BOOL isByteReg(regNumber reg)
{
    return (reg <= REG_EBX);
}
#else
inline BOOL isByteReg(regNumber reg)
{
    return true;
}
#endif

inline regMaskTP genRegMask(regNumber reg);
inline regMaskTP genRegMaskFloat(regNumber reg, var_types type = TYP_DOUBLE);

/*****************************************************************************
 * Return true if the register number is valid
 */
inline bool genIsValidReg(regNumber reg)
{
    /* It's safest to perform an unsigned comparison in case reg is negative */
    return ((unsigned)reg < (unsigned)REG_COUNT);
}

/*****************************************************************************
 * Return true if the register is a valid integer register
 */
inline bool genIsValidIntReg(regNumber reg)
{
    return reg >= REG_INT_FIRST && reg <= REG_INT_LAST;
}

/*****************************************************************************
 * Return true if the register is a valid floating point register
 */
inline bool genIsValidFloatReg(regNumber reg)
{
    return reg >= REG_FP_FIRST && reg <= REG_FP_LAST;
}

#ifdef TARGET_ARM

/*****************************************************************************
 * Return true if the register is a valid floating point double register
 */
inline bool genIsValidDoubleReg(regNumber reg)
{
    return genIsValidFloatReg(reg) && (((reg - REG_FP_FIRST) & 0x1) == 0);
}

#endif // TARGET_ARM

//-------------------------------------------------------------------------------------------
// hasFixedRetBuffReg:
//     Returns true if our target architecture uses a fixed return buffer register
//
inline bool hasFixedRetBuffReg()
{
#ifdef TARGET_ARM64
    return true;
#else
    return false;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffReg:
//     Returns the regNumber to use for the fixed return buffer
//
inline regNumber theFixedRetBuffReg()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return REG_ARG_RET_BUFF;
#else
    return REG_NA;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffMask:
//     Returns the regNumber to use for the fixed return buffer
//
inline regMaskTP theFixedRetBuffMask()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return RBM_ARG_RET_BUFF;
#else
    return 0;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffArgNum:
//     Returns the argNum to use for the fixed return buffer
//
inline unsigned theFixedRetBuffArgNum()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return RET_BUFF_ARGNUM;
#else
    return BAD_VAR_NUM;
#endif
}

//-------------------------------------------------------------------------------------------
// fullIntArgRegMask:
//     Returns the full mask of all possible integer registers
//     Note this includes the fixed return buffer register on Arm64
//
inline regMaskTP fullIntArgRegMask()
{
    if (hasFixedRetBuffReg())
    {
        return RBM_ARG_REGS | theFixedRetBuffMask();
    }
    else
    {
        return RBM_ARG_REGS;
    }
}

//-------------------------------------------------------------------------------------------
// isValidIntArgReg:
//     Returns true if the register is a valid integer argument register
//     Note this method also returns true on Arm64 when 'reg' is the RetBuff register
//
inline bool isValidIntArgReg(regNumber reg)
{
    return (genRegMask(reg) & fullIntArgRegMask()) != 0;
}

//-------------------------------------------------------------------------------------------
// genRegArgNext:
//     Given a register that is an integer or floating point argument register
//     returns the next argument register
//
regNumber genRegArgNext(regNumber argReg);

//-------------------------------------------------------------------------------------------
// isValidFloatArgReg:
//     Returns true if the register is a valid floating-point argument register
//
inline bool isValidFloatArgReg(regNumber reg)
{
    if (reg == REG_NA)
    {
        return false;
    }
    else
    {
        return (reg >= FIRST_FP_ARGREG) && (reg <= LAST_FP_ARGREG);
    }
}

/*****************************************************************************
 *
 *  Can the register hold the argument type?
 */

#ifdef TARGET_ARM
inline bool floatRegCanHoldType(regNumber reg, var_types type)
{
    assert(genIsValidFloatReg(reg));
    if (type == TYP_DOUBLE)
    {
        return ((reg - REG_F0) % 2) == 0;
    }
    else
    {
        // Can be TYP_STRUCT for HFA. It's not clear that's correct; what about
        // HFA of double? We wouldn't be asserting the right alignment, and
        // callers like genRegMaskFloat() wouldn't be generating the right mask.

        assert((type == TYP_FLOAT) || (type == TYP_STRUCT));
        return true;
    }
}
#else
// AMD64: xmm registers can hold any float type
// x86: FP stack can hold any float type
// ARM64: Floating-point/SIMD registers can hold any type.
inline bool floatRegCanHoldType(regNumber reg, var_types type)
{
    return true;
}
#endif

/*****************************************************************************
 *
 *  Map a register number to a register mask.
 */

extern const regMaskSmall regMasks[REG_COUNT];

inline regMaskTP genRegMask(regNumber reg)
{
    assert((unsigned)reg < ArrLen(regMasks));
#ifdef TARGET_AMD64
    // shift is faster than a L1 hit on modern x86
    // (L1 latency on sandy bridge is 4 cycles for [base] and 5 for [base + index*c] )
    // the reason this is AMD-only is because the x86 BE will try to get reg masks for REG_STK
    // and the result needs to be zero.
    regMaskTP result = 1 << reg;
    assert(result == regMasks[reg]);
    return result;
#else
    return regMasks[reg];
#endif
}

/*****************************************************************************
 *
 *  Map a register number to a floating-point register mask.
 */

inline regMaskTP genRegMaskFloat(regNumber reg, var_types type /* = TYP_DOUBLE */)
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_X86)
    assert(genIsValidFloatReg(reg));
    assert((unsigned)reg < ArrLen(regMasks));
    return regMasks[reg];
#elif defined(TARGET_ARM)
    assert(floatRegCanHoldType(reg, type));
    assert(reg >= REG_F0 && reg <= REG_F31);

    if (type == TYP_DOUBLE)
    {
        return regMasks[reg] | regMasks[reg + 1];
    }
    else
    {
        return regMasks[reg];
    }
#else
#error Unsupported or unset target architecture
#endif
}

//------------------------------------------------------------------------
// genRegMask: Given a register, and its type, generate the appropriate regMask
//
// Arguments:
//    regNum   - the register of interest
//    type     - the type of regNum (i.e. the type it is being used as)
//
// Return Value:
//    This will usually return the same value as genRegMask(regNum), but
//    on architectures where multiple registers are used for certain types
//    (e.g. TYP_DOUBLE on ARM), it will return a regMask that includes
//    all the registers.
//    Registers that are used in pairs, but separately named (e.g. TYP_LONG
//    on ARM) will return just the regMask for the given register.
//
// Assumptions:
//    For registers that are used in pairs, the caller will be handling
//    each member of the pair separately.
//
inline regMaskTP genRegMask(regNumber regNum, var_types type)
{
#ifndef TARGET_ARM
    return genRegMask(regNum);
#else
    regMaskTP regMask = RBM_NONE;

    if (varTypeUsesFloatReg(type))
    {
        regMask = genRegMaskFloat(regNum, type);
    }
    else
    {
        regMask = genRegMask(regNum);
    }
    return regMask;
#endif
}

/*****************************************************************************
 *
 *  These arrays list the callee-saved register numbers (and bitmaps, respectively) for
 *  the current architecture.
 */
extern const regNumber raRegCalleeSaveOrder[CNT_CALLEE_SAVED];
extern const regMaskTP raRbmCalleeSaveOrder[CNT_CALLEE_SAVED];

// This method takes a "compact" bitset of the callee-saved registers, and "expands" it to a full register mask.
regMaskSmall genRegMaskFromCalleeSavedMask(unsigned short);

/*****************************************************************************
 *
 *  Assumes that "reg" is of the given "type". Return the next unused reg number after "reg"
 *  of this type, else REG_NA if there are no more.
 */

inline regNumber regNextOfType(regNumber reg, var_types type)
{
    regNumber regReturn;

#ifdef TARGET_ARM
    if (type == TYP_DOUBLE)
    {
        // Skip odd FP registers for double-precision types
        assert(floatRegCanHoldType(reg, type));
        regReturn = regNumber(reg + 2);
    }
    else
    {
        regReturn = REG_NEXT(reg);
    }
#else // TARGET_ARM
    regReturn = REG_NEXT(reg);
#endif

    if (varTypeUsesFloatReg(type))
    {
        if (regReturn > REG_FP_LAST)
        {
            regReturn = REG_NA;
        }
    }
    else
    {
        if (regReturn > REG_INT_LAST)
        {
            regReturn = REG_NA;
        }
    }

    return regReturn;
}

/*****************************************************************************
 *
 *  Type checks
 */

inline bool isFloatRegType(var_types type)
{
    return varTypeUsesFloatReg(type);
}

// If the WINDOWS_AMD64_ABI is defined make sure that TARGET_AMD64 is also defined.
#if defined(WINDOWS_AMD64_ABI)
#if !defined(TARGET_AMD64)
#error When WINDOWS_AMD64_ABI is defined you must define TARGET_AMD64 defined as well.
#endif
#endif

/*****************************************************************************/
// Some sanity checks on some of the register masks
// Stack pointer is never part of RBM_ALLINT
C_ASSERT((RBM_ALLINT & RBM_SPBASE) == RBM_NONE);
C_ASSERT((RBM_INT_CALLEE_SAVED & RBM_SPBASE) == RBM_NONE);

#if ETW_EBP_FRAMED
// Frame pointer isn't either if we're supporting ETW frame chaining
C_ASSERT((RBM_ALLINT & RBM_FPBASE) == RBM_NONE);
C_ASSERT((RBM_INT_CALLEE_SAVED & RBM_FPBASE) == RBM_NONE);
#endif
/*****************************************************************************/

#ifdef TARGET_64BIT
typedef unsigned __int64 target_size_t;
typedef __int64          target_ssize_t;
#define TARGET_SIGN_BIT (1ULL << 63)

#else // !TARGET_64BIT
typedef unsigned int   target_size_t;
typedef int            target_ssize_t;
#define TARGET_SIGN_BIT (1ULL << 31)

#endif // !TARGET_64BIT

C_ASSERT(sizeof(target_size_t) == TARGET_POINTER_SIZE);
C_ASSERT(sizeof(target_ssize_t) == TARGET_POINTER_SIZE);

#if defined(TARGET_X86)
// instrDescCns holds constant values for the emitter. The X86 compiler is unique in that it
// may represent relocated pointer values with these constants. On the 64bit to 32 bit
// cross-targetting jit, the the constant value must be represented as a 64bit value in order
// to represent these pointers.
typedef ssize_t cnsval_ssize_t;
typedef size_t  cnsval_size_t;
#else
typedef target_ssize_t cnsval_ssize_t;
typedef target_size_t  cnsval_size_t;
#endif

/*****************************************************************************/
#endif // TARGET_H_
/*****************************************************************************/

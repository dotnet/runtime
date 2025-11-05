// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_WASM)
#error The file should not be included for this platform.
#endif

// clang-format off
#define CPU_LOAD_STORE_ARCH      1
#define CPU_HAS_FP_SUPPORT       1
#define CPU_HAS_BYTE_REGS        0

// Currently we don't pass any arguments on the linear memory stack so FEATURE_FIXED_OUT_ARGS is not needed.
#define FEATURE_FIXED_OUT_ARGS   0       // Preallocate the outgoing arg area in the prolog
#define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
#define FEATURE_MULTIREG_STRUCT_PROMOTE 1  // True when we want to promote fields of a multireg struct into registers
#define FEATURE_FASTTAILCALL     0       // Tail calls made as epilog+jmp
#define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
#define FEATURE_IMPLICIT_BYREFS       1  // Support for struct parameters passed via pointers to shadow copies
#define FEATURE_MULTIREG_ARGS_OR_RET  0  // Support for passing and/or returning single values in more than one register
#define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
#define FEATURE_MULTIREG_RET          0  // Support for returning a single value in more than one register
#define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (long/double).
#define MAX_PASS_MULTIREG_BYTES       0  // Maximum size of a struct that could be passed in more than one register
#define MAX_RET_MULTIREG_BYTES        0  // Maximum size of a struct that could be returned in more than one register (Max is an HFA or 2 doubles)
#define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass a single argument in multiple registers.
#define MAX_RET_REG_COUNT             1  // Maximum registers used to return a value.
#define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
                                         // This is also the maximum number of registers for a MultiReg node.

#define NOGC_WRITE_BARRIERS      0       // No specialized WriteBarrier JIT Helpers
#define USER_ARGS_COME_LAST      1
#ifdef TARGET_WASM32
#define TARGET_POINTER_SIZE      4       // equal to sizeof(void*) and the managed pointer size in bytes for this target
#else
#define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define ETW_EBP_FRAMED           0       // No frame pointer chaining on WASM

// TODO-WASM-CQ: measure if "CSE_CONSTS" is beneficial.
#define CSE_CONSTS               1       // Enable if we want to CSE constants
#define EMIT_TRACK_STACK_DEPTH   1       // TODO-WASM: set to 0.
#define EMIT_GENERATE_GCINFO     0       // Codegen and emit not responsible for GC liveness tracking and GCInfo generation

// Since we don't have a fixed register set on WASM, we set most of the following register defines to 'none'-like values.
#define REG_FP_FIRST             REG_NA
#define REG_FP_LAST              REG_NA
#define FIRST_FP_ARGREG          REG_NA
#define LAST_FP_ARGREG           REG_NA

#define HAS_FIXED_REGISTER_SET   0       // WASM has an unlimited number of locals/registers.
#define REGNUM_BITS              1       // number of bits in a REG_*
#define REGSIZE_BYTES            TARGET_POINTER_SIZE // number of bytes in one general purpose register
#define FP_REGSIZE_BYTES         8      // number of bytes in one FP/SIMD register
#define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP/SIMD register that are saved/restored, for callee-saved registers

#define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

#define CODE_ALIGN               1       // code alignment requirement
#define STACK_ALIGN              16      // stack alignment requirement

#define FIRST_INT_CALLEE_SAVED  REG_NA
#define LAST_INT_CALLEE_SAVED   REG_NA
#define RBM_INT_CALLEE_SAVED    RBM_NONE
#define RBM_INT_CALLEE_TRASH    RBM_NONE
#define FIRST_FLT_CALLEE_SAVED  REG_NA
#define LAST_FLT_CALLEE_SAVED   REG_NA
#define RBM_FLT_CALLEE_SAVED    RBM_NONE
#define RBM_FLT_CALLEE_TRASH    RBM_NONE

#define RBM_CALLEE_SAVED        RBM_NONE
#define RBM_CALLEE_TRASH        RBM_NONE

#define REG_DEFAULT_HELPER_CALL_TARGET REG_NA
#define RBM_DEFAULT_HELPER_CALL_TARGET REG_NA

#define RBM_ALLINT              RBM_NONE
#define RBM_ALLFLOAT            RBM_NONE
#define RBM_ALLDOUBLE           RBM_NONE

#define REG_VAR_ORDER
#define REG_VAR_ORDER_FLT

// The defines below affect CSE heuristics, so we need to give them some 'sensible' values.
#define CNT_CALLEE_SAVED        (8)
#define CNT_CALLEE_TRASH        (8)
#define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED)

#define CNT_CALLEE_SAVED_FLOAT  (10)
#define CNT_CALLEE_TRASH_FLOAT  (10)
#define CNT_CALLEE_ENREG_FLOAT  (CNT_CALLEE_SAVED_FLOAT)

#define CNT_CALLEE_SAVED_MASK   (0)
#define CNT_CALLEE_TRASH_MASK   (0)
#define CNT_CALLEE_ENREG_MASK   (CNT_CALLEE_SAVED_MASK)

#define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
#define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

#define REG_TMP_0                REG_NA

// Temporary registers used for the GS cookie check.
#define REG_GSCOOKIE_TMP_0       REG_NA
#define REG_GSCOOKIE_TMP_1       REG_NA

// register to hold shift amount
#define REG_SHIFT                REG_NA
#define RBM_SHIFT                RBM_ALLINT

// This is a general scratch register that does not conflict with the argument registers
#define REG_SCRATCH              REG_NA

// This is a general register that can be optionally reserved for other purposes during codegen
#define REG_OPT_RSVD             REG_NA
#define RBM_OPT_RSVD             RBM_NONE

// Where is the exception object on entry to the handler block?
#define REG_EXCEPTION_OBJECT     REG_NA
#define RBM_EXCEPTION_OBJECT     RBM_NONE

#define REG_JUMP_THUNK_PARAM     REG_NA
#define RBM_JUMP_THUNK_PARAM     RBM_NONE

#define REG_WRITE_BARRIER_DST          REG_NA
#define RBM_WRITE_BARRIER_DST          RBM_NONE

#define REG_WRITE_BARRIER_SRC          REG_NA
#define RBM_WRITE_BARRIER_SRC          RBM_NONE

#define REG_WRITE_BARRIER_DST_BYREF    REG_NA
#define RBM_WRITE_BARRIER_DST_BYREF    RBM_NONE

#define REG_WRITE_BARRIER_SRC_BYREF    REG_NA
#define RBM_WRITE_BARRIER_SRC_BYREF    RBM_NONE

#define RBM_CALLEE_TRASH_NOGC          RBM_NONE

// Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
#define RBM_CALLEE_TRASH_WRITEBARRIER         RBM_NONE

// Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
#define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

// Registers killed by CORINFO_HELP_ASSIGN_BYREF.
#define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   RBM_NONE

// Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
#define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF RBM_NONE

// GenericPInvokeCalliHelper VASigCookie Parameter
#define REG_PINVOKE_COOKIE_PARAM          REG_NA
#define RBM_PINVOKE_COOKIE_PARAM          RBM_NONE

// GenericPInvokeCalliHelper unmanaged target Parameter
#define REG_PINVOKE_TARGET_PARAM          REG_NA
#define RBM_PINVOKE_TARGET_PARAM          RBM_NONE

// IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
#define REG_SECRET_STUB_PARAM     REG_NA
#define RBM_SECRET_STUB_PARAM     RBM_NONE

// R2R indirect call. Use the same registers as VSD
#define REG_R2R_INDIRECT_PARAM          REG_NA
#define RBM_R2R_INDIRECT_PARAM          RBM_NONE

// JMP Indirect call register
#define REG_INDIRECT_CALL_TARGET_REG    REG_NA

// The following defines are useful for iterating a regNumber
#define REG_FIRST                REG_NA
#define REG_INT_FIRST            REG_NA
#define REG_INT_LAST             REG_NA
#define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
#define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
#define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

// The following registers are used in emitting Enter/Leave/Tailcall profiler callbacks
#define REG_PROFILER_ENTER_ARG_FUNC_ID    REG_NA
#define RBM_PROFILER_ENTER_ARG_FUNC_ID    RBM_NONE
#define REG_PROFILER_ENTER_ARG_CALLER_SP  REG_NA
#define RBM_PROFILER_ENTER_ARG_CALLER_SP  RBM_NONE
#define REG_PROFILER_LEAVE_ARG_FUNC_ID    REG_PROFILER_ENTER_ARG_FUNC_ID
#define RBM_PROFILER_LEAVE_ARG_FUNC_ID    RBM_PROFILER_ENTER_ARG_FUNC_ID
#define REG_PROFILER_LEAVE_ARG_CALLER_SP  REG_PROFILER_ENTER_ARG_CALLER_SP
#define RBM_PROFILER_LEAVE_ARG_CALLER_SP  RBM_PROFILER_ENTER_ARG_CALLER_SP

// The registers trashed by profiler enter/leave/tailcall hook
#define RBM_PROFILER_ENTER_TRASH     RBM_NONE
#define RBM_PROFILER_LEAVE_TRASH     RBM_PROFILER_ENTER_TRASH
#define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

// Which register are int and long values returned in ?
#define REG_INTRET               REG_NA
#define RBM_INTRET               RBM_NONE
#define REG_LNGRET               REG_NA
#define RBM_LNGRET               RBM_NONE

#define REG_FLOATRET             REG_NA
#define RBM_FLOATRET             RBM_NONE
#define RBM_DOUBLERET            RBM_NONE

// The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper
#define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

// The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
#define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

#define RBM_VALIDATE_INDIRECT_CALL_TRASH RBM_NONE
#define REG_VALIDATE_INDIRECT_CALL_ADDR  REG_NA
#define REG_DISPATCH_INDIRECT_CALL_ADDR  REG_NA

#define REG_ASYNC_CONTINUATION_RET REG_NA
#define RBM_ASYNC_CONTINUATION_RET RBM_NONE

#define REG_FPBASE               REG_NA
#define RBM_FPBASE               RBM_NONE
#define STR_FPBASE               ""
#define REG_SPBASE               REG_NA
#define RBM_SPBASE               RBM_NONE
#define STR_SPBASE               ""

#define FIRST_ARG_STACK_OFFS     0

#define MAX_REG_ARG              -1
#define MAX_FLOAT_REG_ARG        -1

#define REG_ARG_FIRST            REG_NA
#define REG_ARG_LAST             REG_NA
#define REG_ARG_FP_FIRST         REG_NA
#define REG_ARG_FP_LAST          REG_NA
#define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

#define REG_ARG_0                REG_NA
#define RBM_ARG_0                RBM_NONE

#define RBM_ARG_REGS             RBM_NONE
#define RBM_FLTARG_REGS          RBM_NONE

// The number of bytes from the end the last probed page that must also be probed, to allow for some
// small SP adjustments without probes. If zero, then the stack pointer can point to the last byte/word
// on the stack guard page, and must be touched before any further "SUB SP".
#define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

// clang-format on

// TODO-WASM: implement the following functions in terms of a "locals registry" that would hold information
// about the registers.

inline bool genIsValidReg(regNumber reg)
{
    NYI_WASM("genIsValidReg");
    return false;
}

inline bool genIsValidIntReg(regNumber reg)
{
    NYI_WASM("genIsValidIntReg");
    return false;
}

inline bool genIsValidIntOrFakeReg(regNumber reg)
{
    NYI_WASM("genIsValidIntOrFakeReg");
    return false;
}

inline bool genIsValidFloatReg(regNumber reg)
{
    NYI_WASM("genIsValidFloatReg");
    return false;
}

inline bool isValidIntArgReg(regNumber reg, CorInfoCallConvExtension callConv)
{
    NYI_WASM("isValidIntArgReg");
    return false;
}

inline bool isValidFloatArgReg(regNumber reg)
{
    NYI_WASM("isValidFloatArgReg");
    return false;
}

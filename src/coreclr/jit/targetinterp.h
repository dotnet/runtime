// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_INTERP)
#error The file should not be included for this platform.
#endif

// clang-format off

// Interpreter bytecode executes in-process, so the JIT matches the VM it is hosted by.
#if defined(HOST_AMD64)
#define CORINFO_ARCH_TARGET      CORINFO_ARCH_X64
#elif defined(HOST_ARM64)
#define CORINFO_ARCH_TARGET      CORINFO_ARCH_ARM64
#elif defined(HOST_ARM)
#define CORINFO_ARCH_TARGET      CORINFO_ARCH_ARM
#elif defined(HOST_RISCV64)
#define CORINFO_ARCH_TARGET      CORINFO_ARCH_RISCV64
#elif defined(HOST_LOONGARCH64)
#define CORINFO_ARCH_TARGET      CORINFO_ARCH_LOONGARCH64
#else
#error Unsupported host architecture for the interpreter target
#endif

#define CPU_LOAD_STORE_ARCH      1
#define CPU_HAS_FP_SUPPORT       1
#define CPU_HAS_BYTE_REGS        0

// All arguments live on the interpreter stack; there is no outgoing argument area to preallocate.
#define FEATURE_FIXED_OUT_ARGS   0       // Preallocate the outgoing arg area in the prolog
#define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
#define FEATURE_MULTIREG_STRUCT_PROMOTE 0  // True when we want to promote fields of a multireg struct into registers
#define FEATURE_FASTTAILCALL     0       // Tail calls made as epilog+jmp
#define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
#define FEATURE_IMPLICIT_BYREFS       0  // Structs are passed by value on the interpreter stack
#define FEATURE_MULTIREG_ARGS_OR_RET  0  // Support for passing and/or returning single values in more than one register
#define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
#define FEATURE_MULTIREG_RET          0  // Support for returning a single value in more than one register
#define FEATURE_HAS_ZERO_REG          0  // Target does not have a hardware "zero register" usable as a containable source
#define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (long/double).
#define MAX_PASS_MULTIREG_BYTES       0  // Maximum size of a struct that could be passed in more than one register
#define MAX_RET_MULTIREG_BYTES        0  // Maximum size of a struct that could be returned in more than one register
#define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass a single argument in multiple registers.
#define MAX_RET_REG_COUNT             1  // Maximum registers used to return a value.
#define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
                                         // This is also the maximum number of registers for a MultiReg node.

#define NOGC_WRITE_BARRIERS      0       // No specialized WriteBarrier JIT Helpers
#define USER_ARGS_COME_LAST      0
#ifdef TARGET_64BIT
#define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
#else
#define TARGET_POINTER_SIZE      4       // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define ETW_EBP_FRAMED           0       // No native frame pointer chaining

#define CSE_CONSTS               1       // Enable if we want to CSE constants
#define LOWER_DECOMPOSE_LONGS    0       // Interpreter has native 64-bit integer opcodes on every host
#define TARGET_MASKS_SHIFTS      1       // Shift and rotate instructions implicitly mask their count to the operand bit size
#define TARGET_HAS_MULHI         0       // Does not support GT_MULHI, the high bits of an NxN multiply
#define EMIT_TRACK_STACK_DEPTH   0       // No need to track arg pushes/pops
#define EMIT_GENERATE_GCINFO     1       // Codegen and emit generate GC info for interpreter stack slots

// The interpreter has no register file; every value lives in a slot on the interpreter stack.
#define REG_FP_FIRST             REG_NA
#define REG_FP_LAST              REG_NA
#define FIRST_FP_ARGREG          REG_NA
#define LAST_FP_ARGREG           REG_NA

#define HAS_FIXED_REGISTER_SET   0
#define REGNUM_BITS              1       // number of bits in a REG_*
#define REGSIZE_BYTES            TARGET_POINTER_SIZE // number of bytes in one general purpose register
#define FP_REGSIZE_BYTES         8       // number of bytes in one FP register
#define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP register that are saved/restored, for callee-saved registers

#define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

#define CODE_ALIGN               1       // code alignment requirement
#define STACK_ALIGN              16      // INTERP_STACK_ALIGNMENT: alignment of the interpreter stack at the start of a frame

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

// The defines below that affect CSE heuristics are given 'average native target' values.
#define CNT_CALLEE_SAVED          (0)
#define CNT_CALLEE_SAVED_FOR_CSE  (8)
#define CNT_CALLEE_TRASH          (0)
#define CNT_CALLEE_TRASH_FOR_CSE  (8)
#define CNT_CALLEE_ENREG          (CNT_CALLEE_SAVED)
#define CNT_CALLEE_ENREG_FOR_CSE  (CNT_CALLEE_SAVED_FOR_CSE)

#define CNT_CALLEE_SAVED_FLOAT          (0)
#define CNT_CALLEE_SAVED_FLOAT_FOR_CSE  (10)
#define CNT_CALLEE_TRASH_FLOAT          (0)
#define CNT_CALLEE_TRASH_FLOAT_FOR_CSE  (10)
#define CNT_CALLEE_ENREG_FLOAT          (CNT_CALLEE_SAVED_FLOAT)
#define CNT_CALLEE_ENREG_FLOAT_FOR_CSE  (CNT_CALLEE_SAVED_FLOAT_FOR_CSE)

#define CNT_CALLEE_SAVED_MASK          (0)
#define CNT_CALLEE_SAVED_MASK_FOR_CSE  (0)
#define CNT_CALLEE_TRASH_MASK          (0)
#define CNT_CALLEE_TRASH_MASK_FOR_CSE  (0)
#define CNT_CALLEE_ENREG_MASK          (CNT_CALLEE_SAVED_MASK)
#define CNT_CALLEE_ENREG_MASK_FOR_CSE  (CNT_CALLEE_SAVED_MASK_FOR_CSE)

#define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
#define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

#define REG_TMP_0                REG_NA

#define REG_GSCOOKIE_TMP_0       REG_NA
#define REG_GSCOOKIE_TMP_1       REG_NA

#define REG_SHIFT                REG_NA
#define RBM_SHIFT                RBM_ALLINT

#define REG_SCRATCH              REG_NA

#define REG_OPT_RSVD             REG_NA
#define RBM_OPT_RSVD             RBM_NONE

#define REG_EXCEPTION_OBJECT     REG_NA
#define RBM_EXCEPTION_OBJECT     RBM_NONE

#define REG_JUMP_THUNK_PARAM     REG_NA
#define RBM_JUMP_THUNK_PARAM     RBM_NONE

#define REG_WRITE_BARRIER_DST          REG_NA
#define RBM_WRITE_BARRIER_DST          RBM_NONE

#define REG_WRITE_BARRIER_SRC          REG_NA
#define RBM_WRITE_BARRIER_SRC          RBM_NONE

#define RBM_CALLEE_TRASH_NOGC          RBM_NONE

#define RBM_CALLEE_TRASH_WRITEBARRIER         RBM_NONE

#define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

#define REG_SECRET_STUB_PARAM     REG_NA
#define RBM_SECRET_STUB_PARAM     RBM_NONE

#define REG_R2R_INDIRECT_PARAM          REG_NA
#define RBM_R2R_INDIRECT_PARAM          RBM_NONE

#define REG_INDIRECT_CALL_TARGET_REG    REG_NA

#define REG_FIRST                REG_NA
#define REG_INT_FIRST            REG_NA
#define REG_INT_LAST             REG_NA
#define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
#define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
#define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

#define REG_PROFILER_ENTER_ARG_FUNC_ID    REG_NA
#define RBM_PROFILER_ENTER_ARG_FUNC_ID    RBM_NONE
#define REG_PROFILER_ENTER_ARG_CALLER_SP  REG_NA
#define RBM_PROFILER_ENTER_ARG_CALLER_SP  RBM_NONE
#define REG_PROFILER_LEAVE_ARG_FUNC_ID    REG_PROFILER_ENTER_ARG_FUNC_ID
#define RBM_PROFILER_LEAVE_ARG_FUNC_ID    RBM_PROFILER_ENTER_ARG_FUNC_ID
#define REG_PROFILER_LEAVE_ARG_CALLER_SP  REG_PROFILER_ENTER_ARG_CALLER_SP
#define RBM_PROFILER_LEAVE_ARG_CALLER_SP  RBM_PROFILER_ENTER_ARG_CALLER_SP

#define RBM_PROFILER_ENTER_TRASH     RBM_NONE
#define RBM_PROFILER_LEAVE_TRASH     RBM_PROFILER_ENTER_TRASH
#define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

#define REG_INTRET               REG_NA
#define RBM_INTRET               RBM_NONE
#define REG_LNGRET               REG_NA
#define RBM_LNGRET               RBM_NONE

#define REG_FLOATRET             REG_NA
#define RBM_FLOATRET             RBM_NONE
#define RBM_DOUBLERET            RBM_NONE

#define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

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

#define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

// clang-format on

#include "registeropsinterp.h"

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_LOONGARCH64)
#error The file should not be included for this platform.
#endif

// NOTE for LoongArch64:
//  The `REG_R21` which alias `REG_X0` is specially reserved !!!
//  It can be used only manully and very carefully!!!

// clang-format off
  #define CPU_LOAD_STORE_ARCH      1
  #define CPU_HAS_FP_SUPPORT       1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

  #define CPBLK_UNROLL_LIMIT       64      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     64      // Upper bound to let the code generator to loop unroll InitBlk.

#ifdef FEATURE_SIMD
#pragma error("SIMD Unimplemented yet LOONGARCH")
  #define ALIGN_SIMD_TYPES         1       // whether SIMD type locals are to be aligned
  #define FEATURE_PARTIAL_SIMD_CALLEE_SAVE 1 // Whether SIMD registers are partially saved at calls
#endif // FEATURE_SIMD

  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    0       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE 0  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL     1       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     1       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        0       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when the flags need to be set
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register
  #define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (8-byte).
  #define MAX_PASS_MULTIREG_BYTES      16  // Maximum size of a struct that could be passed in more than one register
  #define MAX_RET_MULTIREG_BYTES       16  // Maximum size of a struct that could be returned in more than one register (Max is an HFA of 2 doubles)
  #define MAX_ARG_REG_COUNT             2  // Maximum registers used to pass a single argument in multiple registers.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.
  #define MAX_MULTIREG_COUNT            2  // Maxiumum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.

  #define NOGC_WRITE_BARRIERS      1       // We have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1       // This is something of a workaround.  For both ARM and AMD64, the frame size is fixed, so we don't really
                                           // need to track stack depth, but this is currently necessary to get GC information reported at call sites.
  #define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define FEATURE_EH_FUNCLETS      1
  #define FEATURE_EH_CALLFINALLY_THUNKS 1  // Generate call-to-finally code in "thunks" in the enclosing EH region, protected by "cloned finally" clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use REG_FP as a scratch register and must setup the frame pointer for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  #define REG_FP_FIRST             REG_F0
  #define REG_FP_LAST              REG_F31
  #define FIRST_FP_ARGREG          REG_F0
  #define LAST_FP_ARGREG           REG_F7

  #define REGNUM_BITS              6       // number of bits in a REG_* within registerloongarch64.h
  #define REGSIZE_BYTES            8       // number of bytes in one general purpose register
  #define FP_REGSIZE_BYTES         8       // number of bytes in one FP register
  #define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP register that are saved/restored.

  #define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

  #define CODE_ALIGN               4       // code alignment requirement
  #define STACK_ALIGN              16      // stack alignment requirement

  #define RBM_INT_CALLEE_SAVED    (RBM_S0|RBM_S1|RBM_S2|RBM_S3|RBM_S4|RBM_S5|RBM_S6|RBM_S7|RBM_S8)
  #define RBM_INT_CALLEE_TRASH    (RBM_A0|RBM_A1|RBM_A2|RBM_A3|RBM_A4|RBM_A5|RBM_A6|RBM_A7|RBM_T0|RBM_T1|RBM_T2|RBM_T3|RBM_T4|RBM_T5|RBM_T6|RBM_T7|RBM_T8)
  #define RBM_FLT_CALLEE_SAVED    (RBM_F24|RBM_F25|RBM_F26|RBM_F27|RBM_F28|RBM_F29|RBM_F30|RBM_F31)
  #define RBM_FLT_CALLEE_TRASH    (RBM_F0|RBM_F1|RBM_F2|RBM_F3|RBM_F4|RBM_F5|RBM_F6|RBM_F7)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)

  #define REG_DEFAULT_HELPER_CALL_TARGET REG_T2
  #define RBM_DEFAULT_HELPER_CALL_TARGET RBM_T2

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)
  #define RBM_ALLFLOAT            (RBM_FLT_CALLEE_SAVED | RBM_FLT_CALLEE_TRASH)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT

  // REG_VAR_ORDER is: (CALLEE_TRASH & ~CALLEE_TRASH_NOGC), CALLEE_TRASH_NOGC, CALLEE_SAVED
  #define REG_VAR_ORDER            REG_A0,REG_A1,REG_A2,REG_A3,REG_A4,REG_A5,REG_A6,REG_A7, \
                                   REG_T0,REG_T1,REG_T2,REG_T3,REG_T4,REG_T5,REG_T6,REG_T7,REG_T8, \
                                   REG_CALLEE_SAVED_ORDER

  #define REG_VAR_ORDER_FLT        REG_F12,REG_F13,REG_F14,REG_F15,REG_F16,REG_F17,REG_F18,REG_F19, \
                                   REG_F2,REG_F3,REG_F4,REG_F5,REG_F6,REG_F7,REG_F8,REG_F9,REG_F10, \
                                   REG_F20,REG_F21,REG_F22,REG_F23, \
                                   REG_F24,REG_F25,REG_F26,REG_F27,REG_F28,REG_F29,REG_F30,REG_F31, \
                                   REG_F1,REG_F0

  #define REG_CALLEE_SAVED_ORDER   REG_S0,REG_S1,REG_S2,REG_S3,REG_S4,REG_S5,REG_S6,REG_S7,REG_S8
  #define RBM_CALLEE_SAVED_ORDER   RBM_S0,RBM_S1,RBM_S2,RBM_S3,RBM_S4,RBM_S5,RBM_S6,RBM_S7,RBM_S8

  #define CNT_CALLEE_SAVED        (10)             //s0-s8,fp.
  #define CNT_CALLEE_TRASH        (17)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)

  #define CNT_CALLEE_SAVED_FLOAT  (8)
  #define CNT_CALLEE_TRASH_FLOAT  (24)

  #define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

  #define REG_TMP_0                REG_T0

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_T0
  #define REG_GSCOOKIE_TMP_1       REG_T1

  // register to hold shift amount; no special register is required on LOONGARCH64.
  #define REG_SHIFT                REG_NA
  #define RBM_SHIFT                RBM_ALLINT

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_T0

  // This is a float scratch register that does not conflict with the argument registers
  #define REG_SCRATCH_FLT          REG_F11

  // This is a general register that can be optionally reserved for other purposes during codegen
  #define REG_OPT_RSVD             REG_T1
  #define RBM_OPT_RSVD             RBM_T1

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_A0
  #define RBM_EXCEPTION_OBJECT     RBM_A0

  #define REG_JUMP_THUNK_PARAM     REG_T2
  #define RBM_JUMP_THUNK_PARAM     RBM_T2

  // LOONGARCH64 write barrier ABI (see vm/loongarch64/asmhelpers.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     On entry:
  //       t6: the destination address (LHS of the assignment)
  //       t7: the object reference (RHS of the assignment)
  //     On exit:
  //       t0: trashed
  //       t1: trashed
  //       t3: trashed
  //       t4: trashed
  //       t6: incremented by 8
  //       t7: trashed
  // CORINFO_HELP_ASSIGN_BYREF (JIT_ByRefWriteBarrier):
  //     On entry:
  //       t8: the source address (points to object reference to write)
  //       t6: the destination address (object reference written here)
  //     On exit:
  //       t8: incremented by 8
  //       t6: incremented by 8
  //

  #define REG_WRITE_BARRIER_DST          REG_T6
  #define RBM_WRITE_BARRIER_DST          RBM_T6

  #define REG_WRITE_BARRIER_SRC          REG_T7
  #define RBM_WRITE_BARRIER_SRC          RBM_T7

  #define REG_WRITE_BARRIER_DST_BYREF    REG_T6
  #define RBM_WRITE_BARRIER_DST_BYREF    RBM_T6

  #define REG_WRITE_BARRIER_SRC_BYREF    REG_T8
  #define RBM_WRITE_BARRIER_SRC_BYREF    RBM_T8

  #define RBM_CALLEE_TRASH_NOGC          (RBM_T0|RBM_T1|RBM_T3|RBM_T4|RBM_T6|RBM_T7|RBM_DEFAULT_HELPER_CALL_TARGET)

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER         (RBM_WRITE_BARRIER_DST|RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF | RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that a0 and a1 are still valid byref pointers after this helper call, despite their value being changed.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF RBM_CALLEE_TRASH_NOGC

  // GenericPInvokeCalliHelper VASigCookie Parameter
  #define REG_PINVOKE_COOKIE_PARAM          REG_T3
  #define RBM_PINVOKE_COOKIE_PARAM          RBM_T3

  // GenericPInvokeCalliHelper unmanaged target Parameter
  #define REG_PINVOKE_TARGET_PARAM          REG_T2
  #define RBM_PINVOKE_TARGET_PARAM          RBM_T2

  // IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM     REG_T2
  #define RBM_SECRET_STUB_PARAM     RBM_T2

  // R2R indirect call. Use the same registers as VSD
  #define REG_R2R_INDIRECT_PARAM          REG_T8
  #define RBM_R2R_INDIRECT_PARAM          RBM_T8

  #define REG_INDIRECT_CALL_TARGET_REG    REG_T6

  // Registers used by PInvoke frame setup
  #define REG_PINVOKE_FRAME        REG_T0
  #define RBM_PINVOKE_FRAME        RBM_T0
  #define REG_PINVOKE_TCB          REG_T1
  #define RBM_PINVOKE_TCB          RBM_T1
  #define REG_PINVOKE_SCRATCH      REG_T1
  #define RBM_PINVOKE_SCRATCH      RBM_T1

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_S8
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
  #define RBM_PROFILER_ENTER_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_LEAVE_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_A0
  #define RBM_INTRET               RBM_A0
  #define REG_LNGRET               REG_A0
  #define RBM_LNGRET               RBM_A0
  // second return register for 16-byte structs
  #define REG_INTRET_1             REG_A1
  #define RBM_INTRET_1             RBM_A1

  #define REG_FLOATRET             REG_F0
  #define RBM_FLOATRET             RBM_F0
  #define RBM_DOUBLERET            RBM_F0
  #define REG_FLOATRET_1           REG_F1
  #define RBM_FLOATRET_1           RBM_F1
  #define RBM_DOUBLERET_1          RBM_F1

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper
  #define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH & ~(RBM_A0 | RBM_A1 | RBM_A2 | RBM_A3 | RBM_A4 | RBM_A5 | RBM_A6 | RBM_A7 | RBM_T3))
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_T3
  #define REG_DISPATCH_INDIRECT_CALL_ADDR REG_T0

  #define REG_FPBASE               REG_FP
  #define RBM_FPBASE               RBM_FP
  #define STR_FPBASE               "fp"
  #define REG_SPBASE               REG_SP
  #define RBM_SPBASE               RBM_SP
  #define STR_SPBASE               "sp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved FP and return address

  #define MAX_REG_ARG              8
  #define MAX_FLOAT_REG_ARG        8

  #define REG_ARG_FIRST            REG_A0
  #define REG_ARG_LAST             REG_A7
  #define REG_ARG_FP_FIRST         REG_F0
  #define REG_ARG_FP_LAST          REG_F7
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_A0
  #define REG_ARG_1                REG_A1
  #define REG_ARG_2                REG_A2
  #define REG_ARG_3                REG_A3
  #define REG_ARG_4                REG_A4
  #define REG_ARG_5                REG_A5
  #define REG_ARG_6                REG_A6
  #define REG_ARG_7                REG_A7

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];

  #define RBM_ARG_0                RBM_A0
  #define RBM_ARG_1                RBM_A1
  #define RBM_ARG_2                RBM_A2
  #define RBM_ARG_3                RBM_A3
  #define RBM_ARG_4                RBM_A4
  #define RBM_ARG_5                RBM_A5
  #define RBM_ARG_6                RBM_A6
  #define RBM_ARG_7                RBM_A7

  #define REG_FLTARG_0             REG_F0
  #define REG_FLTARG_1             REG_F1
  #define REG_FLTARG_2             REG_F2
  #define REG_FLTARG_3             REG_F3
  #define REG_FLTARG_4             REG_F4
  #define REG_FLTARG_5             REG_F5
  #define REG_FLTARG_6             REG_F6
  #define REG_FLTARG_7             REG_F7

  #define RBM_FLTARG_0             RBM_F0
  #define RBM_FLTARG_1             RBM_F1
  #define RBM_FLTARG_2             RBM_F2
  #define RBM_FLTARG_3             RBM_F3
  #define RBM_FLTARG_4             RBM_F4
  #define RBM_FLTARG_5             RBM_F5
  #define RBM_FLTARG_6             RBM_F6
  #define RBM_FLTARG_7             RBM_F7

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3|RBM_ARG_4|RBM_ARG_5|RBM_ARG_6|RBM_ARG_7)
  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3|RBM_FLTARG_4|RBM_FLTARG_5|RBM_FLTARG_6|RBM_FLTARG_7)

  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define B_DIST_SMALL_MAX_NEG  (-131072)
  #define B_DIST_SMALL_MAX_POS  (+131071)

  #define OFFSET_DIST_SMALL_MAX_NEG   (-2048)
  #define OFFSET_DIST_SMALL_MAX_POS   (+2047)

  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

// clang-format on

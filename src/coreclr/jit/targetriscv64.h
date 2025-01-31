// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_RISCV64)
#error The file should not be included for this platform.
#endif

// clang-format off
  #define CPU_LOAD_STORE_ARCH      1
  #define CPU_HAS_FP_SUPPORT       1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0


#ifdef FEATURE_SIMD
#pragma error("SIMD Unimplemented yet RISCV64")
#endif // FEATURE_SIMD

  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    1       // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE 1  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL     1       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     1       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_IMPLICIT_BYREFS       1  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register
  #define MAX_PASS_SINGLEREG_BYTES     8  // Maximum size of a struct passed in a single register (8-byte vector).
  #define MAX_PASS_MULTIREG_BYTES      16  // Maximum size of a struct that could be passed in more than one register
  #define MAX_RET_MULTIREG_BYTES       16  // Maximum size of a struct that could be returned in more than one register (Max is an HFA or 2 doubles)
  #define MAX_ARG_REG_COUNT             2  // Maximum registers used to pass a single argument in multiple registers.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.
  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.

  #define NOGC_WRITE_BARRIERS      1       // We have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1       // This is something of a workaround.  For both ARM and AMD64, the frame size is fixed, so we don't really
                                           // need to track stack depth, but this is currently necessary to get GC information reported at call sites.
  #define TARGET_POINTER_SIZE      8       // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH               1       // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use REG_FP as a scratch register and must setup the frame pointer for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants

  #define REG_FP_FIRST             REG_FT0
  #define REG_FP_LAST              REG_FT11
  #define FIRST_FP_ARGREG          REG_FA0
  #define LAST_FP_ARGREG           REG_FA7

  #define REGNUM_BITS              6       // number of bits in a REG_*
  #define REGSIZE_BYTES            8       // number of bytes in one general purpose register
  #define FP_REGSIZE_BYTES         8      // number of bytes in one FP/SIMD register
  #define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP/SIMD register that are saved/restored, for callee-saved registers

  #define MIN_ARG_AREA_FOR_CALL    0       // Minimum required outgoing argument space for a call.

  #define CODE_ALIGN               4       // code alignment requirement
  #define STACK_ALIGN              16      // stack alignment requirement

  #define FIRST_INT_CALLEE_SAVED  REG_S1
  #define LAST_INT_CALLEE_SAVED   REG_S11
  #define RBM_INT_CALLEE_SAVED    (RBM_S1|RBM_S2|RBM_S3|RBM_S4|RBM_S5|RBM_S6|RBM_S7|RBM_S8|RBM_S9|RBM_S10|RBM_S11)
  #define RBM_INT_CALLEE_TRASH    (RBM_A0|RBM_A1|RBM_A2|RBM_A3|RBM_A4|RBM_A5|RBM_A6|RBM_A7|RBM_T0|RBM_T1|RBM_T2|RBM_T3|RBM_T4|RBM_T5|RBM_T6)
  #define FIRST_FLT_CALLEE_SAVED  REG_FS0
  #define LAST_FLT_CALLEE_SAVED   REG_FS11
  #define RBM_FLT_CALLEE_SAVED    (RBM_FS0|RBM_FS1|RBM_FS2|RBM_FS3|RBM_FS4|RBM_FS5|RBM_FS6|RBM_FS7|RBM_FS8|RBM_FS9|RBM_FS10|RBM_FS11)
  #define RBM_FLT_CALLEE_TRASH    (RBM_FA0|RBM_FA1|RBM_FA2|RBM_FA3|RBM_FA4|RBM_FA5|RBM_FA6|RBM_FA7|RBM_FT0|RBM_FT1|RBM_FT2|RBM_FT3|RBM_FT4|RBM_FT5|RBM_FT6|RBM_FT7|RBM_FT8|RBM_FT9|RBM_FT10|RBM_FT11)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH)

  #define REG_DEFAULT_HELPER_CALL_TARGET REG_T2
  #define RBM_DEFAULT_HELPER_CALL_TARGET RBM_T2

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)
  #define RBM_ALLFLOAT            (RBM_FLT_CALLEE_SAVED | RBM_FLT_CALLEE_TRASH)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT

  // REG_VAR_ORDER is: (CALLEE_TRASH & ~CALLEE_TRASH_NOGC), CALLEE_TRASH_NOGC, CALLEE_SAVED
  #define REG_VAR_ORDER            REG_A0,REG_A1,REG_A2,REG_A3,REG_A4,REG_A5,REG_A6,REG_A7, \
                                   REG_T0,REG_T1,REG_T2,REG_T3,REG_T4,REG_T5,REG_T6, \
                                   REG_S1,REG_S2,REG_S3,REG_S4,REG_S5,REG_S6,REG_S7,REG_S8,REG_S9,REG_S10,REG_S11

  #define REG_VAR_ORDER_FLT        REG_FT4, REG_FT5, REG_FT6, REG_FT7, REG_FT8, REG_FT9, REG_FT10, REG_FT11, \
                                   REG_FA2, REG_FA3, REG_FA4, REG_FA5, REG_FA6, REG_FA7, \
                                   REG_FT0, REG_FT1, REG_FT2, REG_FT3, \
                                   REG_FS6, REG_FS7, REG_FS8, REG_FS9, REG_FS10, REG_FS11, REG_FS2, REG_FS3, REG_FS4, REG_FS5, REG_FS0, REG_FS1, \
                                   REG_FA1, REG_FA0

  #define RBM_CALL_GC_REGS_ORDER   RBM_S1,RBM_S2,RBM_S3,RBM_S4,RBM_S5,RBM_S6,RBM_S7,RBM_S8,RBM_S9,RBM_S10,RBM_S11,RBM_INTRET,RBM_INTRET_1
  #define RBM_CALL_GC_REGS         (RBM_S1|RBM_S2|RBM_S3|RBM_S4|RBM_S5|RBM_S6|RBM_S7|RBM_S8|RBM_S9|RBM_S10|RBM_S11|RBM_INTRET|RBM_INTRET_1)

  #define CNT_CALLEE_SAVED        (11)
  #define CNT_CALLEE_TRASH        (15)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)
  #define CNT_CALL_GC_REGS        (CNT_CALLEE_SAVED+2)

  #define CNT_CALLEE_SAVED_FLOAT  (12)
  #define CNT_CALLEE_TRASH_FLOAT  (20)

  #define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

  #define REG_TMP_0                REG_T0

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_T0
  #define REG_GSCOOKIE_TMP_1       REG_T1

  // register to hold shift amount; no special register is required on ARM64.
  #define REG_SHIFT                REG_NA
  #define RBM_SHIFT                RBM_ALLINT

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_T0

  // This is a general register that can be optionally reserved for other purposes during codegen
  #define REG_OPT_RSVD             REG_T6
  #define RBM_OPT_RSVD             RBM_T6

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_A0
  #define RBM_EXCEPTION_OBJECT     RBM_A0

  #define REG_JUMP_THUNK_PARAM     REG_T2
  #define RBM_JUMP_THUNK_PARAM     RBM_T2

  #define REG_WRITE_BARRIER_DST          REG_T3
  #define RBM_WRITE_BARRIER_DST          RBM_T3

  #define REG_WRITE_BARRIER_SRC          REG_T4
  #define RBM_WRITE_BARRIER_SRC          RBM_T4

  #define REG_WRITE_BARRIER_DST_BYREF    REG_T3
  #define RBM_WRITE_BARRIER_DST_BYREF    RBM_T3

  #define REG_WRITE_BARRIER_SRC_BYREF    REG_T5
  #define RBM_WRITE_BARRIER_SRC_BYREF    RBM_T5

  #define RBM_CALLEE_TRASH_NOGC          (RBM_T0|RBM_T1|RBM_T2|RBM_T4|RBM_T6|RBM_DEFAULT_HELPER_CALL_TARGET)

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER         (RBM_WRITE_BARRIER_DST|RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF | RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that t3 and t5 are still valid byref pointers after this helper call, despite their value being changed.
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
  #define REG_R2R_INDIRECT_PARAM          REG_T5
  #define RBM_R2R_INDIRECT_PARAM          RBM_T5

  // JMP Indirect call register
  #define REG_INDIRECT_CALL_TARGET_REG    REG_T5

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_T6
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // The following registers are used in emitting Enter/Leave/Tailcall profiler callbacks
  #define REG_PROFILER_ENTER_ARG_FUNC_ID    REG_T0
  #define RBM_PROFILER_ENTER_ARG_FUNC_ID    RBM_T0
  #define REG_PROFILER_ENTER_ARG_CALLER_SP  REG_T1
  #define RBM_PROFILER_ENTER_ARG_CALLER_SP  RBM_T1
  #define REG_PROFILER_LEAVE_ARG_FUNC_ID    REG_PROFILER_ENTER_ARG_FUNC_ID
  #define RBM_PROFILER_LEAVE_ARG_FUNC_ID    RBM_PROFILER_ENTER_ARG_FUNC_ID
  #define REG_PROFILER_LEAVE_ARG_CALLER_SP  REG_PROFILER_ENTER_ARG_CALLER_SP
  #define RBM_PROFILER_LEAVE_ARG_CALLER_SP  RBM_PROFILER_ENTER_ARG_CALLER_SP

  // The registers trashed by profiler enter/leave/tailcall hook
  #define RBM_PROFILER_ENTER_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_LEAVE_TRASH     RBM_PROFILER_ENTER_TRASH
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_A0
  #define RBM_INTRET               RBM_A0
  #define REG_LNGRET               REG_A0
  #define RBM_LNGRET               RBM_A0
  // second return register for 16-byte structs
  #define REG_INTRET_1             REG_A1
  #define RBM_INTRET_1             RBM_A1

  #define REG_FLOATRET             REG_FA0
  #define RBM_FLOATRET             RBM_FA0
  #define RBM_DOUBLERET            RBM_FA0
  #define REG_FLOATRET_1           REG_FA1
  #define RBM_FLOATRET_1           RBM_FA1
  #define RBM_DOUBLERET_1          RBM_FA1

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
  #define RBM_SPBASE               RBM_SP     // reuse the RBM for REG_ZR
  #define STR_SPBASE               "sp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved FP and return address

  #define MAX_REG_ARG              8
  #define MAX_FLOAT_REG_ARG        8

  #define REG_ARG_FIRST            REG_A0
  #define REG_ARG_LAST             REG_A7
  #define REG_ARG_FP_FIRST         REG_FA0
  #define REG_ARG_FP_LAST          REG_FA7
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

  #define RBM_ARG_REGS            (RBM_A0|RBM_A1|RBM_A2|RBM_A3|RBM_A4|RBM_A5|RBM_A6|RBM_A7)
  #define RBM_FLTARG_REGS         (RBM_FA0|RBM_FA1|RBM_FA2|RBM_FA3|RBM_FA4|RBM_FA5|RBM_FA6|RBM_FA7)

  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];

  #define B_DIST_SMALL_MAX_NEG  (-4096)
  #define B_DIST_SMALL_MAX_POS  (+4095)

  // The number of bytes from the end the last probed page that must also be probed, to allow for some
  // small SP adjustments without probes. If zero, then the stack pointer can point to the last byte/word
  // on the stack guard page, and must be touched before any further "SUB SP".
  #define STACK_PROBE_BOUNDARY_THRESHOLD_BYTES 0

// clang-format on

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_ARM64)
#error The file should not be included for this platform.
#endif

// clang-format off
  #define CPU_LOAD_STORE_ARCH      1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

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
  #define FEATURE_IMPLICIT_BYREFS       1  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register
  #define MAX_PASS_SINGLEREG_BYTES     16  // Maximum size of a struct passed in a single register (16-byte vector).
  #define MAX_PASS_MULTIREG_BYTES      64  // Maximum size of a struct that could be passed in more than one register (max is 4 16-byte vectors using an HVA)
  #define MAX_RET_MULTIREG_BYTES       64  // Maximum size of a struct that could be returned in more than one register (Max is an HVA of 4 16-byte vectors)
  #define MAX_ARG_REG_COUNT             4  // Maximum registers used to pass a single argument in multiple registers. (max is 4 128-bit vectors using an HVA)
  #define MAX_RET_REG_COUNT             4  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            4  // Maximum number of registers defined by a single instruction (including calls).
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
  #define REG_PREDICATE_FIRST      REG_P0
  #define REG_PREDICATE_LAST       REG_P15
  #define REG_PREDICATE_LOW_LAST   REG_P7  // Some instructions can only use the first half of the predicate registers.
  #define REG_PREDICATE_HIGH_FIRST REG_P8  // Similarly, some instructions can only use the second half of the predicate registers.
  #define REG_PREDICATE_HIGH_LAST  REG_P15

  static_assert_no_msg(REG_PREDICATE_HIGH_LAST == REG_PREDICATE_LAST);

  #define REGNUM_BITS              6       // number of bits in a REG_*
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

  // On ARM64 we do not use any additional callee-saves for ENC
  // since there are so many volatile registers available, and
  // callee saves have to be aggressively saved by ENC codegen
  // because a future version could use them.
  #define RBM_ENC_CALLEE_SAVED     0

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_IP0
  #define REG_GSCOOKIE_TMP_1       REG_IP1

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

  #define _RBM_CALLEE_TRASH_NOGC        (RBM_R12|RBM_R15|RBM_IP0|RBM_IP1|RBM_DEFAULT_HELPER_CALL_TARGET)
  #define AllRegsMask_CALLEE_TRASH_NOGC        GprRegsMask(_RBM_CALLEE_TRASH_NOGC)

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define AllRegsMask_CALLEE_TRASH_WRITEBARRIER         GprRegsMask(RBM_R14 | _RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define AllRegsMask_CALLEE_GCTRASH_WRITEBARRIER       AllRegsMask_CALLEE_TRASH_NOGC

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define AllRegsMask_CALLEE_TRASH_WRITEBARRIER_BYREF   GprRegsMask(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF | _RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that x13 and x14 are still valid byref pointers after this helper call, despite their value being changed.
  #define AllRegsMask_CALLEE_GCTRASH_WRITEBARRIER_BYREF AllRegsMask_CALLEE_TRASH_NOGC

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
  #define AllRegsMask_PROFILER_ENTER_TRASH     (AllRegsMask_CALLEE_TRASH | AllRegsMask(~(RBM_ARG_REGS|RBM_ARG_RET_BUFF|RBM_FP), ~RBM_FLTARG_REGS))
  #define AllRegsMask_PROFILER_LEAVE_TRASH  AllRegsMask_PROFILER_ENTER_TRASH
  #define AllRegsMask_PROFILER_TAILCALL_TRASH  AllRegsMask_PROFILER_ENTER_TRASH

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
  #define AllRegsMask_STOP_FOR_GC_TRASH    AllRegsMask_CALLEE_TRASH

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define AllRegsMask_INIT_PINVOKE_FRAME_TRASH  AllRegsMask_CALLEE_TRASH

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH & ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R15))
  #define AllRegsMask_VALIDATE_INDIRECT_CALL_TRASH GprRegsMask(RBM_VALIDATE_INDIRECT_CALL_TRASH)
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_R15
  #define REG_DISPATCH_INDIRECT_CALL_ADDR REG_R9

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
  extern const regMaskGpr intArgMasks[MAX_REG_ARG];

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
  extern const regMaskFloat fltArgMasks[MAX_FLOAT_REG_ARG];

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

// clang-format on

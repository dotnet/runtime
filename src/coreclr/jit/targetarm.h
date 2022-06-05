// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_ARM)
#error The file should not be included for this platform.
#endif

// clang-format off
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
  #define FEATURE_FASTTAILCALL     1       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     1       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.
  #define FEATURE_SET_FLAGS        1       // Set to true to force the JIT to mark the trees with GTF_SET_FLAGS when the flags need to be set
  #define FEATURE_IMPLICIT_BYREFS       0  // Support for struct parameters passed via pointers to shadow copies
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

  #define REG_WRITE_BARRIER_DST          REG_ARG_0
  #define RBM_WRITE_BARRIER_DST          RBM_ARG_0

  #define REG_WRITE_BARRIER_SRC          REG_ARG_1
  #define RBM_WRITE_BARRIER_SRC          RBM_ARG_1

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

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH)
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_R0

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
// clang-format on

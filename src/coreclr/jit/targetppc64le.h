// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_POWERPC64)
#error The file should not be included for this platform.
#endif

// clang-format off
  #define CPU_LOAD_STORE_ARCH      1
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

#ifdef FEATURE_SIMD
  #define ALIGN_SIMD_TYPES         1       // whether SIMD type locals are to be aligned
  #define FEATURE_PARTIAL_SIMD_CALLEE_SAVE 1 // Whether SIMD registers are partially saved a
#endif // FEATURE_SIMD

  #define FEATURE_FIXED_OUT_ARGS   1       // Preallocate the outgoing arg area in the prolog
  #define FEATURE_STRUCTPROMOTE    0 	   // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE 1// True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL 	   0 	   // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls. //Vikas s390x has set this to 0
  #define FEATURE_IMPLICIT_BYREFS       1  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_STRUCT_CLASSIFIER     0  // Uses a classifier function to determine is structs are passed/returned in more than one register

  #define MAX_PASS_SINGLEREG_BYTES     16  // Maximum size of a struct passed in a single register (16-byte vector).
  #define MAX_PASS_MULTIREG_BYTES      64  // Maximum size of a struct that could be passed in more than one register (max is 4 16-byte vectors using an HVA)
  #define MAX_RET_MULTIREG_BYTES       16  // Maximum size of a struct that could be returned in more than one register (16 bytes in r3-r4 for non-HFA structs)
  #define MAX_ARG_REG_COUNT             8  // Maximum registers used to pass a single argument in multiple registers (r3-r10 for structs).
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value (r3-r4 for non-HFA structs up to 16 bytes).
 
  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).

  #define NOGC_WRITE_BARRIERS      1       // We have specialized WriteBarrier JIT Helpers that DO-NOT trash the RBM_CALLEE_TRASH registers
  #define USER_ARGS_COME_LAST      1
  #define EMIT_TRACK_STACK_DEPTH   1       // This is something of a workaround.  For both ARM and AMD64, the frame size is fixed, so we don't really
  #define TARGET_POINTER_SIZE 8 // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH 0 // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.
  #define ETW_EBP_FRAMED           1       // if 1 we cannot use REG_FP as a scratch register and must setup the frame pointer for most methods
  #define CSE_CONSTS               1       // Enable if we want to CSE constants
   
  #define REG_FP_FIRST             REG_F0
  #define REG_FP_LAST              REG_F31
  #define FIRST_FP_ARGREG          REG_F1
  #define LAST_FP_ARGREG           REG_F13

  #define REGNUM_BITS              7       // number of bits in a REG_*
  #define REGSIZE_BYTES            8       // number of bytes in one general purpose register
  #define FP_REGSIZE_BYTES         8       // number of bytes in one FP/SIMD register
  #define FPSAVE_REGSIZE_BYTES     8       // number of bytes in one FP/SIMD register that are saved/restored, for callee-saved registers

  // PPC64LE ELFv2 ABI requires a 32-byte mandatory header at the bottom of each stack frame.
  // The parameter save area starts at offset 32.
  // Each parameter (register or stack) gets an 8-byte slot starting from offset 32.
  // Offset for parameter N = 32 + (N * 8), where N is 0-based.
  #define MIN_ARG_AREA_FOR_CALL    32      // Minimum required outgoing argument space (header only).

  #define CODE_ALIGN               4       // code alignment requirement
  #define STACK_ALIGN              16      // stack alignment requirement

  #define RBM_INT_CALLEE_SAVED    (RBM_R14|RBM_R15|RBM_R16|RBM_R17|RBM_R18|RBM_R19|RBM_R20|RBM_R21|RBM_R22|RBM_R23|RBM_R24|RBM_R25|RBM_R26|RBM_R27|RBM_R28|RBM_R29|RBM_R30)
  #define RBM_INT_CALLEE_TRASH    (RBM_R0|RBM_R2|RBM_R3|RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_R12) //volatile registers
  #define RBM_FLT_CALLEE_SAVED    (RBM_F14|RBM_F15|RBM_F16|RBM_F17|RBM_F18|RBM_F19|RBM_F20|RBM_F21|RBM_F22|RBM_F23|RBM_F24|RBM_F25|RBM_F26|RBM_F27|RBM_F28|RBM_F29|RBM_F30|RBM_F31)
  #define RBM_FLT_CALLEE_TRASH    (RBM_F0|RBM_F1|RBM_F2|RBM_F3|RBM_F4|RBM_F5|RBM_F6|RBM_F7|RBM_F8|RBM_F9|RBM_F10|RBM_F11|RBM_F12|RBM_F13)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)
  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH) //TODO POWERPC64 Vikas

  #define REG_DEFAULT_HELPER_CALL_TARGET REG_R12       //TODO POWERPC64 Vikas
  #define RBM_DEFAULT_HELPER_CALL_TARGET RBM_R12       //TODO POWERPC64 Vikas

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)
  #define RBM_ALLFLOAT            (RBM_FLT_CALLEE_SAVED | RBM_FLT_CALLEE_TRASH)
  #define RBM_ALLDOUBLE            RBM_ALLFLOAT

  // REG_VAR_ORDER is: (CALLEE_TRASH & ~CALLEE_TRASH_NOGC), CALLEE_TRASH_NOGC, CALLEE_SAVED
  #define REG_VAR_ORDER            REG_R0, REG_R3, REG_R4, REG_R5, REG_R6, REG_R7, REG_R8, REG_R9, REG_R10, REG_R11, REG_R12, \
                                   REG_R14, REG_R15, REG_R16, REG_R17, REG_R18, REG_R19, REG_R20, REG_R21, REG_R22, REG_R23, REG_R24, REG_R25, \
                                   REG_R26, REG_R27, REG_R28, REG_R29, REG_R30, REG_R31
  #define REG_VAR_ORDER_FLT        REG_F0, REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7, REG_F8, REG_F9, REG_F10, REG_F11, REG_F12, REG_F13,\
                                   REG_F14, REG_F15, REG_F16, REG_F17, REG_F18, REG_F19, REG_F20, REG_F21, REG_F22, REG_F23, \
                                   REG_F24, REG_F25, REG_F26, REG_F27, REG_F28, REG_F29, REG_F30, REG_F31
  #define RBM_CALL_GC_REGS_ORDER   RBM_R3,RBM_R4,RBM_R5,RBM_R6,RBM_R7,RBM_R8,RBM_R9,RBM_R10
  #define RBM_CALL_GC_REGS         (RBM_INT_CALLEE_SAVED|RBM_INTRET|RBM_INTRET_1)

  #define CNT_CALLEE_SAVED        (18)
  #define CNT_CALLEE_TRASH        (11)
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)
  #define CNT_CALL_GC_REGS        (CNT_CALLEE_SAVED+2)

  #define CNT_CALLEE_SAVED_FLOAT  (18)
  #define CNT_CALLEE_TRASH_FLOAT  (14)
  #define CNT_CALLEE_SAVED_MASK   (0)  //TODO POWERPC64
  #define CNT_CALLEE_TRASH_MASK   (0)   //TODO POWERPC64

  #define CALLEE_SAVED_REG_MAXSZ    (CNT_CALLEE_SAVED * REGSIZE_BYTES)
  #define CALLEE_SAVED_FLOAT_MAXSZ  (CNT_CALLEE_SAVED_FLOAT * FPSAVE_REGSIZE_BYTES)

  // On ARM64 we do not use any additional callee-saves for ENC
  // since there are so many volatile registers available, and
  // callee saves have to be aggressively saved by ENC codegen
  // because a future version could use them.
  #define RBM_ENC_CALLEE_SAVED     0

  // Temporary registers used for the GS cookie check.
  #define REG_GSCOOKIE_TMP_0       REG_R11
  #define REG_GSCOOKIE_TMP_1       REG_R12

  // register to hold shift amount; no special register is required on ARM64.
  #define REG_SHIFT                REG_NA
  #define RBM_SHIFT                RBM_ALLINT

  // This is a general scratch register that does not conflict with the argument registers
  #define REG_SCRATCH              REG_R12

  // This is a general register that can be optionally reserved for other purposes during codegen
  #define REG_OPT_RSVD             REG_R11
  #define RBM_OPT_RSVD             RBM_R11

  // Where is the exception object on entry to the handler block?
  #define REG_EXCEPTION_OBJECT     REG_R3
  #define RBM_EXCEPTION_OBJECT     RBM_R3

  #define REG_JUMP_THUNK_PARAM     REG_R12
  #define RBM_JUMP_THUNK_PARAM     RBM_R12

  /* Read and Write Barriers are yet to be implemented for ppc64le\asmhelpers.asm */

  // ARM64 write barrier ABI (see vm\arm64\asmhelpers.asm, vm\arm64\asmhelpers.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     On entry:
  //       x14: the destination address of the store
  //       x15: the object reference to be stored
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

  /* ppc64lemarker - currently implementing the register set for the lsra, hence removing the unrelated register values */
  /* to fix build errors - please visit back and update values accordingly.					    */

  #define REG_WRITE_BARRIER_DST          REG_R11
  #define RBM_WRITE_BARRIER_DST          RBM_R11

  #define REG_WRITE_BARRIER_SRC          REG_R12
  #define RBM_WRITE_BARRIER_SRC          RBM_R12

  #define REG_WRITE_BARRIER_DST_BYREF    REG_R11
  #define RBM_WRITE_BARRIER_DST_BYREF    RBM_R11

  #define REG_WRITE_BARRIER_SRC_BYREF    REG_R12
  #define RBM_WRITE_BARRIER_SRC_BYREF    RBM_R12



  #define RBM_CALLEE_TRASH_NOGC          (RBM_R11|RBM_R12|RBM_DEFAULT_HELPER_CALL_TARGET)

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
  #define REG_PINVOKE_COOKIE_PARAM          REG_R11
  #define RBM_PINVOKE_COOKIE_PARAM          RBM_R11

  // GenericPInvokeCalliHelper unmanaged target Parameter
  #define REG_PINVOKE_TARGET_PARAM          REG_R13
  #define RBM_PINVOKE_TARGET_PARAM          RBM_R13

  // IL stub's secret MethodDesc parameter (JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM)
  #define REG_SECRET_STUB_PARAM     REG_R13
  #define RBM_SECRET_STUB_PARAM     RBM_R13

  // R2R indirect call. Use the same registers as VSD
  #define REG_R2R_INDIRECT_PARAM          REG_R12
  #define RBM_R2R_INDIRECT_PARAM          RBM_R12

  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_R31
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  // The following registers are used in emitting Enter/Leave/Tailcall profiler callbacks
  #define REG_PROFILER_ENTER_ARG_FUNC_ID    REG_R11
  #define RBM_PROFILER_ENTER_ARG_FUNC_ID    RBM_R11
  #define REG_PROFILER_ENTER_ARG_CALLER_SP  REG_R12
  #define RBM_PROFILER_ENTER_ARG_CALLER_SP  RBM_R12
  #define REG_PROFILER_LEAVE_ARG_FUNC_ID    REG_R11
  #define RBM_PROFILER_LEAVE_ARG_FUNC_ID    RBM_R11
  #define REG_PROFILER_LEAVE_ARG_CALLER_SP  REG_R12
  #define RBM_PROFILER_LEAVE_ARG_CALLER_SP  RBM_R13



  // The registers trashed by profiler enter/leave/tailcall hook
  #define RBM_PROFILER_ENTER_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_LEAVE_TRASH     (RBM_CALLEE_TRASH & ~(RBM_ARG_REGS|RBM_FLTARG_REGS|RBM_FP))
  #define RBM_PROFILER_TAILCALL_TRASH  RBM_PROFILER_LEAVE_TRASH

  // Which register are int and long values returned in ?
  #define REG_INTRET               REG_R3
  #define RBM_INTRET               RBM_R3
  #define RBM_LNGRET               RBM_R3
  // second return register for 16-byte structs
  #define REG_INTRET_1             REG_R4
  #define RBM_INTRET_1             RBM_R4

  #define REG_FLOATRET             REG_F1
  #define RBM_FLOATRET             RBM_F1
  #define RBM_DOUBLERET            RBM_F1

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper
  #define RBM_STOP_FOR_GC_TRASH    RBM_CALLEE_TRASH

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH & ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R9 | RBM_R10 | RBM_R11 | RBM_R12 | RBM_R13))
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_R13
  #define REG_DISPATCH_INDIRECT_CALL_ADDR REG_R11

  #define REG_FPBASE               REG_FP
  #define RBM_FPBASE               RBM_FP
  #define STR_FPBASE               "fp"
  #define REG_SPBASE               REG_SP
  #define RBM_SPBASE               RBM_SP     // reuse the RBM for REG_ZR
  #define STR_SPBASE               "sp"

  #define FIRST_ARG_STACK_OFFS    (2*REGSIZE_BYTES)   // Caller's saved FP and return address

  // On ARM64 the calling convention defines REG_R8 (x8) as an additional argument register.
  // It isn't allocated for the normal user arguments, so it isn't counted by MAX_REG_ARG.
  // Whether we use this register to pass the RetBuff is controlled by the function hasFixedRetBuffReg().
  // It is considered to be the next integer argnum, which is 8.

  #define REG_ARG_RET_BUFF         REG_R3
  #define RBM_ARG_RET_BUFF         RBM_R3
  #define RET_BUFF_ARGNUM          3

  #define MAX_REG_ARG              8
  #define MAX_FLOAT_REG_ARG        13

  #define REG_ARG_FIRST            REG_R3
  #define REG_ARG_LAST             REG_R10
  #define REG_ARG_FP_FIRST         REG_F1
  #define REG_ARG_FP_LAST          REG_F13
  #define INIT_ARG_STACK_SLOT      0                  // No outgoing reserved stack slots

  #define REG_ARG_0                REG_R3
  #define REG_ARG_1                REG_R4
  #define REG_ARG_2                REG_R5
  #define REG_ARG_3                REG_R6
  #define REG_ARG_4                REG_R7
  #define REG_ARG_5                REG_R8
  #define REG_ARG_6                REG_R9
  #define REG_ARG_7                REG_R10

  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];

  #define RBM_ARG_0                RBM_R3
  #define RBM_ARG_1                RBM_R4
  #define RBM_ARG_2                RBM_R5
  #define RBM_ARG_3                RBM_R6
  #define RBM_ARG_4                RBM_R7
  #define RBM_ARG_5                RBM_R8
  #define RBM_ARG_6                RBM_R9
  #define RBM_ARG_7                RBM_R10

  #define REG_FLTARG_0             REG_F1
  #define REG_FLTARG_1             REG_F2
  #define REG_FLTARG_2             REG_F3
  #define REG_FLTARG_3             REG_F4
  #define REG_FLTARG_4             REG_F5
  #define REG_FLTARG_5             REG_F6
  #define REG_FLTARG_6             REG_F7
  #define REG_FLTARG_7             REG_F8
  #define REG_FLTARG_8             REG_F9
  #define REG_FLTARG_9             REG_F10
  #define REG_FLTARG_10            REG_F11
  #define REG_FLTARG_11            REG_F12
  #define REG_FLTARG_12            REG_F13

  #define RBM_FLTARG_0             RBM_F1
  #define RBM_FLTARG_1             RBM_F2
  #define RBM_FLTARG_2             RBM_F3
  #define RBM_FLTARG_3             RBM_F4
  #define RBM_FLTARG_4             RBM_F5
  #define RBM_FLTARG_5             RBM_F6
  #define RBM_FLTARG_6             RBM_F7
  #define RBM_FLTARG_7             RBM_F8
  #define RBM_FLTARG_8             RBM_F9
  #define RBM_FLTARG_9             RBM_F10
  #define RBM_FLTARG_10            RBM_F11
  #define RBM_FLTARG_11            RBM_F12
  #define RBM_FLTARG_12            RBM_F13

  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3|RBM_ARG_4|RBM_ARG_5|RBM_ARG_6|RBM_ARG_7)
  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3|RBM_FLTARG_4|RBM_FLTARG_5|RBM_FLTARG_6|RBM_FLTARG_7|RBM_FLTARG_8|RBM_FLTARG_9|RBM_FLTARG_10|RBM_FLTARG_11|RBM_FLTARG_12)

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

#if 0 //TODO POWERPC64
  // Some "Advanced SIMD / SVE scalar x indexed element" and "Advanced SIMD / SVE vector x indexed element" instructions (e.g. "MLA (by element)")
  // have encoding that restricts what registers that can be used for the indexed element when the element size is H (i.e. 2 bytes).

  #define RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS (RBM_V0|RBM_V1|RBM_V2|RBM_V3|RBM_V4|RBM_V5|RBM_V6|RBM_V7|RBM_V8|RBM_V9|RBM_V10|RBM_V11|RBM_V12|RBM_V13|RBM_V14|RBM_V15)
  #define RBM_SVE_INDEXED_S_ELEMENT_ALLOWED_REGS (RBM_V0|RBM_V1|RBM_V2|RBM_V3|RBM_V4|RBM_V5|RBM_V6|RBM_V7)
  #define RBM_SVE_INDEXED_D_ELEMENT_ALLOWED_REGS RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS

  #define REG_ZERO_INIT_FRAME_REG1 REG_R9
  #define REG_ZERO_INIT_FRAME_REG2 REG_R10
  #define REG_ZERO_INIT_FRAME_SIMD REG_V16

  #define SWIFT_SUPPORT
  #define REG_SWIFT_ERROR REG_R21
  #define RBM_SWIFT_ERROR RBM_R21
  #define REG_SWIFT_SELF  REG_R20
  #define RBM_SWIFT_SELF  RBM_R20
  #define REG_SWIFT_INTRET_ORDER REG_R0,REG_R1,REG_R2,REG_R3
  #define REG_SWIFT_FLOATRET_ORDER REG_V0,REG_V1,REG_V2,REG_V3
#endif
//------------------------------------------------------------------------
// IsPpc64leHfaLikeStruct: Check if a struct is a Homogeneous Float Aggregate (HFA)
//
// Arguments:
//    comp       - Compiler instance
//    hClass     - Class handle for the struct
//    pHfaType   - [out] Type of HFA elements (TYP_FLOAT or TYP_DOUBLE), or TYP_UNDEF if not HFA
//    pNumFields - [out] Number of HFA fields
//
// Return Value:
//    true if the struct is an HFA (all float or all double fields), false otherwise
//
// Notes:
//    This function detects HFA structs without requiring FEATURE_HFA to be enabled.
//    It calls the VM's getHFAType() directly to determine if a struct qualifies as HFA.
//    Per PPC64LE ELFv2 ABI:
//    - For parameters: HFA can use all 13 float registers (f1-f13)
//    - For return values: HFA limited to 8 fields maximum
//
bool IsPpc64leHfaLikeStruct(Compiler* comp, CORINFO_CLASS_HANDLE hClass, var_types* pHfaType, unsigned* pNumFields);

  // clang-format on


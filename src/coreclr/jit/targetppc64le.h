// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_POWERPC64)
#error The file should not be included for this platform.
#endif


  #define FEATURE_STRUCTPROMOTE 0 // JIT Optimization to promote fields of structs into registers
  #define FEATURE_MULTIREG_STRUCT_PROMOTE 1 // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_FASTTAILCALL 	   0 	   // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls. //Vikas s390x has set this to 0

  #define FEATURE_FASTTAILCALL     0       // Tail calls made as epilog+jmp
  #define FEATURE_TAILCALL_OPT     0       // opportunistic Tail calls (i.e. without ".tail" prefix) made as fast tail calls.

  #define TARGET_POINTER_SIZE 8 // equal to sizeof(void*) and the managed pointer size in bytes for this target
  #define FEATURE_EH 0 // To aid platform bring-up, eliminate exceptional EH clauses (catch, filter, filter-handler, fault) and directly execute 'finally' clauses.

  #define REG_FP_FIRST             REG_F0
  #define REG_FP_LAST              REG_F31
  #define FIRST_FP_ARGREG          REG_F0
  #define LAST_FP_ARGREG           REG_F31

  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.
  #define MAX_ARG_REG_COUNT             2  // Maximum registers used to pass a single argument in multiple registers.


  // The following defines are useful for iterating a regNumber
  #define REG_FIRST                REG_R0
  #define REG_INT_FIRST            REG_R0
  #define REG_INT_LAST             REG_R31
  #define REG_INT_COUNT            (REG_INT_LAST - REG_INT_FIRST + 1)
  #define REG_NEXT(reg)           ((regNumber)((unsigned)(reg) + 1))
  #define REG_PREV(reg)           ((regNumber)((unsigned)(reg) - 1))

  #define RBM_ARG_0                RBM_R3
  #define RBM_ARG_1                RBM_R4
  #define RBM_ARG_2                RBM_R5
  #define RBM_ARG_3                RBM_R6
  #define RBM_ARG_4                RBM_R7
  #define RBM_ARG_5                RBM_R8
  #define RBM_ARG_6                RBM_R9
  #define RBM_ARG_7                RBM_R10


  #define RBM_ARG_REGS            (RBM_ARG_0|RBM_ARG_1|RBM_ARG_2|RBM_ARG_3|RBM_ARG_4|RBM_ARG_5|RBM_ARG_6|RBM_ARG_7)



  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)

  #define RBM_SPBASE               RBM_SP

  #define RBM_INT_CALLEE_SAVED    (RBM_R14|RBM_R15|RBM_R16|RBM_R17|RBM_R18|RBM_R19|RBM_R20|RBM_R21|RBM_R22|RBM_R23|RBM_R24|RBM_R25|RBM_R26|RBM_R27|RBM_R28|RBM_R29|RBM_R30|RBM_R31)
  #define RBM_FLT_CALLEE_SAVED    (RBM_F14|RBM_F15|RBM_F16|RBM_F17|RBM_F18|RBM_F19|RBM_F20|RBM_F21|RBM_F22|RBM_F23|RBM_F24|RBM_F25|RBM_F26|RBM_F27|RBM_F28|RBM_F29|RBM_F30|RBM_F31)

  #define CNT_CALLEE_SAVED        (18)
  #define CNT_CALLEE_TRASH        (xx) // need to check vikas
  #define CNT_CALLEE_ENREG        (CNT_CALLEE_SAVED-1)
  #define CNT_CALL_GC_REGS        (CNT_CALLEE_SAVED+2)

  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED)


  #define RBM_INT_CALLEE_TRASH    (RBM_R0|RBM_R2|RBM_R3|RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_R12|RBM_LR) //volatile registers
  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)

  #define REGNUM_BITS              7       // number of bits in a REG_*

  #define CODE_ALIGN               4       // code alignment requirement

  #define REGSIZE_BYTES            8       // number of bytes in one general purpose register

  #define REG_ARG_0                REG_R0
  #define REG_ARG_1                REG_R1
  #define REG_ARG_2                REG_R2
  #define REG_ARG_3                REG_R3
  #define REG_ARG_4                REG_R4
  #define REG_ARG_5                REG_R5
  #define REG_ARG_6                REG_R6
  #define REG_ARG_7                REG_R7

  #define REG_FLTARG_0             REG_F0
  #define REG_FLTARG_1             REG_F1
  #define REG_FLTARG_2             REG_F2
  #define REG_FLTARG_3             REG_F3
  #define REG_FLTARG_4             REG_F4
  #define REG_FLTARG_5             REG_F5
  #define REG_FLTARG_6             REG_F6
  #define REG_FLTARG_7             REG_F7
  #define REG_FLTARG_8             REG_F8
  #define REG_FLTARG_9             REG_F9
  #define REG_FLTARG_10            REG_F10
  #define REG_FLTARG_11            REG_F11
  #define REG_FLTARG_12            REG_F12
  #define REG_FLTARG_13            REG_F13


  #define RBM_FLTARG_0             RBM_F0
  #define RBM_FLTARG_1             RBM_F1
  #define RBM_FLTARG_2             RBM_F2
  #define RBM_FLTARG_3             RBM_F3
  #define RBM_FLTARG_4             RBM_F4
  #define RBM_FLTARG_5             RBM_F5
  #define RBM_FLTARG_6             RBM_F6
  #define RBM_FLTARG_7             RBM_F7
  #define RBM_FLTARG_8             RBM_F8
  #define RBM_FLTARG_9             RBM_F9
  #define RBM_FLTARG_10            RBM_F10
  #define RBM_FLTARG_11            RBM_F11
  #define RBM_FLTARG_12            RBM_F12
  #define RBM_FLTARG_13            RBM_F13

  #define RBM_FLTARG_REGS         (RBM_FLTARG_0|RBM_FLTARG_1|RBM_FLTARG_2|RBM_FLTARG_3|RBM_FLTARG_4|RBM_FLTARG_5|RBM_FLTARG_6|RBM_FLTARG_7|RBM_FLTARG_8|RBM_FLTARG_9|RBM_FLTARG_10|RBM_FLTARG_11|RBM_FLTARG_12|RBM_FLTARG_13)

  #define MAX_REG_ARG              8
  #define MAX_FLOAT_REG_ARG        13
  extern const regNumber intArgRegs [MAX_REG_ARG];
  extern const regMaskTP intArgMasks[MAX_REG_ARG];
  extern const regNumber fltArgRegs [MAX_FLOAT_REG_ARG];
  extern const regMaskTP fltArgMasks[MAX_FLOAT_REG_ARG];




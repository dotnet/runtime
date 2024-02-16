// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_AMD64)
#error The file should not be included for this platform.
#endif

// clang-format off
  // TODO-AMD64-CQ: Fine tune the following xxBlk threshold values:

  #define CPU_LOAD_STORE_ARCH      0
  #define ROUND_FLOAT              0       // Do not round intermed float expression results
  #define CPU_HAS_BYTE_REGS        0

  #define CPOBJ_NONGC_SLOTS_LIMIT  4       // For CpObj code generation, this is the threshold of the number
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
  #define FEATURE_IMPLICIT_BYREFS       0  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         1  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  1  // True when we want to promote fields of a multireg struct into registers
  #define FEATURE_STRUCT_CLASSIFIER     1  // Uses a classifier function to determine if structs are passed/returned in more than one register
  #define MAX_PASS_MULTIREG_BYTES      32  // Maximum size of a struct that could be passed in more than one register (Max is two SIMD16s)
  #define MAX_RET_MULTIREG_BYTES       32  // Maximum size of a struct that could be returned in more than one register  (Max is two SIMD16s)
  #define MAX_ARG_REG_COUNT             2  // Maximum registers used to pass a single argument in multiple registers.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
                                           // This is also the maximum number of registers for a MultiReg node.
#else // !UNIX_AMD64_ABI
  #define WINDOWS_AMD64_ABI                // Uses the Windows ABI for AMD64
  #define FEATURE_IMPLICIT_BYREFS       1  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  0  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          0  // Support for returning a single value in more than one register
  #define FEATURE_MULTIREG_STRUCT_PROMOTE  0  // True when we want to promote fields of a multireg struct into registers
  #define MAX_PASS_MULTIREG_BYTES       0  // No multireg arguments
  #define MAX_RET_MULTIREG_BYTES        0  // No multireg return values
  #define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass a single argument (no arguments are passed using multiple registers)
  #define MAX_RET_REG_COUNT             1  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
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

  #define RBM_LOWFLOAT            (RBM_XMM0 | RBM_XMM1 | RBM_XMM2 | RBM_XMM3 | RBM_XMM4 | RBM_XMM5 | RBM_XMM6 | RBM_XMM7 | RBM_XMM8 | RBM_XMM9 | RBM_XMM10 | RBM_XMM11 | RBM_XMM12 | RBM_XMM13 | RBM_XMM14 | RBM_XMM15 )
  #define RBM_HIGHFLOAT           (RBM_XMM16 | RBM_XMM17 | RBM_XMM18 | RBM_XMM19 | RBM_XMM20 | RBM_XMM21 | RBM_XMM22 | RBM_XMM23 | RBM_XMM24 | RBM_XMM25 | RBM_XMM26 | RBM_XMM27 | RBM_XMM28 | RBM_XMM29 | RBM_XMM30 | RBM_XMM31)
  #define CNT_HIGHFLOAT           16

  #define RBM_ALLFLOAT_INIT       RBM_LOWFLOAT

  #define RBM_ALLFLOAT             get_RBM_ALLFLOAT()

  #define RBM_ALLDOUBLE            RBM_ALLFLOAT
  #define REG_FP_FIRST             REG_XMM0
  #define REG_FP_LAST              REG_XMM31
  #define FIRST_FP_ARGREG          REG_XMM0

  #define REG_MASK_FIRST           REG_K0
  #define REG_MASK_LAST            REG_K7

  #define RBM_ALLMASK_INIT         (0)
  #define RBM_ALLMASK_EVEX         (RBM_K1 | RBM_K2 | RBM_K3 | RBM_K4 | RBM_K5 | RBM_K6 | RBM_K7)
  #define RBM_ALLMASK              get_RBM_ALLMASK()

  #define CNT_MASK_REGS            8

#ifdef    UNIX_AMD64_ABI
  #define LAST_FP_ARGREG        REG_XMM7
#else // !UNIX_AMD64_ABI
  #define LAST_FP_ARGREG        REG_XMM3
#endif // !UNIX_AMD64_ABI

  #define REGNUM_BITS              6       // number of bits in a REG_*
  #define REGSIZE_BYTES            8       // number of bytes in one register
  #define XMM_REGSIZE_BYTES        16      // XMM register size in bytes
  #define YMM_REGSIZE_BYTES        32      // YMM register size in bytes
  #define ZMM_REGSIZE_BYTES        64      // ZMM register size in bytes

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

  /* NOTE: Sync with variable name defined in compiler.h */
  #define RBM_FLT_CALLEE_TRASH_INIT (RBM_XMM0|RBM_XMM1|RBM_XMM2|RBM_XMM3|RBM_XMM4|RBM_XMM5|RBM_XMM6|RBM_XMM7| \
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

  /* NOTE: Sync with variable name defined in compiler.h */
  #define RBM_FLT_CALLEE_TRASH_INIT (RBM_XMM0|RBM_XMM1|RBM_XMM2|RBM_XMM3|RBM_XMM4|RBM_XMM5)
#endif // !UNIX_AMD64_ABI

  #define RBM_FLT_CALLEE_TRASH    get_RBM_FLT_CALLEE_TRASH()

  /* NOTE: Sync with variable name defined in compiler.h */
  #define RBM_MSK_CALLEE_TRASH_INIT (0)
  #define RBM_MSK_CALLEE_TRASH_EVEX RBM_ALLMASK_EVEX

  #define RBM_MSK_CALLEE_SAVED    (0)
  #define RBM_MSK_CALLEE_TRASH    get_RBM_MSK_CALLEE_TRASH()

  #define RBM_OSR_INT_CALLEE_SAVED  (RBM_INT_CALLEE_SAVED | RBM_EBP)

  #define REG_FLT_CALLEE_SAVED_FIRST   REG_XMM6
  #define REG_FLT_CALLEE_SAVED_LAST    REG_XMM15

  #define RBM_CALLEE_TRASH        (RBM_INT_CALLEE_TRASH | RBM_FLT_CALLEE_TRASH | RBM_MSK_CALLEE_TRASH)
  #define RBM_CALLEE_SAVED        (RBM_INT_CALLEE_SAVED | RBM_FLT_CALLEE_SAVED | RBM_MSK_CALLEE_SAVED)

  #define RBM_ALLINT              (RBM_INT_CALLEE_SAVED | RBM_INT_CALLEE_TRASH)

  // AMD64 write barrier ABI (see vm\amd64\JitHelpers_Fast.asm, vm\amd64\JitHelpers_Fast.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     The usual amd64 calling convention is observed.
  //     TODO-CQ: could this be optimized?
  // CORINFO_HELP_ASSIGN_BYREF (JIT_ByRefWriteBarrier):
  //     On entry:
  //       rsi: the source address (points to object reference to write)
  //       rdi: the destination address (object reference written here)
  //     On exit:
  //       rcx: trashed
  //       rdi: incremented by 8
  //       rsi: incremented by 8
  //       rax: trashed when FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP is defined
  //     TODO-CQ: the above description matches the comments for the amd64 assembly code of JIT_ByRefWriteBarrier,
  //              however the defines below assume the normal calling convention *except* for rdi/rsi. If we could
  //              reduce the number of trashed variables, we could improve code quality.
  //

  #define REG_WRITE_BARRIER_DST          REG_ARG_0
  #define RBM_WRITE_BARRIER_DST          RBM_ARG_0

  #define REG_WRITE_BARRIER_SRC          REG_ARG_1
  #define RBM_WRITE_BARRIER_SRC          RBM_ARG_1

  #define RBM_CALLEE_TRASH_NOGC        RBM_CALLEE_TRASH

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER         RBM_CALLEE_TRASH_NOGC

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_CALLEE_TRASH_NOGC

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_RSI | RBM_RDI | RBM_CALLEE_TRASH_NOGC)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF (RBM_CALLEE_TRASH_NOGC & ~(RBM_RDI | RBM_RSI))

  // We have two register classifications
  // * callee trash: aka     volatile or caller saved
  // * callee saved: aka non-volatile
  //
  // Callee trash are used for passing arguments, returning results, and are freely
  // mutable by the method. Because of this, the caller is responsible for saving
  // them if they are in use prior to making a call. This saving doesn't need to
  // happen for leaf methods (that is methods which don't themselves make any calls)
  // and can be done by spilling to the stack or to a callee saved register. This
  // means they are cheaper to use but can have higher overall cost if there are
  // many calls to be made with values in callee trash registers needing to live
  // across the call boundary.
  //
  // Callee saved don't have any special uses but have to be spilled prior to usage
  // and restored prior to returning back to the caller, so they have an inherently
  // higher baseline cost. This cost can be offset by re-using the register across
  // call boundaries to reduce the overall amount of spilling required.
  //
  // Given this, we order the registers here to prefer callee trash first and then
  // callee save. This allows us to use the registers we've already been assumed
  // to overwrite first and then to use those with a higher consumption cost. It
  // is up to the register allocator to preference using any callee saved registers
  // for values that are frequently live across call boundaries.
  //
  // Within those two groups registers are generally preferenced in numerical order
  // based on the encoding. This helps avoid using larger encodings unneccesarily since
  // higher numbered registers typically take more bytes to encode.
  //
  // For integer registers, the numerical order is eax, ecx, edx, ebx, esp, ebp,
  // esi, edi. You then also have r8-r15 which take an additional byte to encode. We
  // deviate from the numerical order slightly because esp, ebp, r12, and r13 have
  // special encoding requirements. In particular, esp is used by the stack and isn't
  // generally usable, instead it can only be used to access locals occupying stack
  // space. Both esp and r12 take an additional byte to encode the addressing form of
  // the instruction. ebp and r13 likewise can take additional bytes to encode certain
  // addressing modes, in particular those with displacements. Because of this ebp is
  // always ordered last of the base 8 registers. r13 and then r12 are likewise last
  // of the upper 8 registers. This helps reduce the total number of emitted bytes
  // quite significantly across typical usages.
  //
  // There are some other minor deviations based on special uses for particular registers
  // on a given platform which give additional size savings for the typical case.
  //
  // For simd registers, the numerical order is xmm0-xmm7. You then have xmm8-xmm15
  // which take an additional byte to encode and can also have xmm16-xmm31 for EVEX
  // when the hardware supports it. There are no additional hidden costs for these.

#ifdef UNIX_AMD64_ABI
  #define REG_VAR_ORDER_CALLEE_TRASH    REG_EAX,REG_ECX,REG_EDX,REG_EDI,REG_ESI,REG_R8,REG_R9,REG_R10,REG_R11
  #define REG_VAR_ORDER_CALLEE_SAVED    REG_EBX,REG_ETW_FRAMED_EBP_LIST REG_R15,REG_R14,REG_R13,REG_R12

  #define REG_VAR_ORDER_FLT_CALLEE_TRASH    REG_XMM0,REG_XMM1,REG_XMM2,REG_XMM3,REG_XMM4,REG_XMM5,REG_XMM6,REG_XMM7,   \
                                            REG_XMM8,REG_XMM9,REG_XMM10,REG_XMM11,REG_XMM12,REG_XMM13,REG_XMM14,       \
                                            REG_XMM15
  #define REG_VAR_ORDER_FLT_CALLEE_SAVED


  #define REG_VAR_ORDER_FLT_EVEX_CALLEE_TRASH   REG_VAR_ORDER_FLT_CALLEE_TRASH,REG_XMM16,REG_XMM17,REG_XMM18,REG_XMM19,\
                                                REG_XMM20,REG_XMM21,REG_XMM22,REG_XMM23,REG_XMM24,REG_XMM25,REG_XMM26, \
                                                REG_XMM27,REG_XMM28,REG_XMM29,REG_XMM30,REG_XMM31
  #define REG_VAR_ORDER_FLT_EVEX_CALLEE_SAVED   REG_VAR_ORDER_FLT_CALLEE_SAVED
#else // !UNIX_AMD64_ABI
  #define REG_VAR_ORDER_CALLEE_TRASH    REG_EAX,REG_ECX,REG_EDX,REG_R8,REG_R10,REG_R9,REG_R11
  #define REG_VAR_ORDER_CALLEE_SAVED    REG_EBX,REG_ESI,REG_EDI,REG_ETW_FRAMED_EBP_LIST REG_R14,REG_R15,REG_R13,REG_R12

  #define REG_VAR_ORDER_FLT_CALLEE_TRASH    REG_XMM0,REG_XMM1,REG_XMM2,REG_XMM3,REG_XMM4,REG_XMM5
  #define REG_VAR_ORDER_FLT_CALLEE_SAVED    REG_XMM6,REG_XMM7,REG_XMM8,REG_XMM9,REG_XMM10,REG_XMM11,REG_XMM12,         \
                                            REG_XMM13,REG_XMM14,REG_XMM15

  #define REG_VAR_ORDER_FLT_EVEX_CALLEE_TRASH   REG_VAR_ORDER_FLT_CALLEE_TRASH,REG_XMM16,REG_XMM17,REG_XMM18,REG_XMM19,\
                                                REG_XMM20,REG_XMM21,REG_XMM22,REG_XMM23,REG_XMM24,REG_XMM25,REG_XMM26, \
                                                REG_XMM27,REG_XMM28,REG_XMM29,REG_XMM30,REG_XMM31
  #define REG_VAR_ORDER_FLT_EVEX_CALLEE_SAVED   REG_VAR_ORDER_FLT_CALLEE_SAVED
#endif // !UNIX_AMD64_ABI

#define REG_VAR_ORDER           REG_VAR_ORDER_CALLEE_TRASH,REG_VAR_ORDER_CALLEE_SAVED
#define REG_VAR_ORDER_FLT       REG_VAR_ORDER_FLT_CALLEE_TRASH,REG_VAR_ORDER_FLT_CALLEE_SAVED
#define REG_VAR_ORDER_FLT_EVEX  REG_VAR_ORDER_FLT_EVEX_CALLEE_TRASH,REG_VAR_ORDER_FLT_EVEX_CALLEE_SAVED
#define REG_VAR_ORDER_MSK       REG_K1,REG_K2,REG_K3,REG_K4,REG_K5,REG_K6,REG_K7

#ifdef UNIX_AMD64_ABI
  #define CNT_CALLEE_SAVED         (5 + REG_ETW_FRAMED_EBP_COUNT)
  #define CNT_CALLEE_TRASH         (9)
  #define CNT_CALLEE_ENREG         (CNT_CALLEE_SAVED)

  #define CNT_CALLEE_SAVED_FLOAT   (0)
  #define CNT_CALLEE_TRASH_FLOAT_INIT (16)
  #define CNT_CALLEE_TRASH_HIGHFLOAT    (16)
  /* NOTE: Sync with variable name defined in compiler.h */
  #define REG_CALLEE_SAVED_ORDER   REG_EBX,REG_ETW_FRAMED_EBP_LIST REG_R12,REG_R13,REG_R14,REG_R15
  #define RBM_CALLEE_SAVED_ORDER   RBM_EBX,RBM_ETW_FRAMED_EBP_LIST RBM_R12,RBM_R13,RBM_R14,RBM_R15

  // For SysV we have more volatile registers so we do not save any callee saves for EnC.
  #define RBM_ENC_CALLEE_SAVED     0
#else // !UNIX_AMD64_ABI
  #define CNT_CALLEE_SAVED         (7 + REG_ETW_FRAMED_EBP_COUNT)
  #define CNT_CALLEE_TRASH         (7)
  #define CNT_CALLEE_ENREG         (CNT_CALLEE_SAVED)

  #define CNT_CALLEE_SAVED_FLOAT        (10)
  #define CNT_CALLEE_TRASH_FLOAT_INIT   (6)
  #define CNT_CALLEE_TRASH_HIGHFLOAT    (16)
  /* NOTE: Sync with variable name defined in compiler.h */
  #define REG_CALLEE_SAVED_ORDER   REG_EBX,REG_ESI,REG_EDI,REG_ETW_FRAMED_EBP_LIST REG_R12,REG_R13,REG_R14,REG_R15
  #define RBM_CALLEE_SAVED_ORDER   RBM_EBX,RBM_ESI,RBM_EDI,RBM_ETW_FRAMED_EBP_LIST RBM_R12,RBM_R13,RBM_R14,RBM_R15

  // Callee-preserved registers we always save and allow use of for EnC code, since there are quite few volatile registers.
  #define RBM_ENC_CALLEE_SAVED     (RBM_RSI | RBM_RDI)
#endif // !UNIX_AMD64_ABI

  #define CNT_CALLEE_TRASH_FLOAT   get_CNT_CALLEE_TRASH_FLOAT()

  #define CNT_CALLEE_SAVED_MASK      (0)

  #define CNT_CALLEE_TRASH_MASK_INIT (0)
  #define CNT_CALLEE_TRASH_MASK_EVEX (7)
  #define CNT_CALLEE_TRASH_MASK      get_CNT_CALLEE_TRASH_MASK()

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

  #define REG_R2R_INDIRECT_PARAM REG_RAX // Indirection cell for R2R fast tailcall
                                         // See ImportThunk.Kind.DelayLoadHelperWithExistingIndirectionCell in crossgen2.
  #define RBM_R2R_INDIRECT_PARAM RBM_RAX

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
  #define RBM_PROFILER_ENTER_TRASH          RBM_CALLEE_TRASH

  #define RBM_PROFILER_TAILCALL_TRASH       RBM_PROFILER_LEAVE_TRASH

  // The registers trashed by the CORINFO_HELP_STOP_FOR_GC helper.
#ifdef UNIX_AMD64_ABI
  // See vm\amd64\unixasmhelpers.S for more details.
  //
  // On Unix a struct of size >=9 and <=16 bytes in size is returned in two return registers.
  // The return registers could be any two from the set { RAX, RDX, XMM0, XMM1 }.
  // STOP_FOR_GC helper preserves all the 4 possible return registers.
  #define RBM_STOP_FOR_GC_TRASH (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET | RBM_FLOATRET_1 | RBM_INTRET_1))
  #define RBM_PROFILER_LEAVE_TRASH  (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET | RBM_FLOATRET_1 | RBM_INTRET_1))
#else
  // See vm\amd64\asmhelpers.asm for more details.
  #define RBM_STOP_FOR_GC_TRASH (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET))
  #define RBM_PROFILER_LEAVE_TRASH  (RBM_CALLEE_TRASH & ~(RBM_FLOATRET | RBM_INTRET))
#endif

  // The registers trashed by the CORINFO_HELP_INIT_PINVOKE_FRAME helper.
  #define RBM_INIT_PINVOKE_FRAME_TRASH  RBM_CALLEE_TRASH

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH & ~(RBM_R10 | RBM_RCX))
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_RCX
  #define REG_DISPATCH_INDIRECT_CALL_ADDR REG_RAX

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

#ifdef UNIX_AMD64_ABI
  #define RBM_STACK_PROBE_HELPER_TRASH RBM_NONE
#else // !UNIX_AMD64_ABI
  #define RBM_STACK_PROBE_HELPER_TRASH RBM_RAX
#endif // !UNIX_AMD64_ABI

  #define SWIFT_SUPPORT
  #define SWIFT_ERROR_REG REG_R12
  #define SWIFT_ERROR_RBM RBM_R12
  #define SWIFT_SELF_REG  REG_R13

// clang-format on

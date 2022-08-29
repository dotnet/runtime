// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

#if !defined(TARGET_X86)
#error The file should not be included for this platform.
#endif

// clang-format off
  #define CPU_LOAD_STORE_ARCH      0
  #define ROUND_FLOAT              1       // round intermed float expression results
  #define CPU_HAS_BYTE_REGS        1

  // TODO-CQ: Fine tune the following xxBlk threshold values:

  #define CPBLK_UNROLL_LIMIT       64      // Upper bound to let the code generator to loop unroll CpBlk.
  #define INITBLK_UNROLL_LIMIT     128     // Upper bound to let the code generator to loop unroll InitBlk.
  #define CPOBJ_NONGC_SLOTS_LIMIT  4       // For CpObj code generation, this is the threshold of the number
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
  #define FEATURE_IMPLICIT_BYREFS       0  // Support for struct parameters passed via pointers to shadow copies
  #define FEATURE_MULTIREG_ARGS_OR_RET  1  // Support for passing and/or returning single values in more than one register
  #define FEATURE_MULTIREG_ARGS         0  // Support for passing a single argument in more than one register
  #define FEATURE_MULTIREG_RET          1  // Support for returning a single value in more than one register
  #define MAX_PASS_SINGLEREG_BYTES      8  // Maximum size of a struct passed in a single register (double).
  #define MAX_PASS_MULTIREG_BYTES       0  // No multireg arguments
  #define MAX_RET_MULTIREG_BYTES        8  // Maximum size of a struct that could be returned in more than one register

  #define MAX_ARG_REG_COUNT             1  // Maximum registers used to pass an argument.
  #define MAX_RET_REG_COUNT             2  // Maximum registers used to return a value.

  #define MAX_MULTIREG_COUNT            2  // Maximum number of registers defined by a single instruction (including calls).
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

  #define REG_R2R_INDIRECT_PARAM   REG_EAX // Indirection cell for R2R fast tailcall, not currently used in x86.
  #define RBM_R2R_INDIRECT_PARAM   RBM_EAX

  // x86 write barrier ABI (see vm\i386\jithelp.asm, vm\i386\jithelp.S):
  // CORINFO_HELP_ASSIGN_REF (JIT_WriteBarrier), CORINFO_HELP_CHECKED_ASSIGN_REF (JIT_CheckedWriteBarrier):
  //     On entry:
  //       edx: the destination address (object reference written here)
  //       For optimized write barriers, one of eax, ecx, ebx, esi, or edi contains the source (object to write).
  //       (There is a separate write barrier for each of these source options.)
  //     On exit:
  //       edx: trashed
  // CORINFO_HELP_ASSIGN_BYREF (JIT_ByRefWriteBarrier):
  //     On entry:
  //       esi: the source address (points to object reference to write)
  //       edi: the destination address (object reference written here)
  //     On exit:
  //       ecx: trashed
  //       edi: incremented by 8
  //       esi: incremented by 8
  //

  #define REG_WRITE_BARRIER_DST          REG_ARG_0
  #define RBM_WRITE_BARRIER_DST          RBM_ARG_0

  #define REG_WRITE_BARRIER_SRC          REG_ARG_1
  #define RBM_WRITE_BARRIER_SRC          RBM_ARG_1

#if NOGC_WRITE_BARRIERS
  #define REG_OPTIMIZED_WRITE_BARRIER_DST   REG_EDX
  #define RBM_OPTIMIZED_WRITE_BARRIER_DST   RBM_EDX

  // We don't allow using ebp as a source register. Maybe we should only prevent this for ETW_EBP_FRAMED
  // (but that is always set right now).
  #define RBM_OPTIMIZED_WRITE_BARRIER_SRC   (RBM_EAX|RBM_ECX|RBM_EBX|RBM_ESI|RBM_EDI)
#endif // NOGC_WRITE_BARRIERS

  #define RBM_CALLEE_TRASH_NOGC    RBM_EDX

  // Registers killed by CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  // Note that x86 normally emits an optimized (source-register-specific) write barrier, but can emit
  // a call to a "general" write barrier.
  CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS
  #define RBM_CALLEE_TRASH_WRITEBARRIER         (RBM_EAX | RBM_EDX)
#else // !FEATURE_USE_ASM_GC_WRITE_BARRIERS
  #define RBM_CALLEE_TRASH_WRITEBARRIER         RBM_CALLEE_TRASH
#endif // !FEATURE_USE_ASM_GC_WRITE_BARRIERS

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_REF and CORINFO_HELP_CHECKED_ASSIGN_REF.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER       RBM_EDX

  // Registers killed by CORINFO_HELP_ASSIGN_BYREF.
  #define RBM_CALLEE_TRASH_WRITEBARRIER_BYREF   (RBM_ESI | RBM_EDI | RBM_ECX)

  // Registers no longer containing GC pointers after CORINFO_HELP_ASSIGN_BYREF.
  // Note that RDI and RSI are still valid byref pointers after this helper call, despite their value being changed.
  #define RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF RBM_ECX

  // GenericPInvokeCalliHelper unmanaged target parameter
  #define REG_PINVOKE_TARGET_PARAM REG_EAX
  #define RBM_PINVOKE_TARGET_PARAM RBM_EAX

  // GenericPInvokeCalliHelper cookie parameter
  #define REG_PINVOKE_COOKIE_PARAM REG_EBX

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

  #define RBM_VALIDATE_INDIRECT_CALL_TRASH (RBM_INT_CALLEE_TRASH & ~RBM_ECX)
  #define REG_VALIDATE_INDIRECT_CALL_ADDR REG_ECX

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
// clang-format on

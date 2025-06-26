# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "asmconstants.h"
#include "unixasmmacros.inc"

#define real4 dword
#define real8 qword

# On linux arguments can be passed in non-sequential registers. Integer arguments are
# passed in sequential integer registers and floating point registers are passed in
# sequential floating point registers. This presents a problem when we go to pass the
# struct argument as a COR_PRF_FUNCTION_ARGUMENT_RANGE which expects the arguments are
# in one contiguous range. This space is a scratch space that the ArgIterator can use
# to copy the structs to so they are sequential.

# Pad the frame size by 0x8 so when the xmm0 and xmm1 register store/restore happens
# we can align to 16 and be guaranteed to not exceed the frame size
.equ STACK_FUDGE_FACTOR, 0x8

# SIZEOF_STACK_FRAME is how many bytes we reserve in our ELT helpers below
# There are three components, the first is space for profiler platform specific
# data struct that we spill the general purpose registers to, then space to
# spill xmm0 and xmm1, then finally 8 bytes of padding to ensure that the xmm
# register reads/writes are aligned on 16 bytes.
.equ SIZEOF_STACK_FRAME, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA + STACK_FUDGE_FACTOR

# ***********************************************************
#   NOTE:
#
#   Register preservation scheme:
#
#       Preserved:
#           - all non-volatile registers
#           - rax, rcx, rdx, r8, r9
#           - xmm0, xmm1
#
#       Not Preserved:
#           - floating point argument registers (xmm2-3)
#           - volatile integer registers (r10, r11)
#           - volatile floating point registers (xmm4-5)
#           - upper halves of ymm registers on AVX (which are volatile)
#
# ***********************************************************

# EXTERN_C void JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle);
# <NOTE>
#
# </NOTE>
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
  ret
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

# EXTERN_C void ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
# <NOTE>
#
# </NOTE>
NESTED_ENTRY ProfileEnterNaked, _TEXT, NoHandler
  #       Upon entry :
  #           r14 = clientInfo
  #           r15 = profiledRsp

  push_nonvol_reg         rax

  lea                     rax, [rsp + 0x10]    # caller rsp
  mov                     r10, [rax - 0x8]     # return address

  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  # correctness of return value in structure doesn't matter for enter probe

  # setup ProfilePlatformSpecificData structure
  xor                     r11, r11 # nullify r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__functionId], r11
  save_reg_postrsp        rbp, PROFILE_PLATFORM_SPECIFIC_DATA__rbp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__probeRsp], rax    # caller rsp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__ip], r10    # return address
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__profiledRsp], r15
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rax], r11    # return value
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg], r11    # r11 is null
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0], xmm0
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1], xmm1
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2], xmm2
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3], xmm3
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4], xmm4
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5], xmm5
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6], xmm6
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7], xmm7
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdi], rdi
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rsi], rsi
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdx], rdx
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rcx], rcx
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r8], r8
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r9], r9
  mov                     r10, PROFILE_ENTER
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flags], r10d

  END_PROLOGUE

  # rdi already contains the clientInfo
  mov                     rdi, r14
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileEnter)

  # restore fp return registers
  movsd                   xmm0, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0]
  movsd                   xmm1, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1]
  movsd                   xmm2, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2]
  movsd                   xmm3, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3]
  movsd                   xmm4, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4]
  movsd                   xmm5, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5]
  movsd                   xmm6, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6]
  movsd                   xmm7, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7]

  # restore arg registers
  mov                     rdi, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdi]
  mov                     rsi, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rsi]
  mov                     rdx, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdx]
  mov                     rcx, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rcx]
  mov                     r8, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r8]
  mov                     r9, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r9]

  # begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rax

  ret
NESTED_END ProfileEnterNaked, _TEXT

# EXTERN_C void ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
# <NOTE>
#
# </NOTE>
NESTED_ENTRY ProfileLeaveNaked, _TEXT, NoHandler
#       Upon entry :
#           rdi = clientInfo
#           rsi = profiledRsp

  push_nonvol_reg         rbx

  lea                     rbx, [rsp + 0x10]    # caller rsp
  mov                     r10, [rbx - 0x8]     # return address

  # rdx should be saved here because it can be used for returning struct values
  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  # correctness of argument registers in structure doesn't matter for leave probe

  # setup ProfilePlatformSpecificData structure
  xor                     r11, r11  # nullify r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__functionId], r11    # r11 is null
  save_reg_postrsp        rbp, PROFILE_PLATFORM_SPECIFIC_DATA__rbp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__probeRsp], rbx    # caller rsp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__ip], r10    # return address
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__profiledRsp], rsi
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rax], rax    # return value
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg], r11    # r11 is null
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0], xmm0
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1], xmm1
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2], xmm2
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3], xmm3
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4], xmm4
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5], xmm5
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6], xmm6
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7], xmm7
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdi], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rsi], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdx], rdx
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rcx], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r8], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r9], r11
  mov                     r10, PROFILE_LEAVE
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flags], r10d

  END_PROLOGUE

  # rdi already contains the clientInfo
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileLeave)

  # restore fp return registers
  movsd                   xmm0, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0]
  movsd                   xmm1, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1]
  movsd                   xmm2, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2]
  movsd                   xmm3, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3]
  movsd                   xmm4, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4]
  movsd                   xmm5, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5]
  movsd                   xmm6, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6]
  movsd                   xmm7, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7]

  # restore int return register
  mov                     rax, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rax]

  # begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rbx

  ret
NESTED_END ProfileLeaveNaked, _TEXT

# EXTERN_C void ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
# <NOTE>
#
# </NOTE>
NESTED_ENTRY ProfileTailcallNaked, _TEXT, NoHandler
#       Upon entry :
#           rdi = clientInfo
#           rsi = profiledRsp

  push_nonvol_reg         rbx

  lea                     rbx, [rsp + 0x10]    # caller rsp
  mov                     r10, [rbx - 0x8]     # return address

  # rdx should be saved here because it can be used for returning struct values
  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  # correctness of argument registers in structure doesn't matter for tailcall probe

  # setup ProfilePlatformSpecificData structure
  xor                     r11, r11  # nullify r11
  mov                     [rsp +  PROFILE_PLATFORM_SPECIFIC_DATA__functionId], r11  # r11 is null
  save_reg_postrsp        rbp, PROFILE_PLATFORM_SPECIFIC_DATA__rbp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__probeRsp], rbx  # caller rsp
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__ip], r10  # return address
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__profiledRsp], rsi
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rax], rax  # return value
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg], r11  # r11 is null
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0], xmm0
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1], xmm1
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2], xmm2
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3], xmm3
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4], xmm4
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5], xmm5
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6], xmm6
  movsd                   real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7], xmm7
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdi], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rsi], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rdx], rdx
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rcx], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r8], r11
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__r9], r11
  mov                     r10, PROFILE_LEAVE
  mov                     [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flags], r10d

  END_PROLOGUE

  # rdi already contains the clientInfo
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileTailcall)

  # restore fp return registers
  movsd                   xmm0, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt0]
  movsd                   xmm1, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt1]
  movsd                   xmm2, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt2]
  movsd                   xmm3, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt3]
  movsd                   xmm4, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt4]
  movsd                   xmm5, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt5]
  movsd                   xmm6, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt6]
  movsd                   xmm7, real8 ptr [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__flt7]

  # restore int return register
  mov                     rax, [rsp + PROFILE_PLATFORM_SPECIFIC_DATA__rax]

  # begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rbx

  ret
NESTED_END ProfileTailcallNaked, _TEXT

#ifdef TARGET_APPLE
# EXTERN_C void* GetThreadVarsAddress()
# <NOTE>
# Helper to calculate the address of relevant __thread_vars section that holds the address of symbol tlv_get_address for thread
# local `t_ThreadStatics`. The address is updated by the linker, which we retrieve here. In JIT code, this address is called
# to retrieve the address of the thread local.
# </NOTE>
LEAF_ENTRY GetThreadVarsAddress, _TEXT
        mov     rdi,    _t_ThreadStatics@TLVP[rip]
        ret
LEAF_END GetThreadVarsAddress, _TEXT
// ------------------------------------------------------------------
#endif // TARGET_APPLE

#ifndef TARGET_APPLE
# EXTERN_C void* GetTlsIndexObjectDescOffset();

#ifndef TARGET_ANDROID

# <NOTE>
# Helper to calculate the offset of native thread local variable `t_ThreadStatics`. The offset has to be found at runtime
# once linker does its relocation and fixup of thread locals. The runtime gets the address of this function, so
# it can walk through the instruction bytes to retrieve the offset embedded by the linker and calculate the
# final offset that should be passed to __tls_get_addr() in order to calculate the address of `t_ThreadStatics` for
# the current thread. Here, we have to call `__tls_get_addr()`, because if the linker tries to find the code pattern
# of "lea t_ThreadStatics@TLSGD", followed by `call __tls_get_addr()`. Without adding the call, the linker complains.
# We never have to call this method directly, and hence there is a `int 3` at the end.
# </NOTE>

LEAF_ENTRY GetTlsIndexObjectDescOffset, _TEXT
# On The `lea` instruction has a data16 prefix and the call instruction has two data16 (0x66) prefixes and one rex64 prefix.
# This is so the total size of lea+call to be 16, suitable for link-time optimization.

        .byte 0x66
        lea     rdi,    t_ThreadStatics@TLSGD[rip]   # instruction where offset is embedded by the linker during compilation
        .byte 0x66
        .byte 0x66
        .byte 0x48 # rex.W prefix for padding
        call    EXTERNAL_C_FUNC(__tls_get_addr)                      # dummy call to have linker see the code pattern to replace the offset
        int 3
LEAF_END GetTlsIndexObjectDescOffset, _TEXT
#endif // !TARGET_ANDROID
#endif // !TARGET_OSX

LEAF_ENTRY  JIT_PollGC, _TEXT
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, rax
        cmp     dword ptr [rax], 0
        jnz     LOCAL_LABEL(JIT_PollGCRarePath)
        ret
LOCAL_LABEL(JIT_PollGCRarePath):
        PREPARE_EXTERNAL_VAR g_pPollGC, rax
        mov     rax, [rax]
        jmp     rax
LEAF_END  JIT_PollGC, _TEXT

//rdi -This pointer
//rsi -ReturnBuffer
LEAF_ENTRY ThisPtrRetBufPrecodeWorker, _TEXT
    mov  METHODDESC_REGISTER, [METHODDESC_REGISTER + ThisPtrRetBufPrecodeData__Target]
    mov r11, rsi
    mov rsi, rdi
    mov rdi, r11
    jmp METHODDESC_REGISTER
LEAF_END ThisPtrRetBufPrecodeWorker, _TEXT

//
// Prologue of all funclet calling helpers (CallXXXXFunclet)
//
.macro FUNCLET_CALL_PROLOGUE localsCount, alignStack
        push_nonvol_reg r15     // save preserved regs for OS stackwalker
        push_nonvol_reg r14     // ...
        push_nonvol_reg r13     // ...
        push_nonvol_reg r12     // ...
        push_nonvol_reg rbx     // ...
        push_nonvol_reg rbp     // ...

        stack_alloc_size = \localsCount * 8 + \alignStack * 8

        alloc_stack     stack_alloc_size

        END_PROLOGUE
.endm

//
// Epilogue of all funclet calling helpers (CallXXXXFunclet)
//
.macro FUNCLET_CALL_EPILOGUE
        free_stack      stack_alloc_size

        pop_nonvol_reg rbp
        pop_nonvol_reg rbx
        pop_nonvol_reg r12
        pop_nonvol_reg r13
        pop_nonvol_reg r14
        pop_nonvol_reg r15
.endm

// This helper enables us to call into a funclet after restoring Fp register
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler
        // On entry:
        //
        // RDI = throwable
        // RSI = PC to invoke
        // RDX = address of RBX register in CONTEXT record; used to restore the non-volatile registers of CrawlFrame
        // RCX = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        //

        FUNCLET_CALL_PROLOGUE 0, 1

        //  Restore RBX, RBP, R12, R13, R14, R15 from CONTEXT
        mov     rbp, [rdx + OFFSETOF__CONTEXT__Rbp - OFFSETOF__CONTEXT__Rbx]
        mov     r12, [rdx + OFFSETOF__CONTEXT__R12 - OFFSETOF__CONTEXT__Rbx]
        mov     r13, [rdx + OFFSETOF__CONTEXT__R13 - OFFSETOF__CONTEXT__Rbx]
        mov     r14, [rdx + OFFSETOF__CONTEXT__R14 - OFFSETOF__CONTEXT__Rbx]
        mov     r15, [rdx + OFFSETOF__CONTEXT__R15 - OFFSETOF__CONTEXT__Rbx]

        // Save the SP of this function.
        mov     [rcx], rsp
        // Invoke the funclet
        call    rsi

        FUNCLET_CALL_EPILOGUE
        ret
NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler
        // On entry:
        //
        // RDI = throwable
        // RSI = RBP of main function
        // RDX = PC to invoke
        // RCX = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        //

        FUNCLET_CALL_PROLOGUE 0, 1

        // Restore RBP
        mov     rbp, rsi
        // Save the SP of this function
        mov     [rcx], rsp
        // Invoke the filter funclet
        call    rdx

        FUNCLET_CALL_EPILOGUE
        ret
NESTED_END CallEHFilterFunclet, _TEXT

#ifdef FEATURE_INTERPRETER

NESTED_ENTRY InterpreterStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        __InterpreterStubStackArgumentsOffset = __PWTB_TransitionBlock + SIZEOF__TransitionBlock
        __InterpreterStubArgumentRegistersOffset = __PWTB_TransitionBlock

        // IR bytecode address
        mov             rbx, METHODDESC_REGISTER

        INLINE_GETTHREAD // result in rax, it can thrash all argument registers as it can call a helper
        mov             r10, rax

        // Load the InterpMethod pointer from the IR bytecode
        mov             rax, qword ptr [rbx]
        mov             rax, qword ptr [rax + OFFSETOF__InterpMethod__pCallStub]
        // Reload the argument registers, the macro to get the thread have likely overwritten them
        mov             rdi, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset]
        mov             rsi, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset + 8]
        mov             rdx, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset + 0x10]
        mov             rcx, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset + 0x18]
        mov             r8, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset + 0x20]
        mov             r9, qword ptr [rsp + __InterpreterStubArgumentRegistersOffset + 0x28]
        movsd           xmm0, real8 ptr [rsp + __PWTB_FloatArgumentRegisters]
        movsd           xmm1, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x10]
        movsd           xmm2, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x20]
        movsd           xmm3, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x30]
        movsd           xmm4, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x40]
        movsd           xmm5, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x50]
        movsd           xmm6, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x60]
        movsd           xmm7, real8 ptr [rsp + __PWTB_FloatArgumentRegisters + 0x70]
        lea             r11, qword ptr [rax + OFFSETOF__CallStubHeader__Routines]
        mov             r10, qword ptr [r10 + OFFSETOF__Thread__m_pInterpThreadContext]
        mov             r10, qword ptr [r10 + OFFSETOF__InterpThreadContext__pStackPointer]
        lea             rax, [rsp + __PWTB_TransitionBlock]
        // rbx contains IR bytecode address
        // Copy the arguments to the interpreter stack, invoke the InterpExecMethod and load the return value
        call            qword ptr [r11]

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

NESTED_END InterpreterStub, _TEXT

NESTED_ENTRY InterpreterStubRetVoid, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetVoid, _TEXT

NESTED_ENTRY InterpreterStubRetI8, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        mov             rax, qword ptr [rax]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetI8, _TEXT

NESTED_ENTRY InterpreterStubRetDouble, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        movsd           xmm0, real8 ptr [rax]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetDouble, _TEXT

NESTED_ENTRY InterpreterStubRetI8I8, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        mov             rdx, qword ptr [rax + 8]
        mov             rax, qword ptr [rax]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetI8I8, _TEXT

NESTED_ENTRY InterpreterStubRetDoubleDouble, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        movsd           xmm0, real8 ptr [rax]
        movsd           xmm1, real8 ptr [rax + 8]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetDoubleDouble, _TEXT

NESTED_ENTRY InterpreterStubRetI8Double, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        movsd           xmm0, real8 ptr [rax + 8]
        mov             rax, qword ptr [rax]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetI8Double, _TEXT

NESTED_ENTRY InterpreterStubRetDoubleI8, _TEXT, NoHandler
        push_register   rcx // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        xor             rdx, rdx
        call            C_FUNC(ExecuteInterpretedMethod)
        movsd           xmm0, real8 ptr [rax]
        mov             rax, qword ptr [rax + 8]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetDoubleI8, _TEXT

NESTED_ENTRY InterpreterStubRetBuff, _TEXT, NoHandler
        push_register   rax // stack alignment
END_PROLOGUE
        mov             rdi, rax // pTransitionBlock*
        mov             rsi, rbx // the IR bytecode pointer
        // Load the return buffer address
        // The 8 + 8 is for the push and the return address slot
        mov             rdx, qword ptr [rsp + 8 + 8 + __InterpreterStubArgumentRegistersOffset]
        call            C_FUNC(ExecuteInterpretedMethod)
        mov             rax, qword ptr [rsp + 8 + 8 + __InterpreterStubArgumentRegistersOffset]
        pop_register    rcx
        ret
NESTED_END InterpreterStubRetBuff, _TEXT

// Routines for passing value type arguments by reference in general purpose registers RDI, RSI, RDX, RCX, R8, R9
// from native code to the interpreter

// Copy arguments from the the processor stack to the interpreter stack.
// The CPU stack slots are aligned to pointer size.
LEAF_ENTRY Store_Stack, _TEXT
        push_nonvol_reg rdi
        push_nonvol_reg rsi
        push_register rcx
        mov esi, dword ptr [r11 + 8]  // SP offset
        mov ecx, dword ptr [r11 + 12] // number of stack slots
        // load the caller Rsp as a based for the stack arguments
        // The 4 * 8 represent the three pushes above and the return address slot
        lea rsi, [rsp + rsi + 4 * 8 + __InterpreterStubStackArgumentsOffset]
        mov rdi, r10
        shr rcx, 3
        rep movsq
        mov r10, rdi
        pop_register rcx
        pop_nonvol_reg rsi
        pop_nonvol_reg rdi
        add r11, 16
        jmp qword ptr [r11]
LEAF_END Store_Stack, _TEXT

LEAF_ENTRY Store_RDI, _TEXT
        mov [r10], rdi
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI, _TEXT

LEAF_ENTRY Store_RDI_RSI, _TEXT
        mov [r10], rdi
        mov [r10 + 8], rsi
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI_RSI, _TEXT

LEAF_ENTRY Store_RDI_RSI_RDX, _TEXT
        mov [r10], rdi
        mov [r10 + 8], rsi
        mov [r10 + 16], rdx
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI_RSI_RDX, _TEXT

LEAF_ENTRY Store_RDI_RSI_RDX_RCX, _TEXT
        mov [r10], rdi
        mov [r10 + 8], rsi
        mov [r10 + 16], rdx
        mov [r10 + 24], rcx
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI_RSI_RDX_RCX, _TEXT

LEAF_ENTRY Store_RDI_RSI_RDX_RCX_R8, _TEXT
        mov [r10], rdi
        mov [r10 + 8], rsi
        mov [r10 + 16], rdx
        mov [r10 + 24], rcx
        mov [r10 + 32], r8
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI_RSI_RDX_RCX_R8, _TEXT

LEAF_ENTRY Store_RDI_RSI_RDX_RCX_R8_R9, _TEXT
        mov [r10], rdi
        mov [r10 + 8], rsi
        mov [r10 + 16], rdx
        mov [r10 + 24], rcx
        mov [r10 + 32], r8
        mov [r10 + 40], r9
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDI_RSI_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Store_RSI, _TEXT
        mov [r10], rsi
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RSI, _TEXT

LEAF_ENTRY Store_RSI_RDX, _TEXT
        mov [r10], rsi
        mov [r10 + 8], rdx
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RSI_RDX, _TEXT

LEAF_ENTRY Store_RSI_RDX_RCX, _TEXT
        mov [r10], rsi
        mov [r10 + 8], rdx
        mov [r10 + 16], rcx
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RSI_RDX_RCX, _TEXT

LEAF_ENTRY Store_RSI_RDX_RCX_R8, _TEXT
        mov [r10], rsi
        mov [r10 + 8], rdx
        mov [r10 + 16], rcx
        mov [r10 + 24], r8
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RSI_RDX_RCX_R8, _TEXT

LEAF_ENTRY Store_RSI_RDX_RCX_R8_R9, _TEXT
        mov [r10], rsi
        mov [r10 + 8], rdx
        mov [r10 + 16], rcx
        mov [r10 + 24], r8
        mov [r10 + 32], r9
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RSI_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Store_RDX, _TEXT
        mov [r10], rdx
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX, _TEXT

LEAF_ENTRY Store_RDX_RCX, _TEXT
        mov [r10], rdx
        mov [r10 + 8], rcx
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX_RCX, _TEXT

LEAF_ENTRY Store_RDX_RCX_R8, _TEXT
        mov [r10], rdx
        mov [r10 + 8], rcx
        mov [r10 + 16], r8
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX_RCX_R8, _TEXT

LEAF_ENTRY Store_RDX_RCX_R8_R9, _TEXT
        mov [r10], rdx
        mov [r10 + 8], rcx
        mov [r10 + 16], r8
        mov [r10 + 24], r9
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Store_RCX, _TEXT
        mov [r10], rcx
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RCX, _TEXT

LEAF_ENTRY Store_RCX_R8, _TEXT
        mov [r10], rcx
        mov [r10 + 8], r8
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RCX_R8, _TEXT

LEAF_ENTRY Store_RCX_R8_R9, _TEXT
        mov [r10], rcx
        mov [r10 + 8], r8
        mov [r10 + 16], r9
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RCX_R8_R9, _TEXT

LEAF_ENTRY Store_RDX_R8, _TEXT
        mov [r10], rdx
        mov [r10 + 8], r8
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX_R8, _TEXT

LEAF_ENTRY Store_RDX_R8_R9, _TEXT
        mov [r10], rdx
        mov [r10 + 8], r8
        mov [r10 + 16], r9
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_RDX_R8_R9, _TEXT

LEAF_ENTRY Store_R8, _TEXT
        mov [r10], r8
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_R8, _TEXT

LEAF_ENTRY Store_R8_R9, _TEXT
        mov [r10], r8
        mov [r10 + 8], r9
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_R8_R9, _TEXT

LEAF_ENTRY Store_R9, _TEXT
        mov [r10], r9
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_R9, _TEXT

LEAF_ENTRY Store_XMM0, _TEXT
        movsd real8 ptr [r10], xmm0
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0, _TEXT

LEAF_ENTRY Store_XMM0_XMM1, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2_XMM3, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        movsd real8 ptr [r10 + 24], xmm3
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2_XMM3, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2_XMM3_XMM4, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        movsd real8 ptr [r10 + 24], xmm3
        movsd real8 ptr [r10 + 32], xmm4
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        movsd real8 ptr [r10 + 24], xmm3
        movsd real8 ptr [r10 + 32], xmm4
        movsd real8 ptr [r10 + 40], xmm5
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        movsd real8 ptr [r10 + 24], xmm3
        movsd real8 ptr [r10 + 32], xmm4
        movsd real8 ptr [r10 + 40], xmm5
        movsd real8 ptr [r10 + 48], xmm6
        add r10, 56
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm0
        movsd real8 ptr [r10 + 8], xmm1
        movsd real8 ptr [r10 + 16], xmm2
        movsd real8 ptr [r10 + 24], xmm3
        movsd real8 ptr [r10 + 32], xmm4
        movsd real8 ptr [r10 + 40], xmm5
        movsd real8 ptr [r10 + 48], xmm6
        movsd real8 ptr [r10 + 56], xmm7
        add r10, 64
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM1, _TEXT
        movsd real8 ptr [r10], xmm1
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1, _TEXT

LEAF_ENTRY Store_XMM1_XMM2, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2, _TEXT

LEAF_ENTRY Store_XMM1_XMM2_XMM3, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        movsd real8 ptr [r10 + 16], xmm3
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2_XMM3, _TEXT

LEAF_ENTRY Store_XMM1_XMM2_XMM3_XMM4, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        movsd real8 ptr [r10 + 16], xmm3
        movsd real8 ptr [r10 + 24], xmm4
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Store_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        movsd real8 ptr [r10 + 16], xmm3
        movsd real8 ptr [r10 + 24], xmm4
        movsd real8 ptr [r10 + 32], xmm5
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        movsd real8 ptr [r10 + 16], xmm3
        movsd real8 ptr [r10 + 24], xmm4
        movsd real8 ptr [r10 + 32], xmm5
        movsd real8 ptr [r10 + 40], xmm6
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm1
        movsd real8 ptr [r10 + 8], xmm2
        movsd real8 ptr [r10 + 16], xmm3
        movsd real8 ptr [r10 + 24], xmm4
        movsd real8 ptr [r10 + 32], xmm5
        movsd real8 ptr [r10 + 40], xmm6
        movsd real8 ptr [r10 + 48], xmm7
        add r10, 56
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM2, _TEXT
        movsd real8 ptr [r10], xmm2
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2, _TEXT

LEAF_ENTRY Store_XMM2_XMM3, _TEXT
        movsd real8 ptr [r10], xmm2
        movsd real8 ptr [r10 + 8], xmm3
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2_XMM3, _TEXT

LEAF_ENTRY Store_XMM2_XMM3_XMM4, _TEXT
        movsd real8 ptr [r10], xmm2
        movsd real8 ptr [r10 + 8], xmm3
        movsd real8 ptr [r10 + 16], xmm4
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Store_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd real8 ptr [r10], xmm2
        movsd real8 ptr [r10 + 8], xmm3
        movsd real8 ptr [r10 + 16], xmm4
        movsd real8 ptr [r10 + 24], xmm5
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Store_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm2
        movsd real8 ptr [r10 + 8], xmm3
        movsd real8 ptr [r10 + 16], xmm4
        movsd real8 ptr [r10 + 24], xmm5
        movsd real8 ptr [r10 + 32], xmm6
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm2
        movsd real8 ptr [r10 + 8], xmm3
        movsd real8 ptr [r10 + 16], xmm4
        movsd real8 ptr [r10 + 24], xmm5
        movsd real8 ptr [r10 + 32], xmm6
        movsd real8 ptr [r10 + 40], xmm7
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM3, _TEXT
        movsd real8 ptr [r10], xmm3
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM3, _TEXT

LEAF_ENTRY Store_XMM3_XMM4, _TEXT
        movsd real8 ptr [r10], xmm3
        movsd real8 ptr [r10 + 8], xmm4
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM3_XMM4, _TEXT

LEAF_ENTRY Store_XMM3_XMM4_XMM5, _TEXT
        movsd real8 ptr [r10], xmm3
        movsd real8 ptr [r10 + 8], xmm4
        movsd real8 ptr [r10 + 16], xmm5
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Store_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm3
        movsd real8 ptr [r10 + 8], xmm4
        movsd real8 ptr [r10 + 16], xmm5
        movsd real8 ptr [r10 + 24], xmm6
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm3
        movsd real8 ptr [r10 + 8], xmm4
        movsd real8 ptr [r10 + 16], xmm5
        movsd real8 ptr [r10 + 24], xmm6
        movsd real8 ptr [r10 + 32], xmm7
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM4, _TEXT
        movsd real8 ptr [r10], xmm4
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM4, _TEXT

LEAF_ENTRY Store_XMM4_XMM5, _TEXT
        movsd real8 ptr [r10], xmm4
        movsd real8 ptr [r10 + 8], xmm5
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM4_XMM5, _TEXT

LEAF_ENTRY Store_XMM4_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm4
        movsd real8 ptr [r10 + 8], xmm5
        movsd real8 ptr [r10 + 16], xmm6
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm4
        movsd real8 ptr [r10 + 8], xmm5
        movsd real8 ptr [r10 + 16], xmm6
        movsd real8 ptr [r10 + 24], xmm7
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM5, _TEXT
        movsd real8 ptr [r10], xmm5
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM5, _TEXT

LEAF_ENTRY Store_XMM5_XMM6, _TEXT
        movsd real8 ptr [r10], xmm5
        movsd real8 ptr [r10 + 8], xmm6
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM5_XMM6, _TEXT

LEAF_ENTRY Store_XMM5_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm5
        movsd real8 ptr [r10 + 8], xmm6
        movsd real8 ptr [r10 + 16], xmm7
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM6, _TEXT
        movsd real8 ptr [r10], xmm6
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM6, _TEXT

LEAF_ENTRY Store_XMM6_XMM7, _TEXT
        movsd real8 ptr [r10], xmm6
        movsd real8 ptr [r10 + 8], xmm7
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM6_XMM7, _TEXT

LEAF_ENTRY Store_XMM7, _TEXT
        movsd real8 ptr [r10], xmm7
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Store_XMM7, _TEXT

// Routines for passing value type arguments by reference in general purpose registers RDI, RSI, RDX, RCX, R8, R9
// from the interpreter to native code

// Copy arguments from the the interpreter stack to the CPU stack.
// The CPU stack slots are aligned to pointer size.
LEAF_ENTRY Load_Stack, _TEXT
        push rdi
        push rsi
        push rcx
        mov edi, dword ptr [r11 + 8]  // SP offset
        mov ecx, dword ptr [r11 + 12] // number of stack slots
        add edi, 0x20 // the 3 pushes above plus return address
        add rdi, rsp
        mov rsi, r10
        shr rcx, 3
        rep movsq
        mov r10, rsi
        pop rcx
        pop rsi
        pop rdi
        add r11, 16
        jmp qword ptr [r11]
LEAF_END Load_Stack, _TEXT

LEAF_ENTRY Load_RDI, _TEXT
        mov rdi, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI, _TEXT

LEAF_ENTRY Load_RDI_RSI, _TEXT
        mov rdi, [r10]
        mov rsi, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI_RSI, _TEXT

LEAF_ENTRY Load_RDI_RSI_RDX, _TEXT
        mov rdi, [r10]
        mov rsi, [r10 + 8]
        mov rdx, [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI_RSI_RDX, _TEXT

LEAF_ENTRY Load_RDI_RSI_RDX_RCX, _TEXT
        mov rdi, [r10]
        mov rsi, [r10 + 8]
        mov rdx, [r10 + 16]
        mov rcx, [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI_RSI_RDX_RCX, _TEXT

LEAF_ENTRY Load_RDI_RSI_RDX_RCX_R8, _TEXT
        mov rdi, [r10]
        mov rsi, [r10 + 8]
        mov rdx, [r10 + 16]
        mov rcx, [r10 + 24]
        mov r8, [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI_RSI_RDX_RCX_R8, _TEXT

LEAF_ENTRY Load_RDI_RSI_RDX_RCX_R8_R9, _TEXT
        mov rdi, [r10]
        mov rsi, [r10 + 8]
        mov rdx, [r10 + 16]
        mov rcx, [r10 + 24]
        mov r8, [r10 + 32]
        mov r9, [r10 + 40]
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDI_RSI_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Load_RSI, _TEXT
        mov rsi, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RSI, _TEXT

LEAF_ENTRY Load_RSI_RDX, _TEXT
        mov rsi, [r10]
        mov rdx, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RSI_RDX, _TEXT

LEAF_ENTRY Load_RSI_RDX_RCX, _TEXT
        mov rsi, [r10]
        mov rdx, [r10 + 8]
        mov rcx, [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RSI_RDX_RCX, _TEXT

LEAF_ENTRY Load_RSI_RDX_RCX_R8, _TEXT
        mov rsi, [r10]
        mov rdx, [r10 + 8]
        mov rcx, [r10 + 16]
        mov r8, [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RSI_RDX_RCX_R8, _TEXT

LEAF_ENTRY Load_RSI_RDX_RCX_R8_R9, _TEXT
        mov rsi, [r10]
        mov rdx, [r10 + 8]
        mov rcx, [r10 + 16]
        mov r8, [r10 + 24]
        mov r9, [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RSI_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Load_RDX, _TEXT
        mov rdx, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX, _TEXT

LEAF_ENTRY Load_RDX_RCX, _TEXT
        mov rdx, [r10]
        mov rcx, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX_RCX, _TEXT

LEAF_ENTRY Load_RDX_RCX_R8, _TEXT
        mov rdx, [r10]
        mov rcx, [r10 + 8]
        mov r8, [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX_RCX_R8, _TEXT

LEAF_ENTRY Load_RDX_RCX_R8_R9, _TEXT
        mov rdx, [r10]
        mov rcx, [r10 + 8]
        mov r8, [r10 + 16]
        mov r9, [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX_RCX_R8_R9, _TEXT

LEAF_ENTRY Load_RCX, _TEXT
        mov rcx, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RCX, _TEXT

LEAF_ENTRY Load_RCX_R8, _TEXT
        mov rcx, [r10]
        mov r8, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RCX_R8, _TEXT

LEAF_ENTRY Load_RCX_R8_R9, _TEXT
        mov rcx, [r10]
        mov r8, [r10 + 8]
        mov r9, [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RCX_R8_R9, _TEXT

LEAF_ENTRY Load_RDX_R8, _TEXT
        mov rdx, [r10]
        mov r8, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX_R8, _TEXT

LEAF_ENTRY Load_RDX_R8_R9, _TEXT
        mov rdx, [r10]
        mov r8, [r10 + 8]
        mov r9, [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_RDX_R8_R9, _TEXT

LEAF_ENTRY Load_R8, _TEXT
        mov r8, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_R8, _TEXT

LEAF_ENTRY Load_R8_R9, _TEXT
        mov r8, [r10]
        mov r9, [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_R8_R9, _TEXT

LEAF_ENTRY Load_R9, _TEXT
        mov r9, [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_R9, _TEXT

// Routines for passing arguments in floating point registers XMM0..XMM7

LEAF_ENTRY Load_XMM0, _TEXT
        movsd xmm0, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0, _TEXT

LEAF_ENTRY Load_XMM0_XMM1, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2_XMM3, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        movsd xmm3, real8 ptr [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2_XMM3, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2_XMM3_XMM4, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        movsd xmm3, real8 ptr [r10 + 24]
        movsd xmm4, real8 ptr [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        movsd xmm3, real8 ptr [r10 + 24]
        movsd xmm4, real8 ptr [r10 + 32]
        movsd xmm5, real8 ptr [r10 + 40]
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        movsd xmm3, real8 ptr [r10 + 24]
        movsd xmm4, real8 ptr [r10 + 32]
        movsd xmm5, real8 ptr [r10 + 40]
        movsd xmm6, real8 ptr [r10 + 48]
        add r10, 56
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd xmm0, real8 ptr [r10]
        movsd xmm1, real8 ptr [r10 + 8]
        movsd xmm2, real8 ptr [r10 + 16]
        movsd xmm3, real8 ptr [r10 + 24]
        movsd xmm4, real8 ptr [r10 + 32]
        movsd xmm5, real8 ptr [r10 + 40]
        movsd xmm6, real8 ptr [r10 + 48]
        movsd xmm7, real8 ptr [r10 + 56]
        add r10, 64
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM0_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM1, _TEXT
        movsd xmm1, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1, _TEXT

LEAF_ENTRY Load_XMM1_XMM2, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2, _TEXT

LEAF_ENTRY Load_XMM1_XMM2_XMM3, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        movsd xmm3, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2_XMM3, _TEXT

LEAF_ENTRY Load_XMM1_XMM2_XMM3_XMM4, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        movsd xmm3, real8 ptr [r10 + 16]
        movsd xmm4, real8 ptr [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Load_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        movsd xmm3, real8 ptr [r10 + 16]
        movsd xmm4, real8 ptr [r10 + 24]
        movsd xmm5, real8 ptr [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        movsd xmm3, real8 ptr [r10 + 16]
        movsd xmm4, real8 ptr [r10 + 24]
        movsd xmm5, real8 ptr [r10 + 32]
        movsd xmm6, real8 ptr [r10 + 40]
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd xmm1, real8 ptr [r10]
        movsd xmm2, real8 ptr [r10 + 8]
        movsd xmm3, real8 ptr [r10 + 16]
        movsd xmm4, real8 ptr [r10 + 24]
        movsd xmm5, real8 ptr [r10 + 32]
        movsd xmm6, real8 ptr [r10 + 40]
        movsd xmm7, real8 ptr [r10 + 48]
        add r10, 56
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM1_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM2, _TEXT
        movsd xmm2, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2, _TEXT

LEAF_ENTRY Load_XMM2_XMM3, _TEXT
        movsd xmm2, real8 ptr [r10]
        movsd xmm3, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2_XMM3, _TEXT

LEAF_ENTRY Load_XMM2_XMM3_XMM4, _TEXT
        movsd xmm2, real8 ptr [r10]
        movsd xmm3, real8 ptr [r10 + 8]
        movsd xmm4, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2_XMM3_XMM4, _TEXT

LEAF_ENTRY Load_XMM2_XMM3_XMM4_XMM5, _TEXT
        movsd xmm2, real8 ptr [r10]
        movsd xmm3, real8 ptr [r10 + 8]
        movsd xmm4, real8 ptr [r10 + 16]
        movsd xmm5, real8 ptr [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Load_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd xmm2, real8 ptr [r10]
        movsd xmm3, real8 ptr [r10 + 8]
        movsd xmm4, real8 ptr [r10 + 16]
        movsd xmm5, real8 ptr [r10 + 24]
        movsd xmm6, real8 ptr [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd xmm2, real8 ptr [r10]
        movsd xmm3, real8 ptr [r10 + 8]
        movsd xmm4, real8 ptr [r10 + 16]
        movsd xmm5, real8 ptr [r10 + 24]
        movsd xmm6, real8 ptr [r10 + 32]
        movsd xmm7, real8 ptr [r10 + 40]
        add r10, 48
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM2_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM3, _TEXT
        movsd xmm3, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM3, _TEXT

LEAF_ENTRY Load_XMM3_XMM4, _TEXT
        movsd xmm3, real8 ptr [r10]
        movsd xmm4, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM3_XMM4, _TEXT

LEAF_ENTRY Load_XMM3_XMM4_XMM5, _TEXT
        movsd xmm3, real8 ptr [r10]
        movsd xmm4, real8 ptr [r10 + 8]
        movsd xmm5, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM3_XMM4_XMM5, _TEXT

LEAF_ENTRY Load_XMM3_XMM4_XMM5_XMM6, _TEXT
        movsd xmm3, real8 ptr [r10]
        movsd xmm4, real8 ptr [r10 + 8]
        movsd xmm5, real8 ptr [r10 + 16]
        movsd xmm6, real8 ptr [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM3_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd xmm3, real8 ptr [r10]
        movsd xmm4, real8 ptr [r10 + 8]
        movsd xmm5, real8 ptr [r10 + 16]
        movsd xmm6, real8 ptr [r10 + 24]
        movsd xmm7, real8 ptr [r10 + 32]
        add r10, 40
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM3_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM4, _TEXT
        movsd xmm4, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM4, _TEXT

LEAF_ENTRY Load_XMM4_XMM5, _TEXT
        movsd xmm4, real8 ptr [r10]
        movsd xmm5, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM4_XMM5, _TEXT

LEAF_ENTRY Load_XMM4_XMM5_XMM6, _TEXT
        movsd xmm4, real8 ptr [r10]
        movsd xmm5, real8 ptr [r10 + 8]
        movsd xmm6, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM4_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM4_XMM5_XMM6_XMM7, _TEXT
        movsd xmm4, real8 ptr [r10]
        movsd xmm5, real8 ptr [r10 + 8]
        movsd xmm6, real8 ptr [r10 + 16]
        movsd xmm7, real8 ptr [r10 + 24]
        add r10, 32
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM4_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM5, _TEXT
        movsd xmm5, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM5, _TEXT

LEAF_ENTRY Load_XMM5_XMM6, _TEXT
        movsd xmm5, real8 ptr [r10]
        movsd xmm6, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM5_XMM6, _TEXT

LEAF_ENTRY Load_XMM5_XMM6_XMM7, _TEXT
        movsd xmm5, real8 ptr [r10]
        movsd xmm6, real8 ptr [r10 + 8]
        movsd xmm7, real8 ptr [r10 + 16]
        add r10, 24
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM5_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM6, _TEXT
        movsd xmm6, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM6, _TEXT

LEAF_ENTRY Load_XMM6_XMM7, _TEXT
        movsd xmm6, real8 ptr [r10]
        movsd xmm7, real8 ptr [r10 + 8]
        add r10, 16
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM6_XMM7, _TEXT

LEAF_ENTRY Load_XMM7, _TEXT
        movsd xmm7, real8 ptr [r10]
        add r10, 8
        add r11, 8
        jmp qword ptr [r11]
LEAF_END Load_XMM7, _TEXT


NESTED_ENTRY CallJittedMethodRetVoid, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        alloc_stack 0x10
        save_reg_postrsp r10, 0 
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov r10, [rsp]
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetVoid, _TEXT

NESTED_ENTRY CallJittedMethodRetBuff, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        alloc_stack 0x10
        save_reg_postrsp r10, 0
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        mov rdi, rdx // return buffer
        call qword ptr [r11]
        mov r10, [rsp]
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetBuff, _TEXT

NESTED_ENTRY CallJittedMethodRetDouble, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        //pop rdx
        mov rdx, [rbp - 8]
        movsd real8 ptr [rdx], xmm0
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetDouble, _TEXT

NESTED_ENTRY CallJittedMethodRetI8, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov rdx, [rbp - 8]
        mov qword ptr [rdx], rax
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetI8, _TEXT

NESTED_ENTRY CallJittedMethodRetI8I8, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov rcx, [rbp - 8]
        mov qword ptr [rcx], rax
        mov qword ptr [rcx + 8], rdx
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetI8I8, _TEXT

NESTED_ENTRY CallJittedMethodRetI8Double, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov rcx, [rbp - 8]
        mov qword ptr [rcx], rax
        movsd real8 ptr [rcx + 8], xmm0
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetI8Double, _TEXT

NESTED_ENTRY CallJittedMethodRetDoubleI8, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov rcx, [rbp - 8]
        movsd real8 ptr [rcx], xmm0
        mov qword ptr [rcx + 8], rax
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetDoubleI8, _TEXT

NESTED_ENTRY CallJittedMethodRetDoubleDouble, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov  rbp, rsp
        push_register rdx
        push_register rax // align
END_PROLOGUE
        sub rsp, rcx // total stack space
        mov r11, rdi // The routines list
        mov r10, rsi // interpreter stack args
        call qword ptr [r11]
        mov rcx, [rbp - 8]
        movsd real8 ptr [rcx], xmm0
        movsd real8 ptr [rcx + 8], xmm1
        mov rsp, rbp
        pop rbp
        ret
NESTED_END CallJittedMethodRetDoubleDouble, _TEXT

#endif // FEATURE_INTERPRETER

# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

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

#ifdef FEATURE_INTERPRETER
NESTED_ENTRY InterpreterStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 8, 0, 0, 0, 0
        mov             [rsp], rax // Return buffer in Swift calling convention

        #
        # call ExecuteInterpretedMethod
        #
        lea             rdi, [rsp + __PWTB_TransitionBlock]     // pTransitionBlock*
        mov             rsi, METHODDESC_REGISTER
        call            C_FUNC(ExecuteInterpretedMethod)

        mov             rax, [rsp]
        EPILOG_WITH_TRANSITION_BLOCK_RETURN

NESTED_END InterpreterStub, _TEXT
#endif // FEATURE_INTERPRETER

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#define real4 dword
#define real8 qword

//
//    file: profile.cpp
//    typedef struct _PROFILE_PLATFORM_SPECIFIC_DATA
//    {
//        FunctionID *functionId; // function ID comes in the r11 register
//        void       *rbp;
//        void       *probersp;
//        void       *ip;
//        void       *profiledRsp;
//        UINT64      rax;
//        LPVOID      hiddenArg;
//        UINT64      flt0;
//        UINT64      flt1;
//        UINT64      flt2;
//        UINT64      flt3;
//        UINT64      flt4;
//        UINT64      flt5;
//        UINT64      flt6;
//        UINT64      flt7;
//        UINT64      rdi;
//        UINT64      rsi;
//        UINT64      rdx;
//        UINT64      rcx;
//        UINT64      r8;
//        UINT64      r9;
//        UINT32      flags;
//        UINT64      buffer[PROFILE_PLATFORM_SPECIFIC_DATA_BUFFER_SIZE];
//    } PROFILE_PLATFORM_SPECIFIC_DATA, *PPROFILE_PLATFORM_SPECIFIC_DATA;

// Buffer to copy structs sequentially
.equ SIZEOF_PROFILE_STRUCT_BUFFER, 0x8*16
.equ SIZEOF_PROFILE_PLATFORM_SPECIFIC_DATA, 0x8*22 + 0x8 + SIZEOF_PROFILE_STRUCT_BUFFER // includes fudge to make FP_SPILL right
.equ SIZEOF_FP_ARG_SPILL, 0x10*2

.equ SIZEOF_STACK_FRAME, SIZEOF_PROFILE_PLATFORM_SPECIFIC_DATA + SIZEOF_FP_ARG_SPILL

.equ PROFILE_ENTER, 0x1
.equ PROFILE_LEAVE, 0x2
.equ PROFILE_TAILCALL, 0x4

// ***********************************************************
//   NOTE:
//
//   Register preservation scheme:
//
//       Preserved:
//           - all non-volatile registers
//           - rax, rdx
//           - xmm0, xmm1
//
//       Not Preserved:
//           - integer argument registers (rcx, rdx, r8, r9)
//           - floating point argument registers (xmm1-3)
//           - volatile integer registers (r10, r11)
//           - volatile floating point registers (xmm4-5)
//           - upper halves of ymm registers on AVX (which are volatile)
//
// ***********************************************************

// EXTERN_C void ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
// <NOTE>
//
// </NOTE>
NESTED_ENTRY ProfileEnterNaked, _TEXT, NoHandler
  //       Upon entry :
  //           r14 = clientInfo
  //           r15 = profiledRsp

  push_nonvol_reg         rax

  lea                     rax, [rsp + 0x10]    // caller rsp
  mov                     r10, [rax - 0x8]     // return address

  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  // correctness of return value in structure doesn't matter for enter probe

  // setup ProfilePlatformSpecificData structure
  xor                     r11, r11 // nullify r11
  mov                     [rsp +  0x0], r11    // r11 is null     -- struct functionId field
  save_reg_postrsp        rbp, 0x8             //                 -- struct rbp field
  mov                     [rsp + 0x10], rax    // caller rsp      -- struct probeRsp field
  mov                     [rsp + 0x18], r10    // return address  -- struct ip field
  mov                     [rsp + 0x20], r15    //                 -- struct profiledRsp field
  mov                     [rsp + 0x28], r11    // return value    -- struct rax field
  mov                     [rsp + 0x30], r11    // r11 is null     -- struct hiddenArg field
  movsd                   real8 ptr [rsp + 0x38], xmm0    //      -- struct flt0 field
  movsd                   real8 ptr [rsp + 0x40], xmm1    //      -- struct flt1 field
  movsd                   real8 ptr [rsp + 0x48], xmm2    //      -- struct flt2 field
  movsd                   real8 ptr [rsp + 0x50], xmm3    //      -- struct flt3 field
  movsd                   real8 ptr [rsp + 0x58], xmm4    //      -- struct flt4 field
  movsd                   real8 ptr [rsp + 0x60], xmm5    //      -- struct flt5 field
  movsd                   real8 ptr [rsp + 0x68], xmm6    //      -- struct flt6 field
  movsd                   real8 ptr [rsp + 0x70], xmm7    //      -- struct flt7 field
  mov                     [rsp + 0x78], rdi               //      -- struct rdi field
  mov                     [rsp + 0x80], rsi               //      -- struct rsi field
  mov                     [rsp + 0x88], rdx               //      -- struct rdx field
  mov                     [rsp + 0x90], rcx               //      -- struct rcx field
  mov                     [rsp + 0x98], r8                //      -- struct r8 field
  mov                     [rsp + 0xa0], r9                //      -- struct r9 field
  mov                     r10, 0x1 // PROFILE_ENTER
  mov                     [rsp + 0xa8], r10d              //      -- struct flags field

  // get aligned stack ptr (rsp + FRAME_SIZE) & (-16)
  lea                     rax, [rsp + 0xb8]
  and                     rax, -16

  // we need to be able to restore the fp return register
  // save fp return registers
  movdqa                  [rax + 0x00], xmm0
  movdqa                  [rax + 0x10], xmm1

  END_PROLOGUE

  // rdi already contains the clientInfo
  mov                     rdi, r14
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileEnter)

  // restore fp return registers
  lea                     rax, [rsp + 0xb8]
  and                     rax, -16
  movdqa                  xmm0, [rax + 0x00]
  movdqa                  xmm1, [rax + 0x10]

  // restore arg registers
  mov                     rdi, [rsp + 0x78]
  mov                     rsi, [rsp + 0x80]
  mov                     rdx, [rsp + 0x88]
  mov                     rcx, [rsp + 0x90]
  mov                     r8, [rsp + 0x98]
  mov                     r9, [rsp + 0xa0]

  // begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rax

  ret
NESTED_END ProfileEnterNaked, _TEXT

// EXTERN_C void ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
// <NOTE>
//
// </NOTE>
NESTED_ENTRY ProfileLeaveNaked, _TEXT, NoHandler
//       Upon entry :
//           rdi = clientInfo
//           rsi = profiledRsp

  push_nonvol_reg         rbx

  lea                     rbx, [rsp + 0x10]    // caller rsp
  mov                     r10, [rbx - 0x8]     // return address

  // rdx should be saved here because it can be used for returning struct values
  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  // correctness of argument registers in structure doesn't matter for leave probe

  // setup ProfilePlatformSpecificData structure
  xor                     r11, r11  // nullify r11
  mov                     [rsp +  0x0], r11    // r11 is null     -- struct functionId field
  save_reg_postrsp        rbp, 0x8             //                 -- struct rbp field
  mov                     [rsp + 0x10], rbx    // caller rsp      -- struct probeRsp field
  mov                     [rsp + 0x18], r10    // return address  -- struct ip field
  mov                     [rsp + 0x20], rsi    //                 -- struct profiledRsp field
  mov                     [rsp + 0x28], rax    // return value    -- struct rax field
  mov                     [rsp + 0x30], r11    // r11 is null     -- struct hiddenArg field
  movsd                   real8 ptr [rsp + 0x38], xmm0    //      -- struct flt0 field
  movsd                   real8 ptr [rsp + 0x40], xmm1    //      -- struct flt1 field
  movsd                   real8 ptr [rsp + 0x48], xmm2    //      -- struct flt2 field
  movsd                   real8 ptr [rsp + 0x50], xmm3    //      -- struct flt3 field
  movsd                   real8 ptr [rsp + 0x58], xmm4    //      -- struct flt4 field
  movsd                   real8 ptr [rsp + 0x60], xmm5    //      -- struct flt5 field
  movsd                   real8 ptr [rsp + 0x68], xmm6    //      -- struct flt6 field
  movsd                   real8 ptr [rsp + 0x70], xmm7    //      -- struct flt7 field
  mov                     [rsp + 0x78], r11     //                -- struct rdi field
  mov                     [rsp + 0x80], r11     //                -- struct rsi field
  mov                     [rsp + 0x88], r11     //                -- struct rdx field
  mov                     [rsp + 0x90], r11     //                -- struct rcx field
  mov                     [rsp + 0x98], r11     //                -- struct r8 field
  mov                     [rsp + 0xa0], r11    //                -- struct r9 field
  mov                     r10, 0x2  // PROFILE_LEAVE
  mov                     [rsp + 0xa8], r10d   // flags           -- struct flags field

  // get aligned stack ptr (rsp + FRAME_SIZE) & (-16)
  lea                     rax, [rsp + 0xb8]
  and                     rax, -16

  // we need to be able to restore the fp return register
  // save fp return registers
  movdqa                  [rax + 0x00], xmm0
  movdqa                  [rax + 0x10], xmm1

  END_PROLOGUE

  // rdi already contains the clientInfo
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileLeave)

  // restore fp return registers
  lea                     rax, [rsp + 0xb8]
  and                     rax, -16
  movdqa                  xmm0, [rax + 0x00]
  movdqa                  xmm1, [rax + 0x10]

  // restore int return register
  mov                     rax, [rsp + 0x28]

  // begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rbx

  ret
NESTED_END ProfileLeaveNaked, _TEXT

// EXTERN_C void ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID, size_t profiledRsp);
// <NOTE>
//
// </NOTE>
NESTED_ENTRY ProfileTailcallNaked, _TEXT, NoHandler
//       Upon entry :
//           rdi = clientInfo
//           rsi = profiledRsp

  push_nonvol_reg         rbx

  lea                     rbx, [rsp + 0x10]    // caller rsp
  mov                     r10, [rbx - 0x8]     // return address

  // rdx should be saved here because it can be used for returning struct values
  push_argument_register  rdx
  alloc_stack             SIZEOF_STACK_FRAME

  // correctness of argument registers in structure doesn't matter for tailcall probe

  // setup ProfilePlatformSpecificData structure
  xor                     r11, r11  // nullify r11
  mov                     [rsp +  0x0], r11    // r11 is null     -- struct functionId field
  save_reg_postrsp        rbp, 0x8             //                 -- struct rbp field
  mov                     [rsp + 0x10], rbx    // caller rsp      -- struct probeRsp field
  mov                     [rsp + 0x18], r10    // return address  -- struct ip field
  mov                     [rsp + 0x20], rsi    //                 -- struct profiledRsp field
  mov                     [rsp + 0x28], rax    // return value    -- struct rax field
  mov                     [rsp + 0x30], r11    // r11 is null     -- struct hiddenArg field
  movsd                   real8 ptr [rsp + 0x38], xmm0    //      -- struct flt0 field
  movsd                   real8 ptr [rsp + 0x40], xmm1    //      -- struct flt1 field
  movsd                   real8 ptr [rsp + 0x48], xmm2    //      -- struct flt2 field
  movsd                   real8 ptr [rsp + 0x50], xmm3    //      -- struct flt3 field
  movsd                   real8 ptr [rsp + 0x58], xmm4    //      -- struct flt4 field
  movsd                   real8 ptr [rsp + 0x60], xmm5    //      -- struct flt5 field
  movsd                   real8 ptr [rsp + 0x68], xmm6    //      -- struct flt6 field
  movsd                   real8 ptr [rsp + 0x70], xmm7    //      -- struct flt7 field
  mov                     [rsp + 0x78], r11     //                -- struct rdi field
  mov                     [rsp + 0x80], r11     //                -- struct rsi field
  mov                     [rsp + 0x88], r11     //                -- struct rdx field
  mov                     [rsp + 0x90], r11     //                -- struct rcx field
  mov                     [rsp + 0x98], r11     //                -- struct r8 field
  mov                     [rsp + 0xa0], r11     //                -- struct r9 field
  mov                     r10, 0x2  // PROFILE_LEAVE
  mov                     [rsp + 0xa8], r10d   // flags           -- struct flags field

  // get aligned stack ptr (rsp + FRAME_SIZE) & (-16)
  lea                     rax, [rsp + 0xc0]
  and                     rax, -16

  // we need to be able to restore the fp return register
  // save fp return registers
  movdqa                  [rax + 0x00], xmm0
  movdqa                  [rax + 0x10], xmm1

  END_PROLOGUE

  // rdi already contains the clientInfo
  lea                     rsi, [rsp + 0x0]
  call                    C_FUNC(ProfileTailcall)

  // restore fp return registers
  lea                     rax, [rsp + 0xc0]
  and                     rax, -16
  movdqa                  xmm0, [rax + 0x00]
  movdqa                  xmm1, [rax + 0x10]

  // restore int return register
  mov                     rax, [rsp + 0x28]

  // begin epilogue
  free_stack              SIZEOF_STACK_FRAME
  pop_argument_register   rdx

  pop_nonvol_reg          rbx

  ret
NESTED_END ProfileTailcallNaked, _TEXT

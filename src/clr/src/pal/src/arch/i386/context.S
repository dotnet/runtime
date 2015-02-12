//
// Copyright (c) Geoff Norton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#if defined(_DEBUG)
    .text
    .globl _DBG_CheckStackAlignment

_DBG_CheckStackAlignment:
  // Prolog - at this point we are at aligned - 8 (for the call)
  pushq %rbp                        // aligned -16
  movq %rsp, %rbp

  testl $0xf,%esp      // can get away with esp even on AMD64.
  jz .+3
  int3

  // Epilog
  popq %rbp
  ret
#endif

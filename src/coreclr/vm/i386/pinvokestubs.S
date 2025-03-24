// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// IN:
// InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeBegin, _TEXT
        // Not yet supported
        int 3
        ret
LEAF_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT
        // Not yet supported
        int 3
        ret
LEAF_END JIT_PInvokeEnd, _TEXT

//
// IN:
// InlinedCallFrame (edi) = pointer to the InlinedCallFrame data
// OUT:
// Thread (esi) = pointer to Thread data
//
//
LEAF_ENTRY JIT_InitPInvokeFrame, _TEXT
        // Not called by JIT, we use CORJIT_FLAG_USE_PINVOKE_HELPERS
        int 3
        ret
LEAF_END JIT_InitPInvokeFrame, _TEXT

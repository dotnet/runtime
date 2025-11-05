// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"


//
// in:
// PINVOKE_CALLI_TARGET_REGISTER (r10) = unmanaged target
// PINVOKE_CALLI_SIGTOKEN_REGNUM (r11) = sig token
//
// out:
// METHODDESC_REGISTER           (r10) = unmanaged target
//
LEAF_ENTRY GenericPInvokeCalliHelper, _TEXT
// NOT YET IMPLEMENTED
.long 0x61
LEAF_END GenericPInvokeCalliHelper, _TEXT

NESTED_ENTRY GenericPInvokeCalliGenILStub, _TEXT, NoHandler
// NOT YET IMPLEMENTED
.long 0x62
NESTED_END GenericPInvokeCalliGenILStub, _TEXT

LEAF_ENTRY VarargPInvokeStub, _TEXT
// NOT YET IMPLEMENTED
.long 0x63
LEAF_END VarargPInvokeStub, _TEXT

LEAF_ENTRY VarargPInvokeStub_RetBuffArg, _TEXT
// NOT YET IMPLEMENTED
.long 0x64
LEAF_END VarargPInvokeStub_RetBuffArg, _TEXT

LEAF_ENTRY VarargPInvokeStubHelper, _TEXT
// NOT YET IMPLEMENTED
.long 0x65
LEAF_END VarargPInvokeStubHelper, _TEXT

//
// IN: METHODDESC_REGISTER (R10) stub secret param
//     PINVOKE_CALLI_SIGTOKEN_REGISTER (R11) VASigCookie*
//
// ASSUMES: we already checked for an existing stub to use
//
NESTED_ENTRY VarargPInvokeGenILStub, _TEXT, NoHandler
// NOT YET IMPLEMENTED
.long 0x66
NESTED_END VarargPInvokeGenILStub, _TEXT

//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler
// NOT YET IMPLEMENTED
.long 0x67
NESTED_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT
// NOT YET IMPLEMENTED
.long 0x68
LEAF_END JIT_PInvokeEnd, _TEXT

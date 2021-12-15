// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

//////////////////////////////////////////////////////////////////////////////

// clang-format off
#if !defined(TARGET_LOONGARCH64)
#error Unexpected target type
#endif

#ifdef DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#undef DEFINE_ID_OPS
enum ID_OPS
{
    ID_OP_NONE, // no additional arguments
    ID_OP_SCNS, // small const  operand (21-bits or less, no reloc)
    ID_OP_JMP,  // local jump
    ID_OP_CALL, // method call
    ID_OP_SPEC, // special handling required
};

//////////////////////////////////////////////////////////////////////////////
#else // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#ifndef IF_DEF
#error Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////

IF_DEF(NONE, IS_NONE, NONE) //


//IF_DEF(LABEL, IS_NONE, JMP)    // label
//IF_DEF(LARGEJMP, IS_NONE, JMP) // large conditional branch pseudo-op (cond branch + uncond branch)
//IF_DEF(LARGEADR, IS_NONE, JMP) // large address pseudo-op (adrp + add)
//IF_DEF(LARGELDC, IS_NONE, JMP) // large constant pseudo-op (adrp + ldr)


IF_DEF(OPCODE, IS_NONE, NONE)
IF_DEF(OPCODES_16, IS_NONE, NONE)
IF_DEF(OP_FMT, IS_NONE, NONE)
IF_DEF(OP_FMT_16, IS_NONE, NONE)
IF_DEF(OP_FMTS_16, IS_NONE, NONE)
IF_DEF(FMT_FUNC, IS_NONE, NONE)
IF_DEF(FMT_FUNC_6, IS_NONE, NONE)
IF_DEF(FMT_FUNC_16, IS_NONE, NONE)
IF_DEF(FMT_FUNCS_6, IS_NONE, NONE)
IF_DEF(FMT_FUNCS_16, IS_NONE, NONE)
IF_DEF(FMT_FUNCS_6A, IS_NONE, NONE)
IF_DEF(FMT_FUNCS_11A, IS_NONE, NONE)
IF_DEF(FUNC, IS_NONE, NONE)
IF_DEF(FUNC_6, IS_NONE, NONE)
IF_DEF(FUNC_16, IS_NONE, NONE)
IF_DEF(FUNC_21, IS_NONE, NONE)
IF_DEF(FUNCS_6, IS_NONE, NONE)
IF_DEF(FUNCS_6A, IS_NONE, NONE)
IF_DEF(FUNCS_6B, IS_NONE, NONE)
IF_DEF(FUNCS_6C, IS_NONE, NONE)
IF_DEF(FUNCS_6D, IS_NONE, NONE)
IF_DEF(FUNCS_6E, IS_NONE, NONE)
IF_DEF(FUNCS_11, IS_NONE, NONE)


//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////

#endif // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////
// clang-format on

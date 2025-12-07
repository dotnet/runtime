// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off
#if !defined(TARGET_WASM)
#error Unexpected target type
#endif

#ifdef DEFINE_ID_OPS
enum ID_OPS
{
    ID_OP_NONE,
};
#undef DEFINE_ID_OPS

#else // !DEFINE_ID_OPS

#ifndef IF_DEF
#error Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////

IF_DEF(NONE,    IS_NONE, NONE)
IF_DEF(OPCODE,  IS_NONE, NONE) // <opcode>
IF_DEF(BLOCK,   IS_NONE, NONE) // <opcode> <0x40>
IF_DEF(LABEL,   IS_NONE, NONE) // <ULEB128 immediate>
IF_DEF(ULEB128, IS_NONE, NONE) // <opcode> <ULEB128 immediate>
IF_DEF(MEMARG,  IS_NONE, NONE) // <opcode> <memarg> (<align> <offset>)

#undef IF_DEF
#endif // !DEFINE_ID_OPS
// clang-format on

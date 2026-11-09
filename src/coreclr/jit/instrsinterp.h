// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  Interpreter instructions
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly display)
 *          info        -- miscellaneous instruction info
 *          fmt         -- instruction format
 *
 *  Interpreter bytecode is generated directly from LIR (see codegeninterp.cpp)
 *  rather than through the emitter, so this list only carries the entries the
 *  shared emitter code requires.
 *
 ******************************************************************************/

#ifndef TARGET_INTERP
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

// clang-format off

INST(invalid,              "INVALID",              0, IF_NONE)
INST(nop,                  "nop",                  0, IF_OPCODE)
INST(breakpoint,           "breakpoint",           0, IF_OPCODE)
INST(br,                   "br",                   0, IF_OPCODE)

// clang-format on

#undef INST

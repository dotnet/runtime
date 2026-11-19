// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILDisassembler;

// Operand kinds, in the order of the native OPCODE_FORMAT enum (opcode.def).
internal enum OperandType
{
    InlineNone,
    ShortInlineVar,
    InlineVar,
    ShortInlineI,
    InlineI,
    InlineI8,
    ShortInlineR,
    InlineR,
    InlineMethod,
    InlineSig,
    ShortInlineBrTarget,
    InlineBrTarget,
    InlineSwitch,
    InlineType,
    InlineString,
    InlineField,
    InlineTok,
    InlineRVA,
    InlinePhi,
}

internal readonly record struct OpcodeInfo(string Name, OperandType Type);

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The names that the dumps print for the JIT flags (CorJitFlag) and the instruction sets of the host architecture.

using System;
using System.Runtime.InteropServices;
using Internal.JitInterface;

namespace SuperPmi;

public static class JitFlagNames
{
    public static string CorJitFlagToString(uint flag) => Enum.GetName((CorJitFlag)flag) ?? "<unknown>";

    public static string InstructionSetToString(uint instructionSet)
    {
        string name = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => Enum.GetName((InstructionSet_X64)instructionSet),
            Architecture.X86 => Enum.GetName((InstructionSet_X86)instructionSet),
            Architecture.Arm64 => Enum.GetName((InstructionSet_ARM64)instructionSet),
            Architecture.RiscV64 => Enum.GetName((InstructionSet_RiscV64)instructionSet),
            Architecture.Wasm => Enum.GetName((InstructionSet_Wasm32)instructionSet),
            _ => null,
        };
        return name is null or nameof(InstructionSet.ILLEGAL) or nameof(InstructionSet.NONE) ? "UnknownInstructionSet" : name;
    }
}

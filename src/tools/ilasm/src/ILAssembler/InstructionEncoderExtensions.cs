// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

internal static class InstructionEncoderExtensions
{
    public static void MarkLabel(this InstructionEncoder encoder, LabelHandle label, int ilOffset)
    {
        // TODO-SRM: Propose a public API for this so we don't need to use UnsafeAccessor into the BCL.
        MarkLabel(encoder.ControlFlowBuilder!, ilOffset, label);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    private static extern void MarkLabel(ControlFlowBuilder builder, int ilOffset, LabelHandle label);
}

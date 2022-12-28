// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

internal static class InstructionEncoderExtensions
{
    // There is no public API to label an instruction at a specific offset,
    // so use reflection for now to get access to the internal method until I bring this up for API review.
    private static readonly Action<ControlFlowBuilder, int, LabelHandle> _markLabelAtOffset =
        (Action<ControlFlowBuilder, int, LabelHandle>)
            typeof(ControlFlowBuilder)
            .GetMethod(nameof(MarkLabel), BindingFlags.NonPublic | BindingFlags.Instance)!
            .CreateDelegate(typeof(Action<ControlFlowBuilder, int, LabelHandle>));
    public static void MarkLabel(this InstructionEncoder encoder, LabelHandle label, int ilOffset)
    {
        _markLabelAtOffset(encoder.ControlFlowBuilder!, ilOffset, label);
    }
}

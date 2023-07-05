// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Encodes the branches of an IL <c>switch</c> instruction.
    /// </summary>
    /// <remarks>
    /// See <see cref="InstructionEncoder.Switch(int)"/> for usage guidelines.
    /// </remarks>
    public readonly struct SwitchInstructionEncoder
    {
        private readonly InstructionEncoder _encoder;

        private readonly int _ilOffset, _instructionEnd;

        internal SwitchInstructionEncoder(InstructionEncoder encoder, int ilOffset, int instructionEnd)
        {
            Debug.Assert(encoder.ControlFlowBuilder is not null);
            _encoder = encoder;
            _ilOffset = ilOffset;
            _instructionEnd = instructionEnd;
        }

        /// <summary>
        /// Encodes a branch that is part of a switch instruction.
        /// </summary>
        /// <remarks>
        /// See <see cref="InstructionEncoder.Switch(int)"/> for usage guidelines.
        /// </remarks>
        public void Branch(LabelHandle label)
        {
            _encoder.ControlFlowBuilder!.SwitchBranchAdded();
            _encoder.LabelOperand(ILOpCode.Switch, label, _instructionEnd - _encoder.Offset, _ilOffset);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class NullCheckInstruction : Instruction
    {
        public static readonly Instruction Instance = new NullCheckInstruction();

        private NullCheckInstruction() { }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Unbox";

        public override int Run(InterpretedFrame frame)
        {
            NullCheck(frame.Peek());
            return 1;
        }
    }
}

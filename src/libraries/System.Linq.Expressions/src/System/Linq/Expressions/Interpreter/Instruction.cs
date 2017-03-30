// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract partial class Instruction
    {
        public const int UnknownInstrIndex = int.MaxValue;

        public virtual int ConsumedStack => 0;
        public virtual int ProducedStack => 0;
        public virtual int ConsumedContinuations => 0;
        public virtual int ProducedContinuations => 0;

        public int StackBalance => ProducedStack - ConsumedStack;
        public int ContinuationsBalance => ProducedContinuations - ConsumedContinuations;

        public abstract int Run(InterpretedFrame frame);

        public abstract string InstructionName { get; }

        public override string ToString() => InstructionName + "()";

        public virtual string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IReadOnlyList<object> objects) => ToString();

        public virtual object GetDebugCookie(LightCompiler compiler) => null;

        // throws NRE when o is null
        protected static void NullCheck(object o)
        {
            if (o == null)
            {
                o.GetType();
            }
        }
    }
}


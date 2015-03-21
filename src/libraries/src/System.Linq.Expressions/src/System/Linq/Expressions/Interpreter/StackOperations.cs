// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Globalization;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class LoadObjectInstruction : Instruction
    {
        private readonly object _value;

        internal LoadObjectInstruction(object value)
        {
            _value = value;
        }

        public override int ProducedStack { get { return 1; } }

        public override string InstructionName
        {
            get { return "LoadObject"; }
        }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = _value;
            return +1;
        }

        public override string ToString()
        {
            return "LoadObject(" + (_value ?? "null") + ")";
        }
    }

    internal sealed class LoadCachedObjectInstruction : Instruction
    {
        private readonly uint _index;

        internal LoadCachedObjectInstruction(uint index)
        {
            _index = index;
        }

        public override int ProducedStack { get { return 1; } }

        public override string InstructionName
        {
            get { return "LoadCachedObject"; }
        }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Interpreter._objects[_index];
            return +1;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return String.Format(CultureInfo.InvariantCulture, "LoadCached({0}: {1})", _index, objects[(int)_index]);
        }

        public override string ToString()
        {
            return "LoadCached(" + _index + ")";
        }
    }

    internal sealed class PopInstruction : Instruction
    {
        internal static readonly PopInstruction Instance = new PopInstruction();

        private PopInstruction() { }

        public override int ConsumedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Pop"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            frame.Pop();
            return +1;
        }

        public override string ToString()
        {
            return "Pop()";
        }
    }

    internal sealed class DupInstruction : Instruction
    {
        internal readonly static DupInstruction Instance = new DupInstruction();

        private DupInstruction() { }

        public override int ConsumedStack { get { return 0; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Dup"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            var value = frame.Peek();
            frame.Data[frame.StackIndex++] = value;
            return +1;
        }

        public override string ToString()
        {
            return "Dup()";
        }
    }
}

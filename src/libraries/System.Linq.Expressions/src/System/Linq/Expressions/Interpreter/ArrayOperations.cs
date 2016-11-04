// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class NewArrayInitInstruction : Instruction
    {
        private readonly Type _elementType;
        private readonly int _elementCount;

        internal NewArrayInitInstruction(Type elementType, int elementCount)
        {
            _elementType = elementType;
            _elementCount = elementCount;
        }

        public override int ConsumedStack => _elementCount;
        public override int ProducedStack => 1;
        public override string InstructionName => "NewArrayInit";

        public override int Run(InterpretedFrame frame)
        {
            Array array = Array.CreateInstance(_elementType, _elementCount);
            for (int i = _elementCount - 1; i >= 0; i--)
            {
                array.SetValue(frame.Pop(), i);
            }
            frame.Push(array);
            return +1;
        }
    }

    internal sealed class NewArrayInstruction : Instruction
    {
        private readonly Type _elementType;

        internal NewArrayInstruction(Type elementType)
        {
            _elementType = elementType;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "NewArray";

        public override int Run(InterpretedFrame frame)
        {
            int length = ConvertHelper.ToInt32NoNull(frame.Pop());
            if (length < 0)
            {
                // to make behavior aligned with array creation emitted by C# compiler
                throw new OverflowException();
            }
            frame.Push(Array.CreateInstance(_elementType, length));
            return +1;
        }
    }

    internal sealed class NewArrayBoundsInstruction : Instruction
    {
        private readonly Type _elementType;
        private readonly int _rank;

        internal NewArrayBoundsInstruction(Type elementType, int rank)
        {
            _elementType = elementType;
            _rank = rank;
        }

        public override int ConsumedStack => _rank;
        public override int ProducedStack => 1;
        public override string InstructionName => "NewArrayBounds";

        public override int Run(InterpretedFrame frame)
        {
            var lengths = new int[_rank];
            for (int i = _rank - 1; i >= 0; i--)
            {
                int length = ConvertHelper.ToInt32NoNull(frame.Pop());

                if (length < 0)
                {
                    // to make behavior aligned with array creation emitted by C# compiler
                    throw new OverflowException();
                }

                lengths[i] = length;
            }
            Array array = Array.CreateInstance(_elementType, lengths);
            frame.Push(array);
            return +1;
        }
    }

    internal sealed class GetArrayItemInstruction : Instruction
    {
        internal static readonly GetArrayItemInstruction Instruction = new GetArrayItemInstruction();

        internal GetArrayItemInstruction() { }

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "GetArrayItem";

        public override int Run(InterpretedFrame frame)
        {
            int index = ConvertHelper.ToInt32NoNull(frame.Pop());
            Array array = (Array)frame.Pop();
            frame.Push(array.GetValue(index));
            return +1;
        }
    }

    internal sealed class SetArrayItemInstruction : Instruction
    {
        internal static readonly SetArrayItemInstruction Instruction = new SetArrayItemInstruction();

        internal SetArrayItemInstruction() { }

        public override int ConsumedStack => 3;
        public override int ProducedStack => 0;
        public override string InstructionName => "SetArrayItem";

        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            int index = ConvertHelper.ToInt32NoNull(frame.Pop());
            Array array = (Array)frame.Pop();
            array.SetValue(value, index);
            return +1;
        }
    }

    internal sealed class ArrayLengthInstruction : Instruction
    {
        public static readonly ArrayLengthInstruction Instance = new ArrayLengthInstruction();

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "ArrayLength";

        private ArrayLengthInstruction() { }

        public override int Run(InterpretedFrame frame)
        {
            object obj = frame.Pop();
            frame.Push(((Array)obj).Length);
            return +1;
        }
    }

    internal sealed class ConvertHelper
    {
        public static int ToInt32NoNull(object val)
        {
            // If the value is null, unbox and cast to throw an InvalidOperationException
            // that the desktop throws.
            return (val == null) ? (int)(int?)val : Convert.ToInt32(val);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class FieldInstruction : Instruction
    {
        protected readonly FieldInfo _field;

        public FieldInstruction(FieldInfo field)
        {
            Assert.NotNull(field);
            _field = field;
        }

        public override string ToString() => InstructionName + "(" + _field + ")";
    }

    internal sealed class LoadStaticFieldInstruction : FieldInstruction
    {
        public LoadStaticFieldInstruction(FieldInfo field)
            : base(field)
        {
            Debug.Assert(field.IsStatic);
        }

        public override string InstructionName => "LoadStaticField";
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            frame.Push(_field.GetValue(obj: null));
            return 1;
        }
    }

    internal sealed class LoadFieldInstruction : FieldInstruction
    {
        public LoadFieldInstruction(FieldInfo field)
            : base(field)
        {
        }

        public override string InstructionName => "LoadField";
        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            object self = frame.Pop();

            NullCheck(self);
            frame.Push(_field.GetValue(self));
            return 1;
        }
    }

    internal sealed class StoreFieldInstruction : FieldInstruction
    {
        public StoreFieldInstruction(FieldInfo field)
            : base(field)
        {
            Assert.NotNull(field);
        }

        public override string InstructionName => "StoreField";
        public override int ConsumedStack => 2;

        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            object self = frame.Pop();

            NullCheck(self);
            _field.SetValue(self, value);
            return 1;
        }
    }

    internal sealed class StoreStaticFieldInstruction : FieldInstruction
    {
        public StoreStaticFieldInstruction(FieldInfo field)
            : base(field)
        {
            Debug.Assert(field.IsStatic);
        }

        public override string InstructionName => "StoreStaticField";

        public override int ConsumedStack => 1;

        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            _field.SetValue(null, value);
            return 1;
        }
    }
}

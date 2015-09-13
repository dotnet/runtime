// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class LoadStaticFieldInstruction : Instruction
    {
        private readonly FieldInfo _field;

        public LoadStaticFieldInstruction(FieldInfo field)
        {
            Debug.Assert(field.IsStatic);
            _field = field;
        }

        public override string InstructionName
        {
            get { return "LoadStaticField"; }
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Push(_field.GetValue(null));
            return +1;
        }
    }

    internal sealed class LoadFieldInstruction : Instruction
    {
        private readonly FieldInfo _field;

        public LoadFieldInstruction(FieldInfo field)
        {
            Assert.NotNull(field);
            _field = field;
        }

        public override string InstructionName
        {
            get { return "LoadField"; }
        }
        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            object self = frame.Pop();

            if (self == null)
            {
                throw new NullReferenceException();
            }

            frame.Push(_field.GetValue(self));
            return +1;
        }
    }

    internal sealed class StoreFieldInstruction : Instruction
    {
        private readonly FieldInfo _field;

        public StoreFieldInstruction(FieldInfo field)
        {
            Assert.NotNull(field);
            _field = field;
        }
        public override string InstructionName
        {
            get { return "StoreField"; }
        }
        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 0; } }

        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            object self = frame.Pop();

            if (self == null)
            {
                throw new NullReferenceException();
            }

            _field.SetValue(self, value);
            return +1;
        }
    }

    internal sealed class StoreStaticFieldInstruction : Instruction
    {
        private readonly FieldInfo _field;

        public StoreStaticFieldInstruction(FieldInfo field)
        {
            Assert.NotNull(field);
            _field = field;
        }

        public override string InstructionName
        {
            get { return "StoreStaticField"; }
        }
        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 0; } }

        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            _field.SetValue(null, value);
            return +1;
        }
    }
}

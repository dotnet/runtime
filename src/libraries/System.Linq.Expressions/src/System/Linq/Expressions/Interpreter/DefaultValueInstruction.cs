// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class DefaultValueInstruction : Instruction
    {
        private readonly Type _type;

        internal DefaultValueInstruction(Type type)
        {
            Debug.Assert(type.IsValueType);
            Debug.Assert(!type.IsNullableType());
            _type = type;
        }

        public override int ProducedStack => 1;

        public override string InstructionName => "DefaultValue";

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern",
            Justification = "_type is a ValueType. You can always create an instance of a ValueType.")]
        public override int Run(InterpretedFrame frame)
        {
            frame.Push(Activator.CreateInstance(_type));
            return 1;
        }

        public override string ToString() => "DefaultValue " + _type;
    }
}

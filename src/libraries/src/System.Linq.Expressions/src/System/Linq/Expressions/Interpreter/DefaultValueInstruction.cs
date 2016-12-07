// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class DefaultValueInstruction : Instruction
    {
        private readonly Type _type;

        internal DefaultValueInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack => 0;
        public override int ProducedStack => 1;
        public override string InstructionName => "DefaultValue";

        public override int Run(InterpretedFrame frame)
        {
            object value = _type.GetTypeInfo().IsValueType ? Activator.CreateInstance(_type) : null;
            frame.Push(value);
            return 1;
        }

        public override string ToString() => "DefaultValue " + _type;
    }
}

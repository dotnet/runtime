// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal class NewInstruction : Instruction
    {
        protected readonly ConstructorInfo _constructor;
        protected readonly int _argumentCount;

        public NewInstruction(ConstructorInfo constructor, int argumentCount)
        {
            _constructor = constructor;
            _argumentCount = argumentCount;
        }

        public override int ConsumedStack => _argumentCount;
        public override int ProducedStack => 1;
        public override string InstructionName => "New";

        public override int Run(InterpretedFrame frame)
        {
            int first = frame.StackIndex - _argumentCount;

            object[] args = GetArgs(frame, first);

            object ret;
            try
            {
                ret = _constructor.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }

            frame.Data[first] = ret;
            frame.StackIndex = first + 1;

            return +1;
        }

        protected object[] GetArgs(InterpretedFrame frame, int first)
        {
            if (_argumentCount > 0)
            {
                var args = new object[_argumentCount];

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = frame.Data[first + i];
                }

                return args;
            }
            else
            {
                return Array.Empty<object>();
            }
        }

        public override string ToString() => "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")";
    }

    internal class ByRefNewInstruction : NewInstruction
    {
        private readonly ByRefUpdater[] _byrefArgs;

        internal ByRefNewInstruction(ConstructorInfo target, int argumentCount, ByRefUpdater[] byrefArgs)
            : base(target, argumentCount)
        {
            _byrefArgs = byrefArgs;
        }

        public override string InstructionName => "ByRefNew";

        public sealed override int Run(InterpretedFrame frame)
        {
            int first = frame.StackIndex - _argumentCount;

            object[] args = GetArgs(frame, first);

            try
            {
                object ret;
                try
                {
                    ret = _constructor.Invoke(args);
                }
                catch (TargetInvocationException e)
                {
                    throw ExceptionHelpers.UpdateForRethrow(e.InnerException);
                }

                frame.Data[first] = ret;
                frame.StackIndex = first + 1;
            }
            finally
            {
                if (args != null)
                {
                    foreach (ByRefUpdater arg in _byrefArgs)
                    {
                        arg.Update(frame, args[arg.ArgumentIndex]);
                    }
                }
            }

            return 1;
        }
    }
}

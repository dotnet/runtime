// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.MethodInfos;

using Internal.Reflection.Core.Execution;

using static System.Reflection.DynamicInvokeInfo;

namespace System.Reflection
{
    public sealed class ConstructorInvoker
    {
        private readonly MethodBaseInvoker _methodBaseInvoker;
        private readonly int _parameterCount;

        internal ConstructorInvoker(RuntimeConstructorInfo constructor)
        {
            _methodBaseInvoker = constructor.MethodInvoker;
            _parameterCount = constructor.GetParametersAsSpan().Length;
        }

        public static ConstructorInvoker Create(ConstructorInfo constructor)
        {
            if (constructor is not RuntimeConstructorInfo runtimeConstructor)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeConstructorInfo, nameof(constructor));
            }

            return new ConstructorInvoker(runtimeConstructor);
        }

        [DebuggerGuidedStepThrough]
        public object Invoke()
        {
            if (_parameterCount != 0)
            {
                ThrowForArgCountMismatch();
            }

            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(default);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1)
        {
            if (_parameterCount != 1)
            {
                ThrowForArgCountMismatch();
            }

            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(new Span<object?>(ref arg1, _parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2)
        {
            if (_parameterCount != 2)
            {
                ThrowForArgCountMismatch();
            }

            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(_parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3)
        {
            if (_parameterCount != 3)
            {
                ThrowForArgCountMismatch();
            }

            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(_parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3, object? arg4)
        {
            if (_parameterCount != 4)
            {
                ThrowForArgCountMismatch();
            }

            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);
            argStorage._args.Set(3, arg4);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(_parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(Span<object?> arguments)
        {
            object result = _methodBaseInvoker.CreateInstance(arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DoesNotReturn]
        private static void ThrowForArgCountMismatch()
        {
            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.MethodInfos;
using System.Runtime.CompilerServices;

using Internal.Reflection.Core.Execution;

namespace System.Reflection
{
    public sealed class ConstructorInvoker
    {
        private readonly MethodBaseInvoker _methodBaseInvoker;

        internal ConstructorInvoker(RuntimeConstructorInfo constructor)
        {
            _methodBaseInvoker = constructor.MethodInvoker;
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
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(default);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1)
        {
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(new Span<object?>(ref arg1));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2)
        {
            InlineArray2<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3)
        {
            InlineArray3<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;
            argStorage[2] = arg3;
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3, object? arg4)
        {
            InlineArray4<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;
            argStorage[2] = arg3;
            argStorage[3] = arg4;
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage);
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
    }
}

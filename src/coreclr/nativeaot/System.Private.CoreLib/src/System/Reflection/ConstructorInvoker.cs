// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Reflection.Core.Execution;
using System.Diagnostics;
using System.Reflection.Runtime.MethodInfos;
using static System.Reflection.DynamicInvokeInfo;

namespace System.Reflection
{
    public sealed class ConstructorInvoker
    {
        private readonly MethodBaseInvoker _methodBaseInvoker;
        private readonly RuntimeTypeHandle _declaringTypeHandle;

        internal ConstructorInvoker(RuntimeConstructorInfo constructor)
        {
            _methodBaseInvoker = constructor.MethodInvoker;
            _declaringTypeHandle = constructor.DeclaringType.TypeHandle;
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
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(new Span<object?>());
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
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(2));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3)
        {
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(3));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object Invoke(object? arg1, object? arg2, object? arg3, object? arg4)
        {
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);
            argStorage._args.Set(3, arg4);
            object result = _methodBaseInvoker.CreateInstanceWithFewArgs(argStorage._args.AsSpan(4));
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

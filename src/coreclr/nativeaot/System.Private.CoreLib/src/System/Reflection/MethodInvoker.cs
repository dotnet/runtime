// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Reflection.Core.Execution;
using System.Diagnostics;
using System.Reflection.Runtime.MethodInfos;
using static System.Reflection.DynamicInvokeInfo;

namespace System.Reflection
{
    public sealed class MethodInvoker
    {
        private readonly MethodBaseInvoker _methodBaseInvoker;

        internal MethodInvoker(RuntimeMethodInfo method)
        {
            _methodBaseInvoker = method.MethodInvoker;
        }

        internal MethodInvoker(RuntimeConstructorInfo constructor)
        {
            _methodBaseInvoker = constructor.MethodInvoker;
        }

        public static MethodInvoker Create(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method, nameof(method));

            if (method is RuntimeMethodInfo rmi)
            {
                return new MethodInvoker(rmi);
            }

            if (method is RuntimeConstructorInfo rci)
            {
                // This is useful for calling a constructor on an already-initialized object
                // such as created from RuntimeHelpers.GetUninitializedObject(Type).
                return new MethodInvoker(rci);
            }

            throw new ArgumentException(SR.Argument_MustBeRuntimeMethod, nameof(method));
        }

        public object? Invoke(object? obj)
        {
            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, new Span<object?>());
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public object? Invoke(object? obj, object? arg1)
        {
            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, new Span<object?>(ref arg1));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public object? Invoke(object? obj, object? arg1, object? arg2)
        {
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(2));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3)
        {
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(3));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);
            argStorage._args.Set(3, arg4);

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(4));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, Span<object?> arguments)
        {
            object? result = _methodBaseInvoker.Invoke(obj, arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }
    }
}

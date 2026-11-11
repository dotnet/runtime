// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.MethodInfos;
using System.Runtime.CompilerServices;

using Internal.Reflection.Core.Execution;

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
            ArgumentNullException.ThrowIfNull(method);

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

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj)
        {
            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, default);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1)
        {
            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, new Span<object?>(ref arg1));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2)
        {
            InlineArray2<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3)
        {
            InlineArray3<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;
            argStorage[2] = arg3;

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
        {
            InlineArray4<object?> argStorage = default;
            argStorage[0] = arg1;
            argStorage[1] = arg2;
            argStorage[2] = arg3;
            argStorage[3] = arg4;

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.MethodInfos;

using Internal.Reflection.Core.Execution;

using static System.Reflection.DynamicInvokeInfo;

namespace System.Reflection
{
    public sealed class MethodInvoker
    {
        private readonly MethodBaseInvoker _methodBaseInvoker;
        private readonly int _parameterCount;

        internal MethodInvoker(RuntimeMethodInfo method)
        {
            _methodBaseInvoker = method.MethodInvoker;
            _parameterCount = method.GetParametersAsSpan().Length;
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

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj)
        {
            if (_parameterCount != 0)
            {
                ThrowForArgCountMismatch();
            }

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, default);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1)
        {
            if (_parameterCount != 1)
            {
                ThrowForArgCountMismatch();
            }

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, new Span<object?>(ref arg1, _parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2)
        {
            if (_parameterCount != 2)
            {
                ThrowForArgCountMismatch();
            }

            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(_parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3)
        {
            if (_parameterCount != 3)
            {
                ThrowForArgCountMismatch();
            }

            StackAllocatedArguments argStorage = default;
            argStorage._args.Set(0, arg1);
            argStorage._args.Set(1, arg2);
            argStorage._args.Set(2, arg3);

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(_parameterCount));
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object? obj, object? arg1, object? arg2, object? arg3, object? arg4)
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

            object? result = _methodBaseInvoker.InvokeDirectWithFewArgs(obj, argStorage._args.AsSpan(_parameterCount));
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

        [DoesNotReturn]
        private static void ThrowForArgCountMismatch()
        {
            throw new TargetParameterCountException(SR.Arg_ParmCnt);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        // See MethodBaseInvoker.CoreCLR.cs for the rationale on storing the intrinsic state inline.
        private IntrinsicInvokeShape _intrinsicShape;
        private IntPtr _intrinsicFn;

        private unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe MethodInvoker(DynamicMethod method) : this(method, method.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            // No _invocationFlags for DynamicMethod.
        }

        private unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args)
        {
            if (_intrinsicShape != IntrinsicInvokeShape.None)
            {
                return IntrinsicInvokeHelper.Dispatch(_intrinsicShape, _intrinsicFn, obj, args, _method.DeclaringType);
            }

            if (IntrinsicInvokeHelper.TryGetShape(_method, out IntrinsicInvokeShape shape, out IntPtr fn))
            {
                _intrinsicFn = fn;
                _intrinsicShape = shape;
                _strategy |= InvokerStrategy.StrategyDetermined_RefArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
                return IntrinsicInvokeHelper.Dispatch(shape, fn, obj, args, _method.DeclaringType);
            }

            InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = CreateInvokeDelegate_RefArgs(_method);
            }

            Type? declaringType = _method.DeclaringType;
            if (declaringType is null || !declaringType.Assembly.IsCollectible)
            {
                _invokeFunc_RefArgs = emitDelegate;
                _strategy |= InvokerStrategy.StrategyDetermined_RefArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
            }

            return emitDelegate(obj, args);
        }
    }
}

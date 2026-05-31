// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        // See MethodBaseInvoker.CoreCLR.cs for the rationale on storing the intrinsic state inline.
        private unsafe delegate*<IntPtr, object?, IntPtr*, Type?, object?> _intrinsicThunk;
        private IntPtr _intrinsicFn;

        internal unsafe ConstructorInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr* args)
        {
            if (_intrinsicThunk is not null)
            {
                return _intrinsicThunk(_intrinsicFn, obj, args, _method.DeclaringType);
            }

            if (IntrinsicInvokeHelper.TryGetShape(_method, out var thunk, out IntPtr fn))
            {
                _intrinsicFn = fn;
                _intrinsicThunk = thunk;
                _strategy |= InvokerStrategy.StrategyDetermined_RefArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
                return thunk(fn, obj, args, _method.DeclaringType);
            }

            InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = CreateInvokeDelegate_RefArgs(_method);
            }

            // Don't cache for collectible assemblies: the DynamicMethod holds token
            // references to types that would prevent the assembly from being unloaded.
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        // Cached intrinsic dispatch state. When `_intrinsicThunk` is non-null, the calli fast path
        // is used and the per-method DynamicMethod emit is skipped. Stored on the invoker (not in a
        // closure) so that `StackCrawlMark.LookForMyCaller` and `Assembly.GetCallingAssembly` see
        // `InterpretedInvoke_Method` as the immediate caller of the target method. That method's
        // class is listed in `SystemDomain::IsReflectionInvocationMethod` and therefore skipped by
        // stack-mark walks, so `Type.GetType(string)` etc. resolve the original user-code caller.
        private unsafe delegate*<IntPtr, object?, IntPtr*, Type?, object?> _intrinsicThunk;
        private IntPtr _intrinsicFn;

        internal unsafe MethodBaseInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(DynamicMethod method, Signature signature) : this(method, signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args)
        {
            // Cached intrinsic thunk from a previous call: dispatch directly.
            if (_intrinsicThunk is not null)
            {
                return _intrinsicThunk(_intrinsicFn, obj, args, _method.DeclaringType);
            }

            // First call: classify. On hit, cache and dispatch with zero further JIT.
            if (IntrinsicInvokeHelper.TryGetShape(_method, out var thunk, out IntPtr fn))
            {
                _intrinsicFn = fn;
                _intrinsicThunk = thunk;
                _strategy |= InvokerStrategy.StrategyDetermined_RefArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
                return thunk(fn, obj, args, _method.DeclaringType);
            }

            // Emit fallback. The QCALL interpreted dispatcher was removed in this branch as part of
            // the UCO migration, so there is no in-process alternative when the intrinsic table misses.
            // ForceAllowDynamicCode avoids an infinite recursion via SR -> custom-attribute -> emit ->
            // PNS -> SR when `IsDynamicCodeSupported` is false.
            InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = CreateInvokeDelegate_RefArgs(_method);
            }

            // Don't cache for collectible assemblies: the DynamicMethod holds token references that
            // would prevent the assembly from being unloaded.
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

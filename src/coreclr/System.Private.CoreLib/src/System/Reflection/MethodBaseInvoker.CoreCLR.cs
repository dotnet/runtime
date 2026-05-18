// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        // Cached intrinsic dispatch state. When _intrinsicShape != None, the calli fast path is used and
        // the per-method DynamicMethod emit is skipped entirely. Stored on the invoker (not in a closure)
        // so that the immediate caller of the target method — as seen by `StackCrawlMark.LookForMyCaller`
        // and `Assembly.GetCallingAssembly` — is `InterpretedInvoke_Method` on this class. That class is
        // listed in `SystemDomain::IsReflectionInvocationMethod` and therefore skipped during stack-mark
        // walks, so `Type.GetType(string)` etc. correctly resolve the original user-code caller.
        private IntrinsicInvokeShape _intrinsicShape;
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
            // Cached intrinsic shape from a previous call: dispatch directly.
            if (_intrinsicShape != IntrinsicInvokeShape.None)
            {
                return IntrinsicInvokeHelper.Dispatch(_intrinsicShape, _intrinsicFn, obj, args, _method.DeclaringType);
            }

            // First call on this invoker: classify the shape. On hit, cache and dispatch with zero JIT.
            if (IntrinsicInvokeHelper.TryGetShape(_method, out IntrinsicInvokeShape shape, out IntPtr fn))
            {
                _intrinsicFn = fn;
                _intrinsicShape = shape;
                _strategy |= InvokerStrategy.StrategyDetermined_RefArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
                return IntrinsicInvokeHelper.Dispatch(shape, fn, obj, args, _method.DeclaringType);
            }

            // Emit fallback. The QCALL interpreted dispatcher was removed in this branch as part of the
            // UCO migration, so there is no in-process alternative when the intrinsic table misses. We
            // force-allow dynamic code to avoid an infinite recursion through the SR resource lookup ->
            // custom-attribute load -> emit -> PNS message -> SR lookup cycle that would otherwise
            // FailFast when `IsDynamicCodeSupported` is false. This is a known policy hole for users
            // who set `ForceInterpretedInvoke=true`; shrinking it requires widening
            // IntrinsicInvokeHelper coverage so emit is never reached for the affected shapes.
            InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = CreateInvokeDelegate_RefArgs(_method);
            }

            // Don't cache for collectible assemblies: the DynamicMethod holds token references that would
            // prevent the assembly from being unloaded.
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

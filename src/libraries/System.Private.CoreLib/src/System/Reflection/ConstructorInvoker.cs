// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class ConstructorInvoker
    {
        private readonly RuntimeConstructorInfo _method;
        public InvocationFlags _invocationFlags;
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc<ConstructorInvoker>? _emitInvoke;

        public ConstructorInvoker(RuntimeConstructorInfo constructorInfo)
        {
            _method = constructorInfo;

#if USE_NATIVE_INVOKE
            // always use the native invoke.
            _strategyDetermined = true;
#elif USE_EMIT_INVOKE
            // always use emit invoke (if it is compatible).
            _invoked = true;
#endif
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, Span<object?> argsForTemporaryMonoSupport, BindingFlags invokeAttr)
        {
            if (!_strategyDetermined)
            {
                if (!_invoked)
                {
                    // The first time, ignoring race conditions, use the slow path.
                    _invoked = true;
                }
                else
                {
                    if (RuntimeFeature.IsDynamicCodeCompiled &&
                        _method.SupportsNewInvoke) // Remove check for SupportsNewInvoke once Mono is updated.
                    {
                        _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate<ConstructorInvoker>(_method);
                    }

                    _strategyDetermined = true;
                }
            }

            // Remove check for SupportsNewInvoke once Mono is updated (Mono's InvokeNonEmitUnsafe has its own exception handling)
            if (_method.SupportsNewInvoke && (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    // For the rarely used and broken scenario of calling the constructor directly through MethodBase.Invoke()
                    // with a non-null 'obj', we use the slow path to avoid having two emit-based delegates.
                    if (_emitInvoke != null && obj == null)
                    {
                        return _emitInvoke(this, obj, args);
                    }

                    return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }

            if (_emitInvoke != null && obj == null)
            {
                return _emitInvoke(this, obj, args);
            }

            return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
        }
    }
}

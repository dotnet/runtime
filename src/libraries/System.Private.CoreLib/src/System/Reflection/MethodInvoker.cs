// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        internal InvocationFlags _invocationFlags;
        private readonly RuntimeMethodInfo _method;
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc<MethodInvoker>? _emitInvoke;

        public MethodInvoker(RuntimeMethodInfo methodInfo)
        {
            _method = methodInfo;

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
                        _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate<MethodInvoker>(_method);
                    }

                    _strategyDetermined = true;
                }
            }

            // Remove check for SupportsNewInvoke once Mono is updated (Mono's InvokeNonEmitUnsafe has its own exception handling)
            if (_method.SupportsNewInvoke && (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    if (_emitInvoke != null)
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

            if (_emitInvoke != null)
            {
                return _emitInvoke(this, obj, args);
            }

            return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
        }
    }
}

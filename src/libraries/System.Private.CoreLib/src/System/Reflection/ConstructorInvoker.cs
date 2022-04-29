// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class ConstructorInvoker
    {
        private readonly RuntimeConstructorInfo _method;

#if !MONO // Temporary until Mono is updated.
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc? _emitInvoke;
#endif

        public ConstructorInvoker(RuntimeConstructorInfo constructorInfo)
        {
            _method = constructorInfo;

#if USE_NATIVE_INVOKE
            // Always use the native invoke; useful for testing.
            _strategyDetermined = true;
#elif USE_EMIT_INVOKE
            // Always use emit invoke (if IsDynamicCodeCompiled == true); useful for testing.
            _invoked = true;
#endif
        }

#if MONO // Temporary until Mono is updated.
        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            return InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }
#else
        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, BindingFlags invokeAttr)
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
                    if (RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate(_method);
                    }
                    _strategyDetermined = true;
                }
            }

            object? ret;
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    // For the rarely used and broken scenario of calling the constructor directly through MethodBase.Invoke()
                    // with a non-null 'obj', we use the slow path to avoid having two emit-based delegates.
                    if (_emitInvoke != null && obj == null)
                    {
                        ret = _emitInvoke(target: null, args);
                    }
                    else
                    {
                        ret = InvokeNonEmitUnsafe(obj, args);
                    }
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (_emitInvoke != null && obj == null)
            {
                ret = _emitInvoke(target: null, args);
            }
            else
            {
                ret = InvokeNonEmitUnsafe(obj, args);
            }

            return ret;
        }
#endif
    }
}

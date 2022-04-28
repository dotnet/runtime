// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        private readonly MethodBase _method;

#if MONO // Temporary until Mono is updated.
        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            return InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }
#else
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc<MethodInvoker>? _emitInvoke;

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
                        _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate<MethodInvoker>(_method);
                    }
                    _strategyDetermined = true;
                }
            }

            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    if (_emitInvoke != null)
                    {
                        return _emitInvoke(this, obj, args);
                    }

                    return InvokeNonEmitUnsafe(obj, args);
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

            return InvokeNonEmitUnsafe(obj, args);
        }
#endif
    }
}

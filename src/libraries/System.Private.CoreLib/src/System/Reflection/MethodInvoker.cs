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

#if true
                    // TEMP HACK FOR FORCING IL ON FIRST TIME:
                    if (RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate<MethodInvoker>(_method);
                    }
                    _strategyDetermined = true;
#endif
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

            if (_method.SupportsNewInvoke)
            {
                if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    try
                    {
                        if (_emitInvoke != null)
                        {
                            return _emitInvoke(this, obj, args);
                        }
                        else
                        {
                            return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new TargetInvocationException(e);
                    }
                }
                else
                {
                    if (_emitInvoke != null)
                    {
                        return _emitInvoke(this, obj, args);
                    }
                    else
                    {
                        return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
                    }
                }
            }
            else
            {
                // Remove this branch once Mono has the same exception handling and managed conversion logic.
                return _method.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
            }
        }
    }
}

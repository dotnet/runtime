// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        internal InvocationFlags _invocationFlags;
        private readonly RuntimeMethodInfo _methodInfo;
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc<MethodInvoker>? _emitInvoke;

        public MethodInvoker(RuntimeMethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, Span<object?> argsForTemporaryMonoSupport, BindingFlags invokeAttr)
        {
            if (!_invoked)
            {
                // Note: disabled for test reasons:
                // The first time, ignoring race conditions, use the slow path.
                // return _methodInfo.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);

                _invoked = true;
            }

            if (!_strategyDetermined)
            {
                if (RuntimeFeature.IsDynamicCodeCompiled &&
                    _methodInfo.DeclaringType != typeof(Type)) // Avoid stack crawl issue with GetType().
                {
                    // For testing slow path, disable Emit for now
                    // _emitInvoke = InvokerEmitUtil.CreateInvokeDelegate<MethodInvoker>(_methodInfo);
                }

                _strategyDetermined = true;
            }

            if (_methodInfo.SupportsNewInvoke)
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
                            return _methodInfo.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
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
                        return _methodInfo.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
                    }
                }
            }
            else
            {
                // Remove this branch once Mono has the same exception handling and managed conversion logic.
                return _methodInfo.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
            }
        }
    }
}

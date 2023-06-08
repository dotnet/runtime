// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        private readonly MethodBase _method;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object? InlinedInvoke(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            if (_invokeFunc != null && (invokeAttr & BindingFlags.DoNotWrapExceptions) != 0)
            {
                return _invokeFunc(obj, args);
            }
            return Invoke(obj, args, invokeAttr);
        }

        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc? _invokeFunc;

        [DebuggerStepThrough]
        [DebuggerHidden]
        private unsafe object? Invoke(object? obj, IntPtr* args, BindingFlags invokeAttr)
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
                    if (RuntimeFeature.IsDynamicCodeSupported)
                    {
                        _invokeFunc = InvokerEmitUtil.CreateInvokeDelegate(_method);
                    }
                    _strategyDetermined = true;
                }
            }

            object? ret;
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    if (_invokeFunc != null)
                    {
                        ret = _invokeFunc(obj, args);
                    }
                    else
                    {
                        ret = InterpretedInvoke(obj, args);
                    }
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (_invokeFunc != null)
            {
                ret = _invokeFunc(obj, args);
            }
            else
            {
                ret = InterpretedInvoke(obj, args);
            }

            return ret;
        }
    }
}

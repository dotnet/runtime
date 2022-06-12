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
        private InvokerEmitUtil.InvokeFunc? _invokeFunc;
#endif

        public ConstructorInvoker(RuntimeConstructorInfo constructorInfo)
        {
            _method = constructorInfo;

#if !MONO // Temporary until Mono is updated.
            if (LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke)
            {
                // Always use the native invoke; useful for testing.
                _strategyDetermined = true;
            }
            else if (LocalAppContextSwitches.ForceEmitInvoke && !LocalAppContextSwitches.ForceInterpretedInvoke)
            {
                // Always use emit invoke (if IsDynamicCodeCompiled == true); useful for testing.
                _invoked = true;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MONO // Temporary until Mono is updated.
        public unsafe object? InlinedInvoke(object? obj, Span<object?> args, BindingFlags invokeAttr) => InterpretedInvoke(obj, args, invokeAttr);
#else
        public unsafe object? InlinedInvoke(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            if (_invokeFunc != null && (invokeAttr & BindingFlags.DoNotWrapExceptions) != 0 && obj == null)
            {
                return _invokeFunc(target: null, args);
            }
            return Invoke(obj, args, invokeAttr);
        }
#endif

#if !MONO // Temporary until Mono is updated.
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
                    if (RuntimeFeature.IsDynamicCodeCompiled)
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
                    // For the rarely used scenario of calling the constructor directly through MethodBase.Invoke()
                    // with a non-null 'obj', we use the slow path to avoid having two emit-based delegates.
                    if (_invokeFunc != null && obj == null)
                    {
                        ret = _invokeFunc(target: null, args);
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
            else if (_invokeFunc != null && obj == null)
            {
                ret = _invokeFunc(target: null, args);
            }
            else
            {
                ret = InterpretedInvoke(obj, args);
            }

            return ret;
        }
#endif
    }
}

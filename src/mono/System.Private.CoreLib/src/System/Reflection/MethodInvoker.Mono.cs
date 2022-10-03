// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class MethodInvoker
    {
        public MethodInvoker(MethodBase method)
        {
            _method = method;

#if USE_NATIVE_INVOKE
            // Always use the native invoke; useful for testing.
            _strategyDetermined = true;
#elif USE_EMIT_INVOKE
            // Always use emit invoke (if IsDynamicCodeCompiled == true); useful for testing.
            _invoked = true;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InterpretedInvoke(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            Exception? exc;
            object? o;

            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out exc);
                }
                catch (Mono.NullByRefReturnException)
                {
                    throw new NullReferenceException();
                }
                catch (OverflowException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                try
                {
                    o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out exc);
                }
                catch (Mono.NullByRefReturnException)
                {
                    throw new NullReferenceException();
                }
            }

            if (exc != null)
                throw exc;

            return o;
        }
    }
}

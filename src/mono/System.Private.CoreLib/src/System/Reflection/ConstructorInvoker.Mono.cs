// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class ConstructorInvoker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InterpretedInvoke(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            Exception exc;
            object? o;

            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    o = _method.InternalInvoke(obj, args, out exc);
                }
                catch (MethodAccessException)
                {
                    throw;
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
                o = _method.InternalInvoke(obj, args, out exc);
            }

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}

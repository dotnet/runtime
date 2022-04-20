// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    internal sealed partial class DynamicMethodInvoker
    {
        private readonly DynamicMethod _dynamicMethod;

        public DynamicMethodInvoker(DynamicMethod dynamicMethod)
        {
            _dynamicMethod = dynamicMethod;
        }

        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            // Always use the slow path; the Emit-based fast path can be added but in general dynamic
            // methods are invoked through a direct delegate, not through Invoke().
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    return _dynamicMethod.InvokeNonEmitUnsafe(obj, args);
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                return _dynamicMethod.InvokeNonEmitUnsafe(obj, args);
            }
        }
    }
}

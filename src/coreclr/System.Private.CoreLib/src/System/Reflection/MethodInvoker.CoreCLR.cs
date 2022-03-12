// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal partial class MethodInvoker
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            // Todo: add strategy for calling IL Emit-based version
            return InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        private unsafe object? InvokeNonEmitUnsafe(object? obj, IntPtr* arguments, BindingFlags invokeAttr)
        {
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                bool rethrow = false;

                try
                {
                    return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _methodInfo.Signature, isConstructor: false, out rethrow);
                }
                catch (Exception e) when (!rethrow)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _methodInfo.Signature, isConstructor: false, out _);
            }
        }
    }
}

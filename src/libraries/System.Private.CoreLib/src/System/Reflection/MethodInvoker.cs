// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        internal InvocationFlags _invocationFlags;
        private readonly RuntimeMethodInfo _methodInfo;

        public MethodInvoker(RuntimeMethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, Span<object?> argsForTemporaryMonoSupport, BindingFlags invokeAttr)
        {
            // Todo: add strategy for calling IL Emit-based version
            return _methodInfo.InvokeNonEmitUnsafe(obj, args, argsForTemporaryMonoSupport, invokeAttr);
        }
    }
}

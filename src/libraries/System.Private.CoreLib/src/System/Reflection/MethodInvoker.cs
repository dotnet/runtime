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

            RuntimeType[] sigTypes = methodInfo.ArgumentTypes;
            for (int i = 0; i < sigTypes.Length; i++)
            {
                if (sigTypes[i].IsByRef)
                {
                    HasRefs = true;
                    break;
                }
            }
        }

        public bool HasRefs { get; }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr** args, BindingFlags invokeAttr)
        {
            // Todo: add strategy for calling IL Emit-based version
            return _methodInfo.InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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
            // Todo: add strategy for calling IL Emit-based version
            return _dynamicMethod.InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }
    }
}

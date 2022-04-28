// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class MethodInvoker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InvokeNonEmitUnsafe(object? obj, IntPtr* arguments)
        {
            Debug.Assert(_signature != null);
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _signature, isConstructor: false);
        }
    }
}

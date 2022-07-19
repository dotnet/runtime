// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class ConstructorInvoker
    {
        public InvocationFlags _invocationFlags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InterpretedInvoke(object? obj, IntPtr* arguments)
        {
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _method.Signature, isConstructor: obj is null)!;
        }
    }
}

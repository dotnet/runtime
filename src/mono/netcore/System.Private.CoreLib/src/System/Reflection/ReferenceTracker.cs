// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed class ReferenceTracker
    {
#pragma warning disable CA1823, 414, 169
        private IntPtr NativeALC;
#pragma warning restore CA1823, 414, 169

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Destroy(IntPtr NativeALC);

        ~ReferenceTracker()
        {
            if (NativeALC != IntPtr.Zero)
                Destroy(NativeALC);
        }
    }
}

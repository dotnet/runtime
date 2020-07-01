// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed class ReferenceTracker
    {
        private IntPtr NativeALC;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Destroy(IntPtr NativeALC);

        private ReferenceTracker(IntPtr native_alc)
        {
            this.NativeALC = native_alc;
        }

        ~ReferenceTracker()
        {
            Destroy(NativeALC);
        }
    }
}

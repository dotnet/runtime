// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Threading
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeOverlapped
    {
        public IntPtr InternalLow;
        public IntPtr InternalHigh;
        public int OffsetLow;
        public int OffsetHigh;
        public IntPtr EventHandle;
    }
}

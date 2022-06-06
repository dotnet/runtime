// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeOverlapped
    {
        public nint InternalLow;
        public nint InternalHigh;
        public int OffsetLow;
        public int OffsetHigh;
        public nint EventHandle;
    }
}

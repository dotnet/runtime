// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

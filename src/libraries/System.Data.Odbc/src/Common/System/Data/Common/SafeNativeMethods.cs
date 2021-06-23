// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Data
{
    internal static partial class SafeNativeMethods
    {
        internal static unsafe void ZeroMemory(IntPtr ptr, int length)
        {
            new Span<byte>((void*)(nint)ptr, length).Clear();
        }
    }
}

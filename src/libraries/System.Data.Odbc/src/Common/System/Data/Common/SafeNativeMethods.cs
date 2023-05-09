// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data
{
    internal static partial class SafeNativeMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ZeroMemory(IntPtr ptr, int length)
        {
#if !NET7_0_OR_GREATER
            new Span<byte>((void*)ptr, length).Clear();
#else
            NativeMemory.Clear((void*)ptr, (uint)length);
#endif
        }
    }
}

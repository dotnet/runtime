// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.JitInterface
{
    internal static class MemoryHelper
    {
        public static void FillStruct<T>(ref T destination, byte value) where T : unmanaged
        {
            Span<T> span = MemoryMarshal.CreateSpan(ref destination, 1);
            MemoryMarshal.AsBytes(span).Fill(value);
        }
    }
}

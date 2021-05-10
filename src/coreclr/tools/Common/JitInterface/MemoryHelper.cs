// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Internal.JitInterface
{
    internal static class MemoryHelper
    {
        public static void FillStruct<T>(ref T destination, byte value) where T : unmanaged
        {
            Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref destination), value, (uint)Unsafe.SizeOf<T>());
        }
    }
}

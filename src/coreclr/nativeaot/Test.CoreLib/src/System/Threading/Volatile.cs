// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public static class Volatile
    {
        private struct VolatileUInt32 { public volatile uint Value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Read(ref uint location) =>
            Unsafe.As<uint, VolatileUInt32>(ref location).Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(ref uint location, uint value)
        {
            Unsafe.As<uint, VolatileUInt32>(ref location).Value = value;
        }

        private struct VolatileUIntPtr { public volatile UIntPtr Value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIntPtr Read(ref UIntPtr location) =>
            Unsafe.As<UIntPtr, VolatileUIntPtr>(ref location).Value;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    //
    // A makeshift native uint type for build configurations that don't build 32/64-bit specific binaries (and hence, can't alias them to UInt32/UInt64 via an ifdef.)
    //
    // Note that .NET Framework x86 JIT inliner is only capable of inlining of limited number of NUInt operations per method. Do not use this type heavily
    // in code that needs to be fast on .NET Framework x86.
    //
    internal unsafe struct NUInt
    {
        private readonly void* _value;

        private NUInt(uint value) => _value = (void*)value;
        private NUInt(ulong value) => _value = (void*)value;

        public static implicit operator NUInt(uint value) => new NUInt(value);
        public static implicit operator IntPtr(NUInt value) => (IntPtr)value._value;

        public static explicit operator NUInt(int value) => new NUInt((uint)value);

        public static explicit operator void* (NUInt value) => value._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator *(NUInt left, NUInt right)
        {
            return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) * (uint)right._value) : new NUInt(((ulong)left._value) * (ulong)right._value);
        }
    }
}

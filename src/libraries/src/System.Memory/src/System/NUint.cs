// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    //
    // A makeshift native uint type for build configurations that don't build 32/64-bit specific binaries (and hence, can't alias them to UInt32/UInt64 via an ifdef.)
    //
    internal struct NUInt
    {
        private readonly UIntPtr _value;

        private NUInt(int value) => _value = (UIntPtr)value;
        private NUInt(uint value) => _value = (UIntPtr)value;
        private NUInt(ulong value) => _value = (UIntPtr)value;

        public static explicit operator NUInt(uint value) => new NUInt(value);
        public static explicit operator UIntPtr(NUInt value) => value._value;

        public static explicit operator NUInt(int value) => new NUInt(value);
        public static explicit operator IntPtr(NUInt value) => (IntPtr)(long)(ulong)value._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator *(NUInt left, int right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) * (uint)right) : new NUInt(((ulong)left._value) * (uint)right); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator +(NUInt left, int right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) + (uint)right) : new NUInt(((ulong)left._value) + (uint)right); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator -(NUInt left, int right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) - (uint)right) : new NUInt(((ulong)left._value) - (uint)right); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator *(NUInt left, NUInt right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) * (uint)right._value) : new NUInt(((ulong)left._value) * (ulong)right._value); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator +(NUInt left, NUInt right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) + (uint)right._value) : new NUInt(((ulong)left._value) + (ulong)right._value); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NUInt operator -(NUInt left, NUInt right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? new NUInt(((uint)left._value) - (uint)right._value) : new NUInt(((ulong)left._value) - (ulong)right._value); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(NUInt left, int right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? ((uint)left._value) >= (uint)right : ((ulong)left._value) >= (uint)right; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(NUInt left, int right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? ((uint)left._value) <= (uint)right : ((ulong)left._value) <= (uint)right; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(NUInt left, int right) => !(left <= right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(NUInt left, int right) => !(left >= right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(NUInt left, NUInt right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? ((uint)left._value) <= (uint)right._value : ((ulong)left._value) <= (ulong)right._value; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(NUInt left, NUInt right)
        {
            unsafe { return (sizeof(IntPtr) == 4) ? ((uint)left._value) >= (uint)right._value : ((ulong)left._value) >= (ulong)right._value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(NUInt left, NUInt right) => !(left <= right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(NUInt left, NUInt right) => !(left >= right);

        public static bool operator ==(NUInt left, NUInt right) => left._value == right._value;
        public static bool operator !=(NUInt left, NUInt right) => left._value != right._value;

        public override bool Equals(object obj) => obj is NUInt other && _value == other._value;
        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();
    }
}

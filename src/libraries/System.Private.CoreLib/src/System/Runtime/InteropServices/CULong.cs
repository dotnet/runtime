// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_WINDOWS
using NativeType = System.UInt32;
#else
using NativeType = System.IntPtr;
#endif

namespace System.Runtime.InteropServices
{
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly struct CULong : IEquatable<CULong>
    {
        private readonly NativeType _value;

        public CULong(uint value)
        {
            _value = value;
        }
        public CULong(nuint value)
        {
            _value = (NativeType)value;
        }

        public nuint Value => _value;

        public override bool Equals(object? o) => o is CULong other && Equals(other);

        public bool Equals(CULong other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();
    }
}

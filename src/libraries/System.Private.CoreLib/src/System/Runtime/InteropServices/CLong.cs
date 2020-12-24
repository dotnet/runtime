// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_WINDOWS
using NativeType = System.Int32;
#else
using NativeType = System.IntPtr;
#endif

namespace System.Runtime.InteropServices
{
    [CLSCompliant(false)]
    public readonly struct CLong : IEquatable<CLong>
    {
        private readonly NativeType _value;

        public CLong(int value)
        {
            _value = value;
        }
        public CLong(nint value)
        {
            _value = (NativeType)value;
        }

        public nint Value => _value;

        public override bool Equals(object? o) => o is CLong other && Equals(other);

        public bool Equals(CLong other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();
    }
}

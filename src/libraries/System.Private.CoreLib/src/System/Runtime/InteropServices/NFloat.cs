// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_32BIT
using NativeType = System.Single;
#else
using NativeType = System.Double;
#endif

namespace System.Runtime.InteropServices
{
    [Intrinsic]
    public readonly struct NFloat : IEquatable<NFloat>
    {
        private readonly NativeType _value;

        public NFloat(float value)
        {
            _value = value;
        }

        public NFloat(double value)
        {
#if TARGET_32BIT
            if (value > NativeType.MaxValue)
            {
                throw new OverflowException();
            }
#endif
            _value = (NativeType)value;
        }

        public double Value => _value;

        public override bool Equals(object? o) => o is NFloat other && Equals(other);

        public bool Equals(NFloat other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();
    }
}

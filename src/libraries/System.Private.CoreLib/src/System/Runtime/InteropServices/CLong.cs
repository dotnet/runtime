// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_WINDOWS
using NativeType = System.Int32;
#else
using NativeType = System.IntPtr;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// <see cref="CLong"/> is an immutable value type that represents the <c>long</c> type in C and C++.
    /// It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent
    /// in managed code unmanaged APIs that use the <c>long</c> type.
    /// This type has 32-bits of storage on all Windows platforms and 32-bit Unix-based platforms.
    /// It has 64-bits of storage on 64-bit Unix platforms.
    /// </summary>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly struct CLong : IEquatable<CLong>
    {
        private readonly NativeType _value;

        /// <summary>
        /// Constructs an instance from a 32-bit integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        public CLong(int value)
        {
            _value = (NativeType)value;
        }

        /// <summary>
        /// Constructs an instance from a native sized integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying storage type.</exception>
        public CLong(nint value)
        {
            _value = checked((NativeType)value);
        }

        /// <summary>
        /// The underlying integer value of this instance.
        /// </summary>
        public nint Value => _value;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="o">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="o"/> is an instance of <see cref="CLong"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? o) => o is CLong other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="CLong"/> value.
        /// </summary>
        /// <param name="other">A <see cref="CLong"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(CLong other) => _value == other._value;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance, consisting of a negative sign if the value is negative, and a sequence of digits ranging from 0 to 9 with no leading zeroes.</returns>
        public override string ToString() => _value.ToString();
    }
}

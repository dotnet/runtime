// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_32BIT
using NativeType = System.Single;
#else
using NativeType = System.Double;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// <see cref="NFloat"/> is an immutable value type that represents a floating type that has the same size
    /// as the native integer size.
    /// It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent
    /// in managed code unmanaged APIs that use a type alias for C or C++'s <c>float</c> on 32-bit platforms
    /// or <c>double</c> on 64-bit platforms, such as the CGFloat type in libraries provided by Apple.
    /// </summary>
    [Intrinsic]
    public readonly struct NFloat : IEquatable<NFloat>
    {
        private readonly NativeType _value;

        /// <summary>
        /// Constructs an instance from a 32-bit floating point value.
        /// </summary>
        /// <param name="value">The floating-point vaule.</param>
        public NFloat(float value)
        {
            _value = value;
        }

        /// <summary>
        /// Constructs an instance from a 64-bit floating point value.
        /// </summary>
        /// <param name="value">The floating-point vaule.</param>
        public NFloat(double value)
        {
            _value = (NativeType)value;
        }

        /// <summary>
        /// The underlying floating-point value of this instance.
        /// </summary>
        public double Value => _value;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="o">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="o"/> is an instance of <see cref="NFloat"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? o) => o is NFloat other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="CLong"/> value.
        /// </summary>
        /// <param name="other">An <see cref="NFloat"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(NFloat other) => _value.Equals(other._value);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString() => _value.ToString();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Colors
{
    /// <summary>
    /// Represents a color in the RGBA (Red, Green, Blue, Alpha) color space.
    /// </summary>
    /// <typeparam name="T">The type of the color components.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("RGBA({R}, {G}, {B}, {A})")]
    public readonly struct Rgba<T> : IEquatable<Rgba<T>>//, IFormattable, ISpanFormattable
        where T : struct
    {
        // big-endian order

        /// <summary>
        /// The red component of the color.
        /// </summary>
        public T R { get; init; }

        /// <summary>
        /// The green component of the color.
        /// </summary>
        public T G { get; init; }

        /// <summary>
        /// The blue component of the color.
        /// </summary>
        public T B { get; init; }

        /// <summary>
        /// The alpha component of the color.
        /// </summary>
        public T A { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rgba{T}"/> color struct with the specified red, green, blue
        /// and alpha components.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="a">The alpha component.</param>
        public Rgba(T r, T g, T b, T a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rgba{T}"/> color struct with the color components from
        /// <paramref name="values"/> in the order red, green, blue, alpha.
        /// </summary>
        /// <param name="values">
        /// The <see cref="ReadOnlySpan{T}"/> containing the color components in the order red, green, blue, alpha.
        /// </param>
        public Rgba(ReadOnlySpan<T> values)
        {
            ThrowHelper.ThrowIfSpanDoesntHaveFourElementsForColor(values);

            this = Unsafe.As<T, Rgba<T>>(ref MemoryMarshal.GetReference(values));
        }

        /// <summary>
        /// Copies this <see cref="Rgba{T}"/> color components to the <paramref name="destination"/> in the order
        /// red, green, blue, alpha.
        /// </summary>
        /// <param name="destination">
        /// The <see cref="Span{T}"/> to which the color components will be copied in the order red, green, blue, alpha.
        /// </param>
        public void CopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowIfSpanDoesntHaveFourElementsForColor((ReadOnlySpan<T>)destination);

            Unsafe.As<T, Rgba<T>>(ref MemoryMarshal.GetReference(destination)) = this;
        }

        /// <summary>
        /// Determines whether this <see cref="Rgba{T}"/> color is equal to the <paramref name="other"/> color.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="Rgba{T}"/> color to compare with this <see cref="Rgba{T}"/> color.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this <see cref="Rgba{T}"/> color is equal to the <paramref name="other"/>
        /// <see cref="Rgba{T}"/> color; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(Rgba<T> other)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(R, other.R) &&
                   comparer.Equals(G, other.G) &&
                   comparer.Equals(B, other.B) &&
                   comparer.Equals(A, other.A);
        }

        /// <summary>
        /// Determines whether this <see cref="Rgba{T}"/> color is equal to the specified <paramref name="obj"/>
        /// object.
        /// </summary>
        /// <param name="obj">The object to compare with this <see cref="Rgba{T}"/> color.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="obj"/> object is equal to this
        /// <see cref="Rgba{T}"/> color; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is Rgba<T> other && Equals(other);

        /// <summary>
        /// Default hash function for this <see cref="Rgba{T}"/> color.
        /// </summary>
        /// <returns>A hash code for this <see cref="Rgba{T}"/> color.</returns>
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        /// <summary>
        /// Returns a string representation of this <see cref="Argb{T}"/> color.
        /// </summary>
        /// <returns>
        /// A string in the format "[RGBA Color: R, G, B, A]", where R, G, B, and A are the red, green, blue, and alpha
        /// component values of the color, respectively.
        /// </returns>
        public override string ToString() => $"[RGBA Color: {R}, {G}, {B}, {A}]";

        /// <summary>
        /// Converts this <see cref="Rgba{T}"/> color to an <see cref="Argb{T}"/> color.
        /// </summary>
        /// <returns>
        /// An new instance of the <see cref="Argb{T}"/> color that represents this <see cref="Rgba{T}"/> color.
        /// </returns>
        public Argb<T> ToArgb() => new Argb<T>(A, R, G, B);
    }
}

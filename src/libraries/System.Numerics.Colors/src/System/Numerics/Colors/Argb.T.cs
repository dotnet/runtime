// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Colors
{
    /// <summary>
    /// Represents a color in the ARGB (Alpha, Red, Green, Blue) color space.
    /// </summary>
    /// <typeparam name="T">The type of the color components.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("ARGB({A}, {R}, {G}, {B})")]
    public readonly struct Argb<T> : IEquatable<Argb<T>>//, IFormattable, ISpanFormattable
        where T : struct
    {
        // big-endian order

        /// <summary>
        /// The alpha component of the color.
        /// </summary>
        public T A { get; init; }

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
        /// Initializes a new instance of the <see cref="Argb{T}"/> color struct with the specified alpha, red, green,
        /// and blue components.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        public Argb(T a, T r, T g, T b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Argb{T}"/> color struct with the color components from
        /// <paramref name="values"/> in the order alpha, red, green, blue.
        /// </summary>
        /// <param name="values">
        /// The <see cref="ReadOnlySpan{T}"/> containing the color components in the order alpha, red, green, blue.
        /// </param>
        public Argb(ReadOnlySpan<T> values)
        {
            ThrowHelper.ThrowIfSpanDoesntHaveFourElementsForColor(values);

            this = Unsafe.As<T, Argb<T>>(ref MemoryMarshal.GetReference(values));
        }

        /// <summary>
        /// Copies this <see cref="Argb{T}"/> color components to the <paramref name="destination"/> in the order
        /// alpha, red, green, blue.
        /// </summary>
        /// <param name="destination">
        /// The <see cref="Span{T}"/> to which the color components will be copied in the order alpha, red, green, blue.
        /// </param>
        public void CopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowIfSpanDoesntHaveFourElementsForColor((ReadOnlySpan<T>)destination);

            Unsafe.As<T, Argb<T>>(ref MemoryMarshal.GetReference(destination)) = this;
        }

        /// <summary>
        /// Determines whether this <see cref="Argb{T}"/> color is equal to the <paramref name="other"/> color.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="Argb{T}"/> color to compare with this <see cref="Argb{T}"/> color.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this <see cref="Argb{T}"/> color is equal to the <paramref name="other"/>
        /// <see cref="Argb{T}"/> color; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(Argb<T> other)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(A, other.A) &&
                   comparer.Equals(R, other.R) &&
                   comparer.Equals(G, other.G) &&
                   comparer.Equals(B, other.B);
        }

        /// <summary>
        /// Determines whether this <see cref="Argb{T}"/> color is equal to the specified <paramref name="obj"/>
        /// object.
        /// </summary>
        /// <param name="obj">The object to compare with this <see cref="Argb{T}"/> color.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="obj"/> object is equal to this
        /// <see cref="Argb{T}"/> color; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is Argb<T> otherColor && Equals(otherColor);

        /// <summary>
        /// Default hash function for this <see cref="Argb{T}"/> color.
        /// </summary>
        /// <returns>A hash code for this <see cref="Argb{T}"/> color.</returns>
        public override int GetHashCode() => HashCode.Combine(A, R, G, B);

        /// <summary>
        /// Returns a string representation of this <see cref="Argb{T}"/> color.
        /// </summary>
        /// <returns>
        /// A string in the format "[ARGB Color: A, R, G, B]", where A, R, G, and B are the alpha, red, green, and blue
        /// component values of the color, respectively.
        /// </returns>
        public override string ToString() => $"[ARGB Color: {A}, {R}, {G}, {B}]";

        /// <summary>
        /// Converts this <see cref="Argb{T}"/> color to an <see cref="Rgba{T}"/> color.
        /// </summary>
        /// <returns>
        /// An new instance of the <see cref="Rgba{T}"/> color that represents this <see cref="Argb{T}"/> color.
        /// </returns>
        public Rgba<T> ToRgba() => new Rgba<T>(R, G, B, A);
    }
}

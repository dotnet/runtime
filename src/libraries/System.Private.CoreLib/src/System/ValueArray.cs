// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Internal.Runtime.CompilerServices;

namespace System
{
    public struct ValueArray<T, R> // where R : System.Array
        : IEquatable<ValueArray<T, R>>
    {
        public int Length => RankOf<R>.Value;

        // For the array of Length N, runtime will add N-1 elements immediately after this one.
        private T Element0;

        /// <summary>
        /// Returns a reference to the first element of the value array.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T GetPinnableReference() => ref new ByReference<T>(ref Element0).Value;

        /// <summary>
        /// Returns a reference to specified element of the value array.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return ref Unsafe.Add(ref GetPinnableReference(), (nint)(uint)index /* force zero-extension */);
            }
        }

        /// <summary>
        /// Forms a slice out of the given value array, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        public Span<T> Slice(int start)
        {
            if ((uint)start > (uint)Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref Element0, (nint)(uint)start /* force zero-extension */), Length - start);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ValueArray<T, R> other && Equals(other);
        }

        public bool Equals(ValueArray<T, R> other)
        {
            for (int i = 0; i < Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(this[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = default;
            for (int i = 0; i < Length; i++)
            {
                hashCode.Add(this[i]);
            }

            return hashCode.ToHashCode();
        }
    }

    // internal helper to compute and cache the Rank of an object array.
    internal static class RankOf<R>
    {
        public static readonly int Value = GetRank();

        private static int GetRank()
        {
            var type = typeof(R);
            if (!type.IsArray)
                throw new ArgumentException("R must be an array");

            if (type.GetElementType() != typeof(object))
                throw new ArgumentException("R must be an object array");

            return type.GetArrayRank();
        }
    }
}

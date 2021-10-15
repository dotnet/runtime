// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public struct ValueArray<T, R> // where R : System.Array
    {
        public static int Length => RankOf<R>.Value;

        // For the array of Length N, runtime will add N-1 elements immediately after this one.
        private T Element0;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            get => Address(index);
            set => Address(index) = value;
        }

        /// <summary>
        /// Gets an element address at the specified index.
        /// </summary>
        public ref T Address(int index)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return ref Unsafe.Add(ref new ByReference<T>(ref Element0).Value, (nint)(uint)index /* force zero-extension */);
        }

        /// <summary>
        /// Returns a slice of the ValueArray.
        /// </summary>
        public Span<T> Slice(int length)
        {
            if ((uint)length >= (uint)Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return new Span<T>(ref Element0, length);
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

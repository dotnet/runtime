// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public struct ValueArray<T, R> // where R : System.Array
    {
        public static readonly int Length = typeof(R).GetArrayRank();

        // For the array of Length N, we will have N+1 elements immdiately follow.
        public T Element0;

        /// <summary>
        /// Indexer.
        /// </summary>
        public T this[int index]
        {
            get => Address(index);
            set => Address(index) = value;
        }

        /// <summary>
        /// Returns an address to an element of the ValueArray.
        /// Caller must statically know the upperBound and pass it in.
        /// </summary>
        public ref T Address(int index)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return ref Unsafe.Add(ref new ByReference<T>(ref Element0).Value, (nint)(uint)index /* force zero-extension */);
        }

        /// <summary>
        /// Returns a slice of the ValueArray.
        /// Caller must statically know the upperBound and pass it in.
        /// </summary>
        public Span<T> Slice(int length)
        {
            if ((uint)length >= (uint)Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return new Span<T>(ref Element0, length);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    //    [Obsolete("Never use this type directly as that would be an array of unknown length.")]
    //    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ValueArray<T>
    {
        // For the array of Length N, we will have N+1 elements immdiately follow.
        public T Element0;

        /// <summary>
        /// Indexer.
        /// Caller must statically know the upperBound and pass it in.
        /// </summary>
        public T this[int index, int upperBound = 42]
        {
            get => Address(index, upperBound);
            set => Address(index, upperBound) = value;
        }

        /// <summary>
        /// Returns an address to an element of the ValueArray.
        /// Caller must statically know the upperBound and pass it in.
        /// </summary>
        public ref T Address(int index, int upperBound = 42)
        {
            if ((uint)index >= (uint)upperBound)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return ref Unsafe.Add(ref new ByReference<T>(ref Element0).Value, (nint)(uint)index /* force zero-extension */);
        }

        /// <summary>
        /// Returns a slice of the ValueArray.
        /// Caller must statically know the upperBound and pass it in.
        /// </summary>
        public Span<T> Slice(int length, int upperBound = 42)
        {
            if ((uint)length >= (uint)upperBound)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return new Span<T>(ref Element0, length);
        }
    }
}

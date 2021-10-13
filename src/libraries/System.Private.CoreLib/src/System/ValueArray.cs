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
        /// NOTE: I am not sure we need the indexer in the final impl.
        ///       We may not want to JIT it for every length.
        ///       It may be better to just let C# compiler code-gen the accesses.
        /// </summary>
        public T this[int index]
        {
            get => this.Address(index);
            set => this.Address(index) = value;
        }
    }

    public static class ValueArrayHelpers
    {
        /// <summary>
        /// Returns an address to an element of the ValueArray.
        /// Caller must statically know the Length and ensure the "index" is within bounds.
        /// </summary>
        public static ref T Address<T>(this ref ValueArray<T> array, int index)
        {
            return ref Unsafe.Add(ref array.Element0, (nint)(uint)index /* force zero-extension */);
        }

        /// <summary>
        /// Returns a slice of the ValueArray.
        /// Caller must statically know the Length and ensure the "length" is within bounds.
        /// </summary>
        public static Span<T> Slice<T>(this ref ValueArray<T> array, int length)
        {
            return new Span<T>(ref array.Element0, length);
        }
    }
}

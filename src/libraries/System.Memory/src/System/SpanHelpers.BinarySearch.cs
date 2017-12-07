// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System
{
    internal static partial class SpanHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BinarySearch<T, TComparable>(
            this ReadOnlySpan<T> span, TComparable comparable)
            where TComparable : IComparable<T>
        {
            if (span.IsEmpty)
            {   return -1; }

            // TODO: Make `ref readonly`/`in` when language permits
            return BinarySearch(ref span.DangerousGetPinnableReference(), span.Length, comparable);
        }

        // TODO: Make s `ref readonly`/`in` when language permits
        // TODO: Make comparable `ref readonly`/`in` to allow pass by ref without forcing ref
        internal static int BinarySearch<T, TComparable>(
            ref T s, int length, TComparable comparable) 
            where TComparable : IComparable<T>
        {
            // Array.BinarySearch implementation:
            // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Array.cs#L802
            int lo = 0;
            int hi = length - 1;
            while (lo <= hi)
            {
                // lo or hi will never be negative inside the loop
                // TODO: Test/investigate if below is faster (gives better asm), perhaps via Unsafe.As to avoid unnecessary conversions
                //       This is safe since we know span.Length < int.MaxValue, and indeces are >= 0
                // int i = (int)(((uint)hi + (uint)lo) >> 1)
                int i = lo + ((hi - lo) >> 1);

                int c = 0;
                // TODO: Is the try/catch needed, can this be removed?
                try
                {
                    //c = comparable.Compare(Unsafe.Add(ref s, i), value);
                    // Note this is reversed, in that `value` is before, not after as above
                    // TODO: We probably need to add `ref readonly`/`in` overloads or `AddReadOnly`to unsafe, 
                    //       if this will be available in language
                    // TODO: Revise all Unsafe APIs for `readonly` applicability...
                    c = comparable.CompareTo(Unsafe.Add(ref s, i));
                }
                catch (Exception e)
                {
                    // TODO: Is this correct? Don't like the try/catch, why should we have this??
                    ThrowHelper.ThrowInvalidOperationException_IComparableFailed(e);
                }
                if (c == 0)
                {
                    return i;
                }
                else if (c > 0) // if (c < 0) old
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return ~lo;
        }

        // Helper to allow sharing all code via IComparable<T> inlineable
        internal struct ComparerComparable<T, TComparer> : IComparable<T>
            where TComparer : IComparer<T>
        {
            readonly T _value;
            readonly TComparer _comparer;

            public ComparerComparable(T value, TComparer comparer)
            {
                _value = value;
                _comparer = comparer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CompareTo(T other) => _comparer.Compare(_value, other);
        }
    }
}

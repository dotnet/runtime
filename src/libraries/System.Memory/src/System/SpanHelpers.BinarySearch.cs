// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class SpanHelpers
    {
        public static int BinarySearch<T, TComparable>(
            this ReadOnlySpan<T> span, TComparable comparable)
            where TComparable : IComparable<T>
        {
            throw new NotImplementedException();
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

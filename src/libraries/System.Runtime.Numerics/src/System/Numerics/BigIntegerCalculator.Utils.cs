// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        // Mutable for unit testing...
        private static int AllocationThreshold = 256;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<uint> ZeroMem(Span<uint> memory)
        {
            memory.Clear();
            return memory;
        }

        public static int Compare(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
        {
            if (left.Length < right.Length)
                return -1;
            if (left.Length > right.Length)
                return 1;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                uint leftElement = left[i];
                uint rightElement = right[i];
                if (leftElement < rightElement)
                    return -1;
                if (leftElement > rightElement)
                    return 1;
            }

            return 0;
        }
    }
}
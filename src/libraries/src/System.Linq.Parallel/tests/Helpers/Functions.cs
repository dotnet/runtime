// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    internal static class Functions
    {
        // Sum a range of integers
        public static int SumRange(int start, int count)
        {
            return unchecked((count / 2) * (2 * start + count - 1) + (count % 2 != 0 ? start + count / 2 : 0));
        }

        public static long SumRange(long start, long count)
        {
            return unchecked((count / 2) * (2 * start + count - 1) + (count % 2 != 0 ? start + count / 2 : 0));
        }

        public static long ProductRange(long start, long count)
        {
            long product = 1;
            for (int i = 0; i < count; i++, start++)
            {
                product = unchecked(product * start);
            }
            return product;
        }

        public static void Enumerate<T>(this IEnumerable<T> e)
        {
            foreach (var x in e) { }
        }
    }
}

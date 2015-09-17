// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    internal static class Functions
    {
        // Sum a range of integers
        public static int SumRange(int start, int count)
        {
            return count * (2 * start + (count - 1)) / 2;
        }

        public static long SumRange(long start, long count)
        {
            return count * (2 * start + (count - 1)) / 2;
        }

        public static long ProductRange(long start, long count)
        {
            long product = 1;
            for (int i = 0; i < count; i++, start++)
            {
                product *= start;
            }
            return product;
        }

        public static void AssertThrowsWrapped<T>(Action query)
        {
            AggregateException ae = Assert.Throws<AggregateException>(query);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<T>(e));
        }

        public static void AssertIsCanceled(CancellationTokenSource source, Action query)
        {
            OperationCanceledException oce = Assert.Throws<OperationCanceledException>(query);
            Assert.Equal(source.Token, oce.CancellationToken);
        }

        public static void Enumerate<T>(this IEnumerable<T> e)
        {
            foreach (var x in e) { }
        }
    }
}

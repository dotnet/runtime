// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector256Tests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
        [InlineData(-1, -1, -1, -1, -1, -1, -1, -1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8)]
        [InlineData(0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue)]
        public void Vector256Int32IndexerTest(params int[] values)
        {
            var vector = Vector256.Create(values);

            Assert.All(
                values.Select((value, index) => (index, value)),
                tuple => Assert.Equal(tuple.value, vector[tuple.index])
            );
        }

        [Theory]
        [InlineData(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)]
        [InlineData(1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(0L, 0L, 50L, 430L, -64L, 0L, long.MaxValue, long.MinValue)]
        public void Vector256Int64IndexerTest(params long[] values)
        {
            var vector = Vector256.Create(values);

            Assert.All(
                values.Select((value, index) => (index, value)),
                tuple => Assert.Equal(tuple.value, vector[tuple.index])
            );
        }
    }
}

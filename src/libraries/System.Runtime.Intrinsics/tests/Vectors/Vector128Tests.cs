// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector128Tests
    {
        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8)]
        [InlineData(50, 430, int.MaxValue, int.MinValue)]
        public void Vector128Int32IndexerTest(params int[] values)
        {
            var vector = Vector128.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
        }

        [Theory]
        [InlineData(0L, 0L)]
        [InlineData(1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(50L, 430L, long.MaxValue, long.MinValue)]
        public void Vector128Int64IndexerTest(params long[] values)
        {
            var vector = Vector128.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
        }
    }
}

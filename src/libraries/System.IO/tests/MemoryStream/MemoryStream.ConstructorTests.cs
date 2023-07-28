// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class MemoryStream_ConstructorTests
    {
        [Theory]
        [InlineData(10, -1, int.MaxValue)]
        [InlineData(10, 6, -1)]
        public static void MemoryStream_Ctor_NegativeIndices(int arraySize, int index, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryStream(new byte[arraySize], index, count));
        }

        [Theory]
        [InlineData(1, 2, 1)]
        [InlineData(7, 8, 2)]
        public static void MemoryStream_Ctor_OutOfRangeIndices(int arraySize, int index, int count)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new MemoryStream(new byte[arraySize], index, count));
        }

        [Fact]
        public static void MemoryStream_Ctor_NullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new MemoryStream(null, 5, 2));
        }

        [Fact]
        public static void MemoryStream_Ctor_InvalidCapacities()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryStream(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryStream(-1));
            if (PlatformDetection.IsNotIntMaxValueArrayIndexSupported)
            {
                Assert.Throws<OutOfMemoryException>(() => new MemoryStream(int.MaxValue));
            }
        }
    }
}

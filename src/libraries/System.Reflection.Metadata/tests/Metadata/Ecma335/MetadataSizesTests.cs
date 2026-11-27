// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MetadataSizesTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(int.MaxValue)]
        public void GetAlignedHeapSize_InvalidIndex(int index)
        {
            MetadataSizes sizes = new MetadataRootBuilder(new MetadataBuilder()).Sizes;

            Assert.Throws<ArgumentOutOfRangeException>("index", () => sizes.GetAlignedHeapSize((HeapIndex)index));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class VBFixedArrayAttributeTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        public void Ctor_Int(int upperBound, int expectedLength)
        {
            var attribute = new VBFixedArrayAttribute(upperBound);
            Assert.Equal(new int[] { upperBound }, attribute.Bounds);
            Assert.Equal(expectedLength, attribute.Length);
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(0, 1, 2)]
        [InlineData(1, 2, 6)]
        public void Ctor_Int_Int(int upperBound1, int upperBound2, int expectedLength)
        {
            var attribute = new VBFixedArrayAttribute(upperBound1, upperBound2);
            Assert.Equal(new int[] { upperBound1, upperBound2 }, attribute.Bounds);
            Assert.Equal(expectedLength, attribute.Length);
        }

        [Fact]
        public void Ctor_NegativeUpperBound1_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new VBFixedArrayAttribute(-1));
            AssertExtensions.Throws<ArgumentException>(null, () => new VBFixedArrayAttribute(-1, 0));
        }

        [Fact]
        public void Ctor_NegativeUpperBound2_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new VBFixedArrayAttribute(0, -1));
        }
    }
}

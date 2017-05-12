// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class UnicodeRangeTests
    {
        [Theory]
        [InlineData(-1, 16)]
        [InlineData(0x10000, 16)]
        public void Ctor_FailureCase_FirstCodePoint(int firstCodePoint, int rangeSize)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("firstCodePoint", () => new UnicodeRange(firstCodePoint, rangeSize));
        }

        [Theory]
        [InlineData(0x0100, -1)]
        [InlineData(0x0100, 0x10000)]
        public void Ctor_FailureCase_RangeSize(int firstCodePoint, int rangeSize)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new UnicodeRange(firstCodePoint, rangeSize));
        }

        [Fact]
        public void Ctor_SuccessCase()
        {
            // Act
            var range = new UnicodeRange(0x0100, 128); // Latin Extended-A

            // Assert
            Assert.Equal(0x0100, range.FirstCodePoint);
            Assert.Equal(128, range.Length);
        }

        [Fact]
        public void FromSpan_FailureCase()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("lastCharacter", () => UnicodeRange.Create('\u0020', '\u0010'));
        }

        [Fact]
        public void FromSpan_SuccessCase()
        {
            // Act
            var range = UnicodeRange.Create('\u0180', '\u024F'); // Latin Extended-B

            // Assert
            Assert.Equal(0x0180, range.FirstCodePoint);
            Assert.Equal(208, range.Length);
        }

        [Fact]
        public void FromSpan_SuccessCase_All()
        {
            // Act
            var range = UnicodeRange.Create('\u0000', '\uFFFF');

            // Assert
            Assert.Equal(0, range.FirstCodePoint);
            Assert.Equal(0x10000, range.Length);
        }
    }
}

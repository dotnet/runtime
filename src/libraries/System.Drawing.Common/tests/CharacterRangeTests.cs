// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using Xunit;

namespace System.Drawing.Tests
{
    public class CharacterRangeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var range = new CharacterRange();
            Assert.Equal(0, range.First);
            Assert.Equal(0, range.Length);
        }

        [Theory]
        [InlineData(-1, -1)]
        [InlineData(1, 1)]
        public void Ctor_First_Length(int first, int length)
        {
            var range = new CharacterRange(first, length);
            Assert.Equal(first, range.First);
            Assert.Equal(length, range.Length);
        }

        [Fact]
        public void First_Set_GetReturnsExpected()
        {
            var range = new CharacterRange { First = 10 };
            Assert.Equal(10, range.First);
        }

        [Fact]
        public void Length_Set_GetReturnsExpected()
        {
            var range = new CharacterRange { Length = 10 };
            Assert.Equal(10, range.Length);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(1, 2), true };
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(2, 2), false };
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(1, 1), false };
            yield return new object[] { new CharacterRange(1, 2), new object(), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Valid_ReturnsExpected(CharacterRange range, object other, bool expected)
        {
            Assert.Equal(expected, range.Equals(other));
            if (other is CharacterRange otherRange)
            {
                Assert.Equal(expected, range == otherRange);
                Assert.Equal(!expected, range != otherRange);
                Assert.Equal(expected, range.GetHashCode().Equals(otherRange.GetHashCode()));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void Equals_NullOther_ThrowsNullReferenceException()
        {
            var range = new CharacterRange();
            Assert.Throws<NullReferenceException>(() => range.Equals(null));
        }
    }
}

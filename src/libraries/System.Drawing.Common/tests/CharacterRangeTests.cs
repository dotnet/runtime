// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void Ctor_Int_Int(int First, int Length)
        {
            var range = new CharacterRange(First, Length);
            Assert.Equal(First, range.First);
            Assert.Equal(Length, range.Length);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        public void First_Set_GetReturnsExpected(int value)
        {
            var range = new CharacterRange
            {
                First = value
            };
            Assert.Equal(value, range.First);

            // Set same.
            range.First = value;
            Assert.Equal(value, range.First);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10)]
        public void Length_Set_GetReturnsExpected(int value)
        {
            var range = new CharacterRange
            {
                Length = value
            };
            Assert.Equal(value, range.Length);

            // Set same.
            range.Length = value;
            Assert.Equal(value, range.Length);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(1, 2), true };
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(2, 2), false };
            yield return new object[] { new CharacterRange(1, 2), new CharacterRange(1, 1), false };
            yield return new object[] { new CharacterRange(1, 2), new object(), false };

            // .NET Framework throws NullReferenceException.
            if (!PlatformDetection.IsNetFramework)
            {
                yield return new object[] { new CharacterRange(1, 2), null, false };
            }
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(CharacterRange range, object obj, bool expected)
        {
            Assert.Equal(expected, range.Equals(obj));
            if (obj is CharacterRange otherRange)
            {
                Assert.Equal(expected, range == otherRange);
                Assert.Equal(!expected, range != otherRange);
                Assert.Equal(expected, range.GetHashCode().Equals(otherRange.GetHashCode()));
            }
        }
    }
}

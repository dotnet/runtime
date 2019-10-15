// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace Windows.UI.Xaml.Controls.Primitives.Tests
{
    public class GeneratorPositionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var position = new GeneratorPosition();
            Assert.Equal(0, position.Index);
            Assert.Equal(0, position.Offset);
        }

        [Theory]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void Ctor_Index_Offset(int index, int offset)
        {
            var position = new GeneratorPosition(index, offset);
            Assert.Equal(index, position.Index);
            Assert.Equal(offset, position.Offset);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Index_Set_GetReturnsExpected(int value)
        {
            var thickness = new GeneratorPosition { Index = value };
            Assert.Equal(value, thickness.Index);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Offset_Set_GetReturnsExpected(int value)
        {
            var thickness = new GeneratorPosition { Offset = value };
            Assert.Equal(value, thickness.Offset);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new GeneratorPosition(1, 2), new GeneratorPosition(1, 2), true };
            yield return new object[] { new GeneratorPosition(1, 2), new GeneratorPosition(2, 2), false };
            yield return new object[] { new GeneratorPosition(1, 2), new GeneratorPosition(1, 3), false };

            yield return new object[] { new GeneratorPosition(1, 2), new object(), false };
            yield return new object[] { new GeneratorPosition(1, 2), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(GeneratorPosition position, object other, bool expected)
        {
            Assert.Equal(expected, position.Equals(other));
            if (other is GeneratorPosition otherPosition)
            {
                Assert.Equal(expected, position.Equals(otherPosition));
                Assert.Equal(expected, position == otherPosition);
                Assert.Equal(!expected, position != otherPosition);
                Assert.Equal(expected, position.GetHashCode().Equals(otherPosition.GetHashCode()));
            }
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var position = new GeneratorPosition(1, 2);
            Assert.Equal("GeneratorPosition (1,2)", position.ToString());
        }
    }
}
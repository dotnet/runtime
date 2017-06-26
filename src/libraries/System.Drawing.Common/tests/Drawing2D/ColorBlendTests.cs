// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class ColorBlendTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var blend = new ColorBlend();
            Assert.Equal(new Color[1], blend.Colors);
            Assert.Equal(new float[1], blend.Positions);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void Ctor_Count(int count)
        {
            var blend = new ColorBlend(count);
            Assert.Equal(new Color[count], blend.Colors);
            Assert.Equal(new float[count], blend.Positions);
        }

        [Fact]
        public void Ctor_InvalidCount_ThrowsOverflowException()
        {
            Assert.Throws<OverflowException>(() => new ColorBlend(-1));
        }
        
        [Fact]
        public void Ctor_LargeCount_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => new ColorBlend(int.MaxValue));
        }

        [Fact]
        public void Colors_Set_GetReturnsExpected()
        {
            var blend = new ColorBlend { Colors = null };
            Assert.Null(blend.Colors);

            blend.Colors = new Color[10];
            Assert.Equal(new Color[10], blend.Colors);
        }

        [Fact]
        public void Positions_Set_GetReturnsExpected()
        {
            var blend = new ColorBlend { Positions = null };
            Assert.Null(blend.Positions);

            blend.Positions = new float[10];
            Assert.Equal(new float[10], blend.Positions);
        }
    }
}

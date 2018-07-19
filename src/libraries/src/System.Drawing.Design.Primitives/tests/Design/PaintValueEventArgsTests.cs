// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Design
{
    public class PaintValueEventArgsTests
    {
        [Fact]
        public void Ctor_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("graphics", () => new PaintValueEventArgs(null, new object(), null, Rectangle.Empty));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_PropertiesAssignedCorrectly()
        {
            using (var bm = new Bitmap(20, 20))
            using (var graphics = Graphics.FromImage(bm))
            {
                var paintValueEventArgs = new PaintValueEventArgs(null, bm, graphics, Rectangle.Empty);
                Assert.Null(paintValueEventArgs.Context);
                Assert.Equal(bm, paintValueEventArgs.Value);
                Assert.Equal(graphics, paintValueEventArgs.Graphics);
                Assert.Equal(Rectangle.Empty, Rectangle.Empty);
            }
        }
    }
}

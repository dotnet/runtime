// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class HatchBrushTests
    {
        public static IEnumerable<object[]> Ctor_HatchStyle_ForeColor_TestData()
        {
            yield return new object[] { HatchStyle.Horizontal, new Color() };
            yield return new object[] { HatchStyle.SolidDiamond, Color.PapayaWhip };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_HatchStyle_ForeColor_TestData))]
        public void Ctor_HatchStyle_ForeColor(HatchStyle hatchStyle, Color foreColor)
        {
            var brush = new HatchBrush(hatchStyle, foreColor);
            Assert.Equal(hatchStyle, brush.HatchStyle);

            Assert.NotEqual(foreColor, brush.ForegroundColor);
            Assert.Equal(foreColor.ToArgb(), brush.ForegroundColor.ToArgb());

            Assert.Equal(Color.FromArgb(255, 0, 0, 0), brush.BackgroundColor);
        }

        public static IEnumerable<object[]> Ctor_HatchStyle_ForeColor_BackColor_TestData()
        {
            yield return new object[] { HatchStyle.Horizontal, new Color(), new Color() };
            yield return new object[] { HatchStyle.SolidDiamond, Color.PapayaWhip, Color.Plum };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_HatchStyle_ForeColor_BackColor_TestData))]
        public void Ctor_HatchStyle_ForeColor_BackColor(HatchStyle hatchStyle, Color foreColor, Color backColor)
        {
            var brush = new HatchBrush(hatchStyle, foreColor, backColor);
            Assert.Equal(hatchStyle, brush.HatchStyle);

            Assert.NotEqual(foreColor, brush.ForegroundColor);
            Assert.Equal(foreColor.ToArgb(), brush.ForegroundColor.ToArgb());

            Assert.NotEqual(backColor, brush.BackgroundColor);
            Assert.Equal(backColor.ToArgb(), brush.BackgroundColor.ToArgb());
        }

        [Theory]
        [InlineData(HatchStyle.Horizontal -1 )]
        [InlineData(HatchStyle.SolidDiamond + 1)]
        public void Ctor_InvalidHatchStyle_ThrowsArgumentException(HatchStyle hatchStyle)
        {
            AssertExtensions.Throws<ArgumentException>("hatchstyle", null, () => new HatchBrush(hatchStyle, Color.Empty));
            AssertExtensions.Throws<ArgumentException>("hatchstyle", null, () => new HatchBrush(hatchStyle, Color.Empty, Color.Empty));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_Brush_ReturnsClone()
        {
            var brush = new HatchBrush(HatchStyle.DarkDownwardDiagonal, Color.Magenta, Color.Peru);
            HatchBrush clone = Assert.IsType<HatchBrush>(brush.Clone());

            Assert.NotSame(clone, brush);
            Assert.Equal(brush.HatchStyle, clone.HatchStyle);
            Assert.Equal(brush.ForegroundColor, clone.ForegroundColor);
            Assert.Equal(brush.BackgroundColor, clone.BackgroundColor);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_ImmutableColor_ReturnsMutableClone()
        {
            SolidBrush brush = Assert.IsType<SolidBrush>(Brushes.Bisque);
            SolidBrush clone = Assert.IsType<SolidBrush>(brush.Clone());

            clone.Color = SystemColors.AppWorkspace;
            Assert.Equal(SystemColors.AppWorkspace, clone.Color);
            Assert.Equal(Color.Bisque, brush.Color);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var brush = new HatchBrush(HatchStyle.DarkHorizontal, Color.PeachPuff, Color.Purple);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Clone());
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void HatchStyle_EmptyAndGetDisposed_ThrowsArgumentException()
        {
            var brush = new HatchBrush(HatchStyle.DarkHorizontal, Color.PeachPuff, Color.Purple);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.HatchStyle);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ForegroundColor_EmptyAndGetDisposed_ThrowsArgumentException()
        {
            var brush = new HatchBrush(HatchStyle.DarkHorizontal, Color.PeachPuff, Color.Purple);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.ForegroundColor);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void BackgroundColor_EmptyAndGetDisposed_ThrowsArgumentException()
        {
            var brush = new HatchBrush(HatchStyle.DarkHorizontal, Color.PeachPuff, Color.Purple);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.BackgroundColor);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Dispose_MultipleTimes_Success()
        {
            var brush = new HatchBrush(HatchStyle.DarkHorizontal, Color.PeachPuff, Color.Purple);
            brush.Dispose();
            brush.Dispose();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Tests
{
    public class SolidBrushTests
    {
        public static IEnumerable<object[]> Colors_TestData()
        {
            yield return new object[] { new Color(), Color.FromArgb(0) };
            yield return new object[] { Color.PapayaWhip, Color.PapayaWhip };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Colors_TestData))]
        public void Ctor_Color(Color color, Color expectedColor)
        {
            var brush = new SolidBrush(color);
            Assert.Equal(expectedColor, brush.Color);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_Color_ReturnsClone()
        {
            var brush = new SolidBrush(Color.PeachPuff);
            SolidBrush clone = Assert.IsType<SolidBrush>(brush.Clone());

            Assert.NotSame(clone, brush);
            Assert.Equal(brush.Color.ToArgb(), clone.Color.ToArgb());

            // Known colors are not preserved across clones.
            Assert.NotEqual(Color.PeachPuff, clone.Color);  

            // Modifying the original brush should not modify the clone.
            brush.Color = Color.PapayaWhip;
            Assert.NotEqual(Color.PapayaWhip, clone.Color);
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
            var brush = new SolidBrush(Color.LavenderBlush);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Clone());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Color_EmptyAndGetDisposed_ThrowsArgumentException()
        {
            var brush = new SolidBrush(new Color());
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Color);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Color_NonEmptyAndGetDisposed_ReturnsExpected()
        {
            var brush = new SolidBrush(Color.Aquamarine);
            brush.Dispose();

            Assert.Equal(Color.Aquamarine, brush.Color);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Color_SetValid_GetReturnsExpected()
        {
            var brush = new SolidBrush(Color.Goldenrod) { Color = Color.GhostWhite };
            Assert.Equal(Color.GhostWhite, brush.Color);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Color_SetDisposed_ThrowsArgumentException()
        {
            var brush = new SolidBrush(new Color());
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Color = Color.WhiteSmoke);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Color_SetImmutable_ThrowsArgumentException()
        {
            SolidBrush brush = Assert.IsType<SolidBrush>(SystemBrushes.ActiveBorder);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Color = Color.AntiqueWhite);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Dispose_MultipleTimes_Success()
        {
            var brush = new SolidBrush(Color.Plum);
            brush.Dispose();
            brush.Dispose();
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Dispose_SetImmutable_ThrowsArgumentException()
        {
            SolidBrush brush = Assert.IsType<SolidBrush>(SystemBrushes.ActiveBorder);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Dispose());
        }
    }
}

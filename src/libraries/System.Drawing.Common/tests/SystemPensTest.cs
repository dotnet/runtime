// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Reflection;
using Xunit;

namespace System.Drawing.Tests
{
    public class SystemPensTests
    {
        public static IEnumerable<object[]> SystemPens_TestData()
        {
            yield return Pen(() => SystemPens.ActiveBorder, SystemColors.ActiveBorder);
            yield return Pen(() => SystemPens.ActiveCaption, SystemColors.ActiveCaption);
            yield return Pen(() => SystemPens.ActiveCaptionText, SystemColors.ActiveCaptionText);
            yield return Pen(() => SystemPens.AppWorkspace, SystemColors.AppWorkspace);
            yield return Pen(() => SystemPens.ButtonFace, SystemColors.ButtonFace);
            yield return Pen(() => SystemPens.ButtonHighlight, SystemColors.ButtonHighlight);
            yield return Pen(() => SystemPens.ButtonShadow, SystemColors.ButtonShadow);
            yield return Pen(() => SystemPens.Control, SystemColors.Control);
            yield return Pen(() => SystemPens.ControlDark, SystemColors.ControlDark);
            yield return Pen(() => SystemPens.ControlDarkDark, SystemColors.ControlDarkDark);
            yield return Pen(() => SystemPens.ControlLight, SystemColors.ControlLight);
            yield return Pen(() => SystemPens.ControlLightLight, SystemColors.ControlLightLight);
            yield return Pen(() => SystemPens.ControlText, SystemColors.ControlText);
            yield return Pen(() => SystemPens.Desktop, SystemColors.Desktop);
            yield return Pen(() => SystemPens.GradientActiveCaption, SystemColors.GradientActiveCaption);
            yield return Pen(() => SystemPens.GradientInactiveCaption, SystemColors.GradientInactiveCaption);
            yield return Pen(() => SystemPens.GrayText, SystemColors.GrayText);
            yield return Pen(() => SystemPens.Highlight, SystemColors.Highlight);
            yield return Pen(() => SystemPens.HighlightText, SystemColors.HighlightText);
            yield return Pen(() => SystemPens.HotTrack, SystemColors.HotTrack);
            yield return Pen(() => SystemPens.InactiveBorder, SystemColors.InactiveBorder);
            yield return Pen(() => SystemPens.InactiveCaption, SystemColors.InactiveCaption);
            yield return Pen(() => SystemPens.InactiveCaptionText, SystemColors.InactiveCaptionText);
            yield return Pen(() => SystemPens.Info, SystemColors.Info);
            yield return Pen(() => SystemPens.InfoText, SystemColors.InfoText);
            yield return Pen(() => SystemPens.Menu, SystemColors.Menu);
            yield return Pen(() => SystemPens.MenuBar, SystemColors.MenuBar);
            yield return Pen(() => SystemPens.MenuHighlight, SystemColors.MenuHighlight);
            yield return Pen(() => SystemPens.MenuText, SystemColors.MenuText);
            yield return Pen(() => SystemPens.ScrollBar, SystemColors.ScrollBar);
            yield return Pen(() => SystemPens.Window, SystemColors.Window);
            yield return Pen(() => SystemPens.WindowFrame, SystemColors.WindowFrame);
            yield return Pen(() => SystemPens.WindowText, SystemColors.WindowText);
        }

        public static object[] Pen(Func<Pen> getPen, Color expectedColor) => new object[] { getPen, expectedColor };

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(SystemPens_TestData))]
        public void SystemPens_Get_ReturnsExpected(Func<Pen> getPen, Color expectedColor)
        {
            Pen pen = getPen();
            Assert.Equal(expectedColor, pen.Color);
            Assert.Equal(PenType.SolidColor, pen.PenType);
            Assert.Same(pen, getPen());

            AssertExtensions.Throws<ArgumentException>(null, () => pen.Dispose());
            AssertExtensions.Throws<ArgumentException>(null, () => pen.SetLineCap(LineCap.ArrowAnchor, LineCap.Custom, DashCap.Round));

            AssertExtensions.Throws<ArgumentException>(null, () => pen.Alignment = PenAlignment.Center);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.Brush = null);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.Color = Color.AliceBlue);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.CompoundArray = null);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.CustomEndCap = null);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.CustomStartCap = null);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.DashCap = DashCap.Flat);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.DashStyle = DashStyle.Custom);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.DashOffset = 10);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.DashPattern = null);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.EndCap = LineCap.RoundAnchor);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.LineJoin = LineJoin.MiterClipped);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.MiterLimit = 10);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.StartCap = LineCap.RoundAnchor);
            using (var matrix = new Matrix())
            {
                Assert.Throws<ArgumentException>(null, () => pen.Transform = matrix);
            }
            AssertExtensions.Throws<ArgumentException>(null, () => pen.Width = 10);
        }

        [Fact]
        public void FromSystemColor_NotSystemColor_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => SystemPens.FromSystemColor(Color.Blue));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Reflection;
using Xunit;

namespace System.Drawing.Tests
{
    public class SystemBrushesTests
    {
        public static IEnumerable<object[]> SystemBrushes_TestData()
        {
            yield return Brush(() => SystemBrushes.ActiveBorder, SystemColors.ActiveBorder);
            yield return Brush(() => SystemBrushes.ActiveCaption, SystemColors.ActiveCaption);
            yield return Brush(() => SystemBrushes.ActiveCaptionText, SystemColors.ActiveCaptionText);
            yield return Brush(() => SystemBrushes.AppWorkspace, SystemColors.AppWorkspace);
            yield return Brush(() => SystemBrushes.ButtonFace, SystemColors.ButtonFace);
            yield return Brush(() => SystemBrushes.ButtonHighlight, SystemColors.ButtonHighlight);
            yield return Brush(() => SystemBrushes.ButtonShadow, SystemColors.ButtonShadow);
            yield return Brush(() => SystemBrushes.Control, SystemColors.Control);
            yield return Brush(() => SystemBrushes.ControlDark, SystemColors.ControlDark);
            yield return Brush(() => SystemBrushes.ControlDarkDark, SystemColors.ControlDarkDark);
            yield return Brush(() => SystemBrushes.ControlLight, SystemColors.ControlLight);
            yield return Brush(() => SystemBrushes.ControlLightLight, SystemColors.ControlLightLight);
            yield return Brush(() => SystemBrushes.ControlText, SystemColors.ControlText);
            yield return Brush(() => SystemBrushes.Desktop, SystemColors.Desktop);
            yield return Brush(() => SystemBrushes.GradientActiveCaption, SystemColors.GradientActiveCaption);
            yield return Brush(() => SystemBrushes.GradientInactiveCaption, SystemColors.GradientInactiveCaption);
            yield return Brush(() => SystemBrushes.GrayText, SystemColors.GrayText);
            yield return Brush(() => SystemBrushes.Highlight, SystemColors.Highlight);
            yield return Brush(() => SystemBrushes.HighlightText, SystemColors.HighlightText);
            yield return Brush(() => SystemBrushes.HotTrack, SystemColors.HotTrack);
            yield return Brush(() => SystemBrushes.InactiveBorder, SystemColors.InactiveBorder);
            yield return Brush(() => SystemBrushes.InactiveCaption, SystemColors.InactiveCaption);
            yield return Brush(() => SystemBrushes.InactiveCaptionText, SystemColors.InactiveCaptionText);
            yield return Brush(() => SystemBrushes.Info, SystemColors.Info);
            yield return Brush(() => SystemBrushes.InfoText, SystemColors.InfoText);
            yield return Brush(() => SystemBrushes.Menu, SystemColors.Menu);
            yield return Brush(() => SystemBrushes.MenuBar, SystemColors.MenuBar);
            yield return Brush(() => SystemBrushes.MenuHighlight, SystemColors.MenuHighlight);
            yield return Brush(() => SystemBrushes.MenuText, SystemColors.MenuText);
            yield return Brush(() => SystemBrushes.ScrollBar, SystemColors.ScrollBar);
            yield return Brush(() => SystemBrushes.Window, SystemColors.Window);
            yield return Brush(() => SystemBrushes.WindowFrame, SystemColors.WindowFrame);
            yield return Brush(() => SystemBrushes.WindowText, SystemColors.WindowText);
        }

        public static object[] Brush(Func<Brush> getBrush, Color expectedColor) => new object[] { getBrush, expectedColor };

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SystemBrushes_TestData))]
        public void SystemBrushes_Get_ReturnsExpected(Func<Brush> getBrush, Color expectedColor)
        {
            SolidBrush brush = Assert.IsType<SolidBrush>(getBrush());
            Assert.Equal(expectedColor, brush.Color);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Color = Color.Red);

            Assert.Same(brush, getBrush());
        }

        [Fact]
        public void FromSystemColor_NotSystemColor_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => SystemBrushes.FromSystemColor(Color.Blue));
        }
    }
}

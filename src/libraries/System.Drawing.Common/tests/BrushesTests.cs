// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Reflection;
using Xunit;

namespace System.Drawing.Tests
{
    public class BrushesTests
    {
        public static IEnumerable<object[]> Brushes_TestData()
        {
            yield return Brush(() => Brushes.AliceBlue, Color.AliceBlue);
            yield return Brush(() => Brushes.AntiqueWhite, Color.AntiqueWhite);
            yield return Brush(() => Brushes.Aqua, Color.Aqua);
            yield return Brush(() => Brushes.Aquamarine, Color.Aquamarine);
            yield return Brush(() => Brushes.Azure, Color.Azure);
            yield return Brush(() => Brushes.Beige, Color.Beige);
            yield return Brush(() => Brushes.Bisque, Color.Bisque);
            yield return Brush(() => Brushes.Black, Color.Black);
            yield return Brush(() => Brushes.BlanchedAlmond, Color.BlanchedAlmond);
            yield return Brush(() => Brushes.Blue, Color.Blue);
            yield return Brush(() => Brushes.BlueViolet, Color.BlueViolet);
            yield return Brush(() => Brushes.Brown, Color.Brown);
            yield return Brush(() => Brushes.BurlyWood, Color.BurlyWood);
            yield return Brush(() => Brushes.CadetBlue, Color.CadetBlue);
            yield return Brush(() => Brushes.Chartreuse, Color.Chartreuse);
            yield return Brush(() => Brushes.Chocolate, Color.Chocolate);
            yield return Brush(() => Brushes.Coral, Color.Coral);
            yield return Brush(() => Brushes.CornflowerBlue, Color.CornflowerBlue);
            yield return Brush(() => Brushes.Cornsilk, Color.Cornsilk);
            yield return Brush(() => Brushes.Crimson, Color.Crimson);
            yield return Brush(() => Brushes.Cyan, Color.Cyan);
            yield return Brush(() => Brushes.DarkBlue, Color.DarkBlue);
            yield return Brush(() => Brushes.DarkCyan, Color.DarkCyan);
            yield return Brush(() => Brushes.DarkGoldenrod, Color.DarkGoldenrod);
            yield return Brush(() => Brushes.DarkGray, Color.DarkGray);
            yield return Brush(() => Brushes.DarkGreen, Color.DarkGreen);
            yield return Brush(() => Brushes.DarkKhaki, Color.DarkKhaki);
            yield return Brush(() => Brushes.DarkMagenta, Color.DarkMagenta);
            yield return Brush(() => Brushes.DarkOliveGreen, Color.DarkOliveGreen);
            yield return Brush(() => Brushes.DarkOrange, Color.DarkOrange);
            yield return Brush(() => Brushes.DarkOrchid, Color.DarkOrchid);
            yield return Brush(() => Brushes.DarkRed, Color.DarkRed);
            yield return Brush(() => Brushes.DarkSalmon, Color.DarkSalmon);
            yield return Brush(() => Brushes.DarkSeaGreen, Color.DarkSeaGreen);
            yield return Brush(() => Brushes.DarkSlateBlue, Color.DarkSlateBlue);
            yield return Brush(() => Brushes.DarkSlateGray, Color.DarkSlateGray);
            yield return Brush(() => Brushes.DarkTurquoise, Color.DarkTurquoise);
            yield return Brush(() => Brushes.DarkViolet, Color.DarkViolet);
            yield return Brush(() => Brushes.DeepPink, Color.DeepPink);
            yield return Brush(() => Brushes.DeepSkyBlue, Color.DeepSkyBlue);
            yield return Brush(() => Brushes.DimGray, Color.DimGray);
            yield return Brush(() => Brushes.DodgerBlue, Color.DodgerBlue);
            yield return Brush(() => Brushes.Firebrick, Color.Firebrick);
            yield return Brush(() => Brushes.FloralWhite, Color.FloralWhite);
            yield return Brush(() => Brushes.ForestGreen, Color.ForestGreen);
            yield return Brush(() => Brushes.Fuchsia, Color.Fuchsia);
            yield return Brush(() => Brushes.Gainsboro, Color.Gainsboro);
            yield return Brush(() => Brushes.GhostWhite, Color.GhostWhite);
            yield return Brush(() => Brushes.Gold, Color.Gold);
            yield return Brush(() => Brushes.Goldenrod, Color.Goldenrod);
            yield return Brush(() => Brushes.Gray, Color.Gray);
            yield return Brush(() => Brushes.Green, Color.Green);
            yield return Brush(() => Brushes.GreenYellow, Color.GreenYellow);
            yield return Brush(() => Brushes.Honeydew, Color.Honeydew);
            yield return Brush(() => Brushes.HotPink, Color.HotPink);
            yield return Brush(() => Brushes.IndianRed, Color.IndianRed);
            yield return Brush(() => Brushes.Indigo, Color.Indigo);
            yield return Brush(() => Brushes.Ivory, Color.Ivory);
            yield return Brush(() => Brushes.Khaki, Color.Khaki);
            yield return Brush(() => Brushes.Lavender, Color.Lavender);
            yield return Brush(() => Brushes.LavenderBlush, Color.LavenderBlush);
            yield return Brush(() => Brushes.LawnGreen, Color.LawnGreen);
            yield return Brush(() => Brushes.LemonChiffon, Color.LemonChiffon);
            yield return Brush(() => Brushes.LightBlue, Color.LightBlue);
            yield return Brush(() => Brushes.LightCoral, Color.LightCoral);
            yield return Brush(() => Brushes.LightCyan, Color.LightCyan);
            yield return Brush(() => Brushes.LightGoldenrodYellow, Color.LightGoldenrodYellow);
            yield return Brush(() => Brushes.LightGray, Color.LightGray);
            yield return Brush(() => Brushes.LightGreen, Color.LightGreen);
            yield return Brush(() => Brushes.LightPink, Color.LightPink);
            yield return Brush(() => Brushes.LightSalmon, Color.LightSalmon);
            yield return Brush(() => Brushes.LightSeaGreen, Color.LightSeaGreen);
            yield return Brush(() => Brushes.LightSkyBlue, Color.LightSkyBlue);
            yield return Brush(() => Brushes.LightSlateGray, Color.LightSlateGray);
            yield return Brush(() => Brushes.LightSteelBlue, Color.LightSteelBlue);
            yield return Brush(() => Brushes.LightYellow, Color.LightYellow);
            yield return Brush(() => Brushes.Lime, Color.Lime);
            yield return Brush(() => Brushes.LimeGreen, Color.LimeGreen);
            yield return Brush(() => Brushes.Linen, Color.Linen);
            yield return Brush(() => Brushes.Magenta, Color.Magenta);
            yield return Brush(() => Brushes.Maroon, Color.Maroon);
            yield return Brush(() => Brushes.MediumAquamarine, Color.MediumAquamarine);
            yield return Brush(() => Brushes.MediumBlue, Color.MediumBlue);
            yield return Brush(() => Brushes.MediumOrchid, Color.MediumOrchid);
            yield return Brush(() => Brushes.MediumPurple, Color.MediumPurple);
            yield return Brush(() => Brushes.MediumSeaGreen, Color.MediumSeaGreen);
            yield return Brush(() => Brushes.MediumSlateBlue, Color.MediumSlateBlue);
            yield return Brush(() => Brushes.MediumSpringGreen, Color.MediumSpringGreen);
            yield return Brush(() => Brushes.MediumTurquoise, Color.MediumTurquoise);
            yield return Brush(() => Brushes.MediumVioletRed, Color.MediumVioletRed);
            yield return Brush(() => Brushes.MidnightBlue, Color.MidnightBlue);
            yield return Brush(() => Brushes.MintCream, Color.MintCream);
            yield return Brush(() => Brushes.MistyRose, Color.MistyRose);
            yield return Brush(() => Brushes.Moccasin, Color.Moccasin);
            yield return Brush(() => Brushes.NavajoWhite, Color.NavajoWhite);
            yield return Brush(() => Brushes.Navy, Color.Navy);
            yield return Brush(() => Brushes.OldLace, Color.OldLace);
            yield return Brush(() => Brushes.Olive, Color.Olive);
            yield return Brush(() => Brushes.OliveDrab, Color.OliveDrab);
            yield return Brush(() => Brushes.Orange, Color.Orange);
            yield return Brush(() => Brushes.OrangeRed, Color.OrangeRed);
            yield return Brush(() => Brushes.Orchid, Color.Orchid);
            yield return Brush(() => Brushes.PaleGoldenrod, Color.PaleGoldenrod);
            yield return Brush(() => Brushes.PaleGreen, Color.PaleGreen);
            yield return Brush(() => Brushes.PaleTurquoise, Color.PaleTurquoise);
            yield return Brush(() => Brushes.PaleVioletRed, Color.PaleVioletRed);
            yield return Brush(() => Brushes.PapayaWhip, Color.PapayaWhip);
            yield return Brush(() => Brushes.PeachPuff, Color.PeachPuff);
            yield return Brush(() => Brushes.Peru, Color.Peru);
            yield return Brush(() => Brushes.Pink, Color.Pink);
            yield return Brush(() => Brushes.Plum, Color.Plum);
            yield return Brush(() => Brushes.PowderBlue, Color.PowderBlue);
            yield return Brush(() => Brushes.Purple, Color.Purple);
            yield return Brush(() => Brushes.Red, Color.Red);
            yield return Brush(() => Brushes.RosyBrown, Color.RosyBrown);
            yield return Brush(() => Brushes.RoyalBlue, Color.RoyalBlue);
            yield return Brush(() => Brushes.SaddleBrown, Color.SaddleBrown);
            yield return Brush(() => Brushes.Salmon, Color.Salmon);
            yield return Brush(() => Brushes.SandyBrown, Color.SandyBrown);
            yield return Brush(() => Brushes.SeaGreen, Color.SeaGreen);
            yield return Brush(() => Brushes.SeaShell, Color.SeaShell);
            yield return Brush(() => Brushes.Sienna, Color.Sienna);
            yield return Brush(() => Brushes.Silver, Color.Silver);
            yield return Brush(() => Brushes.SkyBlue, Color.SkyBlue);
            yield return Brush(() => Brushes.SlateBlue, Color.SlateBlue);
            yield return Brush(() => Brushes.SlateGray, Color.SlateGray);
            yield return Brush(() => Brushes.Snow, Color.Snow);
            yield return Brush(() => Brushes.SpringGreen, Color.SpringGreen);
            yield return Brush(() => Brushes.SteelBlue, Color.SteelBlue);
            yield return Brush(() => Brushes.Tan, Color.Tan);
            yield return Brush(() => Brushes.Teal, Color.Teal);
            yield return Brush(() => Brushes.Thistle, Color.Thistle);
            yield return Brush(() => Brushes.Tomato, Color.Tomato);
            yield return Brush(() => Brushes.Transparent, Color.Transparent);
            yield return Brush(() => Brushes.Turquoise, Color.Turquoise);
            yield return Brush(() => Brushes.Violet, Color.Violet);
            yield return Brush(() => Brushes.Wheat, Color.Wheat);
            yield return Brush(() => Brushes.White, Color.White);
            yield return Brush(() => Brushes.WhiteSmoke, Color.WhiteSmoke);
            yield return Brush(() => Brushes.Yellow, Color.Yellow);
            yield return Brush(() => Brushes.YellowGreen, Color.YellowGreen);
        }

        public static object[] Brush(Func<Brush> getBrush, Color expectedColor) => new object[] { getBrush, expectedColor };

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Brushes_TestData))]
        public void Brushes_Get_ReturnsExpected(Func<Brush> getBrush, Color expectedColor)
        {
            SolidBrush brush = Assert.IsType<SolidBrush>(getBrush());
            Assert.Equal(expectedColor, brush.Color);

            Assert.Same(brush, getBrush());

            // Brushes are not immutable.
            Color color = brush.Color;
            try
            {
                brush.Color = Color.Red;
                Assert.Equal(Color.Red, brush.Color);
            }
            finally
            {
                brush.Color = color;
            }
        }
    }
}

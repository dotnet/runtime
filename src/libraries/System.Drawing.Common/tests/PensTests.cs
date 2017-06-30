// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Reflection;
using Xunit;

namespace System.Drawing.Tests
{
    public class PensTests
    {
        public static IEnumerable<object[]> Pens_TestData()
        {
            yield return Pen(() => Pens.AliceBlue, Color.AliceBlue);
            yield return Pen(() => Pens.AntiqueWhite, Color.AntiqueWhite);
            yield return Pen(() => Pens.Aqua, Color.Aqua);
            yield return Pen(() => Pens.Aquamarine, Color.Aquamarine);
            yield return Pen(() => Pens.Azure, Color.Azure);
            yield return Pen(() => Pens.Beige, Color.Beige);
            yield return Pen(() => Pens.Bisque, Color.Bisque);
            yield return Pen(() => Pens.Black, Color.Black);
            yield return Pen(() => Pens.BlanchedAlmond, Color.BlanchedAlmond);
            yield return Pen(() => Pens.Blue, Color.Blue);
            yield return Pen(() => Pens.BlueViolet, Color.BlueViolet);
            yield return Pen(() => Pens.Brown, Color.Brown);
            yield return Pen(() => Pens.BurlyWood, Color.BurlyWood);
            yield return Pen(() => Pens.CadetBlue, Color.CadetBlue);
            yield return Pen(() => Pens.Chartreuse, Color.Chartreuse);
            yield return Pen(() => Pens.Chocolate, Color.Chocolate);
            yield return Pen(() => Pens.Coral, Color.Coral);
            yield return Pen(() => Pens.CornflowerBlue, Color.CornflowerBlue);
            yield return Pen(() => Pens.Cornsilk, Color.Cornsilk);
            yield return Pen(() => Pens.Crimson, Color.Crimson);
            yield return Pen(() => Pens.Cyan, Color.Cyan);
            yield return Pen(() => Pens.DarkBlue, Color.DarkBlue);
            yield return Pen(() => Pens.DarkCyan, Color.DarkCyan);
            yield return Pen(() => Pens.DarkGoldenrod, Color.DarkGoldenrod);
            yield return Pen(() => Pens.DarkGray, Color.DarkGray);
            yield return Pen(() => Pens.DarkGreen, Color.DarkGreen);
            yield return Pen(() => Pens.DarkKhaki, Color.DarkKhaki);
            yield return Pen(() => Pens.DarkMagenta, Color.DarkMagenta);
            yield return Pen(() => Pens.DarkOliveGreen, Color.DarkOliveGreen);
            yield return Pen(() => Pens.DarkOrange, Color.DarkOrange);
            yield return Pen(() => Pens.DarkOrchid, Color.DarkOrchid);
            yield return Pen(() => Pens.DarkRed, Color.DarkRed);
            yield return Pen(() => Pens.DarkSalmon, Color.DarkSalmon);
            yield return Pen(() => Pens.DarkSeaGreen, Color.DarkSeaGreen);
            yield return Pen(() => Pens.DarkSlateBlue, Color.DarkSlateBlue);
            yield return Pen(() => Pens.DarkSlateGray, Color.DarkSlateGray);
            yield return Pen(() => Pens.DarkTurquoise, Color.DarkTurquoise);
            yield return Pen(() => Pens.DarkViolet, Color.DarkViolet);
            yield return Pen(() => Pens.DeepPink, Color.DeepPink);
            yield return Pen(() => Pens.DeepSkyBlue, Color.DeepSkyBlue);
            yield return Pen(() => Pens.DimGray, Color.DimGray);
            yield return Pen(() => Pens.DodgerBlue, Color.DodgerBlue);
            yield return Pen(() => Pens.Firebrick, Color.Firebrick);
            yield return Pen(() => Pens.FloralWhite, Color.FloralWhite);
            yield return Pen(() => Pens.ForestGreen, Color.ForestGreen);
            yield return Pen(() => Pens.Fuchsia, Color.Fuchsia);
            yield return Pen(() => Pens.Gainsboro, Color.Gainsboro);
            yield return Pen(() => Pens.GhostWhite, Color.GhostWhite);
            yield return Pen(() => Pens.Gold, Color.Gold);
            yield return Pen(() => Pens.Goldenrod, Color.Goldenrod);
            yield return Pen(() => Pens.Gray, Color.Gray);
            yield return Pen(() => Pens.Green, Color.Green);
            yield return Pen(() => Pens.GreenYellow, Color.GreenYellow);
            yield return Pen(() => Pens.Honeydew, Color.Honeydew);
            yield return Pen(() => Pens.HotPink, Color.HotPink);
            yield return Pen(() => Pens.IndianRed, Color.IndianRed);
            yield return Pen(() => Pens.Indigo, Color.Indigo);
            yield return Pen(() => Pens.Ivory, Color.Ivory);
            yield return Pen(() => Pens.Khaki, Color.Khaki);
            yield return Pen(() => Pens.Lavender, Color.Lavender);
            yield return Pen(() => Pens.LavenderBlush, Color.LavenderBlush);
            yield return Pen(() => Pens.LawnGreen, Color.LawnGreen);
            yield return Pen(() => Pens.LemonChiffon, Color.LemonChiffon);
            yield return Pen(() => Pens.LightBlue, Color.LightBlue);
            yield return Pen(() => Pens.LightCoral, Color.LightCoral);
            yield return Pen(() => Pens.LightCyan, Color.LightCyan);
            yield return Pen(() => Pens.LightGoldenrodYellow, Color.LightGoldenrodYellow);
            yield return Pen(() => Pens.LightGray, Color.LightGray);
            yield return Pen(() => Pens.LightGreen, Color.LightGreen);
            yield return Pen(() => Pens.LightPink, Color.LightPink);
            yield return Pen(() => Pens.LightSalmon, Color.LightSalmon);
            yield return Pen(() => Pens.LightSeaGreen, Color.LightSeaGreen);
            yield return Pen(() => Pens.LightSkyBlue, Color.LightSkyBlue);
            yield return Pen(() => Pens.LightSlateGray, Color.LightSlateGray);
            yield return Pen(() => Pens.LightSteelBlue, Color.LightSteelBlue);
            yield return Pen(() => Pens.LightYellow, Color.LightYellow);
            yield return Pen(() => Pens.Lime, Color.Lime);
            yield return Pen(() => Pens.LimeGreen, Color.LimeGreen);
            yield return Pen(() => Pens.Linen, Color.Linen);
            yield return Pen(() => Pens.Magenta, Color.Magenta);
            yield return Pen(() => Pens.Maroon, Color.Maroon);
            yield return Pen(() => Pens.MediumAquamarine, Color.MediumAquamarine);
            yield return Pen(() => Pens.MediumBlue, Color.MediumBlue);
            yield return Pen(() => Pens.MediumOrchid, Color.MediumOrchid);
            yield return Pen(() => Pens.MediumPurple, Color.MediumPurple);
            yield return Pen(() => Pens.MediumSeaGreen, Color.MediumSeaGreen);
            yield return Pen(() => Pens.MediumSlateBlue, Color.MediumSlateBlue);
            yield return Pen(() => Pens.MediumSpringGreen, Color.MediumSpringGreen);
            yield return Pen(() => Pens.MediumTurquoise, Color.MediumTurquoise);
            yield return Pen(() => Pens.MediumVioletRed, Color.MediumVioletRed);
            yield return Pen(() => Pens.MidnightBlue, Color.MidnightBlue);
            yield return Pen(() => Pens.MintCream, Color.MintCream);
            yield return Pen(() => Pens.MistyRose, Color.MistyRose);
            yield return Pen(() => Pens.Moccasin, Color.Moccasin);
            yield return Pen(() => Pens.NavajoWhite, Color.NavajoWhite);
            yield return Pen(() => Pens.Navy, Color.Navy);
            yield return Pen(() => Pens.OldLace, Color.OldLace);
            yield return Pen(() => Pens.Olive, Color.Olive);
            yield return Pen(() => Pens.OliveDrab, Color.OliveDrab);
            yield return Pen(() => Pens.Orange, Color.Orange);
            yield return Pen(() => Pens.OrangeRed, Color.OrangeRed);
            yield return Pen(() => Pens.Orchid, Color.Orchid);
            yield return Pen(() => Pens.PaleGoldenrod, Color.PaleGoldenrod);
            yield return Pen(() => Pens.PaleGreen, Color.PaleGreen);
            yield return Pen(() => Pens.PaleTurquoise, Color.PaleTurquoise);
            yield return Pen(() => Pens.PaleVioletRed, Color.PaleVioletRed);
            yield return Pen(() => Pens.PapayaWhip, Color.PapayaWhip);
            yield return Pen(() => Pens.PeachPuff, Color.PeachPuff);
            yield return Pen(() => Pens.Peru, Color.Peru);
            yield return Pen(() => Pens.Pink, Color.Pink);
            yield return Pen(() => Pens.Plum, Color.Plum);
            yield return Pen(() => Pens.PowderBlue, Color.PowderBlue);
            yield return Pen(() => Pens.Purple, Color.Purple);
            yield return Pen(() => Pens.Red, Color.Red);
            yield return Pen(() => Pens.RosyBrown, Color.RosyBrown);
            yield return Pen(() => Pens.RoyalBlue, Color.RoyalBlue);
            yield return Pen(() => Pens.SaddleBrown, Color.SaddleBrown);
            yield return Pen(() => Pens.Salmon, Color.Salmon);
            yield return Pen(() => Pens.SandyBrown, Color.SandyBrown);
            yield return Pen(() => Pens.SeaGreen, Color.SeaGreen);
            yield return Pen(() => Pens.SeaShell, Color.SeaShell);
            yield return Pen(() => Pens.Sienna, Color.Sienna);
            yield return Pen(() => Pens.Silver, Color.Silver);
            yield return Pen(() => Pens.SkyBlue, Color.SkyBlue);
            yield return Pen(() => Pens.SlateBlue, Color.SlateBlue);
            yield return Pen(() => Pens.SlateGray, Color.SlateGray);
            yield return Pen(() => Pens.Snow, Color.Snow);
            yield return Pen(() => Pens.SpringGreen, Color.SpringGreen);
            yield return Pen(() => Pens.SteelBlue, Color.SteelBlue);
            yield return Pen(() => Pens.Tan, Color.Tan);
            yield return Pen(() => Pens.Teal, Color.Teal);
            yield return Pen(() => Pens.Thistle, Color.Thistle);
            yield return Pen(() => Pens.Tomato, Color.Tomato);
            yield return Pen(() => Pens.Transparent, Color.Transparent);
            yield return Pen(() => Pens.Turquoise, Color.Turquoise);
            yield return Pen(() => Pens.Violet, Color.Violet);
            yield return Pen(() => Pens.Wheat, Color.Wheat);
            yield return Pen(() => Pens.White, Color.White);
            yield return Pen(() => Pens.WhiteSmoke, Color.WhiteSmoke);
            yield return Pen(() => Pens.Yellow, Color.Yellow);
            yield return Pen(() => Pens.YellowGreen, Color.YellowGreen);
        }

        public static object[] Pen(Func<Pen> getPen, Color expectedColor) => new object[] { getPen, expectedColor };

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Pens_TestData))]
        public void Pens_Get_ReturnsExpected(Func<Pen> getPen, Color expectedColor)
        {
            Pen pen = getPen();
            Assert.Equal(expectedColor, pen.Color);
            Assert.Equal(PenType.SolidColor, pen.PenType);
            AssertExtensions.Throws<ArgumentException>(null, () => pen.Color = Color.AliceBlue);

            Assert.Same(pen, getPen());
        }
    }
}

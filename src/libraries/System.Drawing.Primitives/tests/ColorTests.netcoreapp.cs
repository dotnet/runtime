// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Drawing.Primitives.Tests
{
    public partial class ColorTests
    {
        public static readonly IEnumerable<object[]> AllKnownColors = Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>()
            .Where(kc => kc != 0)
            .Select(kc => new object[] { kc })
            .ToArray();

        public static readonly IEnumerable<object[]> SystemColors =
            new[]
            {
                KnownColor.ActiveBorder, KnownColor.ActiveCaption, KnownColor.ActiveCaptionText,
                KnownColor.AppWorkspace, KnownColor.Control, KnownColor.ControlDark, KnownColor.ControlDarkDark,
                KnownColor.ControlLight, KnownColor.ControlLightLight, KnownColor.ControlText, KnownColor.Desktop,
                KnownColor.GrayText, KnownColor.Highlight, KnownColor.HighlightText, KnownColor.HotTrack,
                KnownColor.InactiveBorder, KnownColor.InactiveCaption, KnownColor.InactiveCaptionText, KnownColor.Info,
                KnownColor.InfoText, KnownColor.Menu, KnownColor.MenuText, KnownColor.ScrollBar, KnownColor.Window,
                KnownColor.WindowFrame, KnownColor.WindowText, KnownColor.ButtonFace, KnownColor.ButtonHighlight,
                KnownColor.ButtonShadow, KnownColor.GradientActiveCaption, KnownColor.GradientInactiveCaption,
                KnownColor.MenuBar, KnownColor.MenuHighlight
            }.Select(kc => new object[] { kc }).ToArray();

        public static readonly IEnumerable<object[]> NonSystemColors =
            new[]
            {
                KnownColor.Transparent, KnownColor.AliceBlue, KnownColor.AntiqueWhite, KnownColor.Aqua,
                KnownColor.Aquamarine, KnownColor.Azure, KnownColor.Beige, KnownColor.Bisque, KnownColor.Black,
                KnownColor.BlanchedAlmond, KnownColor.Blue, KnownColor.BlueViolet, KnownColor.Brown,
                KnownColor.BurlyWood, KnownColor.CadetBlue, KnownColor.Chartreuse, KnownColor.Chocolate,
                KnownColor.Coral, KnownColor.CornflowerBlue, KnownColor.Cornsilk, KnownColor.Crimson, KnownColor.Cyan,
                KnownColor.DarkBlue, KnownColor.DarkCyan, KnownColor.DarkGoldenrod, KnownColor.DarkGray,
                KnownColor.DarkGreen, KnownColor.DarkKhaki, KnownColor.DarkMagenta, KnownColor.DarkOliveGreen,
                KnownColor.DarkOrange, KnownColor.DarkOrchid, KnownColor.DarkRed, KnownColor.DarkSalmon,
                KnownColor.DarkSeaGreen, KnownColor.DarkSlateBlue, KnownColor.DarkSlateGray, KnownColor.DarkTurquoise,
                KnownColor.DarkViolet, KnownColor.DeepPink, KnownColor.DeepSkyBlue, KnownColor.DimGray,
                KnownColor.DodgerBlue, KnownColor.Firebrick, KnownColor.FloralWhite, KnownColor.ForestGreen,
                KnownColor.Fuchsia, KnownColor.Gainsboro, KnownColor.GhostWhite, KnownColor.Gold, KnownColor.Goldenrod,
                KnownColor.Gray, KnownColor.Green, KnownColor.GreenYellow, KnownColor.Honeydew, KnownColor.HotPink,
                KnownColor.IndianRed, KnownColor.Indigo, KnownColor.Ivory, KnownColor.Khaki, KnownColor.Lavender,
                KnownColor.LavenderBlush, KnownColor.LawnGreen, KnownColor.LemonChiffon, KnownColor.LightBlue,
                KnownColor.LightCoral, KnownColor.LightCyan, KnownColor.LightGoldenrodYellow, KnownColor.LightGray,
                KnownColor.LightGreen, KnownColor.LightPink, KnownColor.LightSalmon, KnownColor.LightSeaGreen,
                KnownColor.LightSkyBlue, KnownColor.LightSlateGray, KnownColor.LightSteelBlue, KnownColor.LightYellow,
                KnownColor.Lime, KnownColor.LimeGreen, KnownColor.Linen, KnownColor.Magenta, KnownColor.Maroon,
                KnownColor.MediumAquamarine, KnownColor.MediumBlue, KnownColor.MediumOrchid, KnownColor.MediumPurple,
                KnownColor.MediumSeaGreen, KnownColor.MediumSlateBlue, KnownColor.MediumSpringGreen,
                KnownColor.MediumTurquoise, KnownColor.MediumVioletRed, KnownColor.MidnightBlue, KnownColor.MintCream,
                KnownColor.MistyRose, KnownColor.Moccasin, KnownColor.NavajoWhite, KnownColor.Navy, KnownColor.OldLace,
                KnownColor.Olive, KnownColor.OliveDrab, KnownColor.Orange, KnownColor.OrangeRed, KnownColor.Orchid,
                KnownColor.PaleGoldenrod, KnownColor.PaleGreen, KnownColor.PaleTurquoise, KnownColor.PaleVioletRed,
                KnownColor.PapayaWhip, KnownColor.PeachPuff, KnownColor.Peru, KnownColor.Pink, KnownColor.Plum,
                KnownColor.PowderBlue, KnownColor.Purple, KnownColor.Red, KnownColor.RosyBrown, KnownColor.RoyalBlue,
                KnownColor.SaddleBrown, KnownColor.Salmon, KnownColor.SandyBrown, KnownColor.SeaGreen,
                KnownColor.SeaShell, KnownColor.Sienna, KnownColor.Silver, KnownColor.SkyBlue, KnownColor.SlateBlue,
                KnownColor.SlateGray, KnownColor.Snow, KnownColor.SpringGreen, KnownColor.SteelBlue, KnownColor.Tan,
                KnownColor.Teal, KnownColor.Thistle, KnownColor.Tomato, KnownColor.Turquoise, KnownColor.Violet,
                KnownColor.Wheat, KnownColor.White, KnownColor.WhiteSmoke, KnownColor.Yellow, KnownColor.YellowGreen
            }.Select(kc => new object[] { kc }).ToArray();

        [Theory, MemberData(nameof(NamedArgbValues))]
        public void FromKnownColor(string name, int alpha, int red, int green, int blue)
        {
            Color color = Color.FromKnownColor(Enum.Parse<KnownColor>(name));
            Assert.Equal(alpha, color.A);
            Assert.Equal(red, color.R);
            Assert.Equal(green, color.G);
            Assert.Equal(blue, color.B);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(KnownColor.MenuHighlight + 1)]
        public void FromOutOfRangeKnownColor(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Assert.Equal(0, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Theory, MemberData(nameof(AllKnownColors))]
        public void ToKnownColor(KnownColor known) => Assert.Equal(known, Color.FromKnownColor(known).ToKnownColor());

        [Theory, MemberData(nameof(AllKnownColors))]
        public void ToKnownColorMatchesButIsNotKnown(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Color match = Color.FromArgb(color.A, color.R, color.G, color.B);
            Assert.Equal((KnownColor)0, match.ToKnownColor());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(KnownColor.MenuHighlight + 1)]
        public void FromOutOfRangeKnownColorToKnownColor(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Assert.Equal((KnownColor)0, color.ToKnownColor());
        }

        [Fact]
        public void IsSystemColor()
        {
            Assert.True(Color.FromName("ActiveBorder").IsSystemColor);
            Assert.True(Color.FromName("WindowText").IsSystemColor);
            Assert.False(Color.FromName("AliceBlue").IsSystemColor);
        }

        [Theory, MemberData(nameof(SystemColors))]
        public void IsSystemColorTrue(KnownColor known) => Assert.True(Color.FromKnownColor(known).IsSystemColor);

        [Theory, MemberData(nameof(NonSystemColors))]
        public void IsSystemColorFalse(KnownColor known) => Assert.False(Color.FromKnownColor(known).IsSystemColor);

        [Theory, MemberData(nameof(SystemColors))]
        public void IsSystemColorFalseOnMatching(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Color match = Color.FromArgb(color.A, color.R, color.G, color.B);
            Assert.False(match.IsSystemColor);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(KnownColor.MenuHighlight + 1)]
        public void IsSystemColorOutOfRangeKnown(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Assert.False(color.IsSystemColor);
        }

        [Theory, MemberData(nameof(AllKnownColors))]
        public void IsKnownColorTrue(KnownColor known)
        {
            Assert.True(Color.FromKnownColor(known).IsKnownColor);
        }

        [Theory, MemberData(nameof(AllKnownColors))]
        public void IsKnownColorMatchFalse(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Color match = Color.FromArgb(color.A, color.R, color.G, color.B);
            Assert.False(match.IsKnownColor);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(KnownColor.MenuHighlight + 1)]
        public void IsKnownColorOutOfRangeKnown(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Assert.False(color.IsKnownColor);
        }

        [Fact]
        public void GetHashCodeForUnknownNamed()
        {
            // NetFX gives all such colors the same hash code. CoreFX makes more effort with them.
            Color c1 = Color.FromName("SomeUnknownColorName");
            Color c2 = Color.FromName("AnotherUnknownColorName");
            Assert.NotEqual(c2.GetHashCode(), c1.GetHashCode());
            Assert.Equal(c1.GetHashCode(), c1.GetHashCode());
        }
    }
}

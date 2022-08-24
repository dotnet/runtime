// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Primitives.Tests
{
    public class ColorTests
    {
        public static bool SupportsReadingUpdatedSystemColors => PlatformDetection.IsWindows && !PlatformDetection.IsInAppContainer && PlatformDetection.IsNotWindowsNanoNorServerCore;

        public static readonly IEnumerable<object[]> NamedArgbValues =
            new[]
            {
                new object[] {"Transparent", 0, 255, 255, 255},
                new object[] {"AliceBlue", 255, 240, 248, 255},
                new object[] {"AntiqueWhite", 255, 250, 235, 215},
                new object[] {"Aqua", 255, 0, 255, 255},
                new object[] {"Aquamarine", 255, 127, 255, 212},
                new object[] {"Azure", 255, 240, 255, 255},
                new object[] {"Beige", 255, 245, 245, 220},
                new object[] {"Bisque", 255, 255, 228, 196},
                new object[] {"Black", 255, 0, 0, 0},
                new object[] {"BlanchedAlmond", 255, 255, 235, 205},
                new object[] {"Blue", 255, 0, 0, 255},
                new object[] {"BlueViolet", 255, 138, 43, 226},
                new object[] {"Brown", 255, 165, 42, 42},
                new object[] {"BurlyWood", 255, 222, 184, 135},
                new object[] {"CadetBlue", 255, 95, 158, 160},
                new object[] {"Chartreuse", 255, 127, 255, 0},
                new object[] {"Chocolate", 255, 210, 105, 30},
                new object[] {"Coral", 255, 255, 127, 80},
                new object[] {"CornflowerBlue", 255, 100, 149, 237},
                new object[] {"Cornsilk", 255, 255, 248, 220},
                new object[] {"Crimson", 255, 220, 20, 60},
                new object[] {"Cyan", 255, 0, 255, 255},
                new object[] {"DarkBlue", 255, 0, 0, 139},
                new object[] {"DarkCyan", 255, 0, 139, 139},
                new object[] {"DarkGoldenrod", 255, 184, 134, 11},
                new object[] {"DarkGray", 255, 169, 169, 169},
                new object[] {"DarkGreen", 255, 0, 100, 0},
                new object[] {"DarkKhaki", 255, 189, 183, 107},
                new object[] {"DarkMagenta", 255, 139, 0, 139},
                new object[] {"DarkOliveGreen", 255, 85, 107, 47},
                new object[] {"DarkOrange", 255, 255, 140, 0},
                new object[] {"DarkOrchid", 255, 153, 50, 204},
                new object[] {"DarkRed", 255, 139, 0, 0},
                new object[] {"DarkSalmon", 255, 233, 150, 122},
                new object[] {"DarkSeaGreen", 255, 143, 188, 143},
                new object[] {"DarkSlateBlue", 255, 72, 61, 139},
                new object[] {"DarkSlateGray", 255, 47, 79, 79},
                new object[] {"DarkTurquoise", 255, 0, 206, 209},
                new object[] {"DarkViolet", 255, 148, 0, 211},
                new object[] {"DeepPink", 255, 255, 20, 147},
                new object[] {"DeepSkyBlue", 255, 0, 191, 255},
                new object[] {"DimGray", 255, 105, 105, 105},
                new object[] {"DodgerBlue", 255, 30, 144, 255},
                new object[] {"Firebrick", 255, 178, 34, 34},
                new object[] {"FloralWhite", 255, 255, 250, 240},
                new object[] {"ForestGreen", 255, 34, 139, 34},
                new object[] {"Fuchsia", 255, 255, 0, 255},
                new object[] {"Gainsboro", 255, 220, 220, 220},
                new object[] {"GhostWhite", 255, 248, 248, 255},
                new object[] {"Gold", 255, 255, 215, 0},
                new object[] {"Goldenrod", 255, 218, 165, 32},
                new object[] {"Gray", 255, 128, 128, 128},
                new object[] {"Green", 255, 0, 128, 0},
                new object[] {"GreenYellow", 255, 173, 255, 47},
                new object[] {"Honeydew", 255, 240, 255, 240},
                new object[] {"HotPink", 255, 255, 105, 180},
                new object[] {"IndianRed", 255, 205, 92, 92},
                new object[] {"Indigo", 255, 75, 0, 130},
                new object[] {"Ivory", 255, 255, 255, 240},
                new object[] {"Khaki", 255, 240, 230, 140},
                new object[] {"Lavender", 255, 230, 230, 250},
                new object[] {"LavenderBlush", 255, 255, 240, 245},
                new object[] {"LawnGreen", 255, 124, 252, 0},
                new object[] {"LemonChiffon", 255, 255, 250, 205},
                new object[] {"LightBlue", 255, 173, 216, 230},
                new object[] {"LightCoral", 255, 240, 128, 128},
                new object[] {"LightCyan", 255, 224, 255, 255},
                new object[] {"LightGoldenrodYellow", 255, 250, 250, 210},
                new object[] {"LightGreen", 255, 144, 238, 144},
                new object[] {"LightGray", 255, 211, 211, 211},
                new object[] {"LightPink", 255, 255, 182, 193},
                new object[] {"LightSalmon", 255, 255, 160, 122},
                new object[] {"LightSeaGreen", 255, 32, 178, 170},
                new object[] {"LightSkyBlue", 255, 135, 206, 250},
                new object[] {"LightSlateGray", 255, 119, 136, 153},
                new object[] {"LightSteelBlue", 255, 176, 196, 222},
                new object[] {"LightYellow", 255, 255, 255, 224},
                new object[] {"Lime", 255, 0, 255, 0},
                new object[] {"LimeGreen", 255, 50, 205, 50},
                new object[] {"Linen", 255, 250, 240, 230},
                new object[] {"Magenta", 255, 255, 0, 255},
                new object[] {"Maroon", 255, 128, 0, 0},
                new object[] {"MediumAquamarine", 255, 102, 205, 170},
                new object[] {"MediumBlue", 255, 0, 0, 205},
                new object[] {"MediumOrchid", 255, 186, 85, 211},
                new object[] {"MediumPurple", 255, 147, 112, 219},
                new object[] {"MediumSeaGreen", 255, 60, 179, 113},
                new object[] {"MediumSlateBlue", 255, 123, 104, 238},
                new object[] {"MediumSpringGreen", 255, 0, 250, 154},
                new object[] {"MediumTurquoise", 255, 72, 209, 204},
                new object[] {"MediumVioletRed", 255, 199, 21, 133},
                new object[] {"MidnightBlue", 255, 25, 25, 112},
                new object[] {"MintCream", 255, 245, 255, 250},
                new object[] {"MistyRose", 255, 255, 228, 225},
                new object[] {"Moccasin", 255, 255, 228, 181},
                new object[] {"NavajoWhite", 255, 255, 222, 173},
                new object[] {"Navy", 255, 0, 0, 128},
                new object[] {"OldLace", 255, 253, 245, 230},
                new object[] {"Olive", 255, 128, 128, 0},
                new object[] {"OliveDrab", 255, 107, 142, 35},
                new object[] {"Orange", 255, 255, 165, 0},
                new object[] {"OrangeRed", 255, 255, 69, 0},
                new object[] {"Orchid", 255, 218, 112, 214},
                new object[] {"PaleGoldenrod", 255, 238, 232, 170},
                new object[] {"PaleGreen", 255, 152, 251, 152},
                new object[] {"PaleTurquoise", 255, 175, 238, 238},
                new object[] {"PaleVioletRed", 255, 219, 112, 147},
                new object[] {"PapayaWhip", 255, 255, 239, 213},
                new object[] {"PeachPuff", 255, 255, 218, 185},
                new object[] {"Peru", 255, 205, 133, 63},
                new object[] {"Pink", 255, 255, 192, 203},
                new object[] {"Plum", 255, 221, 160, 221},
                new object[] {"PowderBlue", 255, 176, 224, 230},
                new object[] {"Purple", 255, 128, 0, 128},
                new object[] {"RebeccaPurple", 255, 102, 51, 153},
                new object[] {"Red", 255, 255, 0, 0},
                new object[] {"RosyBrown", 255, 188, 143, 143},
                new object[] {"RoyalBlue", 255, 65, 105, 225},
                new object[] {"SaddleBrown", 255, 139, 69, 19},
                new object[] {"Salmon", 255, 250, 128, 114},
                new object[] {"SandyBrown", 255, 244, 164, 96},
                new object[] {"SeaGreen", 255, 46, 139, 87},
                new object[] {"SeaShell", 255, 255, 245, 238},
                new object[] {"Sienna", 255, 160, 82, 45},
                new object[] {"Silver", 255, 192, 192, 192},
                new object[] {"SkyBlue", 255, 135, 206, 235},
                new object[] {"SlateBlue", 255, 106, 90, 205},
                new object[] {"SlateGray", 255, 112, 128, 144},
                new object[] {"Snow", 255, 255, 250, 250},
                new object[] {"SpringGreen", 255, 0, 255, 127},
                new object[] {"SteelBlue", 255, 70, 130, 180},
                new object[] {"Tan", 255, 210, 180, 140},
                new object[] {"Teal", 255, 0, 128, 128},
                new object[] {"Thistle", 255, 216, 191, 216},
                new object[] {"Tomato", 255, 255, 99, 71},
                new object[] {"Turquoise", 255, 64, 224, 208},
                new object[] {"Violet", 255, 238, 130, 238},
                new object[] {"Wheat", 255, 245, 222, 179},
                new object[] {"White", 255, 255, 255, 255},
                new object[] {"WhiteSmoke", 255, 245, 245, 245},
                new object[] {"Yellow", 255, 255, 255, 0},
                new object[] {"YellowGreen", 255, 154, 205, 50},
            };

        public static readonly IEnumerable<object[]> ColorNames = typeof(Color).GetProperties()
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => new object[] { p.Name })
                .ToArray();

        private Color? GetColorByProperty(string name)
        {
            return (Color?)typeof(Color).GetProperty(name)?.GetValue(null);
        }

        [Theory]
        [MemberData(nameof(NamedArgbValues))]
        public void ArgbValues(string name, int alpha, int red, int green, int blue)
        {
            Color? color = GetColorByProperty(name);
            if (color.HasValue)
            {
                Assert.Equal(alpha, color.Value.A);
                Assert.Equal(red, color.Value.R);
                Assert.Equal(green, color.Value.G);
                Assert.Equal(blue, color.Value.B);
            }
        }

        [Theory]
        [InlineData(255, 255, 255, 255)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(255, 0, 0, 0)]
        [InlineData(0, 255, 0, 0)]
        [InlineData(0, 0, 255, 0)]
        [InlineData(0, 0, 0, 255)]
        [InlineData(1, 2, 3, 4)]
        public void FromArgb_Roundtrips(int a, int r, int g, int b)
        {
            Color c1 = Color.FromArgb(unchecked((int)((uint)a << 24 | (uint)r << 16 | (uint)g << 8 | (uint)b)));
            Assert.Equal(a, c1.A);
            Assert.Equal(r, c1.R);
            Assert.Equal(g, c1.G);
            Assert.Equal(b, c1.B);

            Color c2 = Color.FromArgb(a, r, g, b);
            Assert.Equal(a, c2.A);
            Assert.Equal(r, c2.R);
            Assert.Equal(g, c2.G);
            Assert.Equal(b, c2.B);

            Color c3 = Color.FromArgb(r, g, b);
            Assert.Equal(255, c3.A);
            Assert.Equal(r, c3.R);
            Assert.Equal(g, c3.G);
            Assert.Equal(b, c3.B);
        }

        [Fact]
        public void Empty()
        {
            Assert.True(Color.Empty.IsEmpty);
            Assert.False(Color.FromArgb(0, Color.Black).IsEmpty);
        }

        [Fact]
        public void IsNamedColor()
        {
            Assert.True(Color.AliceBlue.IsNamedColor);
            Assert.True(Color.FromName("AliceBlue").IsNamedColor);
            Assert.False(Color.FromArgb(Color.AliceBlue.A, Color.AliceBlue.R, Color.AliceBlue.G, Color.AliceBlue.B).IsNamedColor);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void KnownNames(string name)
        {
            Assert.Equal(name, Color.FromName(name).Name);
            var colorByProperty = GetColorByProperty(name);
            if (colorByProperty.HasValue)
            {
                Assert.Equal(name, colorByProperty.Value.Name);
            }
        }

        [Fact]
        public void Name()
        {
            Assert.Equal("1122ccff", Color.FromArgb(0x11, 0x22, 0xcc, 0xff).Name);
        }

        public static IEnumerable<object[]> ColorNamePairs => ColorNames.Zip(ColorNames.Skip(1), (l, r) => new[] { l[0], r[0] });

        [Theory]
        [MemberData(nameof(ColorNamePairs))]
        public void GetHashCodeTest(string name1, string name2)
        {
            Assert.NotEqual(name1, name2);
            Color c1 = GetColorByProperty(name1) ?? Color.FromName(name1);
            Color c2 = GetColorByProperty(name2) ?? Color.FromName(name2);
            Assert.NotEqual(c2.GetHashCode(), c1.GetHashCode());
            Assert.Equal(c1.GetHashCode(), Color.FromName(name1).GetHashCode());
        }

        [Theory]
        [InlineData(0x11cc8833, 0x11, 0xcc, 0x88, 0x33)]
        [InlineData(unchecked((int)0xf1cc8833), 0xf1, 0xcc, 0x88, 0x33)]
        public void ToArgb(int argb, int alpha, int red, int green, int blue)
        {
            Assert.Equal(argb, Color.FromArgb(alpha, red, green, blue).ToArgb());
        }

        [Fact]
        public void ToStringEmpty()
        {
            Assert.Equal("Color [Empty]", Color.Empty.ToString());
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        [InlineData("SomeUnknownColorName")]
        public void ToStringNamed(string name)
        {
            string expected = $"Color [{name}]";
            Assert.Equal(expected, Color.FromName(name).ToString());
        }

        [Theory]
        [InlineData("Color [A=0, R=0, G=0, B=0]", 0, 0, 0, 0)]
        [InlineData("Color [A=1, R=2, G=3, B=4]", 1, 2, 3, 4)]
        [InlineData("Color [A=255, R=255, G=255, B=255]", 255, 255, 255, 255)]
        public void ToStringArgb(string expected, int alpha, int red, int green, int blue)
        {
            Assert.Equal(expected, Color.FromArgb(alpha, red, green, blue).ToString());
        }

        public static IEnumerable<object[]> InvalidValues =>
            new[]
            {
                new object[] {-1},
                new object[] {256},
            };

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void FromArgb_InvalidAlpha(int alpha)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(alpha, Color.Red);
            });
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(alpha, 0, 0, 0);
            });
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void FromArgb_InvalidRed(int red)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(red, 0, 0);
            });
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(0, red, 0, 0);
            });
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void FromArgb_InvalidGreen(int green)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(0, green, 0);
            });
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(0, 0, green, 0);
            });
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void FromArgb_InvalidBlue(int blue)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(0, 0, blue);
            });
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Color.FromArgb(0, 0, 0, blue);
            });
        }

        [Fact]
        public void FromName_Invalid()
        {
            Color c = Color.FromName("OingoBoingo");
            Assert.True(c.IsNamedColor);
            Assert.Equal(0, c.ToArgb());
            Assert.Equal("OingoBoingo", c.Name);
        }

        private void CheckRed(Color color)
        {
            Assert.Equal(255, color.A);
            Assert.Equal(255, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
            Assert.Equal("Red", color.Name);
            Assert.False(color.IsEmpty, "IsEmpty");
            Assert.True(color.IsNamedColor, "IsNamedColor");
        }

        [Theory]
        [InlineData(0, 0, 0, 0f)]
        [InlineData(255, 255, 255, 1f)]
        [InlineData(255, 0, 0, 0.5f)]
        [InlineData(0, 255, 0, 0.5f)]
        [InlineData(0, 0, 255, 0.5f)]
        [InlineData(255, 255, 0, 0.5f)]
        [InlineData(255, 0, 255, 0.5f)]
        [InlineData(0, 255, 255, 0.5f)]
        [InlineData(51, 255, 255, 0.6f)]
        [InlineData(255, 51, 255, 0.6f)]
        [InlineData(255, 255, 51, 0.6f)]
        [InlineData(255, 51, 51, 0.6f)]
        [InlineData(51, 255, 51, 0.6f)]
        [InlineData(51, 51, 255, 0.6f)]
        [InlineData(51, 51, 51, 0.2f)]
        [InlineData(0, 51, 255, 0.5f)]
        [InlineData(51, 255, 0, 0.5f)]
        [InlineData(0, 255, 51, 0.5f)]
        [InlineData(255, 0, 51, 0.5f)]
        [InlineData(51, 0, 255, 0.5f)]
        [InlineData(255, 51, 0, 0.5f)]
        public void GetBrightness(int r, int g, int b, float expected)
        {
            Assert.Equal(expected, Color.FromArgb(r, g, b).GetBrightness());
        }

        [Theory]
        [InlineData(0, 0, 0, 0f)]
        [InlineData(255, 255, 255, 0f)]
        [InlineData(255, 0, 0, 0f)]
        [InlineData(0, 255, 0, 120f)]
        [InlineData(0, 0, 255, 240f)]
        [InlineData(255, 255, 0, 60f)]
        [InlineData(255, 0, 255, 300f)]
        [InlineData(0, 255, 255, 180f)]
        [InlineData(51, 255, 255, 180f)]
        [InlineData(255, 51, 255, 300f)]
        [InlineData(255, 255, 51, 60f)]
        [InlineData(255, 51, 51, 0f)]
        [InlineData(51, 255, 51, 120f)]
        [InlineData(51, 51, 255, 240f)]
        [InlineData(51, 51, 51, 0f)]
        public void GetHue(int r, int g, int b, float expected)
        {
            Assert.Equal(expected, Color.FromArgb(r, g, b).GetHue());
        }

        [Theory]
        [InlineData(0, 0, 0, 0f)]
        [InlineData(255, 255, 255, 0f)]
        [InlineData(255, 0, 0, 1f)]
        [InlineData(0, 255, 0, 1f)]
        [InlineData(0, 0, 255, 1f)]
        [InlineData(255, 255, 0, 1f)]
        [InlineData(255, 0, 255, 1f)]
        [InlineData(0, 255, 255, 1f)]
        [InlineData(51, 255, 255, 1f)]
        [InlineData(255, 51, 255, 1f)]
        [InlineData(255, 255, 51, 1f)]
        [InlineData(255, 51, 51, 1f)]
        [InlineData(51, 255, 51, 1f)]
        [InlineData(51, 51, 255, 1f)]
        [InlineData(51, 51, 51, 0f)]
        [InlineData(204, 51, 51, 0.6f)]
        [InlineData(221, 221, 204, 0.2f)]
        public void GetSaturation(int r, int g, int b, float expected)
        {
            Assert.Equal(expected, Color.FromArgb(r, g, b).GetSaturation());
        }

        public static IEnumerable<object[]> Equality_MemberData()
        {
            yield return new object[] { Color.AliceBlue, Color.AliceBlue, true };
            yield return new object[] { Color.AliceBlue, Color.White, false};
            yield return new object[] { Color.AliceBlue, Color.Black, false };

            yield return new object[] { Color.FromArgb(255, 1, 2, 3), Color.FromArgb(255, 1, 2, 3), true };
            yield return new object[] { Color.FromArgb(255, 1, 2, 3), Color.FromArgb(1, 2, 3), true };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3), Color.FromArgb(255, 1, 2, 3), false };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3), Color.FromArgb(1, 2, 3), false };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3), Color.FromArgb(0, 0, 2, 3), false };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3), Color.FromArgb(0, 1, 0, 3), false };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3), Color.FromArgb(0, 1, 2, 0), false };

            yield return new object[] { Color.FromName("SomeName"), Color.FromName("SomeName"), true };
            yield return new object[] { Color.FromName("SomeName"), Color.FromName("SomeOtherName"), false };

            yield return new object[] { Color.FromArgb(0, 0, 0), default(Color), false };

            string someNameConstructed = string.Join("", "Some", "Name");
            Assert.NotSame("SomeName", someNameConstructed); // If this fails the above must be changed so this test is correct.
            yield return new object[] {Color.FromName("SomeName"), Color.FromName(someNameConstructed), true};
        }

        [Theory]
        [MemberData(nameof(Equality_MemberData))]
        public void Equality(Color left, Color right, bool expected)
        {
            Assert.True(left.Equals(left), "left should always Equals itself");
            Assert.True(right.Equals(right), "right should always Equals itself");

            Assert.True(left.Equals((object)left), "left should always Equals itself");
            Assert.True(right.Equals((object)right), "right should always Equals itself");

            Assert.Equal(expected, left == right);
            Assert.Equal(expected, right == left);

            Assert.Equal(expected, left.Equals(right));
            Assert.Equal(expected, right.Equals(left));

            Assert.Equal(expected, left.Equals((object)right));
            Assert.Equal(expected, right.Equals((object)left));

            Assert.Equal(!expected, left != right);
            Assert.Equal(!expected, right != left);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void DebuggerAttributesAreValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(Color.Aquamarine);
            DebuggerAttributes.ValidateDebuggerDisplayReferences(Color.FromArgb(4, 3, 2, 1));
        }

        [ConditionalFact(nameof(SupportsReadingUpdatedSystemColors))]
        public void UserPreferenceChangingEventTest()
        {
            int element = 12; // Win32SystemColors.AppWorkSpace.
            Color oldColor = System.Drawing.SystemColors.AppWorkspace;

            // A call to ToArgb is necessary before changing the system colors because it initializes the knownColorTable.
            int oldColorArgb = oldColor.ToArgb();
            int oldColorAbgr = GetColorRefValue(oldColor);

            Color newColor = oldColor != Color.Gold ? Color.Gold : Color.Silver;
            int newColorArgb = newColor.ToArgb();
            int newColorAbgr = GetColorRefValue(newColor);

            Assert.NotEqual(newColorArgb, oldColorArgb);

            try
            {
                Assert.Equal(1, SetSysColors(1, new int[] { element }, new int[] { newColorAbgr }));
                Assert.Equal(newColorArgb, oldColor.ToArgb());
            }
            finally
            {
                Assert.Equal(1, SetSysColors(1, new int[] { element }, new int[] { oldColorAbgr }));
            }
        }

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
        [InlineData((KnownColor)(-1))]
        [InlineData((KnownColor)0)]
        [InlineData(KnownColor.RebeccaPurple + 1)]
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
        [InlineData((KnownColor)(-1))]
        [InlineData((KnownColor)0)]
        [InlineData(KnownColor.RebeccaPurple + 1)]
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

        [Theory, MemberData(nameof(SystemKindKnownColorPairs))]
        public void SystemKindOrdering(bool isSystemColor, KnownColor known) =>
            Assert.Equal(isSystemColor, Color.FromKnownColor(known).IsSystemColor);

        [Theory]
        [InlineData((KnownColor)(-1))]
        [InlineData((KnownColor)0)]
        [InlineData(KnownColor.RebeccaPurple + 1)]
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
        [InlineData((KnownColor)(-1))]
        [InlineData((KnownColor)0)]
        [InlineData(KnownColor.RebeccaPurple + 1)]
        public void IsKnownColorOutOfRangeKnown(KnownColor known)
        {
            Color color = Color.FromKnownColor(known);
            Assert.False(color.IsKnownColor);
        }

        [Fact]
        public void GetHashCodeForUnknownNamed()
        {
            // The .NET Framework gives all unknown colors the same hashcode,
            // .NET Core will provide a unique hashcode.
            Color c1 = Color.FromName("SomeUnknownColorName");
            Color c2 = Color.FromName("AnotherUnknownColorName");
            Assert.NotEqual(c2.GetHashCode(), c1.GetHashCode());
            Assert.Equal(c1.GetHashCode(), c1.GetHashCode());
        }

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
                KnownColor.Wheat, KnownColor.White, KnownColor.WhiteSmoke, KnownColor.Yellow, KnownColor.YellowGreen,
                KnownColor.RebeccaPurple
            }.Select(kc => new object[] { kc }).ToArray();

        public static readonly IEnumerable<bool> SystemKindOrder =
            new[]
            {
                true,       // ActiveBorder
                true,       // ActiveCaption
                true,       // ActiveCaptionText
                true,       // AppWorkspace
                true,       // Control
                true,       // ControlDark
                true,       // ControlDarkDark
                true,       // ControlLight
                true,       // ControlLightLight
                true,       // ControlText
                true,       // Desktop
                true,       // GrayText
                true,       // Highlight
                true,       // HighlightText
                true,       // HotTrack
                true,       // InactiveBorder
                true,       // InactiveCaption
                true,       // InactiveCaptionText
                true,       // Info
                true,       // InfoText
                true,       // Menu
                true,       // MenuText
                true,       // ScrollBar
                true,       // Window
                true,       // WindowFrame
                true,       // WindowText
                false,      // Transparent
                false,      // AliceBlue
                false,      // AntiqueWhite
                false,      // Aqua
                false,      // Aquamarine
                false,      // Azure
                false,      // Beige
                false,      // Bisque
                false,      // Black
                false,      // BlanchedAlmond
                false,      // Blue
                false,      // BlueViolet
                false,      // Brown
                false,      // BurlyWood
                false,      // CadetBlue
                false,      // Chartreuse
                false,      // Chocolate
                false,      // Coral
                false,      // CornflowerBlue
                false,      // Cornsilk
                false,      // Crimson
                false,      // Cyan
                false,      // DarkBlue
                false,      // DarkCyan
                false,      // DarkGoldenrod
                false,      // DarkGray
                false,      // DarkGreen
                false,      // DarkKhaki
                false,      // DarkMagenta
                false,      // DarkOliveGreen
                false,      // DarkOrange
                false,      // DarkOrchid
                false,      // DarkRed
                false,      // DarkSalmon
                false,      // DarkSeaGreen
                false,      // DarkSlateBlue
                false,      // DarkSlateGray
                false,      // DarkTurquoise
                false,      // DarkViolet
                false,      // DeepPink
                false,      // DeepSkyBlue
                false,      // DimGray
                false,      // DodgerBlue
                false,      // Firebrick
                false,      // FloralWhite
                false,      // ForestGreen
                false,      // Fuchsia
                false,      // Gainsboro
                false,      // GhostWhite
                false,      // Gold
                false,      // Goldenrod
                false,      // Gray
                false,      // Green
                false,      // GreenYellow
                false,      // Honeydew
                false,      // HotPink
                false,      // IndianRed
                false,      // Indigo
                false,      // Ivory
                false,      // Khaki
                false,      // Lavender
                false,      // LavenderBlush
                false,      // LawnGreen
                false,      // LemonChiffon
                false,      // LightBlue
                false,      // LightCoral
                false,      // LightCyan
                false,      // LightGoldenrodYellow
                false,      // LightGray
                false,      // LightGreen
                false,      // LightPink
                false,      // LightSalmon
                false,      // LightSeaGreen
                false,      // LightSkyBlue
                false,      // LightSlateGray
                false,      // LightSteelBlue
                false,      // LightYellow
                false,      // Lime
                false,      // LimeGreen
                false,      // Linen
                false,      // Magenta
                false,      // Maroon
                false,      // MediumAquamarine
                false,      // MediumBlue
                false,      // MediumOrchid
                false,      // MediumPurple
                false,      // MediumSeaGreen
                false,      // MediumSlateBlue
                false,      // MediumSpringGreen
                false,      // MediumTurquoise
                false,      // MediumVioletRed
                false,      // MidnightBlue
                false,      // MintCream
                false,      // MistyRose
                false,      // Moccasin
                false,      // NavajoWhite
                false,      // Navy
                false,      // OldLace
                false,      // Olive
                false,      // OliveDrab
                false,      // Orange
                false,      // OrangeRed
                false,      // Orchid
                false,      // PaleGoldenrod
                false,      // PaleGreen
                false,      // PaleTurquoise
                false,      // PaleVioletRed
                false,      // PapayaWhip
                false,      // PeachPuff
                false,      // Peru
                false,      // Pink
                false,      // Plum
                false,      // PowderBlue
                false,      // Purple
                false,      // Red
                false,      // RosyBrown
                false,      // RoyalBlue
                false,      // SaddleBrown
                false,      // Salmon
                false,      // SandyBrown
                false,      // SeaGreen
                false,      // SeaShell
                false,      // Sienna
                false,      // Silver
                false,      // SkyBlue
                false,      // SlateBlue
                false,      // SlateGray
                false,      // Snow
                false,      // SpringGreen
                false,      // SteelBlue
                false,      // Tan
                false,      // Teal
                false,      // Thistle
                false,      // Tomato
                false,      // Turquoise
                false,      // Violet
                false,      // Wheat
                false,      // White
                false,      // WhiteSmoke
                false,      // Yellow
                false,      // YellowGreen
                true,       // ButtonFace
                true,       // ButtonHighlight
                true,       // ButtonShadow
                true,       // GradientActiveCaption
                true,       // GradientInactiveCaption
                true,       // MenuBar
                true,       // MenuHighlight
                false,      // RebeccaPurple
            };

        public static IEnumerable<object[]> SystemKindKnownColorPairs =>
            SystemKindOrder.Zip(AllKnownColors, (isSystemKind, color) => new[] { isSystemKind, color[0] });

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetSysColors(int cElements, int[] lpaElements, int[] lpaRgbValues);

        private static int GetColorRefValue(Color color)
        {
            // The COLORREF value has the following hexadecimal form: 0x00bbggrr.
            return color.B << 16 | color.G << 8 | color.R;
        }
    }
}

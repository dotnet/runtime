// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Colors.Tests
{
    public class ArgbTTests
    {
        [Fact]
        public void ArgbT_Default()
        {
            {
                Argb<byte> color = default;

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Argb<byte> color = new(default, default, default, default);

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Argb<byte> color = new([default, default, default, default]);

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Assert.Throws<ArgumentException>(() => new Argb<byte>(null));
                Assert.Throws<ArgumentException>(() => new Argb<byte>(default));
                Assert.Throws<ArgumentException>(() => new Argb<byte>([default, default, default]));
            }

            {
                Argb<float> color = default;

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Argb<float> color = new(default, default, default, default);

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Argb<float> color = new([default, default, default, default]);

                Assert.Equal(default, color.A);
                Assert.Equal(default, color.R);
                Assert.Equal(default, color.G);
                Assert.Equal(default, color.B);
            }
            {
                Assert.Throws<ArgumentException>(() => new Argb<float>(null));
                Assert.Throws<ArgumentException>(() => new Argb<float>(default));
                Assert.Throws<ArgumentException>(() => new Argb<float>([default, default, default]));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void ArgbT_CtorArgb(byte alpha, byte red, byte green, byte blue)
        {
            {
                var color = new Argb<byte>(alpha, red, green, blue);

                Assert.Equal(alpha, color.A);
                Assert.Equal(red, color.R);
                Assert.Equal(green, color.G);
                Assert.Equal(blue, color.B);
            }

            {
                var alphaF = alpha.FloatC(); var redF = red.FloatC(); var greenF = green.FloatC();
                var blueF = blue.FloatC();

                var color = new Argb<float>(alphaF, redF, greenF, blueF);

                Assert.Equal(alphaF, color.A);
                Assert.Equal(redF, color.R);
                Assert.Equal(greenF, color.G);
                Assert.Equal(blueF, color.B);
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void ArgbT_CtorSpan(byte alpha, byte red, byte green, byte blue)
        {
            {
                byte[] values = [default, alpha, red, green, blue, default];

                var color = new Argb<byte>(values.AsSpan(1, 4));

                Assert.Equal(alpha, color.A);
                Assert.Equal(red, color.R);
                Assert.Equal(green, color.G);
                Assert.Equal(blue, color.B);

                Assert.Throws<ArgumentException>(() => new Argb<byte>([red, green, blue]));
            }

            {
                var alphaF = alpha.FloatC(); var redF = red.FloatC(); var greenF = green.FloatC();
                var blueF = blue.FloatC();

                var color = new Argb<float>([alphaF, redF, greenF, blueF]);

                Assert.Equal(alphaF, color.A);
                Assert.Equal(redF, color.R);
                Assert.Equal(greenF, color.G);
                Assert.Equal(blueF, color.B);

                Assert.Throws<ArgumentException>(() => new Argb<float>([redF, greenF, blueF]));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void ArgbT_CopyTo(byte alpha, byte red, byte green, byte blue)
        {
            {
                var color = new Argb<byte>(alpha, red, green, blue);
                var destination = new byte[6];

                color.CopyTo(destination.AsSpan(1, 4));

                Assert.Equal(default, destination[0]);
                Assert.Equal(alpha, destination[1]);
                Assert.Equal(red, destination[2]);
                Assert.Equal(green, destination[3]);
                Assert.Equal(blue, destination[4]);
                Assert.Equal(default, destination[5]);

                Assert.Throws<ArgumentException>(() => color.CopyTo(null));
                Assert.Throws<ArgumentException>(() => color.CopyTo(default));
                Assert.Throws<ArgumentException>(() => color.CopyTo(destination.AsSpan(1, 3)));
            }

            {
                var alphaF = alpha.FloatC(); var redF = red.FloatC(); var greenF = green.FloatC();
                var blueF = blue.FloatC();
                var color = new Argb<float>(alphaF, redF, greenF, blueF);
                var destination = new float[6];

                color.CopyTo(destination.AsSpan(1, 4));

                Assert.Equal(default, destination[0]);
                Assert.Equal(alphaF, destination[1]);
                Assert.Equal(redF, destination[2]);
                Assert.Equal(greenF, destination[3]);
                Assert.Equal(blueF, destination[4]);
                Assert.Equal(default, destination[5]);

                Assert.Throws<ArgumentException>(() => color.CopyTo(null));
                Assert.Throws<ArgumentException>(() => color.CopyTo(default));
                Assert.Throws<ArgumentException>(() => color.CopyTo(destination.AsSpan(1, 3)));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColorsTwoTimes), MemberType = typeof(TestHelpers))]
        public void ArgbT_Equals((byte, byte, byte, byte) c1, (byte, byte, byte, byte) c2)
        {
            {
                var color1 = new Argb<byte>(c1.Item1, c1.Item2, c1.Item3, c1.Item4);
                var color2 = new Argb<byte>(c2.Item1, c2.Item2, c2.Item3, c2.Item4);
                var color3 = new Argb<byte>((byte)(c1.Item1 + 111), (byte)(c1.Item2 + 222), (byte)(c1.Item3 + 333),
                    (byte)(c1.Item4 + 444));

                if (c1 == c2)
                {
                    Assert.True(color1.Equals(color2));
                    Assert.True(color1.Equals((object)color2));
                    Assert.True(((object)color1).Equals(color2));

                    Assert.True(color2.Equals(color1));
                    Assert.True(color2.Equals((object)color1));
                    Assert.True(((object)color2).Equals(color1));
                }
                else
                {
                    Assert.False(color1.Equals(color2));
                    Assert.False(color1.Equals((object)color2));
                    Assert.False(((object)color1).Equals(color2));

                    Assert.False(color2.Equals(color1));
                    Assert.False(color2.Equals((object)color1));
                    Assert.False(((object)color2).Equals(color1));
                }

                Assert.True(color1.Equals(color1));
                Assert.True(color1.Equals((object)color1));

                Assert.False(color1.Equals(color1 with { A = color3.A }));
                Assert.False(color1.Equals((object)(color1 with { A = color3.A })));
                Assert.False(color1.Equals(color1 with { R = color3.R }));
                Assert.False(color1.Equals((object)(color1 with { R = color3.R })));
                Assert.False(color1.Equals(color1 with { G = color3.G }));
                Assert.False(color1.Equals((object)(color1 with { G = color3.G })));
                Assert.False(color1.Equals(color1 with { B = color3.B }));
                Assert.False(color1.Equals((object)(color1 with { B = color3.B })));

                Assert.False(color1.Equals(null));
            }

            {
                var color1 = new Argb<float>(c1.Item1.FloatC(), c1.Item2.FloatC(), c1.Item3.FloatC(), c1.Item4.FloatC());
                var color2 = new Argb<float>(c2.Item1.FloatC(), c2.Item2.FloatC(), c2.Item3.FloatC(), c2.Item4.FloatC());
                var color3 = new Argb<float>(((byte)(c1.Item1 + 111)).FloatC(), ((byte)(c1.Item2 + 222)).FloatC(),
                    ((byte)(c1.Item3 + 333)).FloatC(), ((byte)(c1.Item4 + 444)).FloatC());

                if (c1 == c2)
                {
                    Assert.True(color1.Equals(color2));
                    Assert.True(color1.Equals((object)color2));
                    Assert.True(((object)color1).Equals(color2));

                    Assert.True(color2.Equals(color1));
                    Assert.True(color2.Equals((object)color1));
                    Assert.True(((object)color2).Equals(color1));
                }
                else
                {
                    Assert.False(color1.Equals(color2));
                    Assert.False(color1.Equals((object)color2));
                    Assert.False(((object)color1).Equals(color2));

                    Assert.False(color2.Equals(color1));
                    Assert.False(color2.Equals((object)color1));
                    Assert.False(((object)color2).Equals(color1));
                }

                Assert.True(color1.Equals(color1));
                Assert.True(color1.Equals((object)color1));

                Assert.False(color1.Equals(color1 with { A = color3.A }));
                Assert.False(color1.Equals(color1 with { R = color3.R }));
                Assert.False(color1.Equals(color1 with { G = color3.G }));
                Assert.False(color1.Equals(color1 with { B = color3.B }));
                Assert.False(color1.Equals((object)(color1 with { A = color3.A })));
                Assert.False(color1.Equals((object)(color1 with { R = color3.R })));
                Assert.False(color1.Equals((object)(color1 with { G = color3.G })));
                Assert.False(color1.Equals((object)(color1 with { B = color3.B })));

                Assert.False(color1.Equals(null));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColorsTwoTimes), MemberType = typeof(TestHelpers))]
        public void ArgbT_GetHashCode((byte, byte, byte, byte) c1, (byte, byte, byte, byte) c2)
        {
            {
                var color1 = new Argb<byte>(c1.Item1, c1.Item2, c1.Item3, c1.Item4);
                var color2 = new Argb<byte>(c2.Item1, c2.Item2, c2.Item3, c2.Item4);
                var color3 = new Argb<byte>((byte)(c1.Item1 + 111), (byte)(c1.Item2 + 222), (byte)(c1.Item3 + 333),
                    (byte)(c1.Item4 + 444));

                if (c1 == c2)
                {
                    Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
                }
                else
                {
                    Assert.NotEqual(color1.GetHashCode(), color2.GetHashCode());
                }

                Assert.Equal(color1.GetHashCode(), color1.GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { A = color3.A }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { R = color3.R }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { G = color3.G }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { B = color3.B }).GetHashCode());
            }

            {
                var color1 = new Argb<float>(c1.Item1.FloatC(), c1.Item2.FloatC(), c1.Item3.FloatC(), c1.Item4.FloatC());
                var color2 = new Argb<float>(c2.Item1.FloatC(), c2.Item2.FloatC(), c2.Item3.FloatC(), c2.Item4.FloatC());
                var color3 = new Argb<float>(((byte)(c1.Item1 + 111)).FloatC(), ((byte)(c1.Item2 + 222)).FloatC(),
                    ((byte)(c1.Item3 + 333)).FloatC(), ((byte)(c1.Item4 + 444)).FloatC());

                if (c1 == c2)
                {
                    Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
                }
                else
                {
                    Assert.NotEqual(color1.GetHashCode(), color2.GetHashCode());
                }

                Assert.Equal(color1.GetHashCode(), color1.GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { A = color3.A }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { R = color3.R }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { G = color3.G }).GetHashCode());
                Assert.NotEqual(color1.GetHashCode(), (color1 with { B = color3.B }).GetHashCode());
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void ArgbT_ToString(byte alpha, byte red, byte green, byte blue)
        {
            {
                var color = new Argb<byte>(alpha, red, green, blue);

                Assert.Equal($"<{alpha}, {red}, {green}, {blue}>", color.ToString());
            }

            {
                var alphaF = alpha.FloatC(); var redF = red.FloatC(); var greenF = green.FloatC();
                var blueF = blue.FloatC();
                var color = new Argb<float>(alphaF, redF, greenF, blueF);

                Assert.Equal($"<{alphaF}, {redF}, {greenF}, {blueF}>", color.ToString());
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void ArgbT_ToRgba(byte alpha, byte red, byte green, byte blue)
        {
            {
                var color = new Argb<byte>(alpha, red, green, blue);
                var expected = new Rgba<byte>(red, green, blue, alpha);

                Assert.Equal(expected, color.ToRgba());
            }

            {
                var alphaF = alpha.FloatC(); var redF = red.FloatC(); var greenF = green.FloatC();
                var blueF = blue.FloatC();
                var color = new Argb<float>(alphaF, redF, greenF, blueF);
                var expected = new Rgba<float>(redF, greenF, blueF, alphaF);

                Assert.Equal(expected, color.ToRgba());
            }
        }
    }
}

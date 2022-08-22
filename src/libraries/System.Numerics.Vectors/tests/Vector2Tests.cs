// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public sealed class Vector2Tests
    {
        [Fact]
        public void Vector2MarshalSizeTest()
        {
            Assert.Equal(8, Marshal.SizeOf<Vector2>());
            Assert.Equal(8, Marshal.SizeOf<Vector2>(new Vector2()));
        }

        [Theory]
        [InlineData(0.0f, 1.0f)]
        [InlineData(1.0f, 0.0f)]
        [InlineData(3.1434343f, 1.1234123f)]
        [InlineData(1.0000001f, 0.0000001f)]
        public void Vector2IndexerGetTest(float x, float y)
        {
            var vector = new Vector2(x, y);

            Assert.Equal(x, vector[0]);
            Assert.Equal(y, vector[1]);
        }

        [Theory]
        [InlineData(0.0f, 1.0f)]
        [InlineData(1.0f, 0.0f)]
        [InlineData(3.1434343f, 1.1234123f)]
        [InlineData(1.0000001f, 0.0000001f)]
        public void Vector2IndexerSetTest(float x, float y)
        {
            var vector = new Vector2(0.0f, 0.0f);

            vector[0] = x;
            vector[1] = y;

            Assert.Equal(x, vector[0]);
            Assert.Equal(y, vector[1]);
        }

        [Fact]
        public void Vector2CopyToTest()
        {
            Vector2 v1 = new Vector2(2.0f, 3.0f);

            float[] a = new float[3];
            float[] b = new float[2];

            Assert.Throws<NullReferenceException>(() => v1.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => v1.CopyTo(a, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => v1.CopyTo(a, a.Length));
            Assert.Throws<ArgumentException>(() => v1.CopyTo(a, 2));

            v1.CopyTo(a, 1);
            v1.CopyTo(b);
            Assert.Equal(0.0, a[0]);
            Assert.Equal(2.0, a[1]);
            Assert.Equal(3.0, a[2]);
            Assert.Equal(2.0, b[0]);
            Assert.Equal(3.0, b[1]);
        }

        [Fact]
        public void Vector2CopyToSpanTest()
        {
            Vector2 vector = new Vector2(1.0f, 2.0f);
            Span<float> destination = new float[2];

            Assert.Throws<ArgumentException>(() => vector.CopyTo(new Span<float>(new float[1])));
            vector.CopyTo(destination);

            Assert.Equal(1.0f, vector.X);
            Assert.Equal(2.0f, vector.Y);
            Assert.Equal(vector.X, destination[0]);
            Assert.Equal(vector.Y, destination[1]);
        }

        [Fact]
        public void Vector2TryCopyToTest()
        {
            Vector2 vector = new Vector2(1.0f, 2.0f);
            Span<float> destination = new float[2];

            Assert.False(vector.TryCopyTo(new Span<float>(new float[1])));
            Assert.True(vector.TryCopyTo(destination));

            Assert.Equal(1.0f, vector.X);
            Assert.Equal(2.0f, vector.Y);
            Assert.Equal(vector.X, destination[0]);
            Assert.Equal(vector.Y, destination[1]);
        }

        [Fact]
        public void Vector2GetHashCodeTest()
        {
            Vector2 v1 = new Vector2(2.0f, 3.0f);
            Vector2 v2 = new Vector2(2.0f, 3.0f);
            Vector2 v3 = new Vector2(3.0f, 2.0f);
            Assert.Equal(v1.GetHashCode(), v1.GetHashCode());
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
            Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
            Vector2 v4 = new Vector2(0.0f, 0.0f);
            Vector2 v6 = new Vector2(1.0f, 0.0f);
            Vector2 v7 = new Vector2(0.0f, 1.0f);
            Vector2 v8 = new Vector2(1.0f, 1.0f);
            Assert.NotEqual(v4.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v4.GetHashCode(), v7.GetHashCode());
            Assert.NotEqual(v4.GetHashCode(), v8.GetHashCode());
            Assert.NotEqual(v7.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v8.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v8.GetHashCode(), v7.GetHashCode());
        }

        [Fact]
        public void Vector2ToStringTest()
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
            CultureInfo enUsCultureInfo = new CultureInfo("en-US");

            Vector2 v1 = new Vector2(2.0f, 3.0f);

            string v1str = v1.ToString();
            string expectedv1 = string.Format(CultureInfo.CurrentCulture
                , "<{1:G}{0} {2:G}>"
                , new object[] { separator, 2, 3 });
            Assert.Equal(expectedv1, v1str);

            string v1strformatted = v1.ToString("c", CultureInfo.CurrentCulture);
            string expectedv1formatted = string.Format(CultureInfo.CurrentCulture
                , "<{1:c}{0} {2:c}>"
                , new object[] { separator, 2, 3 });
            Assert.Equal(expectedv1formatted, v1strformatted);

            string v2strformatted = v1.ToString("c", enUsCultureInfo);
            string expectedv2formatted = string.Format(enUsCultureInfo
                , "<{1:c}{0} {2:c}>"
                , new object[] { enUsCultureInfo.NumberFormat.NumberGroupSeparator, 2, 3 });
            Assert.Equal(expectedv2formatted, v2strformatted);

            string v3strformatted = v1.ToString("c");
            string expectedv3formatted = string.Format(CultureInfo.CurrentCulture
                , "<{1:c}{0} {2:c}>"
                , new object[] { separator, 2, 3 });
            Assert.Equal(expectedv3formatted, v3strformatted);
        }

        // A test for Distance (Vector2f, Vector2f)
        [Fact]
        public void Vector2DistanceTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(3.0f, 4.0f);

            float expected = (float)System.Math.Sqrt(8);
            float actual;

            actual = Vector2.Distance(a, b);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Distance did not return the expected value.");
        }

        // A test for Distance (Vector2f, Vector2f)
        // Distance from the same point
        [Fact]
        public void Vector2DistanceTest2()
        {
            Vector2 a = new Vector2(1.051f, 2.05f);
            Vector2 b = new Vector2(1.051f, 2.05f);

            float actual = Vector2.Distance(a, b);
            Assert.Equal(0.0f, actual);
        }

        // A test for DistanceSquared (Vector2f, Vector2f)
        [Fact]
        public void Vector2DistanceSquaredTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(3.0f, 4.0f);

            float expected = 8.0f;
            float actual;

            actual = Vector2.DistanceSquared(a, b);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.DistanceSquared did not return the expected value.");
        }

        // A test for Dot (Vector2f, Vector2f)
        [Fact]
        public void Vector2DotTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(3.0f, 4.0f);

            float expected = 11.0f;
            float actual;

            actual = Vector2.Dot(a, b);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Dot did not return the expected value.");
        }

        // A test for Dot (Vector2f, Vector2f)
        // Dot test for perpendicular vector
        [Fact]
        public void Vector2DotTest1()
        {
            Vector2 a = new Vector2(1.55f, 1.55f);
            Vector2 b = new Vector2(-1.55f, 1.55f);

            float expected = 0.0f;
            float actual = Vector2.Dot(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Dot (Vector2f, Vector2f)
        // Dot test with specail float values
        [Fact]
        public void Vector2DotTest2()
        {
            Vector2 a = new Vector2(float.MinValue, float.MinValue);
            Vector2 b = new Vector2(float.MaxValue, float.MaxValue);

            float actual = Vector2.Dot(a, b);
            Assert.True(float.IsNegativeInfinity(actual), "Vector2f.Dot did not return the expected value.");
        }

        // A test for Length ()
        [Fact]
        public void Vector2LengthTest()
        {
            Vector2 a = new Vector2(2.0f, 4.0f);

            Vector2 target = a;

            float expected = (float)System.Math.Sqrt(20);
            float actual;

            actual = target.Length();

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Length did not return the expected value.");
        }

        // A test for Length ()
        // Length test where length is zero
        [Fact]
        public void Vector2LengthTest1()
        {
            Vector2 target = new Vector2();
            target.X = 0.0f;
            target.Y = 0.0f;

            float expected = 0.0f;
            float actual;

            actual = target.Length();

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Length did not return the expected value.");
        }

        // A test for LengthSquared ()
        [Fact]
        public void Vector2LengthSquaredTest()
        {
            Vector2 a = new Vector2(2.0f, 4.0f);

            Vector2 target = a;

            float expected = 20.0f;
            float actual;

            actual = target.LengthSquared();

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.LengthSquared did not return the expected value.");
        }

        // A test for LengthSquared ()
        // LengthSquared test where the result is zero
        [Fact]
        public void Vector2LengthSquaredTest1()
        {
            Vector2 a = new Vector2(0.0f, 0.0f);

            float expected = 0.0f;
            float actual = a.LengthSquared();

            Assert.Equal(expected, actual);
        }

        // A test for Min (Vector2f, Vector2f)
        [Fact]
        public void Vector2MinTest()
        {
            Vector2 a = new Vector2(-1.0f, 4.0f);
            Vector2 b = new Vector2(2.0f, 1.0f);

            Vector2 expected = new Vector2(-1.0f, 1.0f);
            Vector2 actual;
            actual = Vector2.Min(a, b);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Min did not return the expected value.");
        }

        [Fact]
        public void Vector2MinMaxCodeCoverageTest()
        {
            Vector2 min = new Vector2(0, 0);
            Vector2 max = new Vector2(1, 1);
            Vector2 actual;

            // Min.
            actual = Vector2.Min(min, max);
            Assert.Equal(actual, min);

            actual = Vector2.Min(max, min);
            Assert.Equal(actual, min);

            // Max.
            actual = Vector2.Max(min, max);
            Assert.Equal(actual, max);

            actual = Vector2.Max(max, min);
            Assert.Equal(actual, max);
        }

        // A test for Max (Vector2f, Vector2f)
        [Fact]
        public void Vector2MaxTest()
        {
            Vector2 a = new Vector2(-1.0f, 4.0f);
            Vector2 b = new Vector2(2.0f, 1.0f);

            Vector2 expected = new Vector2(2.0f, 4.0f);
            Vector2 actual;
            actual = Vector2.Max(a, b);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Max did not return the expected value.");
        }

        // A test for Clamp (Vector2f, Vector2f, Vector2f)
        [Fact]
        public void Vector2ClampTest()
        {
            Vector2 a = new Vector2(0.5f, 0.3f);
            Vector2 min = new Vector2(0.0f, 0.1f);
            Vector2 max = new Vector2(1.0f, 1.1f);

            // Normal case.
            // Case N1: specified value is in the range.
            Vector2 expected = new Vector2(0.5f, 0.3f);
            Vector2 actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");
            // Normal case.
            // Case N2: specified value is bigger than max value.
            a = new Vector2(2.0f, 3.0f);
            expected = max;
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");
            // Case N3: specified value is smaller than max value.
            a = new Vector2(-1.0f, -2.0f);
            expected = min;
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");
            // Case N4: combination case.
            a = new Vector2(-2.0f, 4.0f);
            expected = new Vector2(min.X, max.Y);
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");
            // User specified min value is bigger than max value.
            max = new Vector2(0.0f, 0.1f);
            min = new Vector2(1.0f, 1.1f);

            // Case W1: specified value is in the range.
            a = new Vector2(0.5f, 0.3f);
            expected = max;
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");

            // Normal case.
            // Case W2: specified value is bigger than max and min value.
            a = new Vector2(2.0f, 3.0f);
            expected = max;
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");

            // Case W3: specified value is smaller than min and max value.
            a = new Vector2(-1.0f, -2.0f);
            expected = max;
            actual = Vector2.Clamp(a, min, max);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Clamp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        [Fact]
        public void Vector2LerpTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(3.0f, 4.0f);

            float t = 0.5f;

            Vector2 expected = new Vector2(2.0f, 3.0f);
            Vector2 actual;
            actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with factor zero
        [Fact]
        public void Vector2LerpTest1()
        {
            Vector2 a = new Vector2(0.0f, 0.0f);
            Vector2 b = new Vector2(3.18f, 4.25f);

            float t = 0.0f;
            Vector2 expected = Vector2.Zero;
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with factor one
        [Fact]
        public void Vector2LerpTest2()
        {
            Vector2 a = new Vector2(0.0f, 0.0f);
            Vector2 b = new Vector2(3.18f, 4.25f);

            float t = 1.0f;
            Vector2 expected = new Vector2(3.18f, 4.25f);
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with factor > 1
        [Fact]
        public void Vector2LerpTest3()
        {
            Vector2 a = new Vector2(0.0f, 0.0f);
            Vector2 b = new Vector2(3.18f, 4.25f);

            float t = 2.0f;
            Vector2 expected = b * 2.0f;
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with factor < 0
        [Fact]
        public void Vector2LerpTest4()
        {
            Vector2 a = new Vector2(0.0f, 0.0f);
            Vector2 b = new Vector2(3.18f, 4.25f);

            float t = -2.0f;
            Vector2 expected = -(b * 2.0f);
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with special float value
        [Fact]
        public void Vector2LerpTest5()
        {
            Vector2 a = new Vector2(45.67f, 90.0f);
            Vector2 b = new Vector2(float.PositiveInfinity, float.NegativeInfinity);

            float t = 0.408f;
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(float.IsPositiveInfinity(actual.X), "Vector2f.Lerp did not return the expected value.");
            Assert.True(float.IsNegativeInfinity(actual.Y), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test from the same point
        [Fact]
        public void Vector2LerpTest6()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(1.0f, 2.0f);

            float t = 0.5f;

            Vector2 expected = new Vector2(1.0f, 2.0f);
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with values known to be inaccurate with the old lerp impl
        [Fact]
        public void Vector2LerpTest7()
        {
            Vector2 a = new Vector2(0.44728136f);
            Vector2 b = new Vector2(0.46345946f);

            float t = 0.26402435f;

            Vector2 expected = new Vector2(0.45155275f);
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Lerp (Vector2f, Vector2f, float)
        // Lerp test with values known to be inaccurate with the old lerp impl
        // (Old code incorrectly gets 0.33333588)
        [Fact]
        public void Vector2LerpTest8()
        {
            Vector2 a = new Vector2(-100);
            Vector2 b = new Vector2(0.33333334f);

            float t = 1f;

            Vector2 expected = new Vector2(0.33333334f);
            Vector2 actual = Vector2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Lerp did not return the expected value.");
        }

        // A test for Transform(Vector2f, Matrix4x4)
        [Fact]
        public void Vector2TransformTest()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));
            m.M41 = 10.0f;
            m.M42 = 20.0f;
            m.M43 = 30.0f;

            Vector2 expected = new Vector2(10.316987f, 22.183012f);
            Vector2 actual;

            actual = Vector2.Transform(v, m);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for Transform(Vector2f, Matrix3x2)
        [Fact]
        public void Vector2Transform3x2Test()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Matrix3x2 m = Matrix3x2.CreateRotation(MathHelper.ToRadians(30.0f));
            m.M31 = 10.0f;
            m.M32 = 20.0f;

            Vector2 expected = new Vector2(9.866025f, 22.23205f);
            Vector2 actual;

            actual = Vector2.Transform(v, m);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for TransformNormal (Vector2f, Matrix4x4)
        [Fact]
        public void Vector2TransformNormalTest()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));
            m.M41 = 10.0f;
            m.M42 = 20.0f;
            m.M43 = 30.0f;

            Vector2 expected = new Vector2(0.3169873f, 2.18301272f);
            Vector2 actual;

            actual = Vector2.TransformNormal(v, m);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for TransformNormal (Vector2f, Matrix3x2)
        [Fact]
        public void Vector2TransformNormal3x2Test()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Matrix3x2 m = Matrix3x2.CreateRotation(MathHelper.ToRadians(30.0f));
            m.M31 = 10.0f;
            m.M32 = 20.0f;

            Vector2 expected = new Vector2(-0.133974612f, 2.232051f);
            Vector2 actual;

            actual = Vector2.TransformNormal(v, m);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for Transform (Vector2f, Quaternion)
        [Fact]
        public void Vector2TransformByQuaternionTest()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);

            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));
            Quaternion q = Quaternion.CreateFromRotationMatrix(m);

            Vector2 expected = Vector2.Transform(v, m);
            Vector2 actual = Vector2.Transform(v, q);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for Transform (Vector2f, Quaternion)
        // Transform Vector2f with zero quaternion
        [Fact]
        public void Vector2TransformByQuaternionTest1()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Quaternion q = new Quaternion();
            Vector2 expected = v;

            Vector2 actual = Vector2.Transform(v, q);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for Transform (Vector2f, Quaternion)
        // Transform Vector2f with identity quaternion
        [Fact]
        public void Vector2TransformByQuaternionTest2()
        {
            Vector2 v = new Vector2(1.0f, 2.0f);
            Quaternion q = Quaternion.Identity;
            Vector2 expected = v;

            Vector2 actual = Vector2.Transform(v, q);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Transform did not return the expected value.");
        }

        // A test for Normalize (Vector2f)
        [Fact]
        public void Vector2NormalizeTest()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);
            Vector2 expected = new Vector2(0.554700196225229122018341733457f, 0.8320502943378436830275126001855f);
            Vector2 actual;

            actual = Vector2.Normalize(a);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Normalize did not return the expected value.");
        }

        // A test for Normalize (Vector2f)
        // Normalize zero length vector
        [Fact]
        public void Vector2NormalizeTest1()
        {
            Vector2 a = new Vector2(); // no parameter, default to 0.0f
            Vector2 actual = Vector2.Normalize(a);
            Assert.True(float.IsNaN(actual.X) && float.IsNaN(actual.Y), "Vector2f.Normalize did not return the expected value.");
        }

        // A test for Normalize (Vector2f)
        // Normalize infinite length vector
        [Fact]
        public void Vector2NormalizeTest2()
        {
            Vector2 a = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 actual = Vector2.Normalize(a);
            Vector2 expected = new Vector2(0, 0);
            Assert.Equal(expected, actual);
        }

        // A test for operator - (Vector2f)
        [Fact]
        public void Vector2UnaryNegationTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);

            Vector2 expected = new Vector2(-1.0f, -2.0f);
            Vector2 actual;

            actual = -a;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator - did not return the expected value.");
        }



        // A test for operator - (Vector2f)
        // Negate test with special float value
        [Fact]
        public void Vector2UnaryNegationTest1()
        {
            Vector2 a = new Vector2(float.PositiveInfinity, float.NegativeInfinity);

            Vector2 actual = -a;

            Assert.True(float.IsNegativeInfinity(actual.X), "Vector2f.operator - did not return the expected value.");
            Assert.True(float.IsPositiveInfinity(actual.Y), "Vector2f.operator - did not return the expected value.");
        }

        // A test for operator - (Vector2f)
        // Negate test with special float value
        [Fact]
        public void Vector2UnaryNegationTest2()
        {
            Vector2 a = new Vector2(float.NaN, 0.0f);
            Vector2 actual = -a;

            Assert.True(float.IsNaN(actual.X), "Vector2f.operator - did not return the expected value.");
            Assert.True(float.Equals(0.0f, actual.Y), "Vector2f.operator - did not return the expected value.");
        }

        // A test for operator - (Vector2f, Vector2f)
        [Fact]
        public void Vector2SubtractionTest()
        {
            Vector2 a = new Vector2(1.0f, 3.0f);
            Vector2 b = new Vector2(2.0f, 1.5f);

            Vector2 expected = new Vector2(-1.0f, 1.5f);
            Vector2 actual;

            actual = a - b;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator - did not return the expected value.");
        }

        // A test for operator * (Vector2f, float)
        [Fact]
        public void Vector2MultiplyOperatorTest()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);
            const float factor = 2.0f;

            Vector2 expected = new Vector2(4.0f, 6.0f);
            Vector2 actual;

            actual = a * factor;
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator * did not return the expected value.");
        }

        // A test for operator * (float, Vector2f)
        [Fact]
        public void Vector2MultiplyOperatorTest2()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);
            const float factor = 2.0f;

            Vector2 expected = new Vector2(4.0f, 6.0f);
            Vector2 actual;

            actual = factor * a;
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator * did not return the expected value.");
        }

        // A test for operator * (Vector2f, Vector2f)
        [Fact]
        public void Vector2MultiplyOperatorTest3()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);
            Vector2 b = new Vector2(4.0f, 5.0f);

            Vector2 expected = new Vector2(8.0f, 15.0f);
            Vector2 actual;

            actual = a * b;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator * did not return the expected value.");
        }

        // A test for operator / (Vector2f, float)
        [Fact]
        public void Vector2DivisionTest()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);

            float div = 2.0f;

            Vector2 expected = new Vector2(1.0f, 1.5f);
            Vector2 actual;

            actual = a / div;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator / did not return the expected value.");
        }

        // A test for operator / (Vector2f, Vector2f)
        [Fact]
        public void Vector2DivisionTest1()
        {
            Vector2 a = new Vector2(2.0f, 3.0f);
            Vector2 b = new Vector2(4.0f, 5.0f);

            Vector2 expected = new Vector2(2.0f / 4.0f, 3.0f / 5.0f);
            Vector2 actual;

            actual = a / b;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator / did not return the expected value.");
        }

        // A test for operator / (Vector2f, float)
        // Divide by zero
        [Fact]
        public void Vector2DivisionTest2()
        {
            Vector2 a = new Vector2(-2.0f, 3.0f);

            float div = 0.0f;

            Vector2 actual = a / div;

            Assert.True(float.IsNegativeInfinity(actual.X), "Vector2f.operator / did not return the expected value.");
            Assert.True(float.IsPositiveInfinity(actual.Y), "Vector2f.operator / did not return the expected value.");
        }

        // A test for operator / (Vector2f, Vector2f)
        // Divide by zero
        [Fact]
        public void Vector2DivisionTest3()
        {
            Vector2 a = new Vector2(0.047f, -3.0f);
            Vector2 b = new Vector2();

            Vector2 actual = a / b;

            Assert.True(float.IsInfinity(actual.X), "Vector2f.operator / did not return the expected value.");
            Assert.True(float.IsInfinity(actual.Y), "Vector2f.operator / did not return the expected value.");
        }

        // A test for operator + (Vector2f, Vector2f)
        [Fact]
        public void Vector2AdditionTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(3.0f, 4.0f);

            Vector2 expected = new Vector2(4.0f, 6.0f);
            Vector2 actual;

            actual = a + b;

            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.operator + did not return the expected value.");
        }

        // A test for Vector2f (float, float)
        [Fact]
        public void Vector2ConstructorTest()
        {
            float x = 1.0f;
            float y = 2.0f;

            Vector2 target = new Vector2(x, y);
            Assert.True(MathHelper.Equal(target.X, x) && MathHelper.Equal(target.Y, y), "Vector2f(x,y) constructor did not return the expected value.");
        }

        // A test for Vector2f ()
        // Constructor with no parameter
        [Fact]
        public void Vector2ConstructorTest2()
        {
            Vector2 target = new Vector2();
            Assert.Equal(0.0f, target.X);
            Assert.Equal(0.0f, target.Y);
        }

        // A test for Vector2f (float, float)
        // Constructor with special floating values
        [Fact]
        public void Vector2ConstructorTest3()
        {
            Vector2 target = new Vector2(float.NaN, float.MaxValue);
            Assert.Equal(float.NaN, target.X);
            Assert.Equal(float.MaxValue, target.Y);
        }

        // A test for Vector2f (float)
        [Fact]
        public void Vector2ConstructorTest4()
        {
            float value = 1.0f;
            Vector2 target = new Vector2(value);

            Vector2 expected = new Vector2(value, value);
            Assert.Equal(expected, target);

            value = 2.0f;
            target = new Vector2(value);
            expected = new Vector2(value, value);
            Assert.Equal(expected, target);
        }

        // A test for Vector2f (ReadOnlySpan<float>)
        [Fact]
        public void Vector2ConstructorTest5()
        {
            float value = 1.0f;
            Vector2 target = new Vector2(new[] { value, value });
            Vector2 expected = new Vector2(value);

            Assert.Equal(expected, target);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vector2(new float[1]));
        }

        // A test for Add (Vector2f, Vector2f)
        [Fact]
        public void Vector2AddTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(5.0f, 6.0f);

            Vector2 expected = new Vector2(6.0f, 8.0f);
            Vector2 actual;

            actual = Vector2.Add(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Divide (Vector2f, float)
        [Fact]
        public void Vector2DivideTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            float div = 2.0f;
            Vector2 expected = new Vector2(0.5f, 1.0f);
            Vector2 actual;
            actual = Vector2.Divide(a, div);
            Assert.Equal(expected, actual);
        }

        // A test for Divide (Vector2f, Vector2f)
        [Fact]
        public void Vector2DivideTest1()
        {
            Vector2 a = new Vector2(1.0f, 6.0f);
            Vector2 b = new Vector2(5.0f, 2.0f);

            Vector2 expected = new Vector2(1.0f / 5.0f, 6.0f / 2.0f);
            Vector2 actual;

            actual = Vector2.Divide(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Equals (object)
        [Fact]
        public void Vector2EqualsTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(1.0f, 2.0f);

            // case 1: compare between same values
            object obj = b;

            bool expected = true;
            bool actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.X = 10.0f;
            obj = b;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare between different types.
            obj = new Quaternion();
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare against null.
            obj = null;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Vector2f, float)
        [Fact]
        public void Vector2MultiplyTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            const float factor = 2.0f;
            Vector2 expected = new Vector2(2.0f, 4.0f);
            Vector2 actual = Vector2.Multiply(a, factor);
            Assert.Equal(expected, actual);
        }

        // A test for Multiply (float, Vector2f)
        [Fact]
        public void Vector2MultiplyTest2()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            const float factor = 2.0f;
            Vector2 expected = new Vector2(2.0f, 4.0f);
            Vector2 actual = Vector2.Multiply(factor, a);
            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Vector2f, Vector2f)
        [Fact]
        public void Vector2MultiplyTest3()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(5.0f, 6.0f);

            Vector2 expected = new Vector2(5.0f, 12.0f);
            Vector2 actual;

            actual = Vector2.Multiply(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Negate (Vector2f)
        [Fact]
        public void Vector2NegateTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);

            Vector2 expected = new Vector2(-1.0f, -2.0f);
            Vector2 actual;

            actual = Vector2.Negate(a);
            Assert.Equal(expected, actual);
        }

        // A test for operator != (Vector2f, Vector2f)
        [Fact]
        public void Vector2InequalityTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(1.0f, 2.0f);

            // case 1: compare between same values
            bool expected = false;
            bool actual = a != b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.X = 10.0f;
            expected = true;
            actual = a != b;
            Assert.Equal(expected, actual);
        }

        // A test for operator == (Vector2f, Vector2f)
        [Fact]
        public void Vector2EqualityTest()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(1.0f, 2.0f);

            // case 1: compare between same values
            bool expected = true;
            bool actual = a == b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.X = 10.0f;
            expected = false;
            actual = a == b;
            Assert.Equal(expected, actual);
        }

        // A test for Subtract (Vector2f, Vector2f)
        [Fact]
        public void Vector2SubtractTest()
        {
            Vector2 a = new Vector2(1.0f, 6.0f);
            Vector2 b = new Vector2(5.0f, 2.0f);

            Vector2 expected = new Vector2(-4.0f, 4.0f);
            Vector2 actual;

            actual = Vector2.Subtract(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for UnitX
        [Fact]
        public void Vector2UnitXTest()
        {
            Vector2 val = new Vector2(1.0f, 0.0f);
            Assert.Equal(val, Vector2.UnitX);
        }

        // A test for UnitY
        [Fact]
        public void Vector2UnitYTest()
        {
            Vector2 val = new Vector2(0.0f, 1.0f);
            Assert.Equal(val, Vector2.UnitY);
        }

        // A test for One
        [Fact]
        public void Vector2OneTest()
        {
            Vector2 val = new Vector2(1.0f, 1.0f);
            Assert.Equal(val, Vector2.One);
        }

        // A test for Zero
        [Fact]
        public void Vector2ZeroTest()
        {
            Vector2 val = new Vector2(0.0f, 0.0f);
            Assert.Equal(val, Vector2.Zero);
        }

        // A test for Equals (Vector2f)
        [Fact]
        public void Vector2EqualsTest1()
        {
            Vector2 a = new Vector2(1.0f, 2.0f);
            Vector2 b = new Vector2(1.0f, 2.0f);

            // case 1: compare between same values
            bool expected = true;
            bool actual = a.Equals(b);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.X = 10.0f;
            expected = false;
            actual = a.Equals(b);
            Assert.Equal(expected, actual);
        }

        // A test for Vector2f comparison involving NaN values
        [Fact]
        public void Vector2EqualsNaNTest()
        {
            Vector2 a = new Vector2(float.NaN, 0);
            Vector2 b = new Vector2(0, float.NaN);

            Assert.False(a == Vector2.Zero);
            Assert.False(b == Vector2.Zero);

            Assert.True(a != Vector2.Zero);
            Assert.True(b != Vector2.Zero);

            Assert.False(a.Equals(Vector2.Zero));
            Assert.False(b.Equals(Vector2.Zero));

            Assert.True(a.Equals(a));
            Assert.True(b.Equals(b));
        }

        // A test for Reflect (Vector2f, Vector2f)
        [Fact]
        public void Vector2ReflectTest()
        {
            Vector2 a = Vector2.Normalize(new Vector2(1.0f, 1.0f));

            // Reflect on XZ plane.
            Vector2 n = new Vector2(0.0f, 1.0f);
            Vector2 expected = new Vector2(a.X, -a.Y);
            Vector2 actual = Vector2.Reflect(a, n);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Reflect did not return the expected value.");

            // Reflect on XY plane.
            n = new Vector2(0.0f, 0.0f);
            expected = new Vector2(a.X, a.Y);
            actual = Vector2.Reflect(a, n);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Reflect did not return the expected value.");

            // Reflect on YZ plane.
            n = new Vector2(1.0f, 0.0f);
            expected = new Vector2(-a.X, a.Y);
            actual = Vector2.Reflect(a, n);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Reflect did not return the expected value.");
        }

        // A test for Reflect (Vector2f, Vector2f)
        // Reflection when normal and source are the same
        [Fact]
        public void Vector2ReflectTest1()
        {
            Vector2 n = new Vector2(0.45f, 1.28f);
            n = Vector2.Normalize(n);
            Vector2 a = n;

            Vector2 expected = -n;
            Vector2 actual = Vector2.Reflect(a, n);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Reflect did not return the expected value.");
        }

        // A test for Reflect (Vector2f, Vector2f)
        // Reflection when normal and source are negation
        [Fact]
        public void Vector2ReflectTest2()
        {
            Vector2 n = new Vector2(0.45f, 1.28f);
            n = Vector2.Normalize(n);
            Vector2 a = -n;

            Vector2 expected = n;
            Vector2 actual = Vector2.Reflect(a, n);
            Assert.True(MathHelper.Equal(expected, actual), "Vector2f.Reflect did not return the expected value.");
        }

        [Fact]
        public void Vector2AbsTest()
        {
            Vector2 v1 = new Vector2(-2.5f, 2.0f);
            Vector2 v3 = Vector2.Abs(new Vector2(0.0f, float.NegativeInfinity));
            Vector2 v = Vector2.Abs(v1);
            Assert.Equal(2.5f, v.X);
            Assert.Equal(2.0f, v.Y);
            Assert.Equal(0.0f, v3.X);
            Assert.Equal(float.PositiveInfinity, v3.Y);
        }

        [Fact]
        public void Vector2SqrtTest()
        {
            Vector2 v1 = new Vector2(-2.5f, 2.0f);
            Vector2 v2 = new Vector2(5.5f, 4.5f);
            Assert.Equal(2, (int)Vector2.SquareRoot(v2).X);
            Assert.Equal(2, (int)Vector2.SquareRoot(v2).Y);
            Assert.Equal(float.NaN, Vector2.SquareRoot(v1).X);
        }

        // A test to make sure these types are blittable directly into GPU buffer memory layouts
        [Fact]
        public unsafe void Vector2SizeofTest()
        {
            Assert.Equal(8, sizeof(Vector2));
            Assert.Equal(16, sizeof(Vector2_2x));
            Assert.Equal(12, sizeof(Vector2PlusFloat));
            Assert.Equal(24, sizeof(Vector2PlusFloat_2x));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Vector2_2x
        {
            private Vector2 _a;
            private Vector2 _b;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Vector2PlusFloat
        {
            private Vector2 _v;
            private float _f;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Vector2PlusFloat_2x
        {
            private Vector2PlusFloat _a;
            private Vector2PlusFloat _b;
        }

        [Fact]
        public void SetFieldsTest()
        {
            Vector2 v3 = new Vector2(4f, 5f);
            v3.X = 1.0f;
            v3.Y = 2.0f;
            Assert.Equal(1.0f, v3.X);
            Assert.Equal(2.0f, v3.Y);
            Vector2 v4 = v3;
            v4.Y = 0.5f;
            Assert.Equal(1.0f, v4.X);
            Assert.Equal(0.5f, v4.Y);
            Assert.Equal(2.0f, v3.Y);
        }

        [Fact]
        public void EmbeddedVectorSetFields()
        {
            EmbeddedVectorObject evo = new EmbeddedVectorObject();
            evo.FieldVector.X = 5.0f;
            evo.FieldVector.Y = 5.0f;
            Assert.Equal(5.0f, evo.FieldVector.X);
            Assert.Equal(5.0f, evo.FieldVector.Y);
        }

        private class EmbeddedVectorObject
        {
            public Vector2 FieldVector;
        }
    }
}

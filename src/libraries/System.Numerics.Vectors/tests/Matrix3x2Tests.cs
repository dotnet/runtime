// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public class Matrix3x2Tests
    {
        static Matrix3x2 GenerateIncrementalMatrixNumber(float value = 0.0f)
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = value + 1.0f;
            a.M12 = value + 2.0f;
            a.M21 = value + 3.0f;
            a.M22 = value + 4.0f;
            a.M31 = value + 5.0f;
            a.M32 = value + 6.0f;
            return a;
        }

        static Matrix3x2 GenerateTestMatrix()
        {
            Matrix3x2 m = Matrix3x2.CreateRotation(MathHelper.ToRadians(30.0f));
            m.Translation = new Vector2(111.0f, 222.0f);
            return m;
        }

        // A test for Identity
        [Fact]
        public void Matrix3x2IdentityTest()
        {
            Matrix3x2 val = new Matrix3x2();
            val.M11 = val.M22 = 1.0f;

            Assert.True(MathHelper.Equal(val, Matrix3x2.Identity), "Matrix3x2.Indentity was not set correctly.");
        }

        // A test for Determinant
        [Fact]
        public void Matrix3x2DeterminantTest()
        {
            Matrix3x2 target = Matrix3x2.CreateRotation(MathHelper.ToRadians(30.0f));

            float val = 1.0f;
            float det = target.GetDeterminant();

            Assert.True(MathHelper.Equal(val, det), "Matrix3x2.Determinant was not set correctly.");
        }

        // A test for Determinant
        // Determinant test |A| = 1 / |A'|
        [Fact]
        public void Matrix3x2DeterminantTest1()
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = 5.0f;
            a.M12 = 2.0f;
            a.M21 = 12.0f;
            a.M22 = 6.8f;
            a.M31 = 6.5f;
            a.M32 = 1.0f;
            Matrix3x2 i;
            Assert.True(Matrix3x2.Invert(a, out i));

            float detA = a.GetDeterminant();
            float detI = i.GetDeterminant();
            float t = 1.0f / detI;

            // only accurate to 3 precision
            Assert.True(System.Math.Abs(detA - t) < 1e-3, "Matrix3x2.Determinant was not set correctly.");

            // sanity check against 4x4 version
            Assert.Equal(new Matrix4x4(a).GetDeterminant(), detA);
            Assert.Equal(new Matrix4x4(i).GetDeterminant(), detI);
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertTest()
        {
            Matrix3x2 mtx = Matrix3x2.CreateRotation(MathHelper.ToRadians(30.0f));

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = 0.8660254f;
            expected.M12 = -0.5f;

            expected.M21 = 0.5f;
            expected.M22 = 0.8660254f;

            expected.M31 = 0;
            expected.M32 = 0;

            Matrix3x2 actual;

            Assert.True(Matrix3x2.Invert(mtx, out actual));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.Invert did not return the expected value.");

            Matrix3x2 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix3x2.Identity), "Matrix3x2.Invert did not return the expected value.");
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertIdentityTest()
        {
            Matrix3x2 mtx = Matrix3x2.Identity;

            Matrix3x2 actual;
            Assert.True(Matrix3x2.Invert(mtx, out actual));

            Assert.True(MathHelper.Equal(actual, Matrix3x2.Identity));
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertTranslationTest()
        {
            Matrix3x2 mtx = Matrix3x2.CreateTranslation(23, 42);

            Matrix3x2 actual;
            Assert.True(Matrix3x2.Invert(mtx, out actual));

            Matrix3x2 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix3x2.Identity));
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertRotationTest()
        {
            Matrix3x2 mtx = Matrix3x2.CreateRotation(2);

            Matrix3x2 actual;
            Assert.True(Matrix3x2.Invert(mtx, out actual));

            Matrix3x2 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix3x2.Identity));
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertScaleTest()
        {
            Matrix3x2 mtx = Matrix3x2.CreateScale(23, -42);

            Matrix3x2 actual;
            Assert.True(Matrix3x2.Invert(mtx, out actual));

            Matrix3x2 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix3x2.Identity));
        }

        // A test for Invert (Matrix3x2)
        [Fact]
        public void Matrix3x2InvertAffineTest()
        {
            Matrix3x2 mtx = Matrix3x2.CreateRotation(2) *
                            Matrix3x2.CreateScale(23, -42) *
                            Matrix3x2.CreateTranslation(17, 53);

            Matrix3x2 actual;
            Assert.True(Matrix3x2.Invert(mtx, out actual));

            Matrix3x2 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix3x2.Identity));
        }

        // A test for CreateRotation (float)
        [Fact]
        public void Matrix3x2CreateRotationTest()
        {
            float radians = MathHelper.ToRadians(50.0f);

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = 0.642787635f;
            expected.M12 = 0.766044438f;
            expected.M21 = -0.766044438f;
            expected.M22 = 0.642787635f;

            Matrix3x2 actual;
            actual = Matrix3x2.CreateRotation(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.CreateRotation did not return the expected value.");
        }

        // A test for CreateRotation (float, Vector2f)
        [Fact]
        public void Matrix3x2CreateRotationCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector2 center = new Vector2(23, 42);

            Matrix3x2 rotateAroundZero = Matrix3x2.CreateRotation(radians, Vector2.Zero);
            Matrix3x2 rotateAroundZeroExpected = Matrix3x2.CreateRotation(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix3x2 rotateAroundCenter = Matrix3x2.CreateRotation(radians, center);
            Matrix3x2 rotateAroundCenterExpected = Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateRotation(radians) * Matrix3x2.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CreateRotation (float)
        [Fact]
        public void Matrix3x2CreateRotationRightAngleTest()
        {
            // 90 degree rotations must be exact!
            Matrix3x2 actual = Matrix3x2.CreateRotation(0);
            Assert.Equal(new Matrix3x2(1, 0, 0, 1, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi / 2);
            Assert.Equal(new Matrix3x2(0, 1, -1, 0, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi);
            Assert.Equal(new Matrix3x2(-1, 0, 0, -1, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 3 / 2);
            Assert.Equal(new Matrix3x2(0, -1, 1, 0, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 2);
            Assert.Equal(new Matrix3x2(1, 0, 0, 1, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 5 / 2);
            Assert.Equal(new Matrix3x2(0, 1, -1, 0, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(-MathHelper.Pi / 2);
            Assert.Equal(new Matrix3x2(0, -1, 1, 0, 0, 0), actual);

            // But merely close-to-90 rotations should not be excessively clamped.
            float delta = MathHelper.ToRadians(0.01f);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi + delta);
            Assert.False(MathHelper.Equal(new Matrix3x2(-1, 0, 0, -1, 0, 0), actual));

            actual = Matrix3x2.CreateRotation(MathHelper.Pi - delta);
            Assert.False(MathHelper.Equal(new Matrix3x2(-1, 0, 0, -1, 0, 0), actual));
        }

        // A test for CreateRotation (float, Vector2f)
        [Fact]
        public void Matrix3x2CreateRotationRightAngleCenterTest()
        {
            Vector2 center = new Vector2(3, 7);

            // 90 degree rotations must be exact!
            Matrix3x2 actual = Matrix3x2.CreateRotation(0, center);
            Assert.Equal(new Matrix3x2(1, 0, 0, 1, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi / 2, center);
            Assert.Equal(new Matrix3x2(0, 1, -1, 0, 10, 4), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi, center);
            Assert.Equal(new Matrix3x2(-1, 0, 0, -1, 6, 14), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 3 / 2, center);
            Assert.Equal(new Matrix3x2(0, -1, 1, 0, -4, 10), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 2, center);
            Assert.Equal(new Matrix3x2(1, 0, 0, 1, 0, 0), actual);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi * 5 / 2, center);
            Assert.Equal(new Matrix3x2(0, 1, -1, 0, 10, 4), actual);

            actual = Matrix3x2.CreateRotation(-MathHelper.Pi / 2, center);
            Assert.Equal(new Matrix3x2(0, -1, 1, 0, -4, 10), actual);

            // But merely close-to-90 rotations should not be excessively clamped.
            float delta = MathHelper.ToRadians(0.01f);

            actual = Matrix3x2.CreateRotation(MathHelper.Pi + delta, center);
            Assert.False(MathHelper.Equal(new Matrix3x2(-1, 0, 0, -1, 6, 14), actual));

            actual = Matrix3x2.CreateRotation(MathHelper.Pi - delta, center);
            Assert.False(MathHelper.Equal(new Matrix3x2(-1, 0, 0, -1, 6, 14), actual));
        }

        // A test for Invert (Matrix3x2)
        // Non invertible matrix - determinant is zero - singular matrix
        [Fact]
        public void Matrix3x2InvertTest1()
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = 0.0f;
            a.M12 = 2.0f;
            a.M21 = 0.0f;
            a.M22 = 4.0f;
            a.M31 = 5.0f;
            a.M32 = 6.0f;

            float detA = a.GetDeterminant();
            Assert.True(MathHelper.Equal(detA, 0.0f), "Matrix3x2.Invert did not return the expected value.");

            Matrix3x2 actual;
            Assert.False(Matrix3x2.Invert(a, out actual));

            // all the elements in Actual is NaN
            Assert.True(
                float.IsNaN(actual.M11) && float.IsNaN(actual.M12) &&
                float.IsNaN(actual.M21) && float.IsNaN(actual.M22) &&
                float.IsNaN(actual.M31) && float.IsNaN(actual.M32)
                , "Matrix3x2.Invert did not return the expected value.");
        }

        // A test for Lerp (Matrix3x2, Matrix3x2, float)
        [Fact]
        public void Matrix3x2LerpTest()
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = 11.0f;
            a.M12 = 12.0f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;

            Matrix3x2 b = GenerateIncrementalMatrixNumber();

            float t = 0.5f;

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 + (b.M11 - a.M11) * t;
            expected.M12 = a.M12 + (b.M12 - a.M12) * t;

            expected.M21 = a.M21 + (b.M21 - a.M21) * t;
            expected.M22 = a.M22 + (b.M22 - a.M22) * t;

            expected.M31 = a.M31 + (b.M31 - a.M31) * t;
            expected.M32 = a.M32 + (b.M32 - a.M32) * t;

            Matrix3x2 actual;
            actual = Matrix3x2.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.Lerp did not return the expected value.");
        }

        // A test for operator - (Matrix3x2)
        [Fact]
        public void Matrix3x2UnaryNegationTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M21 = -3.0f;
            expected.M22 = -4.0f;
            expected.M31 = -5.0f;
            expected.M32 = -6.0f;

            Matrix3x2 actual = -a;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.operator - did not return the expected value.");
        }

        // A test for operator - (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2SubtractionTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);
            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;

            Matrix3x2 actual = a - b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.operator - did not return the expected value.");
        }

        // A test for operator * (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2MultiplyTest1()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + b.M31;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + b.M32;

            Matrix3x2 actual = a * b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.operator * did not return the expected value.");

            // Sanity check by comparison with 4x4 multiply.
            a = Matrix3x2.CreateRotation(MathHelper.ToRadians(30)) * Matrix3x2.CreateTranslation(23, 42);
            b = Matrix3x2.CreateScale(3, 7) * Matrix3x2.CreateTranslation(666, -1);

            actual = a * b;

            Matrix4x4 a44 = new Matrix4x4(a);
            Matrix4x4 b44 = new Matrix4x4(b);
            Matrix4x4 expected44 = a44 * b44;
            Matrix4x4 actual44 = new Matrix4x4(actual);

            Assert.True(MathHelper.Equal(expected44, actual44), "Matrix3x2.operator * did not return the expected value.");
        }

        // A test for operator * (Matrix3x2, Matrix3x2)
        // Multiply with identity matrix
        [Fact]
        public void Matrix3x2MultiplyTest4()
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = 1.0f;
            a.M12 = 2.0f;
            a.M21 = 5.0f;
            a.M22 = -6.0f;
            a.M31 = 9.0f;
            a.M32 = 10.0f;

            Matrix3x2 b = new Matrix3x2();
            b = Matrix3x2.Identity;

            Matrix3x2 expected = a;
            Matrix3x2 actual = a * b;

            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.operator * did not return the expected value.");
        }

        // A test for operator + (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2AdditionTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;

            Matrix3x2 actual;

            actual = a + b;

            Assert.True(MathHelper.Equal(expected, actual), "Matrix3x2.operator + did not return the expected value.");
        }

        // A test for ToString ()
        [Fact]
        public void Matrix3x2ToStringTest()
        {
            Matrix3x2 a = new Matrix3x2();
            a.M11 = 11.0f;
            a.M12 = -12.0f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;

            string expected = "{ {M11:11 M12:-12} " +
                                "{M21:21 M22:22} " +
                                "{M31:31 M32:32} }";
            string actual;

            actual = a.ToString();
            Assert.Equal(expected, actual);
        }

        // A test for Add (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2AddTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;

            Matrix3x2 actual;

            actual = Matrix3x2.Add(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Equals (object)
        [Fact]
        public void Matrix3x2EqualsTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            object obj = b;

            bool expected = true;
            bool actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            obj = b;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare between different types.
            obj = new Vector4();
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare against null.
            obj = null;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);
        }

        // A test for GetHashCode ()
        [Fact]
        public void Matrix3x2GetHashCodeTest()
        {
            Matrix3x2 target = GenerateIncrementalMatrixNumber();
            int expected = unchecked(target.M11.GetHashCode() + target.M12.GetHashCode() +
                                     target.M21.GetHashCode() + target.M22.GetHashCode() +
                                     target.M31.GetHashCode() + target.M32.GetHashCode());
            int actual;

            actual = target.GetHashCode();
            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2MultiplyTest3()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + b.M31;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + b.M32;
            Matrix3x2 actual;
            actual = Matrix3x2.Multiply(a, b);

            Assert.Equal(expected, actual);

            // Sanity check by comparison with 4x4 multiply.
            a = Matrix3x2.CreateRotation(MathHelper.ToRadians(30)) * Matrix3x2.CreateTranslation(23, 42);
            b = Matrix3x2.CreateScale(3, 7) * Matrix3x2.CreateTranslation(666, -1);

            actual = Matrix3x2.Multiply(a, b);

            Matrix4x4 a44 = new Matrix4x4(a);
            Matrix4x4 b44 = new Matrix4x4(b);
            Matrix4x4 expected44 = Matrix4x4.Multiply(a44, b44);
            Matrix4x4 actual44 = new Matrix4x4(actual);

            Assert.True(MathHelper.Equal(expected44, actual44), "Matrix3x2.Multiply did not return the expected value.");
        }

        // A test for Multiply (Matrix3x2, float)
        [Fact]
        public void Matrix3x2MultiplyTest5()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 expected = new Matrix3x2(3, 6, 9, 12, 15, 18);
            Matrix3x2 actual = Matrix3x2.Multiply(a, 3);

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix3x2, float)
        [Fact]
        public void Matrix3x2MultiplyTest6()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 expected = new Matrix3x2(3, 6, 9, 12, 15, 18);
            Matrix3x2 actual = a * 3;

            Assert.Equal(expected, actual);
        }

        // A test for Negate (Matrix3x2)
        [Fact]
        public void Matrix3x2NegateTest()
        {
            Matrix3x2 m = GenerateIncrementalMatrixNumber();

            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M21 = -3.0f;
            expected.M22 = -4.0f;
            expected.M31 = -5.0f;
            expected.M32 = -6.0f;
            Matrix3x2 actual;

            actual = Matrix3x2.Negate(m);
            Assert.Equal(expected, actual);
        }

        // A test for operator != (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2InequalityTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = false;
            bool actual = a != b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = true;
            actual = a != b;
            Assert.Equal(expected, actual);
        }

        // A test for operator == (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2EqualityTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = true;
            bool actual = a == b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = false;
            actual = a == b;
            Assert.Equal(expected, actual);
        }

        // A test for Subtract (Matrix3x2, Matrix3x2)
        [Fact]
        public void Matrix3x2SubtractTest()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber(-3.0f);
            Matrix3x2 expected = new Matrix3x2();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;

            Matrix3x2 actual;
            actual = Matrix3x2.Subtract(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (Vector2f)
        [Fact]
        public void Matrix3x2CreateScaleTest1()
        {
            Vector2 scales = new Vector2(2.0f, 3.0f);
            Matrix3x2 expected = new Matrix3x2(
                2.0f, 0.0f,
                0.0f, 3.0f,
                0.0f, 0.0f);
            Matrix3x2 actual = Matrix3x2.CreateScale(scales);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (Vector2f, Vector2f)
        [Fact]
        public void Matrix3x2CreateScaleCenterTest1()
        {
            Vector2 scale = new Vector2(3, 4);
            Vector2 center = new Vector2(23, 42);

            Matrix3x2 scaleAroundZero = Matrix3x2.CreateScale(scale, Vector2.Zero);
            Matrix3x2 scaleAroundZeroExpected = Matrix3x2.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix3x2 scaleAroundCenter = Matrix3x2.CreateScale(scale, center);
            Matrix3x2 scaleAroundCenterExpected = Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float)
        [Fact]
        public void Matrix3x2CreateScaleTest2()
        {
            float scale = 2.0f;
            Matrix3x2 expected = new Matrix3x2(
                2.0f, 0.0f,
                0.0f, 2.0f,
                0.0f, 0.0f);
            Matrix3x2 actual = Matrix3x2.CreateScale(scale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, Vector2f)
        [Fact]
        public void Matrix3x2CreateScaleCenterTest2()
        {
            float scale = 5;
            Vector2 center = new Vector2(23, 42);

            Matrix3x2 scaleAroundZero = Matrix3x2.CreateScale(scale, Vector2.Zero);
            Matrix3x2 scaleAroundZeroExpected = Matrix3x2.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix3x2 scaleAroundCenter = Matrix3x2.CreateScale(scale, center);
            Matrix3x2 scaleAroundCenterExpected = Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float, float)
        [Fact]
        public void Matrix3x2CreateScaleTest3()
        {
            float xScale = 2.0f;
            float yScale = 3.0f;
            Matrix3x2 expected = new Matrix3x2(
                2.0f, 0.0f,
                0.0f, 3.0f,
                0.0f, 0.0f);
            Matrix3x2 actual = Matrix3x2.CreateScale(xScale, yScale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, float, Vector2f)
        [Fact]
        public void Matrix3x2CreateScaleCenterTest3()
        {
            Vector2 scale = new Vector2(3, 4);
            Vector2 center = new Vector2(23, 42);

            Matrix3x2 scaleAroundZero = Matrix3x2.CreateScale(scale.X, scale.Y, Vector2.Zero);
            Matrix3x2 scaleAroundZeroExpected = Matrix3x2.CreateScale(scale.X, scale.Y);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix3x2 scaleAroundCenter = Matrix3x2.CreateScale(scale.X, scale.Y, center);
            Matrix3x2 scaleAroundCenterExpected = Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateScale(scale.X, scale.Y) * Matrix3x2.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateTranslation (Vector2f)
        [Fact]
        public void Matrix3x2CreateTranslationTest1()
        {
            Vector2 position = new Vector2(2.0f, 3.0f);
            Matrix3x2 expected = new Matrix3x2(
                1.0f, 0.0f,
                0.0f, 1.0f,
                2.0f, 3.0f);

            Matrix3x2 actual = Matrix3x2.CreateTranslation(position);
            Assert.Equal(expected, actual);
        }

        // A test for CreateTranslation (float, float)
        [Fact]
        public void Matrix3x2CreateTranslationTest2()
        {
            float xPosition = 2.0f;
            float yPosition = 3.0f;

            Matrix3x2 expected = new Matrix3x2(
                1.0f, 0.0f,
                0.0f, 1.0f,
                2.0f, 3.0f);

            Matrix3x2 actual = Matrix3x2.CreateTranslation(xPosition, yPosition);
            Assert.Equal(expected, actual);
        }

        // A test for Translation
        [Fact]
        public void Matrix3x2TranslationTest()
        {
            Matrix3x2 a = GenerateTestMatrix();
            Matrix3x2 b = a;

            // Transformed vector that has same semantics of property must be same.
            Vector2 val = new Vector2(a.M31, a.M32);
            Assert.Equal(val, a.Translation);

            // Set value and get value must be same.
            val = new Vector2(1.0f, 2.0f);
            a.Translation = val;
            Assert.Equal(val, a.Translation);

            // Make sure it only modifies expected value of matrix.
            Assert.True(
                a.M11 == b.M11 && a.M12 == b.M12 &&
                a.M21 == b.M21 && a.M22 == b.M22 &&
                a.M31 != b.M31 && a.M32 != b.M32,
                "Matrix3x2.Translation modified unexpected value of matrix.");
        }

        // A test for Equals (Matrix3x2)
        [Fact]
        public void Matrix3x2EqualsTest1()
        {
            Matrix3x2 a = GenerateIncrementalMatrixNumber();
            Matrix3x2 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = true;
            bool actual = a.Equals(b);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = false;
            actual = a.Equals(b);
            Assert.Equal(expected, actual);
        }

        // A test for CreateSkew (float, float)
        [Fact]
        public void Matrix3x2CreateSkewIdentityTest()
        {
            Matrix3x2 expected = Matrix3x2.Identity;
            Matrix3x2 actual = Matrix3x2.CreateSkew(0, 0);
            Assert.Equal(expected, actual);
        }

        // A test for CreateSkew (float, float)
        [Fact]
        public void Matrix3x2CreateSkewXTest()
        {
            Matrix3x2 expected = new Matrix3x2(1, 0, -0.414213562373095f, 1, 0, 0);
            Matrix3x2 actual = Matrix3x2.CreateSkew(-MathHelper.Pi / 8, 0);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = new Matrix3x2(1, 0, 0.414213562373095f, 1, 0, 0);
            actual = Matrix3x2.CreateSkew(MathHelper.Pi / 8, 0);
            Assert.True(MathHelper.Equal(expected, actual));

            Vector2 result = Vector2.Transform(new Vector2(0, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(0, 0), result));

            result = Vector2.Transform(new Vector2(0, 1), actual);
            Assert.True(MathHelper.Equal(new Vector2(0.414213568f, 1), result));

            result = Vector2.Transform(new Vector2(0, -1), actual);
            Assert.True(MathHelper.Equal(new Vector2(-0.414213568f, -1), result));

            result = Vector2.Transform(new Vector2(3, 10), actual);
            Assert.True(MathHelper.Equal(new Vector2(7.14213568f, 10), result));
        }

        // A test for CreateSkew (float, float)
        [Fact]
        public void Matrix3x2CreateSkewYTest()
        {
            Matrix3x2 expected = new Matrix3x2(1, -0.414213562373095f, 0, 1, 0, 0);
            Matrix3x2 actual = Matrix3x2.CreateSkew(0, -MathHelper.Pi / 8);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = new Matrix3x2(1, 0.414213562373095f, 0, 1, 0, 0);
            actual = Matrix3x2.CreateSkew(0, MathHelper.Pi / 8);
            Assert.True(MathHelper.Equal(expected, actual));

            Vector2 result = Vector2.Transform(new Vector2(0, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(0, 0), result));

            result = Vector2.Transform(new Vector2(1, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(1, 0.414213568f), result));

            result = Vector2.Transform(new Vector2(-1, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(-1, -0.414213568f), result));

            result = Vector2.Transform(new Vector2(10, 3), actual);
            Assert.True(MathHelper.Equal(new Vector2(10, 7.14213568f), result));
        }

        // A test for CreateSkew (float, float)
        [Fact]
        public void Matrix3x2CreateSkewXYTest()
        {
            Matrix3x2 expected = new Matrix3x2(1, -0.414213562373095f, 1, 1, 0, 0);
            Matrix3x2 actual = Matrix3x2.CreateSkew(MathHelper.Pi / 4, -MathHelper.Pi / 8);
            Assert.True(MathHelper.Equal(expected, actual));

            Vector2 result = Vector2.Transform(new Vector2(0, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(0, 0), result));

            result = Vector2.Transform(new Vector2(1, 0), actual);
            Assert.True(MathHelper.Equal(new Vector2(1, -0.414213562373095f), result));

            result = Vector2.Transform(new Vector2(0, 1), actual);
            Assert.True(MathHelper.Equal(new Vector2(1, 1), result));

            result = Vector2.Transform(new Vector2(1, 1), actual);
            Assert.True(MathHelper.Equal(new Vector2(2, 0.585786437626905f), result));
        }

        // A test for CreateSkew (float, float, Vector2f)
        [Fact]
        public void Matrix3x2CreateSkewCenterTest()
        {
            float skewX = 1, skewY = 2;
            Vector2 center = new Vector2(23, 42);

            Matrix3x2 skewAroundZero = Matrix3x2.CreateSkew(skewX, skewY, Vector2.Zero);
            Matrix3x2 skewAroundZeroExpected = Matrix3x2.CreateSkew(skewX, skewY);
            Assert.True(MathHelper.Equal(skewAroundZero, skewAroundZeroExpected));

            Matrix3x2 skewAroundCenter = Matrix3x2.CreateSkew(skewX, skewY, center);
            Matrix3x2 skewAroundCenterExpected = Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateSkew(skewX, skewY) * Matrix3x2.CreateTranslation(center);
            Assert.True(MathHelper.Equal(skewAroundCenter, skewAroundCenterExpected));
        }

        // A test for IsIdentity
        [Fact]
        public void Matrix3x2IsIdentityTest()
        {
            Assert.True(Matrix3x2.Identity.IsIdentity);
            Assert.True(new Matrix3x2(1, 0, 0, 1, 0, 0).IsIdentity);
            Assert.False(new Matrix3x2(0, 0, 0, 1, 0, 0).IsIdentity);
            Assert.False(new Matrix3x2(1, 1, 0, 1, 0, 0).IsIdentity);
            Assert.False(new Matrix3x2(1, 0, 1, 1, 0, 0).IsIdentity);
            Assert.False(new Matrix3x2(1, 0, 0, 0, 0, 0).IsIdentity);
            Assert.False(new Matrix3x2(1, 0, 0, 1, 1, 0).IsIdentity);
            Assert.False(new Matrix3x2(1, 0, 0, 1, 0, 1).IsIdentity);
        }

        // A test for Matrix3x2 comparison involving NaN values
        [Fact]
        public void Matrix3x2EqualsNanTest()
        {
            Matrix3x2 a = new Matrix3x2(float.NaN, 0, 0, 0, 0, 0);
            Matrix3x2 b = new Matrix3x2(0, float.NaN, 0, 0, 0, 0);
            Matrix3x2 c = new Matrix3x2(0, 0, float.NaN, 0, 0, 0);
            Matrix3x2 d = new Matrix3x2(0, 0, 0, float.NaN, 0, 0);
            Matrix3x2 e = new Matrix3x2(0, 0, 0, 0, float.NaN, 0);
            Matrix3x2 f = new Matrix3x2(0, 0, 0, 0, 0, float.NaN);

            Assert.False(a == new Matrix3x2());
            Assert.False(b == new Matrix3x2());
            Assert.False(c == new Matrix3x2());
            Assert.False(d == new Matrix3x2());
            Assert.False(e == new Matrix3x2());
            Assert.False(f == new Matrix3x2());

            Assert.True(a != new Matrix3x2());
            Assert.True(b != new Matrix3x2());
            Assert.True(c != new Matrix3x2());
            Assert.True(d != new Matrix3x2());
            Assert.True(e != new Matrix3x2());
            Assert.True(f != new Matrix3x2());

            Assert.False(a.Equals(new Matrix3x2()));
            Assert.False(b.Equals(new Matrix3x2()));
            Assert.False(c.Equals(new Matrix3x2()));
            Assert.False(d.Equals(new Matrix3x2()));
            Assert.False(e.Equals(new Matrix3x2()));
            Assert.False(f.Equals(new Matrix3x2()));

            Assert.False(a.IsIdentity);
            Assert.False(b.IsIdentity);
            Assert.False(c.IsIdentity);
            Assert.False(d.IsIdentity);
            Assert.False(e.IsIdentity);
            Assert.False(f.IsIdentity);

            // Counterintuitive result - IEEE rules for NaN comparison are weird!
            Assert.False(a.Equals(a));
            Assert.False(b.Equals(b));
            Assert.False(c.Equals(c));
            Assert.False(d.Equals(d));
            Assert.False(e.Equals(e));
            Assert.False(f.Equals(f));
        }

        // A test to make sure these types are blittable directly into GPU buffer memory layouts
        [Fact]
        public unsafe void Matrix3x2SizeofTest()
        {
            Assert.Equal(24, sizeof(Matrix3x2));
            Assert.Equal(48, sizeof(Matrix3x2_2x));
            Assert.Equal(28, sizeof(Matrix3x2PlusFloat));
            Assert.Equal(56, sizeof(Matrix3x2PlusFloat_2x));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix3x2_2x
        {
            private Matrix3x2 _a;
            private Matrix3x2 _b;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix3x2PlusFloat
        {
            private Matrix3x2 _v;
            private float _f;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix3x2PlusFloat_2x
        {
            private Matrix3x2PlusFloat _a;
            private Matrix3x2PlusFloat _b;
        }

        // A test to make sure the fields are laid out how we expect
        [Fact]
        public unsafe void Matrix3x2FieldOffsetTest()
        {
            Matrix3x2 mat = new Matrix3x2();
            float* basePtr = &mat.M11; // Take address of first element
            Matrix3x2* matPtr = &mat; // Take address of whole matrix

            Assert.Equal(new IntPtr(basePtr), new IntPtr(matPtr));

            Assert.Equal(new IntPtr(basePtr + 0), new IntPtr(&mat.M11));
            Assert.Equal(new IntPtr(basePtr + 1), new IntPtr(&mat.M12));

            Assert.Equal(new IntPtr(basePtr + 2), new IntPtr(&mat.M21));
            Assert.Equal(new IntPtr(basePtr + 3), new IntPtr(&mat.M22));

            Assert.Equal(new IntPtr(basePtr + 4), new IntPtr(&mat.M31));
            Assert.Equal(new IntPtr(basePtr + 5), new IntPtr(&mat.M32));
        }
    }
}

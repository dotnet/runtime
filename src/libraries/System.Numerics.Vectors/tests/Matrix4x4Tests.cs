// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public class Matrix4x4Tests
    {
        static Matrix4x4 GenerateIncrementalMatrixNumber(float value = 0.0f)
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = value + 1.0f;
            a.M12 = value + 2.0f;
            a.M13 = value + 3.0f;
            a.M14 = value + 4.0f;
            a.M21 = value + 5.0f;
            a.M22 = value + 6.0f;
            a.M23 = value + 7.0f;
            a.M24 = value + 8.0f;
            a.M31 = value + 9.0f;
            a.M32 = value + 10.0f;
            a.M33 = value + 11.0f;
            a.M34 = value + 12.0f;
            a.M41 = value + 13.0f;
            a.M42 = value + 14.0f;
            a.M43 = value + 15.0f;
            a.M44 = value + 16.0f;
            return a;
        }

        static Matrix4x4 GenerateTestMatrix()
        {
            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));
            m.Translation = new Vector3(111.0f, 222.0f, 333.0f);
            return m;
        }

        // A test for Identity
        [Fact]
        public void Matrix4x4IdentityTest()
        {
            Matrix4x4 val = new Matrix4x4();
            val.M11 = val.M22 = val.M33 = val.M44 = 1.0f;

            Assert.True(MathHelper.Equal(val, Matrix4x4.Identity), "Matrix4x4.Indentity was not set correctly.");
        }

        // A test for Determinant
        [Fact]
        public void Matrix4x4DeterminantTest()
        {
            Matrix4x4 target =
                    Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                    Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                    Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            float val = 1.0f;
            float det = target.GetDeterminant();

            Assert.True(MathHelper.Equal(val, det), "Matrix4x4.Determinant was not set correctly.");
        }

        // A test for Determinant
        // Determinant test |A| = 1 / |A'|
        [Fact]
        public void Matrix4x4DeterminantTest1()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 5.0f;
            a.M12 = 2.0f;
            a.M13 = 8.25f;
            a.M14 = 1.0f;
            a.M21 = 12.0f;
            a.M22 = 6.8f;
            a.M23 = 2.14f;
            a.M24 = 9.6f;
            a.M31 = 6.5f;
            a.M32 = 1.0f;
            a.M33 = 3.14f;
            a.M34 = 2.22f;
            a.M41 = 0f;
            a.M42 = 0.86f;
            a.M43 = 4.0f;
            a.M44 = 1.0f;
            Matrix4x4 i;
            Assert.True(Matrix4x4.Invert(a, out i));

            float detA = a.GetDeterminant();
            float detI = i.GetDeterminant();
            float t = 1.0f / detI;

            // only accurate to 3 precision
            Assert.True(System.Math.Abs(detA - t) < 1e-3, "Matrix4x4.Determinant was not set correctly.");
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertTest()
        {
            Matrix4x4 mtx =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.74999994f;
            expected.M12 = -0.216506317f;
            expected.M13 = 0.62499994f;
            expected.M14 = 0.0f;

            expected.M21 = 0.433012635f;
            expected.M22 = 0.87499994f;
            expected.M23 = -0.216506317f;
            expected.M24 = 0.0f;

            expected.M31 = -0.49999997f;
            expected.M32 = 0.433012635f;
            expected.M33 = 0.74999994f;
            expected.M34 = 0.0f;

            expected.M41 = 0.0f;
            expected.M42 = 0.0f;
            expected.M43 = 0.0f;
            expected.M44 = 0.99999994f;

            Matrix4x4 actual;

            Assert.True(Matrix4x4.Invert(mtx, out actual));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Invert did not return the expected value.");

            // Make sure M*M is identity matrix
            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity), "Matrix4x4.Invert did not return the expected value.");
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertIdentityTest()
        {
            Matrix4x4 mtx = Matrix4x4.Identity;

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Assert.True(MathHelper.Equal(actual, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertTranslationTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateTranslation(23, 42, 666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertRotationTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateFromYawPitchRoll(3, 4, 5);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertScaleTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateScale(23, 42, -666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertProjectionTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(1, 1.333f, 0.1f, 666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertAffineTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateFromYawPitchRoll(3, 4, 5) *
                            Matrix4x4.CreateScale(23, 42, -666) *
                            Matrix4x4.CreateTranslation(17, 53, 89);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertRank3()
        {
            // A 4x4 Matrix having a rank of 3
            Matrix4x4 mtx = new Matrix4x4(1.0f, 2.0f, 3.0f, 0.0f,
                                          5.0f, 1.0f, 6.0f, 0.0f,
                                          8.0f, 9.0f, 1.0f, 0.0f,
                                          4.0f, 7.0f, 3.0f, 0.0f);

            Matrix4x4 actual;
            Assert.False(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.False(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        void DecomposeTest(float yaw, float pitch, float roll, Vector3 expectedTranslation, Vector3 expectedScales)
        {
            Quaternion expectedRotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw),
                                                                            MathHelper.ToRadians(pitch),
                                                                            MathHelper.ToRadians(roll));

            Matrix4x4 m = Matrix4x4.CreateScale(expectedScales) *
                          Matrix4x4.CreateFromQuaternion(expectedRotation) *
                          Matrix4x4.CreateTranslation(expectedTranslation);

            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            bool actualResult = Matrix4x4.Decompose(m, out scales, out rotation, out translation);
            Assert.True(actualResult, "Matrix4x4.Decompose did not return expected value.");

            bool scaleIsZeroOrNegative = expectedScales.X <= 0 ||
                                         expectedScales.Y <= 0 ||
                                         expectedScales.Z <= 0;

            if (scaleIsZeroOrNegative)
            {
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.X), Math.Abs(scales.X)), "Matrix4x4.Decompose did not return expected value.");
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.Y), Math.Abs(scales.Y)), "Matrix4x4.Decompose did not return expected value.");
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.Z), Math.Abs(scales.Z)), "Matrix4x4.Decompose did not return expected value.");
            }
            else
            {
                Assert.True(MathHelper.Equal(expectedScales, scales), string.Format("Matrix4x4.Decompose did not return expected value Expected:{0} actual:{1}.", expectedScales, scales));
                Assert.True(MathHelper.EqualRotation(expectedRotation, rotation), string.Format("Matrix4x4.Decompose did not return expected value. Expected:{0} actual:{1}.", expectedRotation, rotation));
            }

            Assert.True(MathHelper.Equal(expectedTranslation, translation), string.Format("Matrix4x4.Decompose did not return expected value. Expected:{0} actual:{1}.", expectedTranslation, translation));
        }

        // Various rotation decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest01()
        {
            DecomposeTest(10.0f, 20.0f, 30.0f, new Vector3(10, 20, 30), new Vector3(2, 3, 4));

            const float step = 35.0f;

            for (float yawAngle = -720.0f; yawAngle <= 720.0f; yawAngle += step)
            {
                for (float pitchAngle = -720.0f; pitchAngle <= 720.0f; pitchAngle += step)
                {
                    for (float rollAngle = -720.0f; rollAngle <= 720.0f; rollAngle += step)
                    {
                        DecomposeTest(yawAngle, pitchAngle, rollAngle, new Vector3(10, 20, 30), new Vector3(2, 3, 4));
                    }
                }
            }
        }

        // Various scaled matrix decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest02()
        {
            DecomposeTest(10.0f, 20.0f, 30.0f, new Vector3(10, 20, 30), new Vector3(2, 3, 4));

            // Various scales.
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1, 2, 3));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1, 3, 2));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2, 1, 3));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2, 3, 1));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3, 1, 2));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3, 2, 1));

            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(-2, 1, 1));

            // Small scales.
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1e-4f, 2e-4f, 3e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1e-4f, 3e-4f, 2e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2e-4f, 1e-4f, 3e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2e-4f, 3e-4f, 1e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3e-4f, 1e-4f, 2e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3e-4f, 2e-4f, 1e-4f));

            // Zero scales.
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 0, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 0, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 1, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 0, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 0, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 1, 0));

            // Negative scales.
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, -1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, -1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, 1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, -1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, 1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, -1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 1, -1));
        }

        void DecomposeScaleTest(float sx, float sy, float sz)
        {
            Matrix4x4 m = Matrix4x4.CreateScale(sx, sy, sz);

            Vector3 expectedScales = new Vector3(sx, sy, sz);
            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            bool actualResult = Matrix4x4.Decompose(m, out scales, out rotation, out translation);
            Assert.True(actualResult, "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.Equal(expectedScales, scales), "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.EqualRotation(Quaternion.Identity, rotation), "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.Equal(Vector3.Zero, translation), "Matrix4x4.Decompose did not return expected value.");
        }

        // Tiny scale decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest03()
        {
            DecomposeScaleTest(1, 2e-4f, 3e-4f);
            DecomposeScaleTest(1, 3e-4f, 2e-4f);
            DecomposeScaleTest(2e-4f, 1, 3e-4f);
            DecomposeScaleTest(2e-4f, 3e-4f, 1);
            DecomposeScaleTest(3e-4f, 1, 2e-4f);
            DecomposeScaleTest(3e-4f, 2e-4f, 1);
        }

        [Fact]
        public void Matrix4x4DecomposeTest04()
        {
            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            Assert.False(Matrix4x4.Decompose(GenerateIncrementalMatrixNumber(), out scales, out rotation, out translation), "decompose should have failed.");
            Assert.False(Matrix4x4.Decompose(new Matrix4x4(Matrix3x2.CreateSkew(1, 2)), out scales, out rotation, out translation), "decompose should have failed.");
        }

        // Transform by quaternion test
        [Fact]
        public void Matrix4x4TransformTest()
        {
            Matrix4x4 target = GenerateIncrementalMatrixNumber();

            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            Quaternion q = Quaternion.CreateFromRotationMatrix(m);

            Matrix4x4 expected = target * m;
            Matrix4x4 actual;
            actual = Matrix4x4.Transform(target, q);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transform did not return the expected value.");
        }

        // A test for CreateRotationX (float)
        [Fact]
        public void Matrix4x4CreateRotationXTest()
        {
            float radians = MathHelper.ToRadians(30.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 1.0f;
            expected.M22 = 0.8660254f;
            expected.M23 = 0.5f;
            expected.M32 = -0.5f;
            expected.M33 = 0.8660254f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;

            actual = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationX did not return the expected value.");
        }

        // A test for CreateRotationX (float)
        // CreateRotationX of zero degree
        [Fact]
        public void Matrix4x4CreateRotationXTest1()
        {
            float radians = 0;

            Matrix4x4 expected = Matrix4x4.Identity;
            Matrix4x4 actual = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationX did not return the expected value.");
        }

        // A test for CreateRotationX (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationXCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationX(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationX(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationX(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CreateRotationY (float)
        [Fact]
        public void Matrix4x4CreateRotationYTest()
        {
            float radians = MathHelper.ToRadians(60.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 0.49999997f;
            expected.M13 = -0.866025448f;
            expected.M22 = 1.0f;
            expected.M31 = 0.866025448f;
            expected.M33 = 0.49999997f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationY did not return the expected value.");
        }

        // A test for RotationY (float)
        // CreateRotationY test for negative angle
        [Fact]
        public void Matrix4x4CreateRotationYTest1()
        {
            float radians = MathHelper.ToRadians(-300.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 0.49999997f;
            expected.M13 = -0.866025448f;
            expected.M22 = 1.0f;
            expected.M31 = 0.866025448f;
            expected.M33 = 0.49999997f;
            expected.M44 = 1.0f;

            Matrix4x4 actual = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationY did not return the expected value.");
        }

        // A test for CreateRotationY (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationYCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationY(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationY(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationY(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CreateFromAxisAngle(Vector3f,float)
        [Fact]
        public void Matrix4x4CreateFromAxisAngleTest()
        {
            float radians = MathHelper.ToRadians(-30.0f);

            Matrix4x4 expected = Matrix4x4.CreateRotationX(radians);
            Matrix4x4 actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateRotationY(radians);
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateRotationZ(radians);
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.One), radians));
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(Vector3.One), radians);
            Assert.True(MathHelper.Equal(expected, actual));

            const int rotCount = 16;
            for (int i = 0; i < rotCount; ++i)
            {
                float latitude = (2.0f * MathHelper.Pi) * ((float)i / (float)rotCount);
                for (int j = 0; j < rotCount; ++j)
                {
                    float longitude = -MathHelper.PiOver2 + MathHelper.Pi * ((float)j / (float)rotCount);

                    Matrix4x4 m = Matrix4x4.CreateRotationZ(longitude) * Matrix4x4.CreateRotationY(latitude);
                    Vector3 axis = new Vector3(m.M11, m.M12, m.M13);
                    for (int k = 0; k < rotCount; ++k)
                    {
                        float rot = (2.0f * MathHelper.Pi) * ((float)k / (float)rotCount);
                        expected = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(axis, rot));
                        actual = Matrix4x4.CreateFromAxisAngle(axis, rot);
                        Assert.True(MathHelper.Equal(expected, actual));
                    }
                }
            }
        }

        [Fact]
        public void Matrix4x4CreateFromYawPitchRollTest1()
        {
            float yawAngle = MathHelper.ToRadians(30.0f);
            float pitchAngle = MathHelper.ToRadians(40.0f);
            float rollAngle = MathHelper.ToRadians(50.0f);

            Matrix4x4 yaw = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, yawAngle);
            Matrix4x4 pitch = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, pitchAngle);
            Matrix4x4 roll = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rollAngle);

            Matrix4x4 expected = roll * pitch * yaw;
            Matrix4x4 actual = Matrix4x4.CreateFromYawPitchRoll(yawAngle, pitchAngle, rollAngle);
            Assert.True(MathHelper.Equal(expected, actual));
        }

        // Covers more numeric rigions
        [Fact]
        public void Matrix4x4CreateFromYawPitchRollTest2()
        {
            const float step = 35.0f;

            for (float yawAngle = -720.0f; yawAngle <= 720.0f; yawAngle += step)
            {
                for (float pitchAngle = -720.0f; pitchAngle <= 720.0f; pitchAngle += step)
                {
                    for (float rollAngle = -720.0f; rollAngle <= 720.0f; rollAngle += step)
                    {
                        float yawRad = MathHelper.ToRadians(yawAngle);
                        float pitchRad = MathHelper.ToRadians(pitchAngle);
                        float rollRad = MathHelper.ToRadians(rollAngle);
                        Matrix4x4 yaw = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, yawRad);
                        Matrix4x4 pitch = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, pitchRad);
                        Matrix4x4 roll = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rollRad);

                        Matrix4x4 expected = roll * pitch * yaw;
                        Matrix4x4 actual = Matrix4x4.CreateFromYawPitchRoll(yawRad, pitchRad, rollRad);
                        Assert.True(MathHelper.Equal(expected, actual), string.Format("Yaw:{0} Pitch:{1} Roll:{2}", yawAngle, pitchAngle, rollAngle));
                    }
                }
            }
        }

        // Simple shadow test.
        [Fact]
        public void Matrix4x4CreateShadowTest01()
        {
            Vector3 lightDir = Vector3.UnitY;
            Plane plane = new Plane(Vector3.UnitY, 0);

            Matrix4x4 expected = Matrix4x4.CreateScale(1, 0, 1);

            Matrix4x4 actual = Matrix4x4.CreateShadow(lightDir, plane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateShadow did not returned expected value.");
        }

        // Various plane projections.
        [Fact]
        public void Matrix4x4CreateShadowTest02()
        {
            // Complex cases.
            Plane[] planes = {
                new Plane( 0, 1, 0, 0 ),
                new Plane( 1, 2, 3, 4 ),
                new Plane( 5, 6, 7, 8 ),
                new Plane(-1,-2,-3,-4 ),
                new Plane(-5,-6,-7,-8 ),
            };

            Vector3[] points = {
                new Vector3( 1, 2, 3),
                new Vector3( 5, 6, 7),
                new Vector3( 8, 9, 10),
                new Vector3(-1,-2,-3),
                new Vector3(-5,-6,-7),
                new Vector3(-8,-9,-10),
            };

            foreach (Plane p in planes)
            {
                Plane plane = Plane.Normalize(p);

                // Try various direction of light directions.
                var testDirections = new Vector3[]
                {
                    new Vector3( -1.0f, 1.0f, 1.0f ),
                    new Vector3(  0.0f, 1.0f, 1.0f ),
                    new Vector3(  1.0f, 1.0f, 1.0f ),
                    new Vector3( -1.0f, 0.0f, 1.0f ),
                    new Vector3(  0.0f, 0.0f, 1.0f ),
                    new Vector3(  1.0f, 0.0f, 1.0f ),
                    new Vector3( -1.0f,-1.0f, 1.0f ),
                    new Vector3(  0.0f,-1.0f, 1.0f ),
                    new Vector3(  1.0f,-1.0f, 1.0f ),

                    new Vector3( -1.0f, 1.0f, 0.0f ),
                    new Vector3(  0.0f, 1.0f, 0.0f ),
                    new Vector3(  1.0f, 1.0f, 0.0f ),
                    new Vector3( -1.0f, 0.0f, 0.0f ),
                    new Vector3(  0.0f, 0.0f, 0.0f ),
                    new Vector3(  1.0f, 0.0f, 0.0f ),
                    new Vector3( -1.0f,-1.0f, 0.0f ),
                    new Vector3(  0.0f,-1.0f, 0.0f ),
                    new Vector3(  1.0f,-1.0f, 0.0f ),

                    new Vector3( -1.0f, 1.0f,-1.0f ),
                    new Vector3(  0.0f, 1.0f,-1.0f ),
                    new Vector3(  1.0f, 1.0f,-1.0f ),
                    new Vector3( -1.0f, 0.0f,-1.0f ),
                    new Vector3(  0.0f, 0.0f,-1.0f ),
                    new Vector3(  1.0f, 0.0f,-1.0f ),
                    new Vector3( -1.0f,-1.0f,-1.0f ),
                    new Vector3(  0.0f,-1.0f,-1.0f ),
                    new Vector3(  1.0f,-1.0f,-1.0f ),
                };

                foreach (Vector3 lightDirInfo in testDirections)
                {
                    if (lightDirInfo.Length() < 0.1f)
                        continue;
                    Vector3 lightDir = Vector3.Normalize(lightDirInfo);

                    if (Plane.DotNormal(plane, lightDir) < 0.1f)
                        continue;

                    Matrix4x4 m = Matrix4x4.CreateShadow(lightDir, plane);
                    Vector3 pp = -plane.D * plane.Normal; // origin of the plane.

                    //
                    foreach (Vector3 point in points)
                    {
                        Vector4 v4 = Vector4.Transform(point, m);

                        Vector3 sp = new Vector3(v4.X, v4.Y, v4.Z) / v4.W;

                        // Make sure transformed position is on the plane.
                        Vector3 v = sp - pp;
                        float d = Vector3.Dot(v, plane.Normal);
                        Assert.True(MathHelper.Equal(d, 0), "Matrix4x4.CreateShadow did not provide expected value.");

                        // make sure direction between transformed position and original position are same as light direction.
                        if (Vector3.Dot(point - pp, plane.Normal) > 0.0001f)
                        {
                            Vector3 dir = Vector3.Normalize(point - sp);
                            Assert.True(MathHelper.Equal(dir, lightDir), "Matrix4x4.CreateShadow did not provide expected value.");
                        }
                    }
                }
            }
        }

        void CreateReflectionTest(Plane plane, Matrix4x4 expected)
        {
            Matrix4x4 actual = Matrix4x4.CreateReflection(plane);
            Assert.True(MathHelper.Equal(actual, expected), "Matrix4x4.CreateReflection did not return expected value.");
        }

        [Fact]
        public void Matrix4x4CreateReflectionTest01()
        {
            // XY plane.
            CreateReflectionTest(new Plane(Vector3.UnitZ, 0), Matrix4x4.CreateScale(1, 1, -1));
            // XZ plane.
            CreateReflectionTest(new Plane(Vector3.UnitY, 0), Matrix4x4.CreateScale(1, -1, 1));
            // YZ plane.
            CreateReflectionTest(new Plane(Vector3.UnitX, 0), Matrix4x4.CreateScale(-1, 1, 1));

            // Complex cases.
            Plane[] planes = {
                new Plane( 0, 1, 0, 0 ),
                new Plane( 1, 2, 3, 4 ),
                new Plane( 5, 6, 7, 8 ),
                new Plane(-1,-2,-3,-4 ),
                new Plane(-5,-6,-7,-8 ),
            };

            Vector3[] points = {
                new Vector3( 1, 2, 3),
                new Vector3( 5, 6, 7),
                new Vector3(-1,-2,-3),
                new Vector3(-5,-6,-7),
            };

            foreach (Plane p in planes)
            {
                Plane plane = Plane.Normalize(p);
                Matrix4x4 m = Matrix4x4.CreateReflection(plane);
                Vector3 pp = -plane.D * plane.Normal; // Position on the plane.

                //
                foreach (Vector3 point in points)
                {
                    Vector3 rp = Vector3.Transform(point, m);

                    // Manually compute reflection point and compare results.
                    Vector3 v = point - pp;
                    float d = Vector3.Dot(v, plane.Normal);
                    Vector3 vp = point - 2.0f * d * plane.Normal;
                    Assert.True(MathHelper.Equal(rp, vp), "Matrix4x4.Reflection did not provide expected value.");
                }
            }
        }

        // A test for CreateRotationZ (float)
        [Fact]
        public void Matrix4x4CreateRotationZTest()
        {
            float radians = MathHelper.ToRadians(50.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.642787635f;
            expected.M12 = 0.766044438f;
            expected.M21 = -0.766044438f;
            expected.M22 = 0.642787635f;
            expected.M33 = 1.0f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateRotationZ(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationZ did not return the expected value.");
        }

        // A test for CreateRotationZ (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationZCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationZ(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationZ(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationZ(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationZ(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CrateLookAt (Vector3f, Vector3f, Vector3f)
        [Fact]
        public void Matrix4x4CreateLookAtTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraTarget = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.979457f;
            expected.M12 = -0.0928267762f;
            expected.M13 = 0.179017f;

            expected.M21 = 0.0f;
            expected.M22 = 0.8877481f;
            expected.M23 = 0.460329473f;

            expected.M31 = -0.201652914f;
            expected.M32 = -0.450872928f;
            expected.M33 = 0.8695112f;

            expected.M41 = -3.74498272f;
            expected.M42 = -3.30050683f;
            expected.M43 = -37.0820961f;
            expected.M44 = 1.0f;

            Matrix4x4 actual = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateLookAt did not return the expected value.");
        }

        // A test for CreateWorld (Vector3f, Vector3f, Vector3f)
        [Fact]
        public void Matrix4x4CreateWorldTest()
        {
            Vector3 objectPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 objectForwardDirection = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 objectUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.799999952f;
            expected.M12 = 0;
            expected.M13 = 0.599999964f;
            expected.M14 = 0;

            expected.M21 = -0.2228344f;
            expected.M22 = 0.928476632f;
            expected.M23 = 0.297112525f;
            expected.M24 = 0;

            expected.M31 = -0.557086f;
            expected.M32 = -0.371390671f;
            expected.M33 = 0.742781341f;
            expected.M34 = 0;

            expected.M41 = 10;
            expected.M42 = 20;
            expected.M43 = 30;
            expected.M44 = 1.0f;

            Matrix4x4 actual = Matrix4x4.CreateWorld(objectPosition, objectForwardDirection, objectUpVector);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateWorld did not return the expected value.");

            Assert.Equal(objectPosition, actual.Translation);
            Assert.True(Vector3.Dot(Vector3.Normalize(objectUpVector), new Vector3(actual.M21, actual.M22, actual.M23)) > 0);
            Assert.True(Vector3.Dot(Vector3.Normalize(objectForwardDirection), new Vector3(-actual.M31, -actual.M32, -actual.M33)) > 0.999f);
        }

        // A test for CreateOrtho (float, float, float, float)
        [Fact]
        public void Matrix4x4CreateOrthoTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.02f;
            expected.M22 = 0.01f;
            expected.M33 = -0.00100150227f;
            expected.M43 = -0.00150225335f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateOrtho did not return the expected value.");
        }

        // A test for CreateOrthoOffCenter (float, float, float, float, float, float)
        [Fact]
        public void Matrix4x4CreateOrthoOffCenterTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.025f;
            expected.M22 = 0.0125f;
            expected.M33 = -0.00100150227f;
            expected.M41 = -1.25f;
            expected.M42 = -1.25f;
            expected.M43 = -0.00150225335f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateOrthoOffCenter did not return the expected value.");
        }

        // A test for CreatePerspective (float, float, float, float)
        [Fact]
        public void Matrix4x4CreatePerspectiveTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.03f;
            expected.M22 = 0.015f;
            expected.M33 = -1.00150228f;
            expected.M34 = -1.0f;
            expected.M43 = -1.50225341f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreatePerspective(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreatePerspective did not return the expected value.");
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where znear = zfar
        [Fact]
        public void Matrix4x4CreatePerspectiveTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float width = 100.0f;
                float height = 200.0f;
                float zNearPlane = 0.0f;
                float zFarPlane = 0.0f;

                Matrix4x4 actual = Matrix4x4.CreatePerspective(width, height, zNearPlane, zFarPlane);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where near plane is negative value
        [Fact]
        public void Matrix4x4CreatePerspectiveTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, -10, 10);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where far plane is negative value
        [Fact]
        public void Matrix4x4CreatePerspectiveTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, 10, -10);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where near plane is beyond far plane
        [Fact]
        public void Matrix4x4CreatePerspectiveTest4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, 10, 1);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest()
        {
            float fieldOfView = MathHelper.ToRadians(30.0f);
            float aspectRatio = 1280.0f / 720.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 2.09927845f;
            expected.M22 = 3.73205066f;
            expected.M33 = -1.00150228f;
            expected.M34 = -1.0f;
            expected.M43 = -1.50225341f;
            Matrix4x4 actual;

            actual = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreatePerspectiveFieldOfView did not return the expected value.");
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where filedOfView is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(-1, 1, 1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where filedOfView is more than pi.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.Pi + 0.01f, 1, 1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where nearPlaneDistance is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, -1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where farPlaneDistance is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 1, -10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where nearPlaneDistance is larger than farPlaneDistance.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest5()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 10, 1);
            });
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.0375f;
            expected.M22 = 0.01875f;
            expected.M31 = 1.25f;
            expected.M32 = 1.25f;
            expected.M33 = -1.00150228f;
            expected.M34 = -1.0f;
            expected.M43 = -1.50225341f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreatePerspectiveOffCenter did not return the expected value.");
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where nearPlaneDistance is negative.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, -1, 10);
            });
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where farPlaneDistance is negative.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, 1, -10);
            });
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where test where nearPlaneDistance is larger than farPlaneDistance.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, 10, 1);
            });
        }

        // A test for Invert (Matrix4x4)
        // Non invertible matrix - determinant is zero - singular matrix
        [Fact]
        public void Matrix4x4InvertTest1()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 1.0f;
            a.M12 = 2.0f;
            a.M13 = 3.0f;
            a.M14 = 4.0f;
            a.M21 = 5.0f;
            a.M22 = 6.0f;
            a.M23 = 7.0f;
            a.M24 = 8.0f;
            a.M31 = 9.0f;
            a.M32 = 10.0f;
            a.M33 = 11.0f;
            a.M34 = 12.0f;
            a.M41 = 13.0f;
            a.M42 = 14.0f;
            a.M43 = 15.0f;
            a.M44 = 16.0f;

            float detA = a.GetDeterminant();
            Assert.True(MathHelper.Equal(detA, 0.0f), "Matrix4x4.Invert did not return the expected value.");

            Matrix4x4 actual;
            Assert.False(Matrix4x4.Invert(a, out actual));

            // all the elements in Actual is NaN
            Assert.True(
                float.IsNaN(actual.M11) && float.IsNaN(actual.M12) && float.IsNaN(actual.M13) && float.IsNaN(actual.M14) &&
                float.IsNaN(actual.M21) && float.IsNaN(actual.M22) && float.IsNaN(actual.M23) && float.IsNaN(actual.M24) &&
                float.IsNaN(actual.M31) && float.IsNaN(actual.M32) && float.IsNaN(actual.M33) && float.IsNaN(actual.M34) &&
                float.IsNaN(actual.M41) && float.IsNaN(actual.M42) && float.IsNaN(actual.M43) && float.IsNaN(actual.M44)
                , "Matrix4x4.Invert did not return the expected value.");
        }

        // A test for Lerp (Matrix4x4, Matrix4x4, float)
        [Fact]
        public void Matrix4x4LerpTest()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 11.0f;
            a.M12 = 12.0f;
            a.M13 = 13.0f;
            a.M14 = 14.0f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M23 = 23.0f;
            a.M24 = 24.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;
            a.M33 = 33.0f;
            a.M34 = 34.0f;
            a.M41 = 41.0f;
            a.M42 = 42.0f;
            a.M43 = 43.0f;
            a.M44 = 44.0f;

            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            float t = 0.5f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + (b.M11 - a.M11) * t;
            expected.M12 = a.M12 + (b.M12 - a.M12) * t;
            expected.M13 = a.M13 + (b.M13 - a.M13) * t;
            expected.M14 = a.M14 + (b.M14 - a.M14) * t;

            expected.M21 = a.M21 + (b.M21 - a.M21) * t;
            expected.M22 = a.M22 + (b.M22 - a.M22) * t;
            expected.M23 = a.M23 + (b.M23 - a.M23) * t;
            expected.M24 = a.M24 + (b.M24 - a.M24) * t;

            expected.M31 = a.M31 + (b.M31 - a.M31) * t;
            expected.M32 = a.M32 + (b.M32 - a.M32) * t;
            expected.M33 = a.M33 + (b.M33 - a.M33) * t;
            expected.M34 = a.M34 + (b.M34 - a.M34) * t;

            expected.M41 = a.M41 + (b.M41 - a.M41) * t;
            expected.M42 = a.M42 + (b.M42 - a.M42) * t;
            expected.M43 = a.M43 + (b.M43 - a.M43) * t;
            expected.M44 = a.M44 + (b.M44 - a.M44) * t;

            Matrix4x4 actual;
            actual = Matrix4x4.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Lerp did not return the expected value.");
        }

        // A test for operator - (Matrix4x4)
        [Fact]
        public void Matrix4x4UnaryNegationTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M13 = -3.0f;
            expected.M14 = -4.0f;
            expected.M21 = -5.0f;
            expected.M22 = -6.0f;
            expected.M23 = -7.0f;
            expected.M24 = -8.0f;
            expected.M31 = -9.0f;
            expected.M32 = -10.0f;
            expected.M33 = -11.0f;
            expected.M34 = -12.0f;
            expected.M41 = -13.0f;
            expected.M42 = -14.0f;
            expected.M43 = -15.0f;
            expected.M44 = -16.0f;

            Matrix4x4 actual = -a;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator - did not return the expected value.");
        }

        // A test for operator - (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4SubtractionTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M13 = a.M13 - b.M13;
            expected.M14 = a.M14 - b.M14;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M23 = a.M23 - b.M23;
            expected.M24 = a.M24 - b.M24;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;
            expected.M33 = a.M33 - b.M33;
            expected.M34 = a.M34 - b.M34;
            expected.M41 = a.M41 - b.M41;
            expected.M42 = a.M42 - b.M42;
            expected.M43 = a.M43 - b.M43;
            expected.M44 = a.M44 - b.M44;

            Matrix4x4 actual = a - b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator - did not return the expected value.");
        }

        // A test for operator * (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4MultiplyTest1()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            expected.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            expected.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            expected.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            expected.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            expected.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            expected.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            expected.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            expected.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            expected.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            expected.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;

            Matrix4x4 actual = a * b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator * did not return the expected value.");
        }

        // A test for operator * (Matrix4x4, Matrix4x4)
        // Multiply with identity matrix
        [Fact]
        public void Matrix4x4MultiplyTest4()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 1.0f;
            a.M12 = 2.0f;
            a.M13 = 3.0f;
            a.M14 = 4.0f;
            a.M21 = 5.0f;
            a.M22 = -6.0f;
            a.M23 = 7.0f;
            a.M24 = -8.0f;
            a.M31 = 9.0f;
            a.M32 = 10.0f;
            a.M33 = 11.0f;
            a.M34 = 12.0f;
            a.M41 = 13.0f;
            a.M42 = -14.0f;
            a.M43 = 15.0f;
            a.M44 = -16.0f;

            Matrix4x4 b = new Matrix4x4();
            b = Matrix4x4.Identity;

            Matrix4x4 expected = a;
            Matrix4x4 actual = a * b;

            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator * did not return the expected value.");
        }

        // A test for operator + (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4AdditionTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M13 = a.M13 + b.M13;
            expected.M14 = a.M14 + b.M14;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M23 = a.M23 + b.M23;
            expected.M24 = a.M24 + b.M24;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;
            expected.M33 = a.M33 + b.M33;
            expected.M34 = a.M34 + b.M34;
            expected.M41 = a.M41 + b.M41;
            expected.M42 = a.M42 + b.M42;
            expected.M43 = a.M43 + b.M43;
            expected.M44 = a.M44 + b.M44;

            Matrix4x4 actual = a + b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator + did not return the expected value.");
        }

        // A test for Transpose (Matrix4x4)
        [Fact]
        public void Matrix4x4TransposeTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11;
            expected.M12 = a.M21;
            expected.M13 = a.M31;
            expected.M14 = a.M41;
            expected.M21 = a.M12;
            expected.M22 = a.M22;
            expected.M23 = a.M32;
            expected.M24 = a.M42;
            expected.M31 = a.M13;
            expected.M32 = a.M23;
            expected.M33 = a.M33;
            expected.M34 = a.M43;
            expected.M41 = a.M14;
            expected.M42 = a.M24;
            expected.M43 = a.M34;
            expected.M44 = a.M44;

            Matrix4x4 actual = Matrix4x4.Transpose(a);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transpose did not return the expected value.");
        }

        // A test for Transpose (Matrix4x4)
        // Transpose Identity matrix
        [Fact]
        public void Matrix4x4TransposeTest1()
        {
            Matrix4x4 a = Matrix4x4.Identity;
            Matrix4x4 expected = Matrix4x4.Identity;

            Matrix4x4 actual = Matrix4x4.Transpose(a);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transpose did not return the expected value.");
        }

        // A test for Matrix4x4 (Quaternion)
        [Fact]
        public void Matrix4x4FromQuaternionTest1()
        {
            Vector3 axis = Vector3.Normalize(new Vector3(1.0f, 2.0f, 3.0f));
            Quaternion q = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(30.0f));

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.875595033f;
            expected.M12 = 0.420031041f;
            expected.M13 = -0.2385524f;
            expected.M14 = 0.0f;

            expected.M21 = -0.38175258f;
            expected.M22 = 0.904303849f;
            expected.M23 = 0.1910483f;
            expected.M24 = 0.0f;

            expected.M31 = 0.295970082f;
            expected.M32 = -0.07621294f;
            expected.M33 = 0.952151954f;
            expected.M34 = 0.0f;

            expected.M41 = 0.0f;
            expected.M42 = 0.0f;
            expected.M43 = 0.0f;
            expected.M44 = 1.0f;

            Matrix4x4 target = Matrix4x4.CreateFromQuaternion(q);
            Assert.True(MathHelper.Equal(expected, target), "Matrix4x4.Matrix4x4(Quaternion) did not return the expected value.");
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert X axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest2()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationX(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert Y axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest3()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationY(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert Z axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest4()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationZ(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert XYZ axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest5()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

                Matrix4x4 expected =
                    Matrix4x4.CreateRotationX(angle) *
                    Matrix4x4.CreateRotationY(angle) *
                    Matrix4x4.CreateRotationZ(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for ToString ()
        [Fact]
        public void Matrix4x4ToStringTest()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 11.0f;
            a.M12 = -12.0f;
            a.M13 = -13.3f;
            a.M14 = 14.4f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M23 = 23.0f;
            a.M24 = 24.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;
            a.M33 = 33.0f;
            a.M34 = 34.0f;
            a.M41 = 41.0f;
            a.M42 = 42.0f;
            a.M43 = 43.0f;
            a.M44 = 44.0f;

            string expected = string.Format(CultureInfo.CurrentCulture,
                "{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
                    11.0f, -12.0f, -13.3f, 14.4f,
                    21.0f, 22.0f, 23.0f, 24.0f,
                    31.0f, 32.0f, 33.0f, 34.0f,
                    41.0f, 42.0f, 43.0f, 44.0f);

            string actual = a.ToString();
            Assert.Equal(expected, actual);
        }

        // A test for Add (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4AddTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M13 = a.M13 + b.M13;
            expected.M14 = a.M14 + b.M14;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M23 = a.M23 + b.M23;
            expected.M24 = a.M24 + b.M24;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;
            expected.M33 = a.M33 + b.M33;
            expected.M34 = a.M34 + b.M34;
            expected.M41 = a.M41 + b.M41;
            expected.M42 = a.M42 + b.M42;
            expected.M43 = a.M43 + b.M43;
            expected.M44 = a.M44 + b.M44;

            Matrix4x4 actual = Matrix4x4.Add(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Equals (object)
        [Fact]
        public void Matrix4x4EqualsTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

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
        public void Matrix4x4GetHashCodeTest()
        {
            Matrix4x4 target = GenerateIncrementalMatrixNumber();
            int expected = unchecked(
                target.M11.GetHashCode() + target.M12.GetHashCode() + target.M13.GetHashCode() + target.M14.GetHashCode() +
                target.M21.GetHashCode() + target.M22.GetHashCode() + target.M23.GetHashCode() + target.M24.GetHashCode() +
                target.M31.GetHashCode() + target.M32.GetHashCode() + target.M33.GetHashCode() + target.M34.GetHashCode() +
                target.M41.GetHashCode() + target.M42.GetHashCode() + target.M43.GetHashCode() + target.M44.GetHashCode());
            int actual;

            actual = target.GetHashCode();
            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4MultiplyTest3()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            expected.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            expected.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            expected.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            expected.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            expected.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            expected.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            expected.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            expected.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            expected.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            expected.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;
            Matrix4x4 actual;
            actual = Matrix4x4.Multiply(a, b);

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, float)
        [Fact]
        public void Matrix4x4MultiplyTest5()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 expected = new Matrix4x4(3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48);
            Matrix4x4 actual = Matrix4x4.Multiply(a, 3);

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, float)
        [Fact]
        public void Matrix4x4MultiplyTest6()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 expected = new Matrix4x4(3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48);
            Matrix4x4 actual = a * 3;

            Assert.Equal(expected, actual);
        }

        // A test for Negate (Matrix4x4)
        [Fact]
        public void Matrix4x4NegateTest()
        {
            Matrix4x4 m = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M13 = -3.0f;
            expected.M14 = -4.0f;
            expected.M21 = -5.0f;
            expected.M22 = -6.0f;
            expected.M23 = -7.0f;
            expected.M24 = -8.0f;
            expected.M31 = -9.0f;
            expected.M32 = -10.0f;
            expected.M33 = -11.0f;
            expected.M34 = -12.0f;
            expected.M41 = -13.0f;
            expected.M42 = -14.0f;
            expected.M43 = -15.0f;
            expected.M44 = -16.0f;
            Matrix4x4 actual;

            actual = Matrix4x4.Negate(m);
            Assert.Equal(expected, actual);
        }

        // A test for operator != (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4InequalityTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

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

        // A test for operator == (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4EqualityTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

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

        // A test for Subtract (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4SubtractTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M13 = a.M13 - b.M13;
            expected.M14 = a.M14 - b.M14;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M23 = a.M23 - b.M23;
            expected.M24 = a.M24 - b.M24;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;
            expected.M33 = a.M33 - b.M33;
            expected.M34 = a.M34 - b.M34;
            expected.M41 = a.M41 - b.M41;
            expected.M42 = a.M42 - b.M42;
            expected.M43 = a.M43 - b.M43;
            expected.M44 = a.M44 - b.M44;

            Matrix4x4 actual = Matrix4x4.Subtract(a, b);
            Assert.Equal(expected, actual);
        }

        private void CreateBillboardFact(Vector3 placeDirection, Vector3 cameraUpVector, Matrix4x4 expectedRotation)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotation * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateBillboard did not return the expected value.");
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest01()
        {
            // Object placed at Forward of camera. result must be same as 180 degrees rotate along y-axis.
            CreateBillboardFact(new Vector3(0, 0, -1), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest02()
        {
            // Object placed at Backward of camera. This result must be same as 0 degrees rotate along y-axis.
            CreateBillboardFact(new Vector3(0, 0, 1), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest03()
        {
            // Place object at Right side of camera. This result must be same as 90 degrees rotate along y-axis.
            CreateBillboardFact(new Vector3(1, 0, 0), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest04()
        {
            // Place object at Left side of camera. This result must be same as -90 degrees rotate along y-axis.
            CreateBillboardFact(new Vector3(-1, 0, 0), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest05()
        {
            // Place object at Up side of camera. result must be same as 180 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(new Vector3(0, 1, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest06()
        {
            // Place object at Down side of camera. result must be same as 0 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(new Vector3(0, -1, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest07()
        {
            // Place object at Right side of camera. result must be same as 90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(new Vector3(1, 0, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest08()
        {
            // Place object at Left side of camera. result must be same as -90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(new Vector3(-1, 0, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest09()
        {
            // Place object at Up side of camera. result must be same as -90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(new Vector3(0, 1, 0), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest10()
        {
            // Place object at Down side of camera. result must be same as 90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(new Vector3(0, -1, 0), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest11()
        {
            // Place object at Forward side of camera. result must be same as 180 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest12()
        {
            // Place object at Backward side of camera. result must be same as 0 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(new Vector3(0, 0, 1), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and doesn't pass cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateBillboardTooCloseTest1()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = new Vector3(0, 1, 0);

            // Doesn't pass camera face direction. CreateBillboard uses new Vector3f(0, 0, -1) direction. Result must be same as 180 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, 1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateBillboard did not return the expected value.");
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and passed cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateBillboardTooCloseTest2()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = new Vector3(0, 1, 0);

            // Passes Vector3f.Right as camera face direction. Result must be same as -90 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(1, 0, 0));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateBillboard did not return the expected value.");
        }

        private void CreateConstrainedBillboardFact(Vector3 placeDirection, Vector3 rotateAxis, Matrix4x4 expectedRotation)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotation * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");

            // When you move camera along rotateAxis, result must be same.
            cameraPosition += rotateAxis * 10.0f;
            actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");

            cameraPosition -= rotateAxis * 30.0f;
            actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest01()
        {
            // Object placed at Forward of camera. result must be same as 180 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 0, -1), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest02()
        {
            // Object placed at Backward of camera. This result must be same as 0 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 0, 1), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest03()
        {
            // Place object at Right side of camera. This result must be same as 90 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(new Vector3(1, 0, 0), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest04()
        {
            // Place object at Left side of camera. This result must be same as -90 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(new Vector3(-1, 0, 0), new Vector3(0, 1, 0), Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest05()
        {
            // Place object at Up side of camera. result must be same as 180 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 1, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest06()
        {
            // Place object at Down side of camera. result must be same as 0 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(new Vector3(0, -1, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest07()
        {
            // Place object at Right side of camera. result must be same as 90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(new Vector3(1, 0, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest08()
        {
            // Place object at Left side of camera. result must be same as -90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(new Vector3(-1, 0, 0), new Vector3(0, 0, 1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest09()
        {
            // Place object at Up side of camera. result must be same as -90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 1, 0), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest10()
        {
            // Place object at Down side of camera. result must be same as 90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(new Vector3(0, -1, 0), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest11()
        {
            // Place object at Forward side of camera. result must be same as 180 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest12()
        {
            // Place object at Backward side of camera. result must be same as 0 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(new Vector3(0, 0, 1), new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and doesn't pass cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTooCloseTest1()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = new Vector3(0, 1, 0);

            // Doesn't pass camera face direction. CreateConstrainedBillboard uses new Vector3f(0, 0, -1) direction. Result must be same as 180 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, 1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and passed cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTooCloseTest2()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = new Vector3(0, 1, 0);

            // Passes Vector3f.Right as camera face direction. Result must be same as -90 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(1, 0, 0), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And use doesn't passed objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest1()
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 rotateAxis = new Vector3(0, 1, 0);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            // In this case, CreateConstrainedBillboard picks new Vector3f(0, 0, -1) as object forward vector.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user doesn't passed objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest2()
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 rotateAxis = new Vector3(0, 0, -1);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            // In this case, CreateConstrainedBillboard picks new Vector3f(1, 0, 0) as object forward vector.
            Matrix4x4 expected = Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed correct objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest3()
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 rotateAxis = new Vector3(0, 1, 0);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            // User passes correct objectForwardVector.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed incorrect objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest4()
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 rotateAxis = new Vector3(0, 1, 0);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            // User passes correct objectForwardVector.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed incorrect objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest5()
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 rotateAxis = new Vector3(0, 0, -1);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            // In this case, CreateConstrainedBillboard picks Vector3f.Right as object forward vector.
            Matrix4x4 expected = Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateConstrainedBillboard did not return the expected value.");
        }

        // A test for CreateScale (Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleTest1()
        {
            Vector3 scales = new Vector3(2.0f, 3.0f, 4.0f);
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 3.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 4.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(scales);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (Vector3f, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest1()
        {
            Vector3 scale = new Vector3(3, 4, 5);
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float)
        [Fact]
        public void Matrix4x4CreateScaleTest2()
        {
            float scale = 2.0f;
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 2.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(scale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest2()
        {
            float scale = 5;
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float, float, float)
        [Fact]
        public void Matrix4x4CreateScaleTest3()
        {
            float xScale = 2.0f;
            float yScale = 3.0f;
            float zScale = 4.0f;
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 3.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 4.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(xScale, yScale, zScale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, float, float, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest3()
        {
            Vector3 scale = new Vector3(3, 4, 5);
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateTranslation (Vector3f)
        [Fact]
        public void Matrix4x4CreateTranslationTest1()
        {
            Vector3 position = new Vector3(2.0f, 3.0f, 4.0f);
            Matrix4x4 expected = new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                2.0f, 3.0f, 4.0f, 1.0f);

            Matrix4x4 actual = Matrix4x4.CreateTranslation(position);
            Assert.Equal(expected, actual);
        }

        // A test for CreateTranslation (float, float, float)
        [Fact]
        public void Matrix4x4CreateTranslationTest2()
        {
            float xPosition = 2.0f;
            float yPosition = 3.0f;
            float zPosition = 4.0f;

            Matrix4x4 expected = new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                2.0f, 3.0f, 4.0f, 1.0f);

            Matrix4x4 actual = Matrix4x4.CreateTranslation(xPosition, yPosition, zPosition);
            Assert.Equal(expected, actual);
        }

        // A test for Translation
        [Fact]
        public void Matrix4x4TranslationTest()
        {
            Matrix4x4 a = GenerateTestMatrix();
            Matrix4x4 b = a;

            // Transformed vector that has same semantics of property must be same.
            Vector3 val = new Vector3(a.M41, a.M42, a.M43);
            Assert.Equal(val, a.Translation);

            // Set value and get value must be same.
            val = new Vector3(1.0f, 2.0f, 3.0f);
            a.Translation = val;
            Assert.Equal(val, a.Translation);

            // Make sure it only modifies expected value of matrix.
            Assert.True(
                a.M11 == b.M11 && a.M12 == b.M12 && a.M13 == b.M13 && a.M14 == b.M14 &&
                a.M21 == b.M21 && a.M22 == b.M22 && a.M23 == b.M23 && a.M24 == b.M24 &&
                a.M31 == b.M31 && a.M32 == b.M32 && a.M33 == b.M33 && a.M34 == b.M34 &&
                a.M41 != b.M41 && a.M42 != b.M42 && a.M43 != b.M43 && a.M44 == b.M44);
        }

        // A test for Equals (Matrix4x4)
        [Fact]
        public void Matrix4x4EqualsTest1()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

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

        // A test for IsIdentity
        [Fact]
        public void Matrix4x4IsIdentityTest()
        {
            Assert.True(Matrix4x4.Identity.IsIdentity);
            Assert.True(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0).IsIdentity);
        }

        // A test for Matrix4x4 (Matrix3x2)
        [Fact]
        public void Matrix4x4From3x2Test()
        {
            Matrix3x2 source = new Matrix3x2(1, 2, 3, 4, 5, 6);
            Matrix4x4 result = new Matrix4x4(source);

            Assert.Equal(source.M11, result.M11);
            Assert.Equal(source.M12, result.M12);
            Assert.Equal(0f, result.M13);
            Assert.Equal(0f, result.M14);

            Assert.Equal(source.M21, result.M21);
            Assert.Equal(source.M22, result.M22);
            Assert.Equal(0f, result.M23);
            Assert.Equal(0f, result.M24);

            Assert.Equal(0f, result.M31);
            Assert.Equal(0f, result.M32);
            Assert.Equal(1f, result.M33);
            Assert.Equal(0f, result.M34);

            Assert.Equal(source.M31, result.M41);
            Assert.Equal(source.M32, result.M42);
            Assert.Equal(0f, result.M43);
            Assert.Equal(1f, result.M44);
        }

        // A test for Matrix4x4 comparison involving NaN values
        [Fact]
        public void Matrix4x4EqualsNanTest()
        {
            Matrix4x4 a = new Matrix4x4(float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 b = new Matrix4x4(0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 c = new Matrix4x4(0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 d = new Matrix4x4(0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 e = new Matrix4x4(0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 f = new Matrix4x4(0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 g = new Matrix4x4(0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 h = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 i = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 j = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0);
            Matrix4x4 k = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0);
            Matrix4x4 l = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0);
            Matrix4x4 m = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0);
            Matrix4x4 n = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0);
            Matrix4x4 o = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0);
            Matrix4x4 p = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN);

            Assert.False(a == new Matrix4x4());
            Assert.False(b == new Matrix4x4());
            Assert.False(c == new Matrix4x4());
            Assert.False(d == new Matrix4x4());
            Assert.False(e == new Matrix4x4());
            Assert.False(f == new Matrix4x4());
            Assert.False(g == new Matrix4x4());
            Assert.False(h == new Matrix4x4());
            Assert.False(i == new Matrix4x4());
            Assert.False(j == new Matrix4x4());
            Assert.False(k == new Matrix4x4());
            Assert.False(l == new Matrix4x4());
            Assert.False(m == new Matrix4x4());
            Assert.False(n == new Matrix4x4());
            Assert.False(o == new Matrix4x4());
            Assert.False(p == new Matrix4x4());

            Assert.True(a != new Matrix4x4());
            Assert.True(b != new Matrix4x4());
            Assert.True(c != new Matrix4x4());
            Assert.True(d != new Matrix4x4());
            Assert.True(e != new Matrix4x4());
            Assert.True(f != new Matrix4x4());
            Assert.True(g != new Matrix4x4());
            Assert.True(h != new Matrix4x4());
            Assert.True(i != new Matrix4x4());
            Assert.True(j != new Matrix4x4());
            Assert.True(k != new Matrix4x4());
            Assert.True(l != new Matrix4x4());
            Assert.True(m != new Matrix4x4());
            Assert.True(n != new Matrix4x4());
            Assert.True(o != new Matrix4x4());
            Assert.True(p != new Matrix4x4());

            Assert.False(a.Equals(new Matrix4x4()));
            Assert.False(b.Equals(new Matrix4x4()));
            Assert.False(c.Equals(new Matrix4x4()));
            Assert.False(d.Equals(new Matrix4x4()));
            Assert.False(e.Equals(new Matrix4x4()));
            Assert.False(f.Equals(new Matrix4x4()));
            Assert.False(g.Equals(new Matrix4x4()));
            Assert.False(h.Equals(new Matrix4x4()));
            Assert.False(i.Equals(new Matrix4x4()));
            Assert.False(j.Equals(new Matrix4x4()));
            Assert.False(k.Equals(new Matrix4x4()));
            Assert.False(l.Equals(new Matrix4x4()));
            Assert.False(m.Equals(new Matrix4x4()));
            Assert.False(n.Equals(new Matrix4x4()));
            Assert.False(o.Equals(new Matrix4x4()));
            Assert.False(p.Equals(new Matrix4x4()));

            Assert.False(a.IsIdentity);
            Assert.False(b.IsIdentity);
            Assert.False(c.IsIdentity);
            Assert.False(d.IsIdentity);
            Assert.False(e.IsIdentity);
            Assert.False(f.IsIdentity);
            Assert.False(g.IsIdentity);
            Assert.False(h.IsIdentity);
            Assert.False(i.IsIdentity);
            Assert.False(j.IsIdentity);
            Assert.False(k.IsIdentity);
            Assert.False(l.IsIdentity);
            Assert.False(m.IsIdentity);
            Assert.False(n.IsIdentity);
            Assert.False(o.IsIdentity);
            Assert.False(p.IsIdentity);

            // Counterintuitive result - IEEE rules for NaN comparison are weird!
            Assert.False(a.Equals(a));
            Assert.False(b.Equals(b));
            Assert.False(c.Equals(c));
            Assert.False(d.Equals(d));
            Assert.False(e.Equals(e));
            Assert.False(f.Equals(f));
            Assert.False(g.Equals(g));
            Assert.False(h.Equals(h));
            Assert.False(i.Equals(i));
            Assert.False(j.Equals(j));
            Assert.False(k.Equals(k));
            Assert.False(l.Equals(l));
            Assert.False(m.Equals(m));
            Assert.False(n.Equals(n));
            Assert.False(o.Equals(o));
            Assert.False(p.Equals(p));
        }

        // A test to make sure these types are blittable directly into GPU buffer memory layouts
        [Fact]
        public unsafe void Matrix4x4SizeofTest()
        {
            Assert.Equal(64, sizeof(Matrix4x4));
            Assert.Equal(128, sizeof(Matrix4x4_2x));
            Assert.Equal(68, sizeof(Matrix4x4PlusFloat));
            Assert.Equal(136, sizeof(Matrix4x4PlusFloat_2x));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4_2x
        {
            private Matrix4x4 _a;
            private Matrix4x4 _b;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4PlusFloat
        {
            private Matrix4x4 _v;
            private float _f;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4PlusFloat_2x
        {
            private Matrix4x4PlusFloat _a;
            private Matrix4x4PlusFloat _b;
        }

        // A test to make sure the fields are laid out how we expect
        [Fact]
        public unsafe void Matrix4x4FieldOffsetTest()
        {
            Matrix4x4 mat = new Matrix4x4();

            float* basePtr = &mat.M11; // Take address of first element
            Matrix4x4* matPtr = &mat; // Take address of whole matrix

            Assert.Equal(new IntPtr(basePtr), new IntPtr(matPtr));

            Assert.Equal(new IntPtr(basePtr + 0), new IntPtr(&mat.M11));
            Assert.Equal(new IntPtr(basePtr + 1), new IntPtr(&mat.M12));
            Assert.Equal(new IntPtr(basePtr + 2), new IntPtr(&mat.M13));
            Assert.Equal(new IntPtr(basePtr + 3), new IntPtr(&mat.M14));

            Assert.Equal(new IntPtr(basePtr + 4), new IntPtr(&mat.M21));
            Assert.Equal(new IntPtr(basePtr + 5), new IntPtr(&mat.M22));
            Assert.Equal(new IntPtr(basePtr + 6), new IntPtr(&mat.M23));
            Assert.Equal(new IntPtr(basePtr + 7), new IntPtr(&mat.M24));

            Assert.Equal(new IntPtr(basePtr + 8), new IntPtr(&mat.M31));
            Assert.Equal(new IntPtr(basePtr + 9), new IntPtr(&mat.M32));
            Assert.Equal(new IntPtr(basePtr + 10), new IntPtr(&mat.M33));
            Assert.Equal(new IntPtr(basePtr + 11), new IntPtr(&mat.M34));

            Assert.Equal(new IntPtr(basePtr + 12), new IntPtr(&mat.M41));
            Assert.Equal(new IntPtr(basePtr + 13), new IntPtr(&mat.M42));
            Assert.Equal(new IntPtr(basePtr + 14), new IntPtr(&mat.M43));
            Assert.Equal(new IntPtr(basePtr + 15), new IntPtr(&mat.M44));
        }

        [Fact]
        public void PerspectiveFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspective(1.0f, 1.0f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }

        [Fact]
        public void PerspectiveFieldOfViewFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60.0f), 1.5f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }

        [Fact]
        public void PerspectiveOffCenterFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspectiveOffCenter(0.0f, 0.0f, 1.0f, 1.0f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class Matrix4x4LeftHandTests : Matrix4x4OrientationAwareTests
    {
        [Fact]
        public void Matrix4x4CreateLookAtLeftHandedTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraTarget = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = -0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = -0.460329f;

            expected.M31 = +0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = -0.869511f;

            expected.M41 = +3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = +37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateLookAtMethodName} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateLookToLeftHandedTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraDirection = new Vector3(-7.0f, -18.0f, -34.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = -0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = -0.460329f;

            expected.M31 = +0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = -0.869511f;

            expected.M41 = +3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = +37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = CreateLookTo(cameraPosition, cameraDirection, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateLookToMethodName} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateViewportLeftHandedTest()
        {
            float x = 10.0f, y = 20.0f;
            float width = 3.0f, height = 4.0f;
            float minDepth = 100.0f, maxDepth = 200.0f;

            Matrix4x4 expected = Matrix4x4.Identity;
            expected.M11 = width * 0.5f;
            expected.M22 = -height * 0.5f;
            expected.M33 = maxDepth - minDepth;
            expected.M41 = x + expected.M11;
            expected.M42 = y - expected.M22;
            expected.M43 = minDepth;

            Matrix4x4 actual = CreateViewport(x, y, width, height, minDepth, maxDepth);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateViewportMethodName} did not return the expected value.");
        }

        protected override string CreateLookAtMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAtLeftHanded)}";
        protected override Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
            => Matrix4x4.CreateLookAtLeftHanded(cameraPosition, cameraTarget, cameraUpVector);

        protected override string CreateLookToMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookToLeftHanded)}";
        protected override Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
            => Matrix4x4.CreateLookToLeftHanded(cameraPosition, cameraDirection, cameraUpVector);

        protected override string CreateViewportMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewportLeftHanded)}";
        protected override Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
            => Matrix4x4.CreateViewportLeftHanded(x, y, width, height, minDepth, maxDepth);

        protected override string CreateBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateBillboardLeftHanded)}";
        protected override Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
            => Matrix4x4.CreateBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, cameraForwardVector);

        protected override string CreateConstrainedBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)}";

        protected override Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector)
            => Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, rotateAxis, cameraForwardVector, objectForwardVector);
    }
}

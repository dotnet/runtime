// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{

    public class Matrix4x4RightHandTests : Matrix4x4OrientationAwareTests
    {
        [Fact]
        public void Matrix4x4CreateLookAtTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraTarget = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = +0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = +0.460329f;

            expected.M31 = -0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = +0.869511f;

            expected.M41 = -3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = -37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateLookAtMethodName} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateLookToTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraDirection = new Vector3(-7.0f, -18.0f, -34.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = +0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = +0.460329f;

            expected.M31 = -0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = +0.869511f;

            expected.M41 = -3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = -37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = CreateLookTo(cameraPosition, cameraDirection, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateLookToMethodName} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateViewportTest()
        {
            float x = 10.0f;
            float y = 20.0f;
            float width = 80.0f;
            float height = 160.0f;
            float minDepth = 1.5f;
            float maxDepth = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +40.0f;

            expected.M22 = -80.0f;

            expected.M33 = -998.5f;

            expected.M41 = +50.0f;
            expected.M42 = +100.0f;
            expected.M43 = +1.5f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = CreateViewport(x, y, width, height, minDepth, maxDepth);
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateViewportMethodName} did not return the expected value.");
        }

        private void CreateBillboardFact(Vector3 placeDirection, Vector3 cameraUpVector, Matrix4x4 expectedRotation)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotation * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, 1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(1, 0, 0));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateBillboardMethodName} did not return the expected value.");
        }

        private void CreateConstrainedBillboardFact(Vector3 placeDirection, Vector3 rotateAxis, Matrix4x4 expectedRotation)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotation * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");

            // When you move camera along rotateAxis, result must be same.
            cameraPosition += rotateAxis * 10.0f;
            actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");

            cameraPosition -= rotateAxis * 30.0f;
            actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, 1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName}CreateConstrainedBillboard did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(1, 0, 0), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
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
            Matrix4x4 actual = CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actual), $"{CreateConstrainedBillboardMethodName} did not return the expected value.");
        }

        protected override string CreateLookAtMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAt)}";
        protected override Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
            => Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);

        protected override string CreateLookToMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookTo)}";
        protected override Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
            => Matrix4x4.CreateLookTo(cameraPosition, cameraDirection, cameraUpVector);

        protected override string CreateViewportMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewport)}";
        protected override Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
            => Matrix4x4.CreateViewport(x, y, width, height, minDepth, maxDepth);

        protected override string CreateBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateBillboard)}";
        protected override Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
            => Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, cameraForwardVector);

        protected override string CreateConstrainedBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateConstrainedBillboard)}";

        protected override Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector)
            => Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, cameraForwardVector, objectForwardVector);
    }
}

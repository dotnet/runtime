// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public class Matrix4x4RightHandChangedBasisTests : Matrix4x4LeftHandTests
    {
        protected override string CreateLookAtMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAt)}";
        protected override Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            return RightHandToLeftHand * Matrix4x4.CreateLookAt(
                ChangeLeftHandToRightHand(cameraPosition),
                ChangeLeftHandToRightHand(cameraTarget),
                ChangeLeftHandToRightHand(cameraUpVector)) * LeftHandToRightHand;
        }

        protected override string CreateLookToMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookTo)}";
        protected override Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
        {
            return RightHandToLeftHand * Matrix4x4.CreateLookTo(
                ChangeLeftHandToRightHand(cameraPosition),
                ChangeLeftHandToRightHand(cameraDirection),
                ChangeLeftHandToRightHand(cameraUpVector)) * LeftHandToRightHand;
        }

        protected override string CreateViewportMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewport)}";
        protected override Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
        {
            return RightHandToLeftHand * Matrix4x4.CreateViewport(x, y, width, height, -minDepth, -maxDepth) * LeftHandToRightHand;
        }

        protected override string CreateBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateBillboard)}";
        protected override Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            return RightHandToLeftHand * Matrix4x4.CreateBillboard(
                ChangeLeftHandToRightHand(objectPosition),
                ChangeLeftHandToRightHand(cameraPosition),
                ChangeLeftHandToRightHand(cameraUpVector),
                ChangeLeftHandToRightHand(cameraForwardVector)) * LeftHandToRightHand;
        }

        protected override string CreateConstrainedBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateConstrainedBillboard)}";
        protected override Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector)
        {
            return RightHandToLeftHand * Matrix4x4.CreateConstrainedBillboard(
                ChangeLeftHandToRightHand(objectPosition),
                ChangeLeftHandToRightHand(cameraPosition),
                ChangeLeftHandToRightHand(rotateAxis),
                ChangeLeftHandToRightHand(cameraForwardVector),
                ChangeLeftHandToRightHand(objectForwardVector)) * LeftHandToRightHand;
        }
    }
}

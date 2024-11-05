// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public class Matrix4x4LeftHandChangedBasisTests : Matrix4x4RightHandTests
    {
        protected override string CreateLookAtMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAtLeftHanded)}";
        protected override Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            return LeftHandToRightHand * Matrix4x4.CreateLookAtLeftHanded(
                ChangeRightHandToLeftHand(cameraPosition),
                ChangeRightHandToLeftHand(cameraTarget),
                ChangeRightHandToLeftHand(cameraUpVector)) * RightHandToLeftHand;
        }

        protected override string CreateLookToMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookToLeftHanded)}";
        protected override Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
        {
            return LeftHandToRightHand * Matrix4x4.CreateLookToLeftHanded(
                ChangeRightHandToLeftHand(cameraPosition),
                ChangeRightHandToLeftHand(cameraDirection),
                ChangeRightHandToLeftHand(cameraUpVector)) * RightHandToLeftHand;
        }

        protected override string CreateViewportMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewportLeftHanded)}";
        protected override Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
        {
            return LeftHandToRightHand * Matrix4x4.CreateViewportLeftHanded(x, y, width, height, -minDepth, -maxDepth) * RightHandToLeftHand;
        }

        protected override string CreateBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateBillboardLeftHanded)}";
        protected override Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            return LeftHandToRightHand * Matrix4x4.CreateBillboardLeftHanded(
                ChangeRightHandToLeftHand(objectPosition),
                ChangeRightHandToLeftHand(cameraPosition),
                ChangeRightHandToLeftHand(cameraUpVector),
                ChangeRightHandToLeftHand(cameraForwardVector)) * RightHandToLeftHand;
        }

        protected override string CreateConstrainedBillboardMethodName => $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)}";
        protected override Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector)
        {
            return LeftHandToRightHand * Matrix4x4.CreateConstrainedBillboardLeftHanded(
                ChangeRightHandToLeftHand(objectPosition),
                ChangeRightHandToLeftHand(cameraPosition),
                ChangeRightHandToLeftHand(rotateAxis),
                ChangeRightHandToLeftHand(cameraForwardVector),
                ChangeRightHandToLeftHand(objectForwardVector)) * RightHandToLeftHand;
        }
    }
}

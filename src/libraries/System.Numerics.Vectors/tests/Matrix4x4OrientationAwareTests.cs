// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public abstract class Matrix4x4OrientationAwareTests
    {
        protected static Matrix4x4 RightHandToLeftHand =
            new(1, 0, 0, 0,
                 0, 1, 0, 0,
                 0, 0, -1, 0,
                 0, 0, 0, 1);

        // The basis change is a self-inverse
        protected static Matrix4x4 LeftHandToRightHand = RightHandToLeftHand;

        protected static Vector3 ChangeRightHandToLeftHand(Vector3 v) => new(v.X, v.Y, -v.Z);
        protected static Vector3 ChangeLeftHandToRightHand(Vector3 v) => new(v.X, v.Y, -v.Z);

        protected abstract string CreateLookAtMethodName { get; }
        protected abstract Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector);

        protected abstract string CreateLookToMethodName { get; }
        protected abstract Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector);

        protected abstract string CreateViewportMethodName { get; }
        protected abstract Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth);

        protected abstract string CreateBillboardMethodName { get; }
        protected abstract Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector);

        protected abstract string CreateConstrainedBillboardMethodName { get; }
        protected abstract Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector);
    }
}

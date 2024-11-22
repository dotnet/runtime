// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public abstract class Matrix4x4OrientationAwareTests
    {
        // Both left-to-right and right-to-left have the same transform matrix.
        protected static Matrix4x4 SwapHandednessMatrix =
            new(1, 0, 0, 0,
                 0, 1, 0, 0,
                 0, 0, -1, 0,
                 0, 0, 0, 1);

        protected static Vector3 SwapHandedness(Vector3 v) => new(v.X, v.Y, -v.Z);

        protected abstract Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector);

        protected abstract Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector);

        protected abstract Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth);

        protected abstract Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector);

        protected abstract Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector);
    }
}

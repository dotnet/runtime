// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public class Matrix4x4LeftHandChangedBasisTests : Matrix4x4RightHandTests
    {
        protected override Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            return SwapHandednessMatrix * Matrix4x4.CreateLookAtLeftHanded(
                SwapHandedness(cameraPosition),
                SwapHandedness(cameraTarget),
                SwapHandedness(cameraUpVector)) * SwapHandednessMatrix;
        }

        protected override Matrix4x4 CreateLookTo(Vector3 cameraPosition, Vector3 cameraDirection, Vector3 cameraUpVector)
        {
            return SwapHandednessMatrix * Matrix4x4.CreateLookToLeftHanded(
                SwapHandedness(cameraPosition),
                SwapHandedness(cameraDirection),
                SwapHandedness(cameraUpVector)) * SwapHandednessMatrix;
        }

        protected override Matrix4x4 CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
        {
            return SwapHandednessMatrix * Matrix4x4.CreateViewportLeftHanded(x, y, width, height, -minDepth, -maxDepth) * SwapHandednessMatrix;
        }

        protected override Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            return SwapHandednessMatrix * Matrix4x4.CreateBillboardLeftHanded(
                SwapHandedness(objectPosition),
                SwapHandedness(cameraPosition),
                SwapHandedness(cameraUpVector),
                SwapHandedness(cameraForwardVector)) * SwapHandednessMatrix;
        }

        protected override Matrix4x4 CreateConstrainedBillboard(
            Vector3 objectPosition,
            Vector3 cameraPosition,
            Vector3 rotateAxis,
            Vector3 cameraForwardVector,
            Vector3 objectForwardVector)
        {
            return SwapHandednessMatrix * Matrix4x4.CreateConstrainedBillboardLeftHanded(
                SwapHandedness(objectPosition),
                SwapHandedness(cameraPosition),
                SwapHandedness(rotateAxis),
                SwapHandedness(cameraForwardVector),
                SwapHandedness(objectForwardVector)) * SwapHandednessMatrix;
        }
    }
}

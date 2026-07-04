// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics
{
    public partial struct Matrix4x4
    {
        // See Matrix4x4.cs for an explanation of why this file/type exists
        //
        // Note that we use some particular patterns below, such as defining a result
        // and assigning the fields directly rather than using the object initializer
        // syntax. We do this because it saves roughly 8-bytes of IL per method which
        // in turn helps improve inlining chances.

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Impl AsImpl() => ref Unsafe.As<Matrix4x4, Impl>(ref this);

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref readonly Impl AsROImpl() => ref Unsafe.As<Matrix4x4, Impl>(ref Unsafe.AsRef(in this));

        internal struct Impl : IEquatable<Impl>
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Matrix4x4 AsM4x4() => ref Unsafe.As<Impl, Matrix4x4>(ref this);

            private const float BillboardEpsilon = 1e-4f;
            private const float BillboardMinAngle = 1.0f - (0.1f * (float.Pi / 180.0f)); // 0.1 degrees
            private const float DecomposeEpsilon = 0.0001f;

            public Vector4 X;
            public Vector4 Y;
            public Vector4 Z;
            public Vector4 W;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator +(in Impl left, in Impl right)
            {
                Impl result;

                result.X = left.X + right.X;
                result.Y = left.Y + right.Y;
                result.Z = left.Z + right.Z;
                result.W = left.W + right.W;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(in Impl left, in Impl right)
            {
                return (left.X == right.X)
                    && (left.Y == right.Y)
                    && (left.Z == right.Z)
                    && (left.W == right.W);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(in Impl left, in Impl right)
            {
                return (left.X != right.X)
                    || (left.Y != right.Y)
                    || (left.Z != right.Z)
                    || (left.W != right.W);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator *(in Impl left, float right)
            {
                Impl result;

                result.X = left.X * right;
                result.Y = left.Y * right;
                result.Z = left.Z * right;
                result.W = left.W * right;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator -(in Impl left, in Impl right)
            {
                Impl result;

                result.X = left.X - right.X;
                result.Y = left.Y - right.Y;
                result.Z = left.Z - right.Z;
                result.W = left.W - right.W;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator -(in Impl value)
            {
                Impl result;

                result.X = -value.X;
                result.Y = -value.Y;
                result.Z = -value.Z;
                result.W = -value.W;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateBillboard(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 cameraUpVector, in Vector3 cameraForwardVector)
            {
                // In a right-handed coordinate system, the object's positive z-axis is in the opposite direction as its forward vector,
                // and spherical billboards by construction always face the camera.
                Vector3 axisZ = objectPosition - cameraPosition;

                // When object and camera position are approximately the same, the object should just face the
                // same direction as the camera is facing.
                if (axisZ.LengthSquared() < BillboardEpsilon)
                {
                    axisZ = -cameraForwardVector;
                }
                else
                {
                    axisZ = Vector3.Normalize(axisZ);
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(cameraUpVector, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = axisX.AsVector4();
                result.Y = axisY.AsVector4();
                result.Z = axisZ.AsVector4();
                result.W = Vector4.Create(objectPosition, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateBillboardLeftHanded(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 cameraUpVector, in Vector3 cameraForwardVector)
            {
                // In a left-handed coordinate system, the object's positive z-axis is in the same direction as its forward vector,
                // and spherical billboards by construction always face the camera.
                Vector3 axisZ = cameraPosition - objectPosition;

                // When object and camera position are approximately the same, the object should just face the
                // same direction as the camera is facing.
                if (axisZ.LengthSquared() < BillboardEpsilon)
                {
                    axisZ = cameraForwardVector;
                }
                else
                {
                    axisZ = Vector3.Normalize(axisZ);
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(cameraUpVector, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = axisX.AsVector4();
                result.Y = axisY.AsVector4();
                result.Z = axisZ.AsVector4();
                result.W = Vector4.Create(objectPosition, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateConstrainedBillboard(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 rotateAxis, in Vector3 cameraForwardVector, in Vector3 objectForwardVector)
            {
                // First find the Z-axis of the spherical/unconstrained rotation. We call this faceDir and in a right-handed coordinate system
                // it will be in the opposite direction as from the object to the camera.
                Vector3 faceDir = objectPosition - cameraPosition;

                // When object and camera position are approximately the same this indicates that the object should also just face the
                // same direction as the camera is facing.
                if (faceDir.LengthSquared() < BillboardEpsilon)
                {
                    faceDir = -cameraForwardVector;
                }
                else
                {
                    faceDir = Vector3.Normalize(faceDir);
                }

                Vector3 axisY = rotateAxis;

                float dot = Vector3.Dot(axisY, faceDir);

                // Generally the approximation for small angles is cos theta = 1 - theta^2 / 2,
                // but it seems that here we are using cos theta = 1 - theta. Letting theta be the angle
                // between the rotate axis and the faceDir,
                //
                // dot = cos theta ~ 1 - theta > 1 - .1 * pi/180 = 1 - (.1 degree) => theta < .1 degree
                //
                // So this condition checks if the faceDir is approximately the same as the rotate axis
                // by checking if the angle between them is less than .1 degree.
                if (float.Abs(dot) > BillboardMinAngle)
                {
                    // If the faceDir is approximately the same as the rotate axis, then fallback to using object forward vector
                    // as the faceDir.
                    faceDir = objectForwardVector;

                    dot = Vector3.Dot(axisY, faceDir);

                    // Similar to before, check if the faceDir is still is approximately the rotate axis.
                    // If so, then use either -UnitZ or UnitX as the fallback faceDir.
                    if (float.Abs(dot) > BillboardMinAngle)
                    {
                        // |axisY.Z| = |dot(axisY, -UnitZ)|, so this is checking if the rotate axis is approximately the same as -UnitZ.
                        // If is, then use UnitX as the fallback.
                        faceDir = (float.Abs(axisY.Z) > BillboardMinAngle) ? Vector3.UnitX : Vector3.Create(0, 0, -1);
                    }
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(axisY, faceDir));
                Vector3 axisZ = Vector3.Normalize(Vector3.Cross(axisX, axisY));

                Impl result;

                result.X = axisX.AsVector4();
                result.Y = axisY.AsVector4();
                result.Z = axisZ.AsVector4();
                result.W = Vector4.Create(objectPosition, 1);

                return result;
            }

            public static Impl CreateConstrainedBillboardLeftHanded(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 rotateAxis, in Vector3 cameraForwardVector, in Vector3 objectForwardVector)
            {
                // First find the Z-axis of the spherical/unconstrained rotation. We call this faceDir and in a left-handed coordinate system
                // it will be in the same direction as from the object to the camera.
                Vector3 faceDir = cameraPosition - objectPosition;

                // When object and camera position are approximately the same this indicates that the object should also just face the
                // same direction as the camera is facing.
                if (faceDir.LengthSquared() < BillboardEpsilon)
                {
                    faceDir = cameraForwardVector;
                }
                else
                {
                    faceDir = Vector3.Normalize(faceDir);
                }

                Vector3 axisY = rotateAxis;

                float dot = Vector3.Dot(axisY, faceDir);

                // Generally the approximation for small angles is cos theta = 1 - theta^2 / 2,
                // but it seems that here we are using cos theta = 1 - theta. Letting theta be the angle
                // between the rotate axis and the faceDir,
                //
                // dot = cos theta ~ 1 - theta > 1 - .1 * pi/180 = 1 - (.1 degree) => theta < .1 degree
                //
                // So this condition checks if the faceDir is approximately the same as the rotate axis
                // by checking if the angle between them is less than .1 degree.
                if (float.Abs(dot) > BillboardMinAngle)
                {
                    // If the faceDir is approximately the same as the rotate axis, then fallback to using object forward vector
                    // as the faceDir.
                    faceDir = -objectForwardVector;

                    dot = Vector3.Dot(axisY, faceDir);

                    // Similar to before, check if the faceDir is still is approximately the rotate axis.
                    // If so, then use either -UnitZ or -UnitX as the fallback faceDir.
                    if (float.Abs(dot) > BillboardMinAngle)
                    {
                        // |axisY.Z| = |dot(axisY, -UnitZ)|, so this is checking if the rotate axis is approximately the same as -UnitZ.
                        // If is, then use -UnitX as the fallback.
                        faceDir = (float.Abs(axisY.Z) > BillboardMinAngle) ? Vector3.Create(-1, 0, 0) : Vector3.Create(0, 0, -1);
                    }
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(axisY, faceDir));
                Vector3 axisZ = Vector3.Normalize(Vector3.Cross(axisX, axisY));

                Impl result;

                result.X = axisX.AsVector4();
                result.Y = axisY.AsVector4();
                result.Z = axisZ.AsVector4();
                result.W = Vector4.Create(objectPosition, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateFromAxisAngle(in Vector3 axis, float angle)
            {
                Quaternion q = Quaternion.CreateFromAxisAngle(axis, angle);
                return CreateFromQuaternion(q);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateFromQuaternion(in Quaternion quaternion)
            {
                float xx = quaternion.X * quaternion.X;
                float yy = quaternion.Y * quaternion.Y;
                float zz = quaternion.Z * quaternion.Z;

                float xy = quaternion.X * quaternion.Y;
                float wz = quaternion.Z * quaternion.W;
                float xz = quaternion.Z * quaternion.X;
                float wy = quaternion.Y * quaternion.W;
                float yz = quaternion.Y * quaternion.Z;
                float wx = quaternion.X * quaternion.W;

                Impl result;

                result.X = Vector4.Create(
                    1.0f - 2.0f * (yy + zz),
                    2.0f * (xy + wz),
                    2.0f * (xz - wy),
                    0
                );
                result.Y = Vector4.Create(
                    2.0f * (xy - wz),
                    1.0f - 2.0f * (zz + xx),
                    2.0f * (yz + wx),
                    0
                );
                result.Z = Vector4.Create(
                    2.0f * (xz + wy),
                    2.0f * (yz - wx),
                    1.0f - 2.0f * (yy + xx),
                    0
                );
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateFromYawPitchRoll(float yaw, float pitch, float roll)
            {
                Quaternion q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
                return CreateFromQuaternion(q);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateLookTo(in Vector3 cameraPosition, in Vector3 cameraDirection, in Vector3 cameraUpVector)
            {
                // This implementation is based on the DirectX Math Library XMMatrixLookToRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                return CreateLookToLeftHanded(cameraPosition, -cameraDirection, cameraUpVector);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateLookToLeftHanded(in Vector3 cameraPosition, in Vector3 cameraDirection, in Vector3 cameraUpVector)
            {
                // This implementation is based on the DirectX Math Library XMMatrixLookToLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                Vector3 axisZ = Vector3.Normalize(cameraDirection);
                Vector3 axisX = Vector3.Normalize(Vector3.Cross(cameraUpVector, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);
                Vector3 negativeCameraPosition = -cameraPosition;

                Impl result;

                result.X = Vector4.Create(axisX, Vector3.Dot(axisX, negativeCameraPosition));
                result.Y = Vector4.Create(axisY, Vector3.Dot(axisY, negativeCameraPosition));
                result.Z = Vector4.Create(axisZ, Vector3.Dot(axisZ, negativeCameraPosition));
                result.W = Vector4.UnitW;

                return Transpose(result);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
            {
                // This implementation is based on the DirectX Math Library XMMatrixOrthographicRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                float range = 1.0f / (zNearPlane - zFarPlane);

                Impl result;

                result.X = Vector4.Create(2.0f / width, 0, 0, 0);
                result.Y = Vector4.Create(0, 2.0f / height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 0);
                result.W = Vector4.Create(0, 0, range * zNearPlane, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographicLeftHanded(float width, float height, float zNearPlane, float zFarPlane)
            {
                // This implementation is based on the DirectX Math Library XMMatrixOrthographicLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                float range = 1.0f / (zFarPlane - zNearPlane);

                Impl result;

                result.X = Vector4.Create(2.0f / width, 0, 0, 0);
                result.Y = Vector4.Create(0, 2.0f / height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 0);
                result.W = Vector4.Create(0, 0, -range * zNearPlane, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
            {
                // This implementation is based on the DirectX Math Library XMMatrixOrthographicOffCenterRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                float reciprocalWidth = 1.0f / (right - left);
                float reciprocalHeight = 1.0f / (top - bottom);
                float range = 1.0f / (zNearPlane - zFarPlane);

                Impl result;

                result.X = Vector4.Create(reciprocalWidth + reciprocalWidth, 0, 0, 0);
                result.Y = Vector4.Create(0, reciprocalHeight + reciprocalHeight, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 0);
                result.W = Vector4.Create(
                    -(left + right) * reciprocalWidth,
                    -(top + bottom) * reciprocalHeight,
                    range * zNearPlane,
                    1
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
            {
                // This implementation is based on the DirectX Math Library XMMatrixOrthographicOffCenterLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                float reciprocalWidth = 1.0f / (right - left);
                float reciprocalHeight = 1.0f / (top - bottom);
                float range = 1.0f / (zFarPlane - zNearPlane);

                Impl result;

                result.X = Vector4.Create(reciprocalWidth + reciprocalWidth, 0, 0, 0);
                result.Y = Vector4.Create(0, reciprocalHeight + reciprocalHeight, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 0);
                result.W = Vector4.Create(
                    -(left + right) * reciprocalWidth,
                    -(top + bottom) * reciprocalHeight,
                    -range * zNearPlane,
                    1
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float dblNearPlaneDistance = nearPlaneDistance + nearPlaneDistance;
                float range = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = Vector4.Create(dblNearPlaneDistance / width, 0, 0, 0);
                result.Y = Vector4.Create(0, dblNearPlaneDistance / height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, -1.0f);
                result.W = Vector4.Create(0, 0, range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveLeftHanded(float width, float height, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float dblNearPlaneDistance = nearPlaneDistance + nearPlaneDistance;
                float range = float.IsPositiveInfinity(farPlaneDistance) ? 1.0f : farPlaneDistance / (farPlaneDistance - nearPlaneDistance);

                Impl result;

                result.X = Vector4.Create(dblNearPlaneDistance / width, 0, 0, 0);
                result.Y = Vector4.Create(0, dblNearPlaneDistance / height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 1.0f);
                result.W = Vector4.Create(0, 0, -range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveFovRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, float.Pi);

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float height = 1.0f / float.Tan(fieldOfView * 0.5f);
                float width = height / aspectRatio;
                float range = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = Vector4.Create(width, 0, 0, 0);
                result.Y = Vector4.Create(0, height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, -1.0f);
                result.W = Vector4.Create(0, 0, range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveFieldOfViewLeftHanded(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveFovLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, float.Pi);

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float height = 1.0f / float.Tan(fieldOfView * 0.5f);
                float width = height / aspectRatio;
                float range = float.IsPositiveInfinity(farPlaneDistance) ? 1.0f : farPlaneDistance / (farPlaneDistance - nearPlaneDistance);

                Impl result;

                result.X = Vector4.Create(width, 0, 0, 0);
                result.Y = Vector4.Create(0, height, 0, 0);
                result.Z = Vector4.Create(0, 0, range, 1.0f);
                result.W = Vector4.Create(0, 0, -range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveOffCenterRH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float dblNearPlaneDistance = nearPlaneDistance + nearPlaneDistance;
                float reciprocalWidth = 1.0f / (right - left);
                float reciprocalHeight = 1.0f / (top - bottom);
                float range = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = Vector4.Create(dblNearPlaneDistance * reciprocalWidth, 0, 0, 0);
                result.Y = Vector4.Create(0, dblNearPlaneDistance * reciprocalHeight, 0, 0);
                result.Z = Vector4.Create(
                    (left + right) * reciprocalWidth,
                    (top + bottom) * reciprocalHeight,
                    range,
                    -1.0f
                );
                result.W = Vector4.Create(0, 0, range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveOffCenterLeftHanded(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
            {
                // This implementation is based on the DirectX Math Library XMMatrixPerspectiveOffCenterLH method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float dblNearPlaneDistance = nearPlaneDistance + nearPlaneDistance;
                float reciprocalWidth = 1.0f / (right - left);
                float reciprocalHeight = 1.0f / (top - bottom);
                float range = float.IsPositiveInfinity(farPlaneDistance) ? 1.0f : farPlaneDistance / (farPlaneDistance - nearPlaneDistance);

                Impl result;

                result.X = Vector4.Create(dblNearPlaneDistance * reciprocalWidth, 0, 0, 0);
                result.Y = Vector4.Create(0, dblNearPlaneDistance * reciprocalHeight, 0, 0);
                result.Z = Vector4.Create(
                    -(left + right) * reciprocalWidth,
                    -(top + bottom) * reciprocalHeight,
                    range,
                    1.0f
                );
                result.W = Vector4.Create(0, 0, -range * nearPlaneDistance, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateReflection(in Plane value)
            {
                // This implementation is based on the DirectX Math Library XMMatrixReflect method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                Vector4 p = Plane.Normalize(value).AsVector4();
                Vector4 s = p * Vector4.Create(-2.0f, -2.0f, -2.0f, 0.0f);

                Impl result;

                result.X = Vector4.MultiplyAddEstimate(Vector4.Create(p.X), s, Vector4.UnitX);
                result.Y = Vector4.MultiplyAddEstimate(Vector4.Create(p.Y), s, Vector4.UnitY);
                result.Z = Vector4.MultiplyAddEstimate(Vector4.Create(p.Z), s, Vector4.UnitZ);
                result.W = Vector4.MultiplyAddEstimate(Vector4.Create(p.W), s, Vector4.UnitW);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationX(float radians)
            {
                (float s, float c) = float.SinCos(radians);

                // [  1  0  0  0 ]
                // [  0  c  s  0 ]
                // [  0 -s  c  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.Create(0,  c, s, 0);
                result.Z = Vector4.Create(0, -s, c, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationX(float radians, in Vector3 centerPoint)
            {
                (float s, float c) = float.SinCos(radians);

                float y = float.MultiplyAddEstimate(centerPoint.Y, 1 - c, +centerPoint.Z * s);
                float z = float.MultiplyAddEstimate(centerPoint.Z, 1 - c, -centerPoint.Y * s);

                // [  1  0  0  0 ]
                // [  0  c  s  0 ]
                // [  0 -s  c  0 ]
                // [  0  y  z  1 ]

                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.Create(0,  c, s, 0);
                result.Z = Vector4.Create(0, -s, c, 0);
                result.W = Vector4.Create(0,  y, z, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationY(float radians)
            {
                (float s, float c) = float.SinCos(radians);

                // [  c  0 -s  0 ]
                // [  0  1  0  0 ]
                // [  s  0  c  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = Vector4.Create(c, 0, -s, 0);
                result.Y = Vector4.UnitY;
                result.Z = Vector4.Create(s, 0,  c, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationY(float radians, in Vector3 centerPoint)
            {
                (float s, float c) = float.SinCos(radians);

                float x = float.MultiplyAddEstimate(centerPoint.X, 1 - c, -centerPoint.Z * s);
                float z = float.MultiplyAddEstimate(centerPoint.Z, 1 - c, +centerPoint.X * s);

                // [  c  0 -s  0 ]
                // [  0  1  0  0 ]
                // [  s  0  c  0 ]
                // [  x  0  z  1 ]

                Impl result;

                result.X = Vector4.Create(c, 0, -s, 0);
                result.Y = Vector4.UnitY;
                result.Z = Vector4.Create(s, 0,  c, 0);
                result.W = Vector4.Create(x, 0,  z, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationZ(float radians)
            {
                (float s, float c) = float.SinCos(radians);

                // [  c  s  0  0 ]
                // [ -s  c  0  0 ]
                // [  0  0  1  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = Vector4.Create( c, s, 0, 0);
                result.Y = Vector4.Create(-s, c, 0, 0);
                result.Z = Vector4.UnitZ;
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationZ(float radians, in Vector3 centerPoint)
            {
                (float s, float c) = float.SinCos(radians);

                float x = float.MultiplyAddEstimate(centerPoint.X, 1 - c, +centerPoint.Y * s);
                float y = float.MultiplyAddEstimate(centerPoint.Y, 1 - c, -centerPoint.X * s);

                // [  c  s  0  0 ]
                // [ -s  c  0  0 ]
                // [  0  0  1  0 ]
                // [  x  y  0  1 ]

                Impl result;

                result.X = Vector4.Create( c, s, 0, 0);
                result.Y = Vector4.Create(-s, c, 0, 0);
                result.Z = Vector4.UnitZ;
                result.W = Vector4.Create(x, y, 0, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY, float scaleZ)
            {
                Impl result;

                result.X = Vector4.Create(scaleX, 0, 0, 0);
                result.Y = Vector4.Create(0, scaleY, 0, 0);
                result.Z = Vector4.Create(0, 0, scaleZ, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY, float scaleZ, in Vector3 centerPoint)
            {
                Impl result;

                result.X = Vector4.Create(scaleX, 0, 0, 0);
                result.Y = Vector4.Create(0, scaleY, 0, 0);
                result.Z = Vector4.Create(0, 0, scaleZ, 0);
                result.W = Vector4.Create(centerPoint * (Vector3.One - Vector3.Create(scaleX, scaleY, scaleZ)), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(in Vector3 scales)
            {
                Impl result;

                result.X = Vector4.Create(scales.X, 0, 0, 0);
                result.Y = Vector4.Create(0, scales.Y, 0, 0);
                result.Z = Vector4.Create(0, 0, scales.Z, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(in Vector3 scales, in Vector3 centerPoint)
            {
                Impl result;

                result.X = Vector4.Create(scales.X, 0, 0, 0);
                result.Y = Vector4.Create(0, scales.Y, 0, 0);
                result.Z = Vector4.Create(0, 0, scales.Z, 0);
                result.W = Vector4.Create(centerPoint * (Vector3.One - scales), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale)
            {
                Impl result;

                result.X = Vector4.Create(scale, 0, 0, 0);
                result.Y = Vector4.Create(0, scale, 0, 0);
                result.Z = Vector4.Create(0, 0, scale, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale, in Vector3 centerPoint)
            {
                Impl result;

                result.X = Vector4.Create(scale, 0, 0, 0);
                result.Y = Vector4.Create(0, scale, 0, 0);
                result.Z = Vector4.Create(0, 0, scale, 0);
                result.W = Vector4.Create(centerPoint * (Vector3.One - Vector3.Create(scale)), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateShadow(in Vector3 lightDirection, in Plane plane)
            {
                Vector4 p = Plane.Normalize(plane).AsVector4();
                Vector4 l = lightDirection.AsVector4();
                float dot = Vector4.Dot(p, l);

                p = -p;

                Impl result;

                result.X = Vector4.MultiplyAddEstimate(l, Vector4.Create(p.X), Vector4.Create(dot, 0, 0, 0));
                result.Y = Vector4.MultiplyAddEstimate(l, Vector4.Create(p.Y), Vector4.Create(0, dot, 0, 0));
                result.Z = Vector4.MultiplyAddEstimate(l, Vector4.Create(p.Z), Vector4.Create(0, 0, dot, 0));
                result.W = Vector4.MultiplyAddEstimate(l, Vector4.Create(p.W), Vector4.Create(0, 0, 0, dot));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(in Vector3 position)
            {
                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.UnitY;
                result.Z = Vector4.UnitZ;
                result.W = Vector4.Create(position, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(float positionX, float positionY, float positionZ)
            {
                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.UnitY;
                result.Z = Vector4.UnitZ;
                result.W = Vector4.Create(positionX, positionY, positionZ, 1);

                return result;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
            {
                Impl result;

                // 4x SIMD fields to get a lot better codegen
                result.W = Vector4.Create(width, height, 0f, 0f);
                result.W *= Vector4.Create(0.5f, 0.5f, 0f, 0f);

                result.X = Vector4.Create(result.W.X, 0f, 0f, 0f);
                result.Y = Vector4.Create(0f, -result.W.Y, 0f, 0f);
                result.Z = Vector4.Create(0f, 0f, minDepth - maxDepth, 0f);
                result.W += Vector4.Create(x, y, minDepth, 1f);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateViewportLeftHanded(float x, float y, float width, float height, float minDepth, float maxDepth)
            {
                Impl result;

                // 4x SIMD fields to get a lot better codegen
                result.W = Vector4.Create(width, height, 0f, 0f);
                result.W *= Vector4.Create(0.5f, 0.5f, 0f, 0f);

                result.X = Vector4.Create(result.W.X, 0f, 0f, 0f);
                result.Y = Vector4.Create(0f, -result.W.Y, 0f, 0f);
                result.Z = Vector4.Create(0f, 0f, maxDepth - minDepth, 0f);
                result.W += Vector4.Create(x, y, minDepth, 1f);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateWorld(in Vector3 position, in Vector3 forward, in Vector3 up)
            {
                Vector3 axisZ = Vector3.Normalize(-forward);
                Vector3 axisX = Vector3.Normalize(Vector3.Cross(up, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = axisX.AsVector4();
                result.Y = axisY.AsVector4();
                result.Z = axisZ.AsVector4();
                result.W = Vector4.Create(position, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool Decompose(in Impl matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
            {
                Impl matTemp = Matrix4x4.Identity.AsImpl();

                Vector3* canonicalBasis = stackalloc Vector3[3] {
                    Vector3.UnitX,
                    Vector3.UnitY,
                    Vector3.UnitZ,
                };

                translation = matrix.W.AsVector3();

                Vector3** vectorBasis = stackalloc Vector3*[3] {
                    (Vector3*)&matTemp.X,
                    (Vector3*)&matTemp.Y,
                    (Vector3*)&matTemp.Z,
                };

                *(vectorBasis[0]) = matrix.X.AsVector3();
                *(vectorBasis[1]) = matrix.Y.AsVector3();
                *(vectorBasis[2]) = matrix.Z.AsVector3();

                float* scales = stackalloc float[3] {
                    vectorBasis[0]->Length(),
                    vectorBasis[1]->Length(),
                    vectorBasis[2]->Length(),
                };

                uint a, b, c;

                #region Ranking
                float x = scales[0];
                float y = scales[1];
                float z = scales[2];

                if (x < y)
                {
                    if (y < z)
                    {
                        a = 2;
                        b = 1;
                        c = 0;
                    }
                    else
                    {
                        a = 1;

                        if (x < z)
                        {
                            b = 2;
                            c = 0;
                        }
                        else
                        {
                            b = 0;
                            c = 2;
                        }
                    }
                }
                else
                {
                    if (x < z)
                    {
                        a = 2;
                        b = 0;
                        c = 1;
                    }
                    else
                    {
                        a = 0;

                        if (y < z)
                        {
                            b = 2;
                            c = 1;
                        }
                        else
                        {
                            b = 1;
                            c = 2;
                        }
                    }
                }
                #endregion

                if (scales[a] < DecomposeEpsilon)
                {
                    *(vectorBasis[a]) = canonicalBasis[a];
                }

                *vectorBasis[a] = Vector3.Normalize(*vectorBasis[a]);

                if (scales[b] < DecomposeEpsilon)
                {
                    uint cc;
                    float fAbsX, fAbsY, fAbsZ;

                    fAbsX = float.Abs(vectorBasis[a]->X);
                    fAbsY = float.Abs(vectorBasis[a]->Y);
                    fAbsZ = float.Abs(vectorBasis[a]->Z);

                    #region Ranking
                    if (fAbsX < fAbsY)
                    {
                        if (fAbsY < fAbsZ)
                        {
                            cc = 0;
                        }
                        else
                        {
                            if (fAbsX < fAbsZ)
                            {
                                cc = 0;
                            }
                            else
                            {
                                cc = 2;
                            }
                        }
                    }
                    else
                    {
                        if (fAbsX < fAbsZ)
                        {
                            cc = 1;
                        }
                        else
                        {
                            if (fAbsY < fAbsZ)
                            {
                                cc = 1;
                            }
                            else
                            {
                                cc = 2;
                            }
                        }
                    }
                    #endregion

                    *vectorBasis[b] = Vector3.Cross(*vectorBasis[a], canonicalBasis[cc]);
                }

                *vectorBasis[b] = Vector3.Normalize(*vectorBasis[b]);

                if (scales[c] < DecomposeEpsilon)
                {
                    *vectorBasis[c] = Vector3.Cross(*vectorBasis[a], *vectorBasis[b]);
                }

                *vectorBasis[c] = Vector3.Normalize(*vectorBasis[c]);

                float det = matTemp.GetDeterminant();

                // use Kramer's rule to check for handedness of coordinate system
                if (det < 0.0f)
                {
                    // switch coordinate system by negating the scale and inverting the basis vector on the x-axis
                    scales[a] = -scales[a];
                    *vectorBasis[a] = -(*vectorBasis[a]);

                    det = -det;
                }

                det -= 1.0f;
                det *= det;

                bool result;

                if (DecomposeEpsilon < det)
                {
                    // Non-SRT matrix encountered
                    rotation = Quaternion.Identity;
                    result = false;
                }
                else
                {
                    // generate the quaternion from the matrix
                    rotation = Quaternion.CreateFromRotationMatrix(matTemp.AsM4x4());
                    result = true;
                }

                scale = Unsafe.ReadUnaligned<Vector3>(scales);
                return result;
            }

            public static bool Invert(in Impl matrix, out Impl result)
            {
                // This implementation is based on the DirectX Math Library XMMatrixInverse method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                //                                       -1
                // If you have matrix M, inverse Matrix M   can compute
                //
                //     -1       1
                //    M   = --------- A
                //            det(M)
                //
                // A is adjugate (adjoint) of M, where,
                //
                //      T
                // A = C
                //
                // C is Cofactor matrix of M, where,
                //           i + j
                // C   = (-1)      * det(M  )
                //  ij                    ij
                //
                //     [ a b c d ]
                // M = [ e f g h ]
                //     [ i j k l ]
                //     [ m n o p ]
                //
                // First Row
                //           2 | f g h |
                // C   = (-1)  | j k l | = + ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
                //  11         | n o p |
                //
                //           3 | e g h |
                // C   = (-1)  | i k l | = - ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
                //  12         | m o p |
                //
                //           4 | e f h |
                // C   = (-1)  | i j l | = + ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
                //  13         | m n p |
                //
                //           5 | e f g |
                // C   = (-1)  | i j k | = - ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
                //  14         | m n o |
                //
                // Second Row
                //           3 | b c d |
                // C   = (-1)  | j k l | = - ( b ( kp - lo ) - c ( jp - ln ) + d ( jo - kn ) )
                //  21         | n o p |
                //
                //           4 | a c d |
                // C   = (-1)  | i k l | = + ( a ( kp - lo ) - c ( ip - lm ) + d ( io - km ) )
                //  22         | m o p |
                //
                //           5 | a b d |
                // C   = (-1)  | i j l | = - ( a ( jp - ln ) - b ( ip - lm ) + d ( in - jm ) )
                //  23         | m n p |
                //
                //           6 | a b c |
                // C   = (-1)  | i j k | = + ( a ( jo - kn ) - b ( io - km ) + c ( in - jm ) )
                //  24         | m n o |
                //
                // Third Row
                //           4 | b c d |
                // C   = (-1)  | f g h | = + ( b ( gp - ho ) - c ( fp - hn ) + d ( fo - gn ) )
                //  31         | n o p |
                //
                //           5 | a c d |
                // C   = (-1)  | e g h | = - ( a ( gp - ho ) - c ( ep - hm ) + d ( eo - gm ) )
                //  32         | m o p |
                //
                //           6 | a b d |
                // C   = (-1)  | e f h | = + ( a ( fp - hn ) - b ( ep - hm ) + d ( en - fm ) )
                //  33         | m n p |
                //
                //           7 | a b c |
                // C   = (-1)  | e f g | = - ( a ( fo - gn ) - b ( eo - gm ) + c ( en - fm ) )
                //  34         | m n o |
                //
                // Fourth Row
                //           5 | b c d |
                // C   = (-1)  | f g h | = - ( b ( gl - hk ) - c ( fl - hj ) + d ( fk - gj ) )
                //  41         | j k l |
                //
                //           6 | a c d |
                // C   = (-1)  | e g h | = + ( a ( gl - hk ) - c ( el - hi ) + d ( ek - gi ) )
                //  42         | i k l |
                //
                //           7 | a b d |
                // C   = (-1)  | e f h | = - ( a ( fl - hj ) - b ( el - hi ) + d ( ej - fi ) )
                //  43         | i j l |
                //
                //           8 | a b c |
                // C   = (-1)  | e f g | = + ( a ( fk - gj ) - b ( ek - gi ) + c ( ej - fi ) )
                //  44         | i j k |

                // Load the matrix values into rows
                Vector128<float> row1 = matrix.X.AsVector128();
                Vector128<float> row2 = matrix.Y.AsVector128();
                Vector128<float> row3 = matrix.Z.AsVector128();
                Vector128<float> row4 = matrix.W.AsVector128();

                // Transpose the matrix
                Vector128<float> vTemp1 = Vector128.ConcatLowerLower(row1, row2);
                Vector128<float> vTemp3 = Vector128.ConcatUpperUpper(row1, row2);
                Vector128<float> vTemp2 = Vector128.ConcatLowerLower(row3, row4);
                Vector128<float> vTemp4 = Vector128.ConcatUpperUpper(row3, row4);

                row1 = Vector128.UnzipEven(vTemp1, vTemp2);
                row2 = Vector128.UnzipOdd(vTemp1, vTemp2);
                row3 = Vector128.UnzipEven(vTemp3, vTemp4);
                row4 = Vector128.UnzipOdd(vTemp3, vTemp4);

                Vector128<float> V00 = Vector128.Shuffle(row3, Vector128.Create(0, 0, 1, 1));
                Vector128<float> V10 = Vector128.Shuffle(row4, Vector128.Create(2, 3, 2, 3));
                Vector128<float> V01 = Vector128.Shuffle(row1, Vector128.Create(0, 0, 1, 1));
                Vector128<float> V11 = Vector128.Shuffle(row2, Vector128.Create(2, 3, 2, 3));
                Vector128<float> V02 = Vector128.UnzipEven(row3, row1);
                Vector128<float> V12 = Vector128.UnzipOdd(row4, row2);

                Vector128<float> D0 = V00 * V10;
                Vector128<float> D1 = V01 * V11;
                Vector128<float> D2 = V02 * V12;

                V00 = Vector128.Shuffle(row3, Vector128.Create(2, 3, 2, 3));
                V10 = Vector128.Shuffle(row4, Vector128.Create(0, 0, 1, 1));
                V01 = Vector128.Shuffle(row1, Vector128.Create(2, 3, 2, 3));
                V11 = Vector128.Shuffle(row2, Vector128.Create(0, 0, 1, 1));
                V02 = Vector128.UnzipOdd(row3, row1);
                V12 = Vector128.UnzipEven(row4, row2);

                D0 = Vector128.MultiplyAddEstimate(-V00, V10, D0);
                D1 = Vector128.MultiplyAddEstimate(-V01, V11, D1);
                D2 = Vector128.MultiplyAddEstimate(-V02, V12, D2);

                // V11 = D0Y,D0W,D2Y,D2Y
                V11 = Shuffle2(D0, D2, 1, 3, 1, 1);
                V00 = Vector128.Shuffle(row2, Vector128.Create(1, 2, 0, 1));
                V10 = Shuffle2(V11, D0, 2, 0, 3, 0);
                V01 = Vector128.Shuffle(row1, Vector128.Create(2, 0, 1, 0));
                V11 = Shuffle2(V11, D0, 1, 2, 1, 2);

                // V13 = D1Y,D1W,D2W,D2W
                Vector128<float> V13 = Shuffle2(D1, D2, 1, 3, 3, 3);
                V02 = Vector128.Shuffle(row4, Vector128.Create(1, 2, 0, 1));
                V12 = Shuffle2(V13, D1, 2, 0, 3, 0);
                Vector128<float> V03 = Vector128.Shuffle(row3, Vector128.Create(2, 0, 1, 0));
                V13 = Shuffle2(V13, D1, 1, 2, 1, 2);

                Vector128<float> C0 = V00 * V10;
                Vector128<float> C2 = V01 * V11;
                Vector128<float> C4 = V02 * V12;
                Vector128<float> C6 = V03 * V13;

                // V11 = D0X,D0Y,D2X,D2X
                V11 = Shuffle2(D0, D2, 0, 1, 0, 0);
                V00 = Vector128.Shuffle(row2, Vector128.Create(2, 3, 1, 2));
                V10 = Shuffle2(D0, V11, 3, 0, 1, 2);
                V01 = Vector128.Shuffle(row1, Vector128.Create(3, 2, 3, 1));
                V11 = Shuffle2(D0, V11, 2, 1, 2, 0);

                // V13 = D1X,D1Y,D2Z,D2Z
                V13 = Shuffle2(D1, D2, 0, 1, 2, 2);
                V02 = Vector128.Shuffle(row4, Vector128.Create(2, 3, 1, 2));
                V12 = Shuffle2(D1, V13, 3, 0, 1, 2);
                V03 = Vector128.Shuffle(row3, Vector128.Create(3, 2, 3, 1));
                V13 = Shuffle2(D1, V13, 2, 1, 2, 0);

                C0 = Vector128.MultiplyAddEstimate(-V00, V10, C0);
                C2 = Vector128.MultiplyAddEstimate(-V01, V11, C2);
                C4 = Vector128.MultiplyAddEstimate(-V02, V12, C4);
                C6 = Vector128.MultiplyAddEstimate(-V03, V13, C6);

                V00 = Vector128.Shuffle(row2, Vector128.Create(3, 0, 3, 0));

                // V10 = D0Z,D0Z,D2X,D2Y
                V10 = Shuffle2(D0, D2, 2, 2, 0, 1);
                V10 = Vector128.Shuffle(V10, Vector128.Create(0, 3, 2, 0));
                V01 = Vector128.Shuffle(row1, Vector128.Create(1, 3, 0, 2));

                // V11 = D0X,D0W,D2X,D2Y
                V11 = Shuffle2(D0, D2, 0, 3, 0, 1);
                V11 = Vector128.Shuffle(V11, Vector128.Create(3, 0, 1, 2));
                V02 = Vector128.Shuffle(row4, Vector128.Create(3, 0, 3, 0));

                // V12 = D1Z,D1Z,D2Z,D2W
                V12 = Shuffle2(D1, D2, 2, 2, 2, 3);
                V12 = Vector128.Shuffle(V12, Vector128.Create(0, 3, 2, 0));
                V03 = Vector128.Shuffle(row3, Vector128.Create(1, 3, 0, 2));

                // V13 = D1X,D1W,D2Z,D2W
                V13 = Shuffle2(D1, D2, 0, 3, 2, 3);
                V13 = Vector128.Shuffle(V13, Vector128.Create(3, 0, 1, 2));

                V00 *= V10;
                V01 *= V11;
                V02 *= V12;
                V03 *= V13;

                Vector128<float> C1 = C0 - V00;
                C0 += V00;

                Vector128<float> C3 = C2 + V01;
                C2 -= V01;

                Vector128<float> C5 = C4 - V02;
                C4 += V02;

                Vector128<float> C7 = C6 + V03;
                C6 -= V03;

                C0 = Shuffle2(C0, C1, 0, 2, 1, 3);
                C2 = Shuffle2(C2, C3, 0, 2, 1, 3);
                C4 = Shuffle2(C4, C5, 0, 2, 1, 3);
                C6 = Shuffle2(C6, C7, 0, 2, 1, 3);

                C0 = Vector128.Shuffle(C0, Vector128.Create(0, 2, 1, 3));
                C2 = Vector128.Shuffle(C2, Vector128.Create(0, 2, 1, 3));
                C4 = Vector128.Shuffle(C4, Vector128.Create(0, 2, 1, 3));
                C6 = Vector128.Shuffle(C6, Vector128.Create(0, 2, 1, 3));

                // Get the determinant
                float det = Vector4.Dot(C0.AsVector4(), row1.AsVector4());

                // Check determinant is not zero
                if (float.Abs(det) < float.Epsilon)
                {
                    Vector4 vNaN = Vector4.NaN;

                    result.X = vNaN;
                    result.Y = vNaN;
                    result.Z = vNaN;
                    result.W = vNaN;

                    return false;
                }

                // Create Vector128<float> copy of the determinant and invert them.

                Vector128<float> vTemp = Vector128<float>.One / det;

                result.X = (C0 * vTemp).AsVector4();
                result.Y = (C2 * vTemp).AsVector4();
                result.Z = (C4 * vTemp).AsVector4();
                result.W = (C6 * vTemp).AsVector4();

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl Lerp(in Impl left, in Impl right, float amount)
            {
                Impl result;

                result.X = Vector4.Lerp(left.X, right.X, amount);
                result.Y = Vector4.Lerp(left.Y, right.Y, amount);
                result.Z = Vector4.Lerp(left.Z, right.Z, amount);
                result.W = Vector4.Lerp(left.W, right.W, amount);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl Transform(in Impl value, in Quaternion rotation)
            {
                // Compute rotation matrix.
                float x2 = rotation.X + rotation.X;
                float y2 = rotation.Y + rotation.Y;
                float z2 = rotation.Z + rotation.Z;

                float wx2 = rotation.W * x2;
                float wy2 = rotation.W * y2;
                float wz2 = rotation.W * z2;

                float xx2 = rotation.X * x2;
                float xy2 = rotation.X * y2;
                float xz2 = rotation.X * z2;

                float yy2 = rotation.Y * y2;
                float yz2 = rotation.Y * z2;
                float zz2 = rotation.Z * z2;

                float q11 = 1.0f - yy2 - zz2;
                float q21 = xy2 - wz2;
                float q31 = xz2 + wy2;

                float q12 = xy2 + wz2;
                float q22 = 1.0f - xx2 - zz2;
                float q32 = yz2 - wx2;

                float q13 = xz2 - wy2;
                float q23 = yz2 + wx2;
                float q33 = 1.0f - xx2 - yy2;

                Impl result;

                result.X = Vector4.Create(
                    value.X.X * q11 + value.X.Y * q21 + value.X.Z * q31,
                    value.X.X * q12 + value.X.Y * q22 + value.X.Z * q32,
                    value.X.X * q13 + value.X.Y * q23 + value.X.Z * q33,
                    value.X.W
                );
                result.Y = Vector4.Create(
                    value.Y.X * q11 + value.Y.Y * q21 + value.Y.Z * q31,
                    value.Y.X * q12 + value.Y.Y * q22 + value.Y.Z * q32,
                    value.Y.X * q13 + value.Y.Y * q23 + value.Y.Z * q33,
                    value.Y.W
                );
                result.Z = Vector4.Create(
                    value.Z.X * q11 + value.Z.Y * q21 + value.Z.Z * q31,
                    value.Z.X * q12 + value.Z.Y * q22 + value.Z.Z * q32,
                    value.Z.X * q13 + value.Z.Y * q23 + value.Z.Z * q33,
                    value.Z.W
                );
                result.W = Vector4.Create(
                    value.W.X * q11 + value.W.Y * q21 + value.W.Z * q31,
                    value.W.X * q12 + value.W.Y * q22 + value.W.Z * q32,
                    value.W.X * q13 + value.W.Y * q23 + value.W.Z * q33,
                    value.W.W
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl Transpose(in Impl matrix)
            {
                // This implementation is based on the DirectX Math Library XMMatrixTranspose method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                Impl result;

                Vector128<float> x = matrix.X.AsVector128();
                Vector128<float> y = matrix.Y.AsVector128();
                Vector128<float> z = matrix.Z.AsVector128();
                Vector128<float> w = matrix.W.AsVector128();

                Vector128<float> lowerXZ = Vector128.ZipLower(x, z);         // x[0], z[0], x[1], z[1]
                Vector128<float> lowerYW = Vector128.ZipLower(y, w);         // y[0], w[0], y[1], w[1]
                Vector128<float> upperXZ = Vector128.ZipUpper(x, z);         // x[2], z[2], x[3], z[3]
                Vector128<float> upperYW = Vector128.ZipUpper(y, w);         // y[2], w[2], y[3], w[3]

                result.X = Vector128.ZipLower(lowerXZ, lowerYW).AsVector4(); // x[0], y[0], z[0], w[0]
                result.Y = Vector128.ZipUpper(lowerXZ, lowerYW).AsVector4(); // x[1], y[1], z[1], w[1]
                result.Z = Vector128.ZipLower(upperXZ, upperYW).AsVector4(); // x[2], y[2], z[2], w[2]
                result.W = Vector128.ZipUpper(upperXZ, upperYW).AsVector4(); // x[3], y[3], z[3], w[3]

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override readonly bool Equals([NotNullWhen(true)] object? obj)
                => (obj is Matrix4x4 other) && Equals(in other.AsImpl());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Equals(in Impl other)
            {
                // This function needs to account for floating-point equality around NaN
                // and so must behave equivalently to the underlying float/double.Equals

                return X.Equals(other.X)
                    && Y.Equals(other.Y)
                    && Z.Equals(other.Z)
                    && W.Equals(other.W);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly float GetDeterminant()
            {
                // | a b c d |     | f g h |     | e g h |     | e f h |     | e f g |
                // | e f g h | = a | j k l | - b | i k l | + c | i j l | - d | i j k |
                // | i j k l |     | n o p |     | m o p |     | m n p |     | m n o |
                // | m n o p |
                //
                //   | f g h |
                // a | j k l | = a ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
                //   | n o p |
                //
                //   | e g h |
                // b | i k l | = b ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
                //   | m o p |
                //
                //   | e f h |
                // c | i j l | = c ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
                //   | m n p |
                //
                //   | e f g |
                // d | i j k | = d ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
                //   | m n o |

                Vector128<float> w = W.AsVector128();
                Vector128<float> x = X.AsVector128();
                Vector128<float> y = Y.AsVector128();
                Vector128<float> z = Z.AsVector128();

                Vector128<float> z_kjji = Vector128.Shuffle(z, Vector128.Create(2, 1, 1, 0));
                Vector128<float> z_iiij = Vector128.Shuffle(z, Vector128.Create(0, 0, 0, 1));
                Vector128<float> w_ppop = Vector128.Shuffle(w, Vector128.Create(3, 3, 2, 3));
                Vector128<float> w_onpo = Vector128.Shuffle(w, Vector128.Create(2, 1, 3, 2));
                Vector128<float> z_llkl = Vector128.Shuffle(z, Vector128.Create(3, 3, 2, 3));
                Vector128<float> z_kjlk = Vector128.Shuffle(z, Vector128.Create(2, 1, 3, 2));
                Vector128<float> w_onnm = Vector128.Shuffle(w, Vector128.Create(2, 1, 1, 0));
                Vector128<float> w_mmmn = Vector128.Shuffle(w, Vector128.Create(0, 0, 0, 1));
                Vector128<float> y_feee = Vector128.Shuffle(y, Vector128.Create(1, 0, 0, 0));
                Vector128<float> y_ggff = Vector128.Shuffle(y, Vector128.Create(2, 2, 1, 1));
                Vector128<float> y_hhhg = Vector128.Shuffle(y, Vector128.Create(3, 3, 3, 2));

                // tmp1[0] = kp_lo
                // tmp1[1] = jp_ln
                // tmp1[2] = jo_kn
                // tmp1[3] = ip_lm
                Vector128<float> tmp1 = z_kjji * w_ppop - z_llkl * w_onnm;

                // tmp2[0] = io_km
                // tmp2[1] = in_jm
                // tmp2[2] = ip_lm
                // tmp2[3] = jo_kn
                Vector128<float> tmp2 = z_iiij * w_onpo - z_kjlk * w_mmmn;

                // tmp3[0] = kp_lo
                // tmp3[1] = kp_lo
                // tmp3[2] = jp_ln
                // tmp3[3] = jo_kn
                Vector128<float> tmp3 = Vector128.Shuffle(tmp1, Vector128.Create(0, 0, 1, 2));

                // tmp4[0] = jp_ln
                // tmp4[1] = ip_lm
                // tmp4[2] = ip_lm
                // tmp4[3] = io_km
                Vector128<float> tmp4 = Shuffle2(tmp1, tmp2, 1, 3, 2, 0);

                // tmp5[0] = jo_kn
                // tmp5[1] = io_km
                // tmp5[2] = in_jm
                // tmp5[3] = in_jm
                Vector128<float> tmp5 = Vector128.Shuffle(tmp2, Vector128.Create(3, 0, 1, 1));

                Vector128<float> tmp6 = x * (y_feee * tmp3 - y_ggff * tmp4 + y_hhhg * tmp5);
                Vector128<float> tmp7 = tmp6 - Vector128.Shuffle(tmp6, Vector128.Create(1, 0, 3, 0));
                return tmp7[0] + tmp7[2];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

            readonly bool IEquatable<Impl>.Equals(Impl other) => Equals(in other);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<float> Shuffle2(Vector128<float> lower, Vector128<float> upper, [ConstantExpected] byte xIndex, [ConstantExpected] byte yIndex, [ConstantExpected] byte zIndex, [ConstantExpected] byte wIndex)
            {
                if (RuntimeHelpers.IsKnownConstant(xIndex) && RuntimeHelpers.IsKnownConstant(yIndex) && RuntimeHelpers.IsKnownConstant(zIndex) && RuntimeHelpers.IsKnownConstant(wIndex))
                {
                    if (Sse.IsSupported)
                    {
#pragma warning disable CA1857 // The parameter expects a constant for optimal performance
                        return Sse.Shuffle(lower, upper, (byte)((wIndex << 6) | (zIndex << 4) | (yIndex << 2) | xIndex));
#pragma warning restore CA1857 // The parameter expects a constant for optimal performance
                    }
                    else if (AdvSimd.Arm64.IsSupported)
                    {
                        return AdvSimd.Arm64.VectorTableLookup((lower.AsByte(), upper.AsByte()), ((Vector128.Create(xIndex, yIndex, zIndex, wIndex) * Vector128.Create(0x04040404)) + Vector128.Create(0x03020100, 0x03020100, 0x13121110, 0x13121110)).AsByte()).AsSingle();
                    }
                    else if (PackedSimd.IsSupported)
                    {
                        return PackedSimd.Shuffle(lower.AsByte(), upper.AsByte(), ((Vector128.Create(xIndex, yIndex, zIndex, wIndex) * Vector128.Create(0x04040404)) + Vector128.Create(0x03020100, 0x03020100, 0x13121110, 0x13121110)).AsByte()).AsSingle();
                    }
                }

                return Vector128.ConcatLowerLower(
                    Vector128.Shuffle(lower, Vector128.Create(xIndex, yIndex, 0, 0)),
                    Vector128.Shuffle(upper, Vector128.Create(zIndex, wIndex, 0, 0))
                );
            }
        }
    }
}

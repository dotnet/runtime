// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
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

        // TODO: Vector3 is "inefficient" and we'd be better off taking Vector4 or Vector128<T>

        internal const uint RowCount = 4;
        internal const uint ColumnCount = 4;

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
            private const float BillboardMinAngle = 1.0f - (0.1f * (MathF.PI / 180.0f)); // 0.1 degrees
            private const float DecomposeEpsilon = 0.0001f;

            public Vector4 X;
            public Vector4 Y;
            public Vector4 Z;
            public Vector4 W;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(float m11, float m12, float m13, float m14,
                             float m21, float m22, float m23, float m24,
                             float m31, float m32, float m33, float m34,
                             float m41, float m42, float m43, float m44)
            {
                X = new Vector4(m11, m12, m13, m14);
                Y = new Vector4(m21, m22, m23, m24);
                Z = new Vector4(m31, m32, m33, m34);
                W = new Vector4(m41, m42, m43, m44);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(in Matrix3x2.Impl value)
            {
                X = new Vector4(value.X, 0, 0);
                Y = new Vector4(value.Y, 0, 0);
                Z = Vector4.UnitZ;
                W = new Vector4(value.Z, 0, 1);
            }

            public static Impl Identity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    Impl result;

                    result.X = Vector4.UnitX;
                    result.Y = Vector4.UnitY;
                    result.Z = Vector4.UnitZ;
                    result.W = Vector4.UnitW;

                    return result;
                }
            }

            public float this[int row, int column]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                readonly get
                {
                    if ((uint)row >= RowCount)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }

                    return Unsafe.Add(ref Unsafe.AsRef(in this.X), row)[column];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    if ((uint)row >= RowCount)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    Unsafe.Add(ref this.X, row)[column] = value;
                }
            }

            public readonly bool IsIdentity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (X == Vector4.UnitX)
                        && (Y == Vector4.UnitY)
                        && (Z == Vector4.UnitZ)
                        && (W == Vector4.UnitW);
                }
            }

            public Vector3 Translation
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                readonly get => new Vector3(W.X, W.Y, W.Z);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    W = new Vector4(value, W.W);
                }
            }

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
            public static Impl operator *(in Impl left, in Impl right)
            {
                Impl result;

                // result.X = Transform(left.X, in right);
                result.X = right.X * left.X.X;
                result.X += right.Y * left.X.Y;
                result.X += right.Z * left.X.Z;
                result.X += right.W * left.X.W;

                // result.Y = Transform(left.Y, in right);
                result.Y = right.X * left.Y.X;
                result.Y += right.Y * left.Y.Y;
                result.Y += right.Z * left.Y.Z;
                result.Y += right.W * left.Y.W;

                // result.Z = Transform(left.Z, in right);
                result.Z = right.X * left.Z.X;
                result.Z += right.Y * left.Z.Y;
                result.Z += right.Z * left.Z.Z;
                result.Z += right.W * left.Z.W;

                // result.W = Transform(left.W, in right);
                result.W = right.X * left.W.X;
                result.W += right.Y * left.W.Y;
                result.W += right.Z * left.W.Z;
                result.W += right.W * left.W.W;

                return result;
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
                Vector3 axisZ = objectPosition - cameraPosition;
                float norm = axisZ.LengthSquared();

                if (norm < BillboardEpsilon)
                {
                    axisZ = -cameraForwardVector;
                }
                else
                {
                    axisZ = Vector3.Multiply(axisZ, 1.0f / MathF.Sqrt(norm));
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(cameraUpVector, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = new Vector4(axisX, 0);
                result.Y = new Vector4(axisY, 0);
                result.Z = new Vector4(axisZ, 0);
                result.W = new Vector4(objectPosition, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateConstrainedBillboard(in Vector3 objectPosition, in Vector3 cameraPosition, in Vector3 rotateAxis, in Vector3 cameraForwardVector, in Vector3 objectForwardVector)
            {
                // Treat the case when object and camera positions are too close.
                Vector3 faceDir = objectPosition - cameraPosition;
                float norm = faceDir.LengthSquared();

                if (norm < BillboardEpsilon)
                {
                    faceDir = -cameraForwardVector;
                }
                else
                {
                    faceDir = Vector3.Multiply(faceDir, (1.0f / MathF.Sqrt(norm)));
                }

                Vector3 axisY = rotateAxis;

                // Treat the case when angle between faceDir and rotateAxis is too close to 0.
                float dot = Vector3.Dot(axisY, faceDir);

                if (MathF.Abs(dot) > BillboardMinAngle)
                {
                    faceDir = objectForwardVector;

                    // Make sure passed values are useful for compute.
                    dot = Vector3.Dot(axisY, faceDir);

                    if (MathF.Abs(dot) > BillboardMinAngle)
                    {
                        faceDir = (MathF.Abs(axisY.Z) > BillboardMinAngle) ? Vector3.UnitX : new Vector3(0, 0, -1);
                    }
                }

                Vector3 axisX = Vector3.Normalize(Vector3.Cross(axisY, faceDir));
                Vector3 axisZ = Vector3.Normalize(Vector3.Cross(axisX, axisY));

                Impl result;

                result.X = new Vector4(axisX, 0);
                result.Y = new Vector4(axisY, 0);
                result.Z = new Vector4(axisZ, 0);
                result.W = new Vector4(objectPosition, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateFromAxisAngle(in Vector3 axis, float angle)
            {
                // a: angle
                // x, y, z: unit vector for axis.
                //
                // Rotation matrix M can compute by using below equation.
                //
                //        T               T
                //  M = uu + (cos a)( I-uu ) + (sin a)S
                //
                // Where:
                //
                //  u = ( x, y, z )
                //
                //      [  0 -z  y ]
                //  S = [  z  0 -x ]
                //      [ -y  x  0 ]
                //
                //      [ 1 0 0 ]
                //  I = [ 0 1 0 ]
                //      [ 0 0 1 ]
                //
                //
                //     [  xx+cosa*(1-xx)   yx-cosa*yx-sina*z zx-cosa*xz+sina*y ]
                // M = [ xy-cosa*yx+sina*z    yy+cosa(1-yy)  yz-cosa*yz-sina*x ]
                //     [ zx-cosa*zx-sina*y zy-cosa*zy+sina*x   zz+cosa*(1-zz)  ]
                //

                float x = axis.X;
                float y = axis.Y;
                float z = axis.Z;

                float sa = MathF.Sin(angle);
                float ca = MathF.Cos(angle);

                float xx = x * x;
                float yy = y * y;
                float zz = z * z;

                float xy = x * y;
                float xz = x * z;
                float yz = y * z;

                Impl result;

                result.X = new Vector4(
                    xx + ca * (1.0f - xx),
                    xy - ca * xy + sa * z,
                    xz - ca * xz - sa * y,
                    0
                );
                result.Y = new Vector4(
                    xy - ca * xy - sa * z,
                    yy + ca * (1.0f - yy),
                    yz - ca * yz + sa * x,
                    0
                );
                result.Z = new Vector4(
                    xz - ca * xz + sa * y,
                    yz - ca * yz - sa * x,
                    zz + ca * (1.0f - zz),
                    0
                );
                result.W = Vector4.UnitW;

                return result;
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

                result.X = new Vector4(
                    1.0f - 2.0f * (yy + zz),
                    2.0f * (xy + wz),
                    2.0f * (xz - wy),
                    0
                );
                result.Y = new Vector4(
                    2.0f * (xy - wz),
                    1.0f - 2.0f * (zz + xx),
                    2.0f * (yz + wx),
                    0
                );
                result.Z = new Vector4(
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
            public static Impl CreateLookAt(in Vector3 cameraPosition, in Vector3 cameraTarget, in Vector3 cameraUpVector)
            {
                Vector3 axisZ = Vector3.Normalize(cameraPosition - cameraTarget);
                Vector3 axisX = Vector3.Normalize(Vector3.Cross(cameraUpVector, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = new Vector4(
                    axisX.X,
                    axisY.X,
                    axisZ.X,
                    0
                );
                result.Y = new Vector4(
                    axisX.Y,
                    axisY.Y,
                    axisZ.Y,
                    0
                );
                result.Z = new Vector4(
                    axisX.Z,
                    axisY.Z,
                    axisZ.Z,
                    0
                );
                result.W = new Vector4(
                    -Vector3.Dot(axisX, cameraPosition),
                    -Vector3.Dot(axisY, cameraPosition),
                    -Vector3.Dot(axisZ, cameraPosition),
                    1
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
            {
                Impl result;

                result.X = new Vector4(2.0f / width, 0, 0, 0);
                result.Y = new Vector4(0, 2.0f / height, 0, 0);
                result.Z = new Vector4(0, 0, 1.0f / (zNearPlane - zFarPlane), 0);
                result.W = new Vector4(0, 0, zNearPlane / (zNearPlane - zFarPlane), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
            {
                Impl result;

                result.X = new Vector4(2.0f / (right - left), 0, 0, 0);
                result.Y = new Vector4(0, 2.0f / (top - bottom), 0, 0);
                result.Z = new Vector4(0, 0, 1.0f / (zNearPlane - zFarPlane), 0);
                result.W = new Vector4(
                    (left + right) / (left - right),
                    (top + bottom) / (bottom - top),
                    zNearPlane / (zNearPlane - zFarPlane),
                    1
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = new Vector4(2.0f * nearPlaneDistance / width, 0, 0, 0);
                result.Y = new Vector4(0, 2.0f * nearPlaneDistance / height, 0, 0);
                result.Z = new Vector4(0, 0, negFarRange, -1.0f);
                result.W = new Vector4(0, 0, nearPlaneDistance * negFarRange, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, MathF.PI);

                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float scaleY = 1.0f / MathF.Tan(fieldOfView * 0.5f);
                float scaleX = scaleY / aspectRatio;
                float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = new Vector4(scaleX, 0, 0, 0);
                result.Y = new Vector4(0, scaleY, 0, 0);
                result.Z = new Vector4(0, 0, negFarRange, -1.0f);
                result.W = new Vector4(0, 0, nearPlaneDistance * negFarRange, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(farPlaneDistance, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearPlaneDistance, farPlaneDistance);

                float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

                Impl result;

                result.X = new Vector4(2.0f * nearPlaneDistance / (right - left), 0, 0, 0);
                result.Y = new Vector4(0, 2.0f * nearPlaneDistance / (top - bottom), 0, 0);
                result.Z = new Vector4(
                    (left + right) / (right - left),
                    (top + bottom) / (top - bottom),
                    negFarRange,
                    -1.0f
                );
                result.W = new Vector4(0, 0, nearPlaneDistance * negFarRange, 0);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateReflection(in Plane value)
            {
                Plane p = Plane.Normalize(value);
                Vector3 f = p.Normal * -2.0f;

                Impl result;

                result.X = new Vector4(f * p.Normal.X, 0) + Vector4.UnitX;
                result.Y = new Vector4(f * p.Normal.Y, 0) + Vector4.UnitY;
                result.Z = new Vector4(f * p.Normal.Z, 0) + Vector4.UnitZ;
                result.W = new Vector4(f * p.D, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationX(float radians)
            {
                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                // [  1  0  0  0 ]
                // [  0  c  s  0 ]
                // [  0 -s  c  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = Vector4.UnitX;
                result.Y = new Vector4(0,  c, s, 0);
                result.Z = new Vector4(0, -s, c, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationX(float radians, in Vector3 centerPoint)
            {
                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                float y = centerPoint.Y * (1 - c) + centerPoint.Z * s;
                float z = centerPoint.Z * (1 - c) - centerPoint.Y * s;

                // [  1  0  0  0 ]
                // [  0  c  s  0 ]
                // [  0 -s  c  0 ]
                // [  0  y  z  1 ]

                Impl result;

                result.X = Vector4.UnitX;
                result.Y = new Vector4(0,  c, s, 0);
                result.Z = new Vector4(0, -s, c, 0);
                result.W = new Vector4(0,  y, z, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationY(float radians)
            {
                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                // [  c  0 -s  0 ]
                // [  0  1  0  0 ]
                // [  s  0  c  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = new Vector4(c, 0, -s, 0);
                result.Y = Vector4.UnitY;
                result.Z = new Vector4(s, 0,  c, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationY(float radians, in Vector3 centerPoint)
            {
                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                float x = centerPoint.X * (1 - c) - centerPoint.Z * s;
                float z = centerPoint.Z * (1 - c) + centerPoint.X * s;

                // [  c  0 -s  0 ]
                // [  0  1  0  0 ]
                // [  s  0  c  0 ]
                // [  x  0  z  1 ]

                Impl result;

                result.X = new Vector4(c, 0, -s, 0);
                result.Y = Vector4.UnitY;
                result.Z = new Vector4(s, 0,  c, 0);
                result.W = new Vector4(x, 0,  z, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationZ(float radians)
            {

                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                // [  c  s  0  0 ]
                // [ -s  c  0  0 ]
                // [  0  0  1  0 ]
                // [  0  0  0  1 ]

                Impl result;

                result.X = new Vector4( c, s, 0, 0);
                result.Y = new Vector4(-s, c, 0, 0);
                result.Z = Vector4.UnitZ;
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotationZ(float radians, in Vector3 centerPoint)
            {
                float c = MathF.Cos(radians);
                float s = MathF.Sin(radians);

                float x = centerPoint.X * (1 - c) + centerPoint.Y * s;
                float y = centerPoint.Y * (1 - c) - centerPoint.X * s;

                // [  c  s  0  0 ]
                // [ -s  c  0  0 ]
                // [  0  0  1  0 ]
                // [  x  y  0  1 ]

                Impl result;

                result.X = new Vector4( c, s, 0, 0);
                result.Y = new Vector4(-s, c, 0, 0);
                result.Z = Vector4.UnitZ;
                result.W = new Vector4(x, y, 0, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY, float scaleZ)
            {
                Impl result;

                result.X = new Vector4(scaleX, 0, 0, 0);
                result.Y = new Vector4(0, scaleY, 0, 0);
                result.Z = new Vector4(0, 0, scaleZ, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY, float scaleZ, in Vector3 centerPoint)
            {
                Impl result;

                result.X = new Vector4(scaleX, 0, 0, 0);
                result.Y = new Vector4(0, scaleY, 0, 0);
                result.Z = new Vector4(0, 0, scaleZ, 0);
                result.W = new Vector4(centerPoint * (Vector3.One - new Vector3(scaleX, scaleY, scaleZ)), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(in Vector3 scales)
            {
                Impl result;

                result.X = new Vector4(scales.X, 0, 0, 0);
                result.Y = new Vector4(0, scales.Y, 0, 0);
                result.Z = new Vector4(0, 0, scales.Z, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(in Vector3 scales, in Vector3 centerPoint)
            {
                Impl result;

                result.X = new Vector4(scales.X, 0, 0, 0);
                result.Y = new Vector4(0, scales.Y, 0, 0);
                result.Z = new Vector4(0, 0, scales.Z, 0);
                result.W = new Vector4(centerPoint * (Vector3.One - scales), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale)
            {
                Impl result;

                result.X = new Vector4(scale, 0, 0, 0);
                result.Y = new Vector4(0, scale, 0, 0);
                result.Z = new Vector4(0, 0, scale, 0);
                result.W = Vector4.UnitW;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale, in Vector3 centerPoint)
            {
                Impl result;

                result.X = new Vector4(scale, 0, 0, 0);
                result.Y = new Vector4(0, scale, 0, 0);
                result.Z = new Vector4(0, 0, scale, 0);
                result.W = new Vector4(centerPoint * (Vector3.One - new Vector3(scale)), 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateShadow(in Vector3 lightDirection, in Plane plane)
            {
                Plane p = Plane.Normalize(plane);
                float dot = Vector3.Dot(lightDirection, p.Normal);

                Vector3 normal = -p.Normal;

                Impl result;

                result.X = new Vector4(lightDirection * normal.X, 0) + new Vector4(dot, 0, 0, 0);
                result.Y = new Vector4(lightDirection * normal.Y, 0) + new Vector4(0, dot, 0, 0);
                result.Z = new Vector4(lightDirection * normal.Z, 0) + new Vector4(0, 0, dot, 0);
                result.W = new Vector4(lightDirection * -p.D, dot);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(in Vector3 position)
            {
                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.UnitY;
                result.Z = Vector4.UnitZ;
                result.W = new Vector4(position, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(float positionX, float positionY, float positionZ)
            {
                Impl result;

                result.X = Vector4.UnitX;
                result.Y = Vector4.UnitY;
                result.Z = Vector4.UnitZ;
                result.W = new Vector4(positionX, positionY, positionZ, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateWorld(in Vector3 position, in Vector3 forward, in Vector3 up)
            {
                Vector3 axisZ = Vector3.Normalize(-forward);
                Vector3 axisX = Vector3.Normalize(Vector3.Cross(up, axisZ));
                Vector3 axisY = Vector3.Cross(axisZ, axisX);

                Impl result;

                result.X = new Vector4(axisX, 0);
                result.Y = new Vector4(axisY, 0);
                result.Z = new Vector4(axisZ, 0);
                result.W = new Vector4(position, 1);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool Decompose(in Impl matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
            {
                bool result = true;

                fixed (Vector3* scaleBase = &scale)
                {
                    float* pfScales = (float*)scaleBase;
                    float det;

                    VectorBasis vectorBasis;
                    Vector3** pVectorBasis = (Vector3**)&vectorBasis;

                    Impl matTemp = Identity;
                    CanonicalBasis canonicalBasis = default;
                    Vector3* pCanonicalBasis = &canonicalBasis.Row0;

                    canonicalBasis.Row0 = new Vector3(1.0f, 0.0f, 0.0f);
                    canonicalBasis.Row1 = new Vector3(0.0f, 1.0f, 0.0f);
                    canonicalBasis.Row2 = new Vector3(0.0f, 0.0f, 1.0f);

                    translation = new Vector3(
                        matrix.W.X,
                        matrix.W.Y,
                        matrix.W.Z);

                    pVectorBasis[0] = (Vector3*)&matTemp.X;
                    pVectorBasis[1] = (Vector3*)&matTemp.Y;
                    pVectorBasis[2] = (Vector3*)&matTemp.Z;

                    *(pVectorBasis[0]) = new Vector3(matrix.X.X, matrix.X.Y, matrix.X.Z);
                    *(pVectorBasis[1]) = new Vector3(matrix.Y.X, matrix.Y.Y, matrix.Y.Z);
                    *(pVectorBasis[2]) = new Vector3(matrix.Z.X, matrix.Z.Y, matrix.Z.Z);

                    scale.X = pVectorBasis[0]->Length();
                    scale.Y = pVectorBasis[1]->Length();
                    scale.Z = pVectorBasis[2]->Length();

                    uint a, b, c;

                    #region Ranking
                    float x = pfScales[0];
                    float y = pfScales[1];
                    float z = pfScales[2];

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

                    if (pfScales[a] < DecomposeEpsilon)
                    {
                        *(pVectorBasis[a]) = pCanonicalBasis[a];
                    }

                    *pVectorBasis[a] = Vector3.Normalize(*pVectorBasis[a]);

                    if (pfScales[b] < DecomposeEpsilon)
                    {
                        uint cc;
                        float fAbsX, fAbsY, fAbsZ;

                        fAbsX = MathF.Abs(pVectorBasis[a]->X);
                        fAbsY = MathF.Abs(pVectorBasis[a]->Y);
                        fAbsZ = MathF.Abs(pVectorBasis[a]->Z);

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

                        *pVectorBasis[b] = Vector3.Cross(*pVectorBasis[a], *(pCanonicalBasis + cc));
                    }

                    *pVectorBasis[b] = Vector3.Normalize(*pVectorBasis[b]);

                    if (pfScales[c] < DecomposeEpsilon)
                    {
                        *pVectorBasis[c] = Vector3.Cross(*pVectorBasis[a], *pVectorBasis[b]);
                    }

                    *pVectorBasis[c] = Vector3.Normalize(*pVectorBasis[c]);

                    det = matTemp.GetDeterminant();

                    // use Kramer's rule to check for handedness of coordinate system
                    if (det < 0.0f)
                    {
                        // switch coordinate system by negating the scale and inverting the basis vector on the x-axis
                        pfScales[a] = -pfScales[a];
                        *pVectorBasis[a] = -(*pVectorBasis[a]);

                        det = -det;
                    }

                    det -= 1.0f;
                    det *= det;

                    if ((DecomposeEpsilon < det))
                    {
                        // Non-SRT matrix encountered
                        rotation = Quaternion.Identity;
                        result = false;
                    }
                    else
                    {
                        // generate the quaternion from the matrix
                        rotation = Quaternion.CreateFromRotationMatrix(Unsafe.As<Impl, Matrix4x4>(ref matTemp));
                    }
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Invert(in Impl matrix, out Impl result)
            {
                // This implementation is based on the DirectX Math Library XMMatrixInverse method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                if (Sse.IsSupported)
                {
                    return SseImpl(in matrix, out result);
                }

                return SoftwareFallback(in matrix, out result);

                [CompExactlyDependsOn(typeof(Sse))]
                static bool SseImpl(in Impl matrix, out Impl result)
                {
                    if (!Sse.IsSupported)
                    {
                        // Redundant test so we won't prejit remainder of this method on platforms without SSE.
                        throw new PlatformNotSupportedException();
                    }

                    // Load the matrix values into rows
                    Vector128<float> row1 = matrix.X.AsVector128();
                    Vector128<float> row2 = matrix.Y.AsVector128();
                    Vector128<float> row3 = matrix.Z.AsVector128();
                    Vector128<float> row4 = matrix.W.AsVector128();

                    // Transpose the matrix
                    Vector128<float> vTemp1 = Sse.Shuffle(row1, row2, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
                    Vector128<float> vTemp3 = Sse.Shuffle(row1, row2, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)
                    Vector128<float> vTemp2 = Sse.Shuffle(row3, row4, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
                    Vector128<float> vTemp4 = Sse.Shuffle(row3, row4, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)

                    row1 = Sse.Shuffle(vTemp1, vTemp2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                    row2 = Sse.Shuffle(vTemp1, vTemp2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
                    row3 = Sse.Shuffle(vTemp3, vTemp4, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                    row4 = Sse.Shuffle(vTemp3, vTemp4, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

                    Vector128<float> V00 = Permute(row3, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                    Vector128<float> V10 = Permute(row4, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                    Vector128<float> V01 = Permute(row1, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                    Vector128<float> V11 = Permute(row2, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                    Vector128<float> V02 = Sse.Shuffle(row3, row1, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                    Vector128<float> V12 = Sse.Shuffle(row4, row2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

                    Vector128<float> D0 = V00 * V10;
                    Vector128<float> D1 = V01 * V11;
                    Vector128<float> D2 = V02 * V12;

                    V00 = Permute(row3, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                    V10 = Permute(row4, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                    V01 = Permute(row1, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                    V11 = Permute(row2, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                    V02 = Sse.Shuffle(row3, row1, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
                    V12 = Sse.Shuffle(row4, row2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)

                    // Note:  We use this expansion pattern instead of Fused Multiply Add
                    // in order to support older hardware
                    D0 -= V00 * V10;
                    D1 -= V01 * V11;
                    D2 -= V02 * V12;

                    // V11 = D0Y,D0W,D2Y,D2Y
                    V11 = Sse.Shuffle(D0, D2, 0x5D);  //_MM_SHUFFLE(1, 1, 3, 1)
                    V00 = Permute(row2, 0x49);        //_MM_SHUFFLE(1, 0, 2, 1)
                    V10 = Sse.Shuffle(V11, D0, 0x32); //_MM_SHUFFLE(0, 3, 0, 2)
                    V01 = Permute(row1, 0x12);        //_MM_SHUFFLE(0, 1, 0, 2)
                    V11 = Sse.Shuffle(V11, D0, 0x99); //_MM_SHUFFLE(2, 1, 2, 1)

                    // V13 = D1Y,D1W,D2W,D2W
                    Vector128<float> V13 = Sse.Shuffle(D1, D2, 0xFD); //_MM_SHUFFLE(3, 3, 3, 1)
                    V02 = Permute(row4, 0x49);                        //_MM_SHUFFLE(1, 0, 2, 1)
                    V12 = Sse.Shuffle(V13, D1, 0x32);                 //_MM_SHUFFLE(0, 3, 0, 2)
                    Vector128<float> V03 = Permute(row3, 0x12);       //_MM_SHUFFLE(0, 1, 0, 2)
                    V13 = Sse.Shuffle(V13, D1, 0x99);                 //_MM_SHUFFLE(2, 1, 2, 1)

                    Vector128<float> C0 = V00 * V10;
                    Vector128<float> C2 = V01 * V11;
                    Vector128<float> C4 = V02 * V12;
                    Vector128<float> C6 = V03 * V13;

                    // V11 = D0X,D0Y,D2X,D2X
                    V11 = Sse.Shuffle(D0, D2, 0x4);    //_MM_SHUFFLE(0, 0, 1, 0)
                    V00 = Permute(row2, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
                    V10 = Sse.Shuffle(D0, V11, 0x93);  //_MM_SHUFFLE(2, 1, 0, 3)
                    V01 = Permute(row1, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
                    V11 = Sse.Shuffle(D0, V11, 0x26);  //_MM_SHUFFLE(0, 2, 1, 2)

                    // V13 = D1X,D1Y,D2Z,D2Z
                    V13 = Sse.Shuffle(D1, D2, 0xa4);   //_MM_SHUFFLE(2, 2, 1, 0)
                    V02 = Permute(row4, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
                    V12 = Sse.Shuffle(D1, V13, 0x93);  //_MM_SHUFFLE(2, 1, 0, 3)
                    V03 = Permute(row3, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
                    V13 = Sse.Shuffle(D1, V13, 0x26);  //_MM_SHUFFLE(0, 2, 1, 2)

                    C0 -= V00 * V10;
                    C2 -= V01 * V11;
                    C4 -= V02 * V12;
                    C6 -= V03 * V13;

                    V00 = Permute(row2, 0x33); //_MM_SHUFFLE(0, 3, 0, 3)

                    // V10 = D0Z,D0Z,D2X,D2Y
                    V10 = Sse.Shuffle(D0, D2, 0x4A); //_MM_SHUFFLE(1, 0, 2, 2)
                    V10 = Permute(V10, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
                    V01 = Permute(row1, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

                    // V11 = D0X,D0W,D2X,D2Y
                    V11 = Sse.Shuffle(D0, D2, 0x4C); //_MM_SHUFFLE(1, 0, 3, 0)
                    V11 = Permute(V11, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)
                    V02 = Permute(row4, 0x33);       //_MM_SHUFFLE(0, 3, 0, 3)

                    // V12 = D1Z,D1Z,D2Z,D2W
                    V12 = Sse.Shuffle(D1, D2, 0xEA); //_MM_SHUFFLE(3, 2, 2, 2)
                    V12 = Permute(V12, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
                    V03 = Permute(row3, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

                    // V13 = D1X,D1W,D2Z,D2W
                    V13 = Sse.Shuffle(D1, D2, 0xEC); //_MM_SHUFFLE(3, 2, 3, 0)
                    V13 = Permute(V13, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)

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

                    C0 = Sse.Shuffle(C0, C1, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C2 = Sse.Shuffle(C2, C3, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C4 = Sse.Shuffle(C4, C5, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C6 = Sse.Shuffle(C6, C7, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                    C0 = Permute(C0, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C2 = Permute(C2, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C4 = Permute(C4, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                    C6 = Permute(C6, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                    // Get the determinant
                    float det = Vector4.Dot(C0.AsVector4(), row1.AsVector4());

                    // Check determinate is not zero
                    if (MathF.Abs(det) < float.Epsilon)
                    {
                        Vector4 vNaN = new Vector4(float.NaN);

                        result.X = vNaN;
                        result.Y = vNaN;
                        result.Z = vNaN;
                        result.W = vNaN;

                        return false;
                    }

                    // Create Vector128<float> copy of the determinant and invert them.

                    Vector128<float> ones = Vector128.Create(1.0f);
                    Vector128<float> vTemp = Vector128.Create(det);

                    vTemp = ones / vTemp;

                    result.X = (C0 * vTemp).AsVector4();
                    result.Y = (C2 * vTemp).AsVector4();
                    result.Z = (C4 * vTemp).AsVector4();
                    result.W = (C6 * vTemp).AsVector4();

                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector128<float> Permute(Vector128<float> value, [ConstantExpected] byte control)
                {
                    if (Avx.IsSupported)
                    {
                        return Avx.Permute(value, control);
                    }
                    else if (Sse.IsSupported)
                    {
                        return Sse.Shuffle(value, value, control);
                    }
                    else
                    {
                        // Redundant test so we won't prejit remainder of this method on platforms without SSE.
                        throw new PlatformNotSupportedException();
                    }
                }

                static bool SoftwareFallback(in Impl matrix, out Impl result)
                {
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
                    //
                    // Cost of operation
                    // 53 adds, 104 muls, and 1 div.

                    float a = matrix.X.X, b = matrix.X.Y, c = matrix.X.Z, d = matrix.X.W;
                    float e = matrix.Y.X, f = matrix.Y.Y, g = matrix.Y.Z, h = matrix.Y.W;
                    float i = matrix.Z.X, j = matrix.Z.Y, k = matrix.Z.Z, l = matrix.Z.W;
                    float m = matrix.W.X, n = matrix.W.Y, o = matrix.W.Z, p = matrix.W.W;

                    float kp_lo = k * p - l * o;
                    float jp_ln = j * p - l * n;
                    float jo_kn = j * o - k * n;
                    float ip_lm = i * p - l * m;
                    float io_km = i * o - k * m;
                    float in_jm = i * n - j * m;

                    float a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
                    float a12 = -(e * kp_lo - g * ip_lm + h * io_km);
                    float a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
                    float a14 = -(e * jo_kn - f * io_km + g * in_jm);

                    float det = a * a11 + b * a12 + c * a13 + d * a14;

                    if (MathF.Abs(det) < float.Epsilon)
                    {
                        Vector4 vNaN = new Vector4(float.NaN);

                        result.X = vNaN;
                        result.Y = vNaN;
                        result.Z = vNaN;
                        result.W = vNaN;

                        return false;
                    }

                    float invDet = 1.0f / det;

                    result.X.X = a11 * invDet;
                    result.Y.X = a12 * invDet;
                    result.Z.X = a13 * invDet;
                    result.W.X = a14 * invDet;

                    result.X.Y = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
                    result.Y.Y = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
                    result.Z.Y = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
                    result.W.Y = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

                    float gp_ho = g * p - h * o;
                    float fp_hn = f * p - h * n;
                    float fo_gn = f * o - g * n;
                    float ep_hm = e * p - h * m;
                    float eo_gm = e * o - g * m;
                    float en_fm = e * n - f * m;

                    result.X.Z = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
                    result.Y.Z = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
                    result.Z.Z = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
                    result.W.Z = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

                    float gl_hk = g * l - h * k;
                    float fl_hj = f * l - h * j;
                    float fk_gj = f * k - g * j;
                    float el_hi = e * l - h * i;
                    float ek_gi = e * k - g * i;
                    float ej_fi = e * j - f * i;

                    result.X.W = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
                    result.Y.W = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
                    result.Z.W = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
                    result.W.W = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

                    return true;
                }
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

                result.X = new Vector4(
                    value.X.X * q11 + value.X.Y * q21 + value.X.Z * q31,
                    value.X.X * q12 + value.X.Y * q22 + value.X.Z * q32,
                    value.X.X * q13 + value.X.Y * q23 + value.X.Z * q33,
                    value.X.W
                );
                result.Y = new Vector4(
                    value.Y.X * q11 + value.Y.Y * q21 + value.Y.Z * q31,
                    value.Y.X * q12 + value.Y.Y * q22 + value.Y.Z * q32,
                    value.Y.X * q13 + value.Y.Y * q23 + value.Y.Z * q33,
                    value.Y.W
                );
                result.Z = new Vector4(
                    value.Z.X * q11 + value.Z.Y * q21 + value.Z.Z * q31,
                    value.Z.X * q12 + value.Z.Y * q22 + value.Z.Z * q32,
                    value.Z.X * q13 + value.Z.Y * q23 + value.Z.Z * q33,
                    value.Z.W
                );
                result.W = new Vector4(
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

                if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<float> x = matrix.X.AsVector128();
                    Vector128<float> y = matrix.Y.AsVector128();
                    Vector128<float> z = matrix.Z.AsVector128();
                    Vector128<float> w = matrix.W.AsVector128();

                    Vector128<float> lowerXZ = AdvSimd.Arm64.ZipLow(x, z);          // x[0], z[0], x[1], z[1]
                    Vector128<float> lowerYW = AdvSimd.Arm64.ZipLow(y, w);          // y[0], w[0], y[1], w[1]
                    Vector128<float> upperXZ = AdvSimd.Arm64.ZipHigh(x, z);         // x[2], z[2], x[3], z[3]
                    Vector128<float> upperYW = AdvSimd.Arm64.ZipHigh(y, w);         // y[2], w[2], y[3], z[3]

                    result.X = AdvSimd.Arm64.ZipLow(lowerXZ, lowerYW).AsVector4();  // x[0], y[0], z[0], w[0]
                    result.Y = AdvSimd.Arm64.ZipHigh(lowerXZ, lowerYW).AsVector4(); // x[1], y[1], z[1], w[1]
                    result.Z = AdvSimd.Arm64.ZipLow(upperXZ, upperYW).AsVector4();  // x[2], y[2], z[2], w[2]
                    result.W = AdvSimd.Arm64.ZipHigh(upperXZ, upperYW).AsVector4(); // x[3], y[3], z[3], w[3]
                }
                else if (Sse.IsSupported)
                {
                    Vector128<float> x = matrix.X.AsVector128();
                    Vector128<float> y = matrix.Y.AsVector128();
                    Vector128<float> z = matrix.Z.AsVector128();
                    Vector128<float> w = matrix.W.AsVector128();

                    Vector128<float> lowerXZ = Sse.UnpackLow(x, z);                 // x[0], z[0], x[1], z[1]
                    Vector128<float> lowerYW = Sse.UnpackLow(y, w);                 // y[0], w[0], y[1], w[1]
                    Vector128<float> upperXZ = Sse.UnpackHigh(x, z);                // x[2], z[2], x[3], z[3]
                    Vector128<float> upperYW = Sse.UnpackHigh(y, w);                // y[2], w[2], y[3], z[3]

                    result.X = Sse.UnpackLow(lowerXZ, lowerYW).AsVector4();         // x[0], y[0], z[0], w[0]
                    result.Y = Sse.UnpackHigh(lowerXZ, lowerYW).AsVector4();        // x[1], y[1], z[1], w[1]
                    result.Z = Sse.UnpackLow(upperXZ, upperYW).AsVector4();         // x[2], y[2], z[2], w[2]
                    result.W = Sse.UnpackHigh(upperXZ, upperYW).AsVector4();        // x[3], y[3], z[3], w[3]
                }
                else
                {
                    result.X = new Vector4(matrix.X.X, matrix.Y.X, matrix.Z.X, matrix.W.X);
                    result.Y = new Vector4(matrix.X.Y, matrix.Y.Y, matrix.Z.Y, matrix.W.Y);
                    result.Z = new Vector4(matrix.X.Z, matrix.Y.Z, matrix.Z.Z, matrix.W.Z);
                    result.W = new Vector4(matrix.X.W, matrix.Y.W, matrix.Z.W, matrix.W.W);
                }

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
                //
                // Cost of operation
                // 17 adds and 28 muls.
                //
                // add: 6 + 8 + 3 = 17
                // mul: 12 + 16 = 28

                float a = X.X, b = X.Y, c = X.Z, d = X.W;
                float e = Y.X, f = Y.Y, g = Y.Z, h = Y.W;
                float i = Z.X, j = Z.Y, k = Z.Z, l = Z.W;
                float m = W.X, n = W.Y, o = W.Z, p = W.W;

                float kp_lo = k * p - l * o;
                float jp_ln = j * p - l * n;
                float jo_kn = j * o - k * n;
                float ip_lm = i * p - l * m;
                float io_km = i * o - k * m;
                float in_jm = i * n - j * m;

                return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
                       b * (e * kp_lo - g * ip_lm + h * io_km) +
                       c * (e * jp_ln - f * ip_lm + h * in_jm) -
                       d * (e * jo_kn - f * io_km + g * in_jm);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

            bool IEquatable<Impl>.Equals(Impl other) => Equals(in other);

            private struct CanonicalBasis
            {
                public Vector3 Row0;
                public Vector3 Row1;
                public Vector3 Row2;
            };

            private unsafe struct VectorBasis
            {
                public Vector3* Element0;
                public Vector3* Element1;
                public Vector3* Element2;
            }
        }
    }
}

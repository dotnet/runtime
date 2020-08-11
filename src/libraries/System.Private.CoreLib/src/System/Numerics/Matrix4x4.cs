// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Internal.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating a 4x4 matrix.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        private const float BillboardEpsilon = 1e-4f;
        private const float BillboardMinAngle = 1.0f - (0.1f * (MathF.PI / 180.0f)); // 0.1 degrees
        private const float DecomposeEpsilon = 0.0001f;

        #region Public Fields
        /// <summary>
        /// Value at row 1, column 1 of the matrix.
        /// </summary>
        public float M11;
        /// <summary>
        /// Value at row 1, column 2 of the matrix.
        /// </summary>
        public float M12;
        /// <summary>
        /// Value at row 1, column 3 of the matrix.
        /// </summary>
        public float M13;
        /// <summary>
        /// Value at row 1, column 4 of the matrix.
        /// </summary>
        public float M14;

        /// <summary>
        /// Value at row 2, column 1 of the matrix.
        /// </summary>
        public float M21;
        /// <summary>
        /// Value at row 2, column 2 of the matrix.
        /// </summary>
        public float M22;
        /// <summary>
        /// Value at row 2, column 3 of the matrix.
        /// </summary>
        public float M23;
        /// <summary>
        /// Value at row 2, column 4 of the matrix.
        /// </summary>
        public float M24;

        /// <summary>
        /// Value at row 3, column 1 of the matrix.
        /// </summary>
        public float M31;
        /// <summary>
        /// Value at row 3, column 2 of the matrix.
        /// </summary>
        public float M32;
        /// <summary>
        /// Value at row 3, column 3 of the matrix.
        /// </summary>
        public float M33;
        /// <summary>
        /// Value at row 3, column 4 of the matrix.
        /// </summary>
        public float M34;

        /// <summary>
        /// Value at row 4, column 1 of the matrix.
        /// </summary>
        public float M41;
        /// <summary>
        /// Value at row 4, column 2 of the matrix.
        /// </summary>
        public float M42;
        /// <summary>
        /// Value at row 4, column 3 of the matrix.
        /// </summary>
        public float M43;
        /// <summary>
        /// Value at row 4, column 4 of the matrix.
        /// </summary>
        public float M44;
        #endregion Public Fields

        private static readonly Matrix4x4 _identity = new Matrix4x4
        (
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f
        );

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix4x4 Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public readonly bool IsIdentity
        {
            get
            {
                return M11 == 1f && M22 == 1f && M33 == 1f && M44 == 1f && // Check diagonal element first for early out.
                                    M12 == 0f && M13 == 0f && M14 == 0f &&
                       M21 == 0f && M23 == 0f && M24 == 0f &&
                       M31 == 0f && M32 == 0f && M34 == 0f &&
                       M41 == 0f && M42 == 0f && M43 == 0f;
            }
        }

        /// <summary>
        /// Gets or sets the translation component of this matrix.
        /// </summary>
        public Vector3 Translation
        {
            readonly get
            {
                return new Vector3(M41, M42, M43);
            }
            set
            {
                M41 = value.X;
                M42 = value.Y;
                M43 = value.Z;
            }
        }

        /// <summary>
        /// Constructs a Matrix4x4 from the given components.
        /// </summary>
        public Matrix4x4(float m11, float m12, float m13, float m14,
                         float m21, float m22, float m23, float m24,
                         float m31, float m32, float m33, float m34,
                         float m41, float m42, float m43, float m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }

        /// <summary>
        /// Constructs a Matrix4x4 from the given Matrix3x2.
        /// </summary>
        /// <param name="value">The source Matrix3x2.</param>
        public Matrix4x4(Matrix3x2 value)
        {
            M11 = value.M11;
            M12 = value.M12;
            M13 = 0f;
            M14 = 0f;
            M21 = value.M21;
            M22 = value.M22;
            M23 = 0f;
            M24 = 0f;
            M31 = 0f;
            M32 = 0f;
            M33 = 1f;
            M34 = 0f;
            M41 = value.M31;
            M42 = value.M32;
            M43 = 0f;
            M44 = 1f;
        }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard matrix</returns>
        public static Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            Vector3 zaxis = new Vector3(
                objectPosition.X - cameraPosition.X,
                objectPosition.Y - cameraPosition.Y,
                objectPosition.Z - cameraPosition.Z);

            float norm = zaxis.LengthSquared();

            if (norm < BillboardEpsilon)
            {
                zaxis = -cameraForwardVector;
            }
            else
            {
                zaxis = Vector3.Multiply(zaxis, 1.0f / MathF.Sqrt(norm));
            }

            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));

            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            Matrix4x4 result;

            result.M11 = xaxis.X;
            result.M12 = xaxis.Y;
            result.M13 = xaxis.Z;
            result.M14 = 0.0f;
            result.M21 = yaxis.X;
            result.M22 = yaxis.Y;
            result.M23 = yaxis.Z;
            result.M24 = 0.0f;
            result.M31 = zaxis.X;
            result.M32 = zaxis.Y;
            result.M33 = zaxis.Z;
            result.M34 = 0.0f;

            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;

            return result;
        }

        /// <summary>
        /// Creates a cylindrical billboard that rotates around a specified axis.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="rotateAxis">Axis to rotate the billboard around.</param>
        /// <param name="cameraForwardVector">Forward vector of the camera.</param>
        /// <param name="objectForwardVector">Forward vector of the object.</param>
        /// <returns>The created billboard matrix.</returns>
        public static Matrix4x4 CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3 cameraForwardVector, Vector3 objectForwardVector)
        {
            // Treat the case when object and camera positions are too close.
            Vector3 faceDir = new Vector3(
                objectPosition.X - cameraPosition.X,
                objectPosition.Y - cameraPosition.Y,
                objectPosition.Z - cameraPosition.Z);

            float norm = faceDir.LengthSquared();

            if (norm < BillboardEpsilon)
            {
                faceDir = -cameraForwardVector;
            }
            else
            {
                faceDir = Vector3.Multiply(faceDir, (1.0f / MathF.Sqrt(norm)));
            }

            Vector3 yaxis = rotateAxis;
            Vector3 xaxis;
            Vector3 zaxis;

            // Treat the case when angle between faceDir and rotateAxis is too close to 0.
            float dot = Vector3.Dot(rotateAxis, faceDir);

            if (MathF.Abs(dot) > BillboardMinAngle)
            {
                zaxis = objectForwardVector;

                // Make sure passed values are useful for compute.
                dot = Vector3.Dot(rotateAxis, zaxis);

                if (MathF.Abs(dot) > BillboardMinAngle)
                {
                    zaxis = (MathF.Abs(rotateAxis.Z) > BillboardMinAngle) ? new Vector3(1, 0, 0) : new Vector3(0, 0, -1);
                }

                xaxis = Vector3.Normalize(Vector3.Cross(rotateAxis, zaxis));
                zaxis = Vector3.Normalize(Vector3.Cross(xaxis, rotateAxis));
            }
            else
            {
                xaxis = Vector3.Normalize(Vector3.Cross(rotateAxis, faceDir));
                zaxis = Vector3.Normalize(Vector3.Cross(xaxis, yaxis));
            }

            Matrix4x4 result;

            result.M11 = xaxis.X;
            result.M12 = xaxis.Y;
            result.M13 = xaxis.Z;
            result.M14 = 0.0f;
            result.M21 = yaxis.X;
            result.M22 = yaxis.Y;
            result.M23 = yaxis.Z;
            result.M24 = 0.0f;
            result.M31 = zaxis.X;
            result.M32 = zaxis.Y;
            result.M33 = zaxis.Z;
            result.M34 = 0.0f;

            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;

            return result;
        }

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="position">The amount to translate in each axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(Vector3 position)
        {
            Matrix4x4 result = _identity;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            return result;
        }

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="xPosition">The amount to translate on the X-axis.</param>
        /// <param name="yPosition">The amount to translate on the Y-axis.</param>
        /// <param name="zPosition">The amount to translate on the Z-axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(float xPosition, float yPosition, float zPosition)
        {
            Matrix4x4 result = _identity;
            result.M41 = xPosition;
            result.M42 = yPosition;
            result.M43 = zPosition;
            return result;
        }

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="zScale">Value to scale by on the Z-axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale)
        {
            Matrix4x4 result = _identity;
            result.M11 = xScale;
            result.M22 = yScale;
            result.M33 = zScale;
            return result;
        }

        /// <summary>
        /// Creates a scaling matrix with a center point.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="zScale">Value to scale by on the Z-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float xScale, float yScale, float zScale, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float tx = centerPoint.X * (1 - xScale);
            float ty = centerPoint.Y * (1 - yScale);
            float tz = centerPoint.Z * (1 - zScale);

            result.M11 = xScale;
            result.M22 = yScale;
            result.M33 = zScale;
            result.M41 = tx;
            result.M42 = ty;
            result.M43 = tz;
            return result;
        }

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="scales">The vector containing the amount to scale by on each axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales)
        {
            Matrix4x4 result = _identity;
            result.M11 = scales.X;
            result.M22 = scales.Y;
            result.M33 = scales.Z;
            return result;
        }

        /// <summary>
        /// Creates a scaling matrix with a center point.
        /// </summary>
        /// <param name="scales">The vector containing the amount to scale by on each axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float tx = centerPoint.X * (1 - scales.X);
            float ty = centerPoint.Y * (1 - scales.Y);
            float tz = centerPoint.Z * (1 - scales.Z);

            result.M11 = scales.X;
            result.M22 = scales.Y;
            result.M33 = scales.Z;
            result.M41 = tx;
            result.M42 = ty;
            result.M43 = tz;
            return result;
        }

        /// <summary>
        /// Creates a uniform scaling matrix that scales equally on each axis.
        /// </summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float scale)
        {
            Matrix4x4 result = _identity;

            result.M11 = scale;
            result.M22 = scale;
            result.M33 = scale;

            return result;
        }

        /// <summary>
        /// Creates a uniform scaling matrix that scales equally on each axis with a center point.
        /// </summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(float scale, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float tx = centerPoint.X * (1 - scale);
            float ty = centerPoint.Y * (1 - scale);
            float tz = centerPoint.Z * (1 - scale);

            result.M11 = scale;
            result.M22 = scale;
            result.M33 = scale;

            result.M41 = tx;
            result.M42 = ty;
            result.M43 = tz;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the X-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(float radians)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            // [  1  0  0  0 ]
            // [  0  c  s  0 ]
            // [  0 -s  c  0 ]
            // [  0  0  0  1 ]

            result.M22 = c;
            result.M23 = s;
            result.M32 = -s;
            result.M33 = c;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the X-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(float radians, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            float y = centerPoint.Y * (1 - c) + centerPoint.Z * s;
            float z = centerPoint.Z * (1 - c) - centerPoint.Y * s;

            // [  1  0  0  0 ]
            // [  0  c  s  0 ]
            // [  0 -s  c  0 ]
            // [  0  y  z  1 ]

            result.M22 = c;
            result.M23 = s;
            result.M32 = -s;
            result.M33 = c;
            result.M42 = y;
            result.M43 = z;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(float radians)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            // [  c  0 -s  0 ]
            // [  0  1  0  0 ]
            // [  s  0  c  0 ]
            // [  0  0  0  1 ]
            result.M11 = c;
            result.M13 = -s;
            result.M31 = s;
            result.M33 = c;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Y-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(float radians, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            float x = centerPoint.X * (1 - c) - centerPoint.Z * s;
            float z = centerPoint.Z * (1 - c) + centerPoint.X * s;

            // [  c  0 -s  0 ]
            // [  0  1  0  0 ]
            // [  s  0  c  0 ]
            // [  x  0  z  1 ]
            result.M11 = c;
            result.M13 = -s;
            result.M31 = s;
            result.M33 = c;
            result.M41 = x;
            result.M43 = z;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(float radians)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            // [  c  s  0  0 ]
            // [ -s  c  0  0 ]
            // [  0  0  1  0 ]
            // [  0  0  0  1 ]
            result.M11 = c;
            result.M12 = s;
            result.M21 = -s;
            result.M22 = c;

            return result;
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Z-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(float radians, Vector3 centerPoint)
        {
            Matrix4x4 result = _identity;

            float c = MathF.Cos(radians);
            float s = MathF.Sin(radians);

            float x = centerPoint.X * (1 - c) + centerPoint.Y * s;
            float y = centerPoint.Y * (1 - c) - centerPoint.X * s;

            // [  c  s  0  0 ]
            // [ -s  c  0  0 ]
            // [  0  0  1  0 ]
            // [  x  y  0  1 ]
            result.M11 = c;
            result.M12 = s;
            result.M21 = -s;
            result.M22 = c;
            result.M41 = x;
            result.M42 = y;

            return result;
        }

        /// <summary>
        /// Creates a matrix that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the given axis, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromAxisAngle(Vector3 axis, float angle)
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
            float x = axis.X, y = axis.Y, z = axis.Z;
            float sa = MathF.Sin(angle), ca = MathF.Cos(angle);
            float xx = x * x, yy = y * y, zz = z * z;
            float xy = x * y, xz = x * z, yz = y * z;

            Matrix4x4 result = _identity;

            result.M11 = xx + ca * (1.0f - xx);
            result.M12 = xy - ca * xy + sa * z;
            result.M13 = xz - ca * xz - sa * y;

            result.M21 = xy - ca * xy - sa * z;
            result.M22 = yy + ca * (1.0f - yy);
            result.M23 = yz - ca * yz + sa * x;

            result.M31 = xz - ca * xz + sa * y;
            result.M32 = yz - ca * yz - sa * x;
            result.M33 = zz + ca * (1.0f - zz);

            return result;
        }

        /// <summary>
        /// Creates a perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances.
        /// </summary>
        /// <param name="fieldOfView">Field of view in the y direction, in radians.</param>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            if (fieldOfView <= 0.0f || fieldOfView >= MathF.PI)
                throw new ArgumentOutOfRangeException(nameof(fieldOfView));

            if (nearPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            float yScale = 1.0f / MathF.Tan(fieldOfView * 0.5f);
            float xScale = yScale / aspectRatio;

            Matrix4x4 result;

            result.M11 = xScale;
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = yScale;
            result.M21 = result.M23 = result.M24 = 0.0f;

            result.M31 = result.M32 = 0.0f;
            float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M33 = negFarRange;
            result.M34 = -1.0f;

            result.M41 = result.M42 = result.M44 = 0.0f;
            result.M43 = nearPlaneDistance * negFarRange;

            return result;
        }

        /// <summary>
        /// Creates a perspective projection matrix from the given view volume dimensions.
        /// </summary>
        /// <param name="width">Width of the view volume at the near view plane.</param>
        /// <param name="height">Height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
        {
            if (nearPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            Matrix4x4 result;

            result.M11 = 2.0f * nearPlaneDistance / width;
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = 2.0f * nearPlaneDistance / height;
            result.M21 = result.M23 = result.M24 = 0.0f;

            float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M33 = negFarRange;
            result.M31 = result.M32 = 0.0f;
            result.M34 = -1.0f;

            result.M41 = result.M42 = result.M44 = 0.0f;
            result.M43 = nearPlaneDistance * negFarRange;

            return result;
        }

        /// <summary>
        /// Creates a customized, perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">Maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">Minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">Maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to of the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
        {
            if (nearPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            Matrix4x4 result;

            result.M11 = 2.0f * nearPlaneDistance / (right - left);
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = 2.0f * nearPlaneDistance / (top - bottom);
            result.M21 = result.M23 = result.M24 = 0.0f;

            result.M31 = (left + right) / (right - left);
            result.M32 = (top + bottom) / (top - bottom);
            float negFarRange = float.IsPositiveInfinity(farPlaneDistance) ? -1.0f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M33 = negFarRange;
            result.M34 = -1.0f;

            result.M43 = nearPlaneDistance * negFarRange;
            result.M41 = result.M42 = result.M44 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates an orthographic perspective matrix from the given view volume dimensions.
        /// </summary>
        /// <param name="width">Width of the view volume.</param>
        /// <param name="height">Height of the view volume.</param>
        /// <param name="zNearPlane">Minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum Z-value of the view volume.</param>
        /// <returns>The orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
        {
            Matrix4x4 result = _identity;

            result.M11 = 2.0f / width;
            result.M22 = 2.0f / height;
            result.M33 = 1.0f / (zNearPlane - zFarPlane);
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);

            return result;
        }

        /// <summary>
        /// Builds a customized, orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum X-value of the view volume.</param>
        /// <param name="right">Maximum X-value of the view volume.</param>
        /// <param name="bottom">Minimum Y-value of the view volume.</param>
        /// <param name="top">Maximum Y-value of the view volume.</param>
        /// <param name="zNearPlane">Minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum Z-value of the view volume.</param>
        /// <returns>The orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
        {
            Matrix4x4 result = _identity;

            result.M11 = 2.0f / (right - left);

            result.M22 = 2.0f / (top - bottom);

            result.M33 = 1.0f / (zNearPlane - zFarPlane);

            result.M41 = (left + right) / (left - right);
            result.M42 = (top + bottom) / (bottom - top);
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);

            return result;
        }

        /// <summary>
        /// Creates a view matrix.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The view matrix.</returns>
        public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 zaxis = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));
            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            Matrix4x4 result = _identity;

            result.M11 = xaxis.X;
            result.M12 = yaxis.X;
            result.M13 = zaxis.X;

            result.M21 = xaxis.Y;
            result.M22 = yaxis.Y;
            result.M23 = zaxis.Y;

            result.M31 = xaxis.Z;
            result.M32 = yaxis.Z;
            result.M33 = zaxis.Z;

            result.M41 = -Vector3.Dot(xaxis, cameraPosition);
            result.M42 = -Vector3.Dot(yaxis, cameraPosition);
            result.M43 = -Vector3.Dot(zaxis, cameraPosition);

            return result;
        }

        /// <summary>
        /// Creates a world matrix with the specified parameters.
        /// </summary>
        /// <param name="position">The position of the object; used in translation operations.</param>
        /// <param name="forward">Forward direction of the object.</param>
        /// <param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        /// <returns>The world matrix.</returns>
        public static Matrix4x4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
        {
            Vector3 zaxis = Vector3.Normalize(-forward);
            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(up, zaxis));
            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            Matrix4x4 result = _identity;

            result.M11 = xaxis.X;
            result.M12 = xaxis.Y;
            result.M13 = xaxis.Z;

            result.M21 = yaxis.X;
            result.M22 = yaxis.Y;
            result.M23 = yaxis.Z;

            result.M31 = zaxis.X;
            result.M32 = zaxis.Y;
            result.M33 = zaxis.Z;

            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;

            return result;
        }

        /// <summary>
        /// Creates a rotation matrix from the given Quaternion rotation value.
        /// </summary>
        /// <param name="quaternion">The source Quaternion.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix4x4 result = _identity;

            float xx = quaternion.X * quaternion.X;
            float yy = quaternion.Y * quaternion.Y;
            float zz = quaternion.Z * quaternion.Z;

            float xy = quaternion.X * quaternion.Y;
            float wz = quaternion.Z * quaternion.W;
            float xz = quaternion.Z * quaternion.X;
            float wy = quaternion.Y * quaternion.W;
            float yz = quaternion.Y * quaternion.Z;
            float wx = quaternion.X * quaternion.W;

            result.M11 = 1.0f - 2.0f * (yy + zz);
            result.M12 = 2.0f * (xy + wz);
            result.M13 = 2.0f * (xz - wy);

            result.M21 = 2.0f * (xy - wz);
            result.M22 = 1.0f - 2.0f * (zz + xx);
            result.M23 = 2.0f * (yz + wx);

            result.M31 = 2.0f * (xz + wy);
            result.M32 = 2.0f * (yz - wx);
            result.M33 = 1.0f - 2.0f * (yy + xx);

            return result;
        }

        /// <summary>
        /// Creates a rotation matrix from the specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the Y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the X-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the Z-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

            return Matrix4x4.CreateFromQuaternion(q);
        }

        /// <summary>
        /// Creates a Matrix that flattens geometry into a specified Plane as if casting a shadow from a specified light source.
        /// </summary>
        /// <param name="lightDirection">The direction from which the light that will cast the shadow is coming.</param>
        /// <param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        /// <returns>A new Matrix that can be used to flatten geometry onto the specified plane from the specified direction.</returns>
        public static Matrix4x4 CreateShadow(Vector3 lightDirection, Plane plane)
        {
            Plane p = Plane.Normalize(plane);

            float dot = p.Normal.X * lightDirection.X + p.Normal.Y * lightDirection.Y + p.Normal.Z * lightDirection.Z;
            float a = -p.Normal.X;
            float b = -p.Normal.Y;
            float c = -p.Normal.Z;
            float d = -p.D;

            Matrix4x4 result = _identity;

            result.M11 = a * lightDirection.X + dot;
            result.M21 = b * lightDirection.X;
            result.M31 = c * lightDirection.X;
            result.M41 = d * lightDirection.X;

            result.M12 = a * lightDirection.Y;
            result.M22 = b * lightDirection.Y + dot;
            result.M32 = c * lightDirection.Y;
            result.M42 = d * lightDirection.Y;

            result.M13 = a * lightDirection.Z;
            result.M23 = b * lightDirection.Z;
            result.M33 = c * lightDirection.Z + dot;
            result.M43 = d * lightDirection.Z;

            result.M44 = dot;

            return result;
        }

        /// <summary>
        /// Creates a Matrix that reflects the coordinate system about a specified Plane.
        /// </summary>
        /// <param name="value">The Plane about which to create a reflection.</param>
        /// <returns>A new matrix expressing the reflection.</returns>
        public static Matrix4x4 CreateReflection(Plane value)
        {
            value = Plane.Normalize(value);

            float a = value.Normal.X;
            float b = value.Normal.Y;
            float c = value.Normal.Z;

            float fa = -2.0f * a;
            float fb = -2.0f * b;
            float fc = -2.0f * c;

            Matrix4x4 result = _identity;

            result.M11 = fa * a + 1.0f;
            result.M12 = fb * a;
            result.M13 = fc * a;

            result.M21 = fa * b;
            result.M22 = fb * b + 1.0f;
            result.M23 = fc * b;

            result.M31 = fa * c;
            result.M32 = fb * c;
            result.M33 = fc * c + 1.0f;

            result.M41 = fa * value.D;
            result.M42 = fb * value.D;
            result.M43 = fc * value.D;

            return result;
        }

        /// <summary>
        /// Calculates the determinant of the matrix.
        /// </summary>
        /// <returns>The determinant of the matrix.</returns>
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

            float a = M11, b = M12, c = M13, d = M14;
            float e = M21, f = M22, g = M23, h = M24;
            float i = M31, j = M32, k = M33, l = M34;
            float m = M41, n = M42, o = M43, p = M44;

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
        private static Vector128<float> Permute(Vector128<float> value, byte control)
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
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Attempts to calculate the inverse of the given matrix. If successful, result will contain the inverted matrix.
        /// </summary>
        /// <param name="matrix">The source matrix to invert.</param>
        /// <param name="result">If successful, contains the inverted matrix.</param>
        /// <returns>True if the source matrix could be inverted; False otherwise.</returns>
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool Invert(Matrix4x4 matrix, out Matrix4x4 result)
        {
            // This implementation is based on the DirectX Math Library XMMatrixInverse method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

            if (Sse.IsSupported)
            {
                return SseImpl(matrix, out result);
            }

            return SoftwareFallback(matrix, out result);

            static unsafe bool SseImpl(Matrix4x4 matrix, out Matrix4x4 result)
            {
                if (!Sse.IsSupported)
                {
                    // Redundant test so we won't prejit remainder of this method on platforms without SSE.
                    throw new PlatformNotSupportedException();
                }

                // Load the matrix values into rows
                Vector128<float> row1 = Sse.LoadVector128(&matrix.M11);
                Vector128<float> row2 = Sse.LoadVector128(&matrix.M21);
                Vector128<float> row3 = Sse.LoadVector128(&matrix.M31);
                Vector128<float> row4 = Sse.LoadVector128(&matrix.M41);

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

                Vector128<float> D0 = Sse.Multiply(V00, V10);
                Vector128<float> D1 = Sse.Multiply(V01, V11);
                Vector128<float> D2 = Sse.Multiply(V02, V12);

                V00 = Permute(row3, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                V10 = Permute(row4, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                V01 = Permute(row1, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                V11 = Permute(row2, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                V02 = Sse.Shuffle(row3, row1, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
                V12 = Sse.Shuffle(row4, row2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)

                // Note:  We use this expansion pattern instead of Fused Multiply Add
                // in order to support older hardware
                D0 = Sse.Subtract(D0, Sse.Multiply(V00, V10));
                D1 = Sse.Subtract(D1, Sse.Multiply(V01, V11));
                D2 = Sse.Subtract(D2, Sse.Multiply(V02, V12));

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

                Vector128<float> C0 = Sse.Multiply(V00, V10);
                Vector128<float> C2 = Sse.Multiply(V01, V11);
                Vector128<float> C4 = Sse.Multiply(V02, V12);
                Vector128<float> C6 = Sse.Multiply(V03, V13);

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

                C0 = Sse.Subtract(C0, Sse.Multiply(V00, V10));
                C2 = Sse.Subtract(C2, Sse.Multiply(V01, V11));
                C4 = Sse.Subtract(C4, Sse.Multiply(V02, V12));
                C6 = Sse.Subtract(C6, Sse.Multiply(V03, V13));

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

                V00 = Sse.Multiply(V00, V10);
                V01 = Sse.Multiply(V01, V11);
                V02 = Sse.Multiply(V02, V12);
                V03 = Sse.Multiply(V03, V13);

                Vector128<float> C1 = Sse.Subtract(C0, V00);
                C0 = Sse.Add(C0, V00);
                Vector128<float> C3 = Sse.Add(C2, V01);
                C2 = Sse.Subtract(C2, V01);
                Vector128<float> C5 = Sse.Subtract(C4, V02);
                C4 = Sse.Add(C4, V02);
                Vector128<float> C7 = Sse.Add(C6, V03);
                C6 = Sse.Subtract(C6, V03);

                C0 = Sse.Shuffle(C0, C1, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C2 = Sse.Shuffle(C2, C3, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C4 = Sse.Shuffle(C4, C5, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C6 = Sse.Shuffle(C6, C7, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                C0 = Permute(C0, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C2 = Permute(C2, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C4 = Permute(C4, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C6 = Permute(C6, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                // Get the determinant
                vTemp2 = row1;
                float det = Vector4.Dot(C0.AsVector4(), vTemp2.AsVector4());

                // Check determinate is not zero
                if (MathF.Abs(det) < float.Epsilon)
                {
                    result = new Matrix4x4(float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN);
                    return false;
                }

                // Create Vector128<float> copy of the determinant and invert them.
                Vector128<float> ones = Vector128.Create(1.0f);
                Vector128<float> vTemp = Vector128.Create(det);
                vTemp = Sse.Divide(ones, vTemp);

                row1 = Sse.Multiply(C0, vTemp);
                row2 = Sse.Multiply(C2, vTemp);
                row3 = Sse.Multiply(C4, vTemp);
                row4 = Sse.Multiply(C6, vTemp);

                Unsafe.SkipInit(out result);
                ref Vector128<float> vResult = ref Unsafe.As<Matrix4x4, Vector128<float>>(ref result);

                vResult = row1;
                Unsafe.Add(ref vResult, 1) = row2;
                Unsafe.Add(ref vResult, 2) = row3;
                Unsafe.Add(ref vResult, 3) = row4;

                return true;
            }

            static bool SoftwareFallback(Matrix4x4 matrix, out Matrix4x4 result)
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
                float a = matrix.M11, b = matrix.M12, c = matrix.M13, d = matrix.M14;
                float e = matrix.M21, f = matrix.M22, g = matrix.M23, h = matrix.M24;
                float i = matrix.M31, j = matrix.M32, k = matrix.M33, l = matrix.M34;
                float m = matrix.M41, n = matrix.M42, o = matrix.M43, p = matrix.M44;

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
                    result = new Matrix4x4(float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN);
                    return false;
                }

                float invDet = 1.0f / det;

                result.M11 = a11 * invDet;
                result.M21 = a12 * invDet;
                result.M31 = a13 * invDet;
                result.M41 = a14 * invDet;

                result.M12 = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
                result.M22 = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
                result.M32 = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
                result.M42 = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

                float gp_ho = g * p - h * o;
                float fp_hn = f * p - h * n;
                float fo_gn = f * o - g * n;
                float ep_hm = e * p - h * m;
                float eo_gm = e * o - g * m;
                float en_fm = e * n - f * m;

                result.M13 = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
                result.M23 = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
                result.M33 = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
                result.M43 = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

                float gl_hk = g * l - h * k;
                float fl_hj = f * l - h * j;
                float fk_gj = f * k - g * j;
                float el_hi = e * l - h * i;
                float ek_gi = e * k - g * i;
                float ej_fi = e * j - f * i;

                result.M14 = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
                result.M24 = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
                result.M34 = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
                result.M44 = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

                return true;
            }
        }

        private struct CanonicalBasis
        {
            public Vector3 Row0;
            public Vector3 Row1;
            public Vector3 Row2;
        };

        private struct VectorBasis
        {
            public unsafe Vector3* Element0;
            public unsafe Vector3* Element1;
            public unsafe Vector3* Element2;
        }

        /// <summary>
        /// Attempts to extract the scale, translation, and rotation components from the given scale/rotation/translation matrix.
        /// If successful, the out parameters will contained the extracted values.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="scale">The scaling component of the transformation matrix.</param>
        /// <param name="rotation">The rotation component of the transformation matrix.</param>
        /// <param name="translation">The translation component of the transformation matrix</param>
        /// <returns>True if the source matrix was successfully decomposed; False otherwise.</returns>
        public static bool Decompose(Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            bool result = true;

            unsafe
            {
                fixed (Vector3* scaleBase = &scale)
                {
                    float* pfScales = (float*)scaleBase;
                    float det;

                    VectorBasis vectorBasis;
                    Vector3** pVectorBasis = (Vector3**)&vectorBasis;

                    Matrix4x4 matTemp = Matrix4x4.Identity;
                    CanonicalBasis canonicalBasis = default;
                    Vector3* pCanonicalBasis = &canonicalBasis.Row0;

                    canonicalBasis.Row0 = new Vector3(1.0f, 0.0f, 0.0f);
                    canonicalBasis.Row1 = new Vector3(0.0f, 1.0f, 0.0f);
                    canonicalBasis.Row2 = new Vector3(0.0f, 0.0f, 1.0f);

                    translation = new Vector3(
                        matrix.M41,
                        matrix.M42,
                        matrix.M43);

                    pVectorBasis[0] = (Vector3*)&matTemp.M11;
                    pVectorBasis[1] = (Vector3*)&matTemp.M21;
                    pVectorBasis[2] = (Vector3*)&matTemp.M31;

                    *(pVectorBasis[0]) = new Vector3(matrix.M11, matrix.M12, matrix.M13);
                    *(pVectorBasis[1]) = new Vector3(matrix.M21, matrix.M22, matrix.M23);
                    *(pVectorBasis[2]) = new Vector3(matrix.M31, matrix.M32, matrix.M33);

                    scale.X = pVectorBasis[0]->Length();
                    scale.Y = pVectorBasis[1]->Length();
                    scale.Z = pVectorBasis[2]->Length();

                    uint a, b, c;
                    #region Ranking
                    float x = pfScales[0], y = pfScales[1], z = pfScales[2];
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
                        rotation = Quaternion.CreateFromRotationMatrix(matTemp);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Transforms the given matrix by applying the given Quaternion rotation.
        /// </summary>
        /// <param name="value">The source matrix to transform.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed matrix.</returns>
        public static Matrix4x4 Transform(Matrix4x4 value, Quaternion rotation)
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

            Matrix4x4 result;

            // First row
            result.M11 = value.M11 * q11 + value.M12 * q21 + value.M13 * q31;
            result.M12 = value.M11 * q12 + value.M12 * q22 + value.M13 * q32;
            result.M13 = value.M11 * q13 + value.M12 * q23 + value.M13 * q33;
            result.M14 = value.M14;

            // Second row
            result.M21 = value.M21 * q11 + value.M22 * q21 + value.M23 * q31;
            result.M22 = value.M21 * q12 + value.M22 * q22 + value.M23 * q32;
            result.M23 = value.M21 * q13 + value.M22 * q23 + value.M23 * q33;
            result.M24 = value.M24;

            // Third row
            result.M31 = value.M31 * q11 + value.M32 * q21 + value.M33 * q31;
            result.M32 = value.M31 * q12 + value.M32 * q22 + value.M33 * q32;
            result.M33 = value.M31 * q13 + value.M32 * q23 + value.M33 * q33;
            result.M34 = value.M34;

            // Fourth row
            result.M41 = value.M41 * q11 + value.M42 * q21 + value.M43 * q31;
            result.M42 = value.M41 * q12 + value.M42 * q22 + value.M43 * q32;
            result.M43 = value.M41 * q13 + value.M42 * q23 + value.M43 * q33;
            result.M44 = value.M44;

            return result;
        }

        /// <summary>
        /// Transposes the rows and columns of a matrix.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <returns>The transposed matrix.</returns>
        public static unsafe Matrix4x4 Transpose(Matrix4x4 matrix)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                // This implementation is based on the DirectX Math Library XMMatrixTranspose method
                // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

                Vector128<float> M11 = AdvSimd.LoadVector128(&matrix.M11);
                Vector128<float> M31 = AdvSimd.LoadVector128(&matrix.M31);

                Vector128<float> P00 = AdvSimd.Arm64.ZipLow(M11, M31);
                Vector128<float> P01 = AdvSimd.Arm64.ZipHigh(M11, M31);

                Vector128<float> M21 = AdvSimd.LoadVector128(&matrix.M21);
                Vector128<float> M41 = AdvSimd.LoadVector128(&matrix.M41);

                Vector128<float> P10 = AdvSimd.Arm64.ZipLow(M21, M41);
                Vector128<float> P11 = AdvSimd.Arm64.ZipHigh(M21, M41);

                AdvSimd.Store(&matrix.M11, AdvSimd.Arm64.ZipLow(P00, P10));
                AdvSimd.Store(&matrix.M21, AdvSimd.Arm64.ZipHigh(P00, P10));
                AdvSimd.Store(&matrix.M31, AdvSimd.Arm64.ZipLow(P01, P11));
                AdvSimd.Store(&matrix.M41, AdvSimd.Arm64.ZipHigh(P01, P11));

                return matrix;
            }
            else if (Sse.IsSupported)
            {
                Vector128<float> row1 = Sse.LoadVector128(&matrix.M11);
                Vector128<float> row2 = Sse.LoadVector128(&matrix.M21);
                Vector128<float> row3 = Sse.LoadVector128(&matrix.M31);
                Vector128<float> row4 = Sse.LoadVector128(&matrix.M41);

                Vector128<float> l12 = Sse.UnpackLow(row1, row2);
                Vector128<float> l34 = Sse.UnpackLow(row3, row4);
                Vector128<float> h12 = Sse.UnpackHigh(row1, row2);
                Vector128<float> h34 = Sse.UnpackHigh(row3, row4);

                Sse.Store(&matrix.M11, Sse.MoveLowToHigh(l12, l34));
                Sse.Store(&matrix.M21, Sse.MoveHighToLow(l34, l12));
                Sse.Store(&matrix.M31, Sse.MoveLowToHigh(h12, h34));
                Sse.Store(&matrix.M41, Sse.MoveHighToLow(h34, h12));

                return matrix;
            }

            Matrix4x4 result;

            result.M11 = matrix.M11;
            result.M12 = matrix.M21;
            result.M13 = matrix.M31;
            result.M14 = matrix.M41;
            result.M21 = matrix.M12;
            result.M22 = matrix.M22;
            result.M23 = matrix.M32;
            result.M24 = matrix.M42;
            result.M31 = matrix.M13;
            result.M32 = matrix.M23;
            result.M33 = matrix.M33;
            result.M34 = matrix.M43;
            result.M41 = matrix.M14;
            result.M42 = matrix.M24;
            result.M43 = matrix.M34;
            result.M44 = matrix.M44;

            return result;
        }

        /// <summary>
        /// Linearly interpolates between the corresponding values of two matrices.
        /// </summary>
        /// <param name="matrix1">The first source matrix.</param>
        /// <param name="matrix2">The second source matrix.</param>
        /// <param name="amount">The relative weight of the second source matrix.</param>
        /// <returns>The interpolated matrix.</returns>
        public static unsafe Matrix4x4 Lerp(Matrix4x4 matrix1, Matrix4x4 matrix2, float amount)
        {
            if (AdvSimd.IsSupported)
            {
                Vector128<float> amountVec = Vector128.Create(amount);
                AdvSimd.Store(&matrix1.M11, VectorMath.Lerp(AdvSimd.LoadVector128(&matrix1.M11), AdvSimd.LoadVector128(&matrix2.M11), amountVec));
                AdvSimd.Store(&matrix1.M21, VectorMath.Lerp(AdvSimd.LoadVector128(&matrix1.M21), AdvSimd.LoadVector128(&matrix2.M21), amountVec));
                AdvSimd.Store(&matrix1.M31, VectorMath.Lerp(AdvSimd.LoadVector128(&matrix1.M31), AdvSimd.LoadVector128(&matrix2.M31), amountVec));
                AdvSimd.Store(&matrix1.M41, VectorMath.Lerp(AdvSimd.LoadVector128(&matrix1.M41), AdvSimd.LoadVector128(&matrix2.M41), amountVec));
                return matrix1;
            }
            else if (Sse.IsSupported)
            {
                Vector128<float> amountVec = Vector128.Create(amount);
                Sse.Store(&matrix1.M11, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M11), Sse.LoadVector128(&matrix2.M11), amountVec));
                Sse.Store(&matrix1.M21, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M21), Sse.LoadVector128(&matrix2.M21), amountVec));
                Sse.Store(&matrix1.M31, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M31), Sse.LoadVector128(&matrix2.M31), amountVec));
                Sse.Store(&matrix1.M41, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M41), Sse.LoadVector128(&matrix2.M41), amountVec));
                return matrix1;
            }

            Matrix4x4 result;

            // First row
            result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
            result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
            result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
            result.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;

            // Second row
            result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
            result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
            result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
            result.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;

            // Third row
            result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
            result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
            result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
            result.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;

            // Fourth row
            result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
            result.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;

            return result;
        }

        /// <summary>
        /// Returns a new matrix with the negated elements of the given matrix.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix4x4 Negate(Matrix4x4 value) => -value;

        /// <summary>
        /// Adds two matrices together.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix4x4 Add(Matrix4x4 value1, Matrix4x4 value2) => value1 + value2;

        /// <summary>
        /// Subtracts the second matrix from the first.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the subtraction.</returns>
        public static Matrix4x4 Subtract(Matrix4x4 value1, Matrix4x4 value2) => value1 - value2;

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Matrix4x4 Multiply(Matrix4x4 value1, Matrix4x4 value2) => value1 * value2;

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling factor.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix4x4 Multiply(Matrix4x4 value1, float value2) => value1 * value2;

        /// <summary>
        /// Returns a new matrix with the negated elements of the given matrix.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static unsafe Matrix4x4 operator -(Matrix4x4 value)
        {
            if (AdvSimd.IsSupported)
            {
                AdvSimd.Store(&value.M11, AdvSimd.Negate(AdvSimd.LoadVector128(&value.M11)));
                AdvSimd.Store(&value.M21, AdvSimd.Negate(AdvSimd.LoadVector128(&value.M21)));
                AdvSimd.Store(&value.M31, AdvSimd.Negate(AdvSimd.LoadVector128(&value.M31)));
                AdvSimd.Store(&value.M41, AdvSimd.Negate(AdvSimd.LoadVector128(&value.M41)));
                return value;
            }
            else if (Sse.IsSupported)
            {
                Vector128<float> zero = Vector128<float>.Zero;
                Sse.Store(&value.M11, Sse.Subtract(zero, Sse.LoadVector128(&value.M11)));
                Sse.Store(&value.M21, Sse.Subtract(zero, Sse.LoadVector128(&value.M21)));
                Sse.Store(&value.M31, Sse.Subtract(zero, Sse.LoadVector128(&value.M31)));
                Sse.Store(&value.M41, Sse.Subtract(zero, Sse.LoadVector128(&value.M41)));
                return value;
            }

            Matrix4x4 m;

            m.M11 = -value.M11;
            m.M12 = -value.M12;
            m.M13 = -value.M13;
            m.M14 = -value.M14;
            m.M21 = -value.M21;
            m.M22 = -value.M22;
            m.M23 = -value.M23;
            m.M24 = -value.M24;
            m.M31 = -value.M31;
            m.M32 = -value.M32;
            m.M33 = -value.M33;
            m.M34 = -value.M34;
            m.M41 = -value.M41;
            m.M42 = -value.M42;
            m.M43 = -value.M43;
            m.M44 = -value.M44;

            return m;
        }

        /// <summary>
        /// Adds two matrices together.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The resulting matrix.</returns>
        public static unsafe Matrix4x4 operator +(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (AdvSimd.IsSupported)
            {
                AdvSimd.Store(&value1.M11, AdvSimd.Add(AdvSimd.LoadVector128(&value1.M11), AdvSimd.LoadVector128(&value2.M11)));
                AdvSimd.Store(&value1.M21, AdvSimd.Add(AdvSimd.LoadVector128(&value1.M21), AdvSimd.LoadVector128(&value2.M21)));
                AdvSimd.Store(&value1.M31, AdvSimd.Add(AdvSimd.LoadVector128(&value1.M31), AdvSimd.LoadVector128(&value2.M31)));
                AdvSimd.Store(&value1.M41, AdvSimd.Add(AdvSimd.LoadVector128(&value1.M41), AdvSimd.LoadVector128(&value2.M41)));
                return value1;
            }
            else if (Sse.IsSupported)
            {
                Sse.Store(&value1.M11, Sse.Add(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)));
                Sse.Store(&value1.M21, Sse.Add(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)));
                Sse.Store(&value1.M31, Sse.Add(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)));
                Sse.Store(&value1.M41, Sse.Add(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41)));
                return value1;
            }

            Matrix4x4 m;

            m.M11 = value1.M11 + value2.M11;
            m.M12 = value1.M12 + value2.M12;
            m.M13 = value1.M13 + value2.M13;
            m.M14 = value1.M14 + value2.M14;
            m.M21 = value1.M21 + value2.M21;
            m.M22 = value1.M22 + value2.M22;
            m.M23 = value1.M23 + value2.M23;
            m.M24 = value1.M24 + value2.M24;
            m.M31 = value1.M31 + value2.M31;
            m.M32 = value1.M32 + value2.M32;
            m.M33 = value1.M33 + value2.M33;
            m.M34 = value1.M34 + value2.M34;
            m.M41 = value1.M41 + value2.M41;
            m.M42 = value1.M42 + value2.M42;
            m.M43 = value1.M43 + value2.M43;
            m.M44 = value1.M44 + value2.M44;

            return m;
        }

        /// <summary>
        /// Subtracts the second matrix from the first.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the subtraction.</returns>
        public static unsafe Matrix4x4 operator -(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (AdvSimd.IsSupported)
            {
                AdvSimd.Store(&value1.M11, AdvSimd.Subtract(AdvSimd.LoadVector128(&value1.M11), AdvSimd.LoadVector128(&value2.M11)));
                AdvSimd.Store(&value1.M21, AdvSimd.Subtract(AdvSimd.LoadVector128(&value1.M21), AdvSimd.LoadVector128(&value2.M21)));
                AdvSimd.Store(&value1.M31, AdvSimd.Subtract(AdvSimd.LoadVector128(&value1.M31), AdvSimd.LoadVector128(&value2.M31)));
                AdvSimd.Store(&value1.M41, AdvSimd.Subtract(AdvSimd.LoadVector128(&value1.M41), AdvSimd.LoadVector128(&value2.M41)));
                return value1;
            }
            else if (Sse.IsSupported)
            {
                Sse.Store(&value1.M11, Sse.Subtract(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)));
                Sse.Store(&value1.M21, Sse.Subtract(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)));
                Sse.Store(&value1.M31, Sse.Subtract(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)));
                Sse.Store(&value1.M41, Sse.Subtract(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41)));
                return value1;
            }

            Matrix4x4 m;

            m.M11 = value1.M11 - value2.M11;
            m.M12 = value1.M12 - value2.M12;
            m.M13 = value1.M13 - value2.M13;
            m.M14 = value1.M14 - value2.M14;
            m.M21 = value1.M21 - value2.M21;
            m.M22 = value1.M22 - value2.M22;
            m.M23 = value1.M23 - value2.M23;
            m.M24 = value1.M24 - value2.M24;
            m.M31 = value1.M31 - value2.M31;
            m.M32 = value1.M32 - value2.M32;
            m.M33 = value1.M33 - value2.M33;
            m.M34 = value1.M34 - value2.M34;
            m.M41 = value1.M41 - value2.M41;
            m.M42 = value1.M42 - value2.M42;
            m.M43 = value1.M43 - value2.M43;
            m.M44 = value1.M44 - value2.M44;

            return m;
        }

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static unsafe Matrix4x4 operator *(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                Unsafe.SkipInit(out Matrix4x4 result);

                // Perform the operation on the first row

                Vector128<float> M11 = AdvSimd.LoadVector128(&value1.M11);

                Vector128<float> vX = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M11), M11, 0);
                Vector128<float> vY = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M21), M11, 1);
                Vector128<float> vZ = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vX, AdvSimd.LoadVector128(&value2.M31), M11, 2);
                Vector128<float> vW = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vY, AdvSimd.LoadVector128(&value2.M41), M11, 3);

                AdvSimd.Store(&result.M11, AdvSimd.Add(vZ, vW));

                // Repeat for the other 3 rows

                Vector128<float> M21 = AdvSimd.LoadVector128(&value1.M21);

                vX = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M11), M21, 0);
                vY = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M21), M21, 1);
                vZ = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vX, AdvSimd.LoadVector128(&value2.M31), M21, 2);
                vW = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vY, AdvSimd.LoadVector128(&value2.M41), M21, 3);

                AdvSimd.Store(&result.M21, AdvSimd.Add(vZ, vW));

                Vector128<float> M31 = AdvSimd.LoadVector128(&value1.M31);

                vX = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M11), M31, 0);
                vY = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M21), M31, 1);
                vZ = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vX, AdvSimd.LoadVector128(&value2.M31), M31, 2);
                vW = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vY, AdvSimd.LoadVector128(&value2.M41), M31, 3);

                AdvSimd.Store(&result.M31, AdvSimd.Add(vZ, vW));

                Vector128<float> M41 = AdvSimd.LoadVector128(&value1.M41);

                vX = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M11), M41, 0);
                vY = AdvSimd.MultiplyBySelectedScalar(AdvSimd.LoadVector128(&value2.M21), M41, 1);
                vZ = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vX, AdvSimd.LoadVector128(&value2.M31), M41, 2);
                vW = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(vY, AdvSimd.LoadVector128(&value2.M41), M41, 3);

                AdvSimd.Store(&result.M41, AdvSimd.Add(vZ, vW));

                return result;
            }
            else if (Sse.IsSupported)
            {
                Vector128<float> row = Sse.LoadVector128(&value1.M11);
                Sse.Store(&value1.M11,
                    Sse.Add(Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                            Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                // 0x00 is _MM_SHUFFLE(0,0,0,0), 0x55 is _MM_SHUFFLE(1,1,1,1), etc.
                // TODO: Replace with a method once it's added to the API.

                row = Sse.LoadVector128(&value1.M21);
                Sse.Store(&value1.M21,
                    Sse.Add(Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                            Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                row = Sse.LoadVector128(&value1.M31);
                Sse.Store(&value1.M31,
                    Sse.Add(Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                            Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                row = Sse.LoadVector128(&value1.M41);
                Sse.Store(&value1.M41,
                    Sse.Add(Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                            Sse.Add(Sse.Multiply(Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                                    Sse.Multiply(Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));
                return value1;
            }

            Matrix4x4 m;

            // First row
            m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41;
            m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42;
            m.M13 = value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43;
            m.M14 = value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 + value1.M14 * value2.M44;

            // Second row
            m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41;
            m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42;
            m.M23 = value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43;
            m.M24 = value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 + value1.M24 * value2.M44;

            // Third row
            m.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41;
            m.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42;
            m.M33 = value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43;
            m.M34 = value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 + value1.M34 * value2.M44;

            // Fourth row
            m.M41 = value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41;
            m.M42 = value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42;
            m.M43 = value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43;
            m.M44 = value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value1.M44 * value2.M44;

            return m;
        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling factor.</param>
        /// <returns>The scaled matrix.</returns>
        public static unsafe Matrix4x4 operator *(Matrix4x4 value1, float value2)
        {
            if (AdvSimd.IsSupported)
            {
                Vector128<float> value2Vec = Vector128.Create(value2);
                AdvSimd.Store(&value1.M11, AdvSimd.Multiply(AdvSimd.LoadVector128(&value1.M11), value2Vec));
                AdvSimd.Store(&value1.M21, AdvSimd.Multiply(AdvSimd.LoadVector128(&value1.M21), value2Vec));
                AdvSimd.Store(&value1.M31, AdvSimd.Multiply(AdvSimd.LoadVector128(&value1.M31), value2Vec));
                AdvSimd.Store(&value1.M41, AdvSimd.Multiply(AdvSimd.LoadVector128(&value1.M41), value2Vec));
                return value1;
            }
            else if (Sse.IsSupported)
            {
                Vector128<float> value2Vec = Vector128.Create(value2);
                Sse.Store(&value1.M11, Sse.Multiply(Sse.LoadVector128(&value1.M11), value2Vec));
                Sse.Store(&value1.M21, Sse.Multiply(Sse.LoadVector128(&value1.M21), value2Vec));
                Sse.Store(&value1.M31, Sse.Multiply(Sse.LoadVector128(&value1.M31), value2Vec));
                Sse.Store(&value1.M41, Sse.Multiply(Sse.LoadVector128(&value1.M41), value2Vec));
                return value1;
            }

            Matrix4x4 m;

            m.M11 = value1.M11 * value2;
            m.M12 = value1.M12 * value2;
            m.M13 = value1.M13 * value2;
            m.M14 = value1.M14 * value2;
            m.M21 = value1.M21 * value2;
            m.M22 = value1.M22 * value2;
            m.M23 = value1.M23 * value2;
            m.M24 = value1.M24 * value2;
            m.M31 = value1.M31 * value2;
            m.M32 = value1.M32 * value2;
            m.M33 = value1.M33 * value2;
            m.M34 = value1.M34 * value2;
            m.M41 = value1.M41 * value2;
            m.M42 = value1.M42 * value2;
            m.M43 = value1.M43 * value2;
            m.M44 = value1.M44 * value2;
            return m;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are equal; False otherwise.</returns>
        public static unsafe bool operator ==(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return VectorMath.Equal(AdvSimd.LoadVector128(&value1.M11), AdvSimd.LoadVector128(&value2.M11)) &&
                       VectorMath.Equal(AdvSimd.LoadVector128(&value1.M21), AdvSimd.LoadVector128(&value2.M21)) &&
                       VectorMath.Equal(AdvSimd.LoadVector128(&value1.M31), AdvSimd.LoadVector128(&value2.M31)) &&
                       VectorMath.Equal(AdvSimd.LoadVector128(&value1.M41), AdvSimd.LoadVector128(&value2.M41));
            }
            else if (Sse.IsSupported)
            {
                return VectorMath.Equal(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)) &&
                       VectorMath.Equal(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)) &&
                       VectorMath.Equal(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)) &&
                       VectorMath.Equal(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41));
            }

            return (value1.M11 == value2.M11 && value1.M22 == value2.M22 && value1.M33 == value2.M33 && value1.M44 == value2.M44 && // Check diagonal element first for early out.
                    value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M14 == value2.M14 && value1.M21 == value2.M21 &&
                    value1.M23 == value2.M23 && value1.M24 == value2.M24 && value1.M31 == value2.M31 && value1.M32 == value2.M32 &&
                    value1.M34 == value2.M34 && value1.M41 == value2.M41 && value1.M42 == value2.M42 && value1.M43 == value2.M43);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are not equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are not equal; False if they are equal.</returns>
        public static unsafe bool operator !=(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return VectorMath.NotEqual(AdvSimd.LoadVector128(&value1.M11), AdvSimd.LoadVector128(&value2.M11)) ||
                       VectorMath.NotEqual(AdvSimd.LoadVector128(&value1.M21), AdvSimd.LoadVector128(&value2.M21)) ||
                       VectorMath.NotEqual(AdvSimd.LoadVector128(&value1.M31), AdvSimd.LoadVector128(&value2.M31)) ||
                       VectorMath.NotEqual(AdvSimd.LoadVector128(&value1.M41), AdvSimd.LoadVector128(&value2.M41));
            }
            else if (Sse.IsSupported)
            {
                return
                    VectorMath.NotEqual(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)) ||
                    VectorMath.NotEqual(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)) ||
                    VectorMath.NotEqual(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)) ||
                    VectorMath.NotEqual(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41));
            }

            return (value1.M11 != value2.M11 || value1.M12 != value2.M12 || value1.M13 != value2.M13 || value1.M14 != value2.M14 ||
                    value1.M21 != value2.M21 || value1.M22 != value2.M22 || value1.M23 != value2.M23 || value1.M24 != value2.M24 ||
                    value1.M31 != value2.M31 || value1.M32 != value2.M32 || value1.M33 != value2.M33 || value1.M34 != value2.M34 ||
                    value1.M41 != value2.M41 || value1.M42 != value2.M42 || value1.M43 != value2.M43 || value1.M44 != value2.M44);
        }

        /// <summary>
        /// Returns a boolean indicating whether this matrix instance is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The matrix to compare this instance to.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public readonly bool Equals(Matrix4x4 other) => this == other;

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override readonly bool Equals(object? obj) => (obj is Matrix4x4 other) && (this == other);

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override readonly string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
                                 M11, M12, M13, M14,
                                 M21, M22, M23, M24,
                                 M31, M32, M33, M34,
                                 M41, M42, M43, M44);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            unchecked
            {
                return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() +
                       M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() +
                       M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() +
                       M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
            }
        }
    }
}

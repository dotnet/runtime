// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a plane in three-dimensional space.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public struct Plane : IEquatable<Plane>
    {
        private const float NormalizeEpsilon = 1.192092896e-07f; // smallest such that 1.0+NormalizeEpsilon != 1.0

        /// <summary>The normal vector of the plane.</summary>
        public Vector3 Normal;

        /// <summary>The distance of the plane along its normal from the origin.</summary>
        public float D;

        /// <summary>Creates a <see cref="System.Numerics.Plane" /> object from the X, Y, and Z components of its normal, and its distance from the origin on that normal.</summary>
        /// <param name="x">The X component of the normal.</param>
        /// <param name="y">The Y component of the normal.</param>
        /// <param name="z">The Z component of the normal.</param>
        /// <param name="d">The distance of the plane along its normal from the origin.</param>
        public Plane(float x, float y, float z, float d)
        {
            Normal = new Vector3(x, y, z);
            D = d;
        }

        /// <summary>Creates a <see cref="System.Numerics.Plane" /> object from a specified normal and the distance along the normal from the origin.</summary>
        /// <param name="normal">The plane's normal vector.</param>
        /// <param name="d">The plane's distance from the origin along its normal vector.</param>
        public Plane(Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        /// <summary>Creates a <see cref="System.Numerics.Plane" /> object from a specified four-dimensional vector.</summary>
        /// <param name="value">A vector whose first three elements describe the normal vector, and whose <see cref="System.Numerics.Vector4.W" /> defines the distance along that normal from the origin.</param>
        public Plane(Vector4 value)
        {
            Normal = new Vector3(value.X, value.Y, value.Z);
            D = value.W;
        }

        /// <summary>Creates a <see cref="System.Numerics.Plane" /> object that contains three specified points.</summary>
        /// <param name="point1">The first point defining the plane.</param>
        /// <param name="point2">The second point defining the plane.</param>
        /// <param name="point3">The third point defining the plane.</param>
        /// <returns>The plane containing the three points.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane CreateFromVertices(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            if (Vector.IsHardwareAccelerated)
            {
                Vector3 a = point2 - point1;
                Vector3 b = point3 - point1;

                // N = Cross(a, b)
                Vector3 n = Vector3.Cross(a, b);
                Vector3 normal = Vector3.Normalize(n);

                // D = - Dot(N, point1)
                float d = -Vector3.Dot(normal, point1);

                return new Plane(normal, d);
            }
            else
            {
                float ax = point2.X - point1.X;
                float ay = point2.Y - point1.Y;
                float az = point2.Z - point1.Z;

                float bx = point3.X - point1.X;
                float by = point3.Y - point1.Y;
                float bz = point3.Z - point1.Z;

                // N=Cross(a,b)
                float nx = ay * bz - az * by;
                float ny = az * bx - ax * bz;
                float nz = ax * by - ay * bx;

                // Normalize(N)
                float ls = nx * nx + ny * ny + nz * nz;
                float invNorm = 1.0f / MathF.Sqrt(ls);

                Vector3 normal = new Vector3(
                    nx * invNorm,
                    ny * invNorm,
                    nz * invNorm);

                return new Plane(
                    normal,
                    -(normal.X * point1.X + normal.Y * point1.Y + normal.Z * point1.Z));
            }
        }

        /// <summary>Calculates the dot product of a plane and a 4-dimensional vector.</summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The four-dimensional vector.</param>
        /// <returns>The dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Plane plane, Vector4 value)
        {
            return plane.Normal.X * value.X +
                   plane.Normal.Y * value.Y +
                   plane.Normal.Z * value.Z +
                   plane.D * value.W;
        }

        /// <summary>Returns the dot product of a specified three-dimensional vector and the normal vector of this plane plus the distance (<see cref="System.Numerics.Plane.D" />) value of the plane.</summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The 3-dimensional vector.</param>
        /// <returns>The dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotCoordinate(Plane plane, Vector3 value)
        {
            if (Vector.IsHardwareAccelerated)
            {
                return Vector3.Dot(plane.Normal, value) + plane.D;
            }
            else
            {
                return plane.Normal.X * value.X +
                       plane.Normal.Y * value.Y +
                       plane.Normal.Z * value.Z +
                       plane.D;
            }
        }

        /// <summary>Returns the dot product of a specified three-dimensional vector and the <see cref="System.Numerics.Plane.Normal" /> vector of this plane.</summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The three-dimensional vector.</param>
        /// <returns>The dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotNormal(Plane plane, Vector3 value)
        {
            if (Vector.IsHardwareAccelerated)
            {
                return Vector3.Dot(plane.Normal, value);
            }
            else
            {
                return plane.Normal.X * value.X +
                       plane.Normal.Y * value.Y +
                       plane.Normal.Z * value.Z;
            }
        }

        /// <summary>Creates a new <see cref="System.Numerics.Plane" /> object whose normal vector is the source plane's normal vector normalized.</summary>
        /// <param name="value">The source plane.</param>
        /// <returns>The normalized plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane Normalize(Plane value)
        {
            if (Vector.IsHardwareAccelerated)
            {
                float normalLengthSquared = value.Normal.LengthSquared();
                if (MathF.Abs(normalLengthSquared - 1.0f) < NormalizeEpsilon)
                {
                    // It already normalized, so we don't need to farther process.
                    return value;
                }
                float normalLength = MathF.Sqrt(normalLengthSquared);
                return new Plane(
                    value.Normal / normalLength,
                    value.D / normalLength);
            }
            else
            {
                float f = value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z;

                if (MathF.Abs(f - 1.0f) < NormalizeEpsilon)
                {
                    return value; // It already normalized, so we don't need to further process.
                }

                float fInv = 1.0f / MathF.Sqrt(f);

                return new Plane(
                    value.Normal.X * fInv,
                    value.Normal.Y * fInv,
                    value.Normal.Z * fInv,
                    value.D * fInv);
            }
        }

        /// <summary>Transforms a normalized plane by a 4x4 matrix.</summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix to apply to <paramref name="plane" />.</param>
        /// <returns>The transformed plane.</returns>
        /// <remarks><paramref name="plane" /> must already be normalized so that its <see cref="System.Numerics.Plane.Normal" /> vector is of unit length before this method is called.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane Transform(Plane plane, Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out Matrix4x4 m);

            float x = plane.Normal.X, y = plane.Normal.Y, z = plane.Normal.Z, w = plane.D;

            return new Plane(
                x * m.M11 + y * m.M12 + z * m.M13 + w * m.M14,
                x * m.M21 + y * m.M22 + z * m.M23 + w * m.M24,
                x * m.M31 + y * m.M32 + z * m.M33 + w * m.M34,
                x * m.M41 + y * m.M42 + z * m.M43 + w * m.M44);
        }

        /// <summary>Transforms a normalized plane by a Quaternion rotation.</summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply to the plane.</param>
        /// <returns>A new plane that results from applying the Quaternion rotation.</returns>
        /// <remarks><paramref name="plane" /> must already be normalized so that its <see cref="System.Numerics.Plane.Normal" /> vector is of unit length before this method is called.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane Transform(Plane plane, Quaternion rotation)
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

            float m11 = 1.0f - yy2 - zz2;
            float m21 = xy2 - wz2;
            float m31 = xz2 + wy2;

            float m12 = xy2 + wz2;
            float m22 = 1.0f - xx2 - zz2;
            float m32 = yz2 - wx2;

            float m13 = xz2 - wy2;
            float m23 = yz2 + wx2;
            float m33 = 1.0f - xx2 - yy2;

            float x = plane.Normal.X, y = plane.Normal.Y, z = plane.Normal.Z;

            return new Plane(
                x * m11 + y * m21 + z * m31,
                x * m12 + y * m22 + z * m32,
                x * m13 + y * m23 + z * m33,
                plane.D);
        }

        /// <summary>Returns a value that indicates whether two planes are equal.</summary>
        /// <param name="value1">The first plane to compare.</param>
        /// <param name="value2">The second plane to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="System.Numerics.Plane" /> objects are equal if their <see cref="System.Numerics.Plane.Normal" /> and <see cref="System.Numerics.Plane.D" /> fields are equal.
        /// The <see cref="System.Numerics.Plane.op_Equality" /> method defines the operation of the equality operator for <see cref="System.Numerics.Plane" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Plane value1, Plane value2)
        {
            return (value1.Normal.X == value2.Normal.X &&
                    value1.Normal.Y == value2.Normal.Y &&
                    value1.Normal.Z == value2.Normal.Z &&
                    value1.D == value2.D);
        }

        /// <summary>Returns a value that indicates whether two planes are not equal.</summary>
        /// <param name="value1">The first plane to compare.</param>
        /// <param name="value2">The second plane to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>The <see cref="System.Numerics.Plane.op_Inequality" /> method defines the operation of the inequality operator for <see cref="System.Numerics.Plane" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Plane value1, Plane value2)
        {
            return !(value1 == value2);
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Numerics.Plane" /> object and their <see cref="System.Numerics.Plane.Normal" /> and <see cref="System.Numerics.Plane.D" /> fields are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Plane other) && Equals(other);
        }

        /// <summary>Returns a value that indicates whether this instance and another plane object are equal.</summary>
        /// <param name="other">The other plane.</param>
        /// <returns><see langword="true" /> if the two planes are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="System.Numerics.Plane" /> objects are equal if their <see cref="System.Numerics.Plane.Normal" /> and <see cref="System.Numerics.Plane.D" /> fields are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Plane other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector128.IsHardwareAccelerated)
            {
                return Vector128.LoadUnsafe(ref Unsafe.AsRef(in Normal.X)).Equals(Vector128.LoadUnsafe(ref other.Normal.X));
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Plane self, Plane other)
            {
                return self.Normal.Equals(other.Normal)
                    && self.D.Equals(other.D);
            }
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Normal, D);
        }

        /// <summary>Returns the string representation of this plane object.</summary>
        /// <returns>A string that represents this <see cref="System.Numerics.Plane" /> object.</returns>
        /// <remarks>The string representation of a <see cref="System.Numerics.Plane" /> object use the formatting conventions of the current culture to format the numeric values in the returned string. For example, a <see cref="System.Numerics.Plane" /> object whose string representation is formatted by using the conventions of the en-US culture might appear as <c>{Normal:&lt;1.1, 2.2, 3.3&gt; D:4.4}</c>.</remarks>
        public override readonly string ToString() => $"{{Normal:{Normal} D:{D}}}";
    }
}

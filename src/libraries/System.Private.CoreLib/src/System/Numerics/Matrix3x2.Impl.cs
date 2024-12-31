// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    public partial struct Matrix3x2
    {
        // See Matrix3x2.cs for an explanation of why this file/type exists
        //
        // Note that we use some particular patterns below, such as defining a result
        // and assigning the fields directly rather than using the object initializer
        // syntax. We do this because it saves roughly 8-bytes of IL per method which
        // in turn helps improve inlining chances.

        internal const uint RowCount = 3;
        internal const uint ColumnCount = 2;

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Impl AsImpl() => ref Unsafe.As<Matrix3x2, Impl>(ref this);

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref readonly Impl AsROImpl() => ref Unsafe.As<Matrix3x2, Impl>(ref Unsafe.AsRef(in this));

        internal struct Impl : IEquatable<Impl>
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Matrix3x2 AsM3x2() => ref Unsafe.As<Impl, Matrix3x2>(ref this);

            private const float RotationEpsilon = 0.001f * float.Pi / 180f;     // 0.1% of a degree

            public Vector2 X;
            public Vector2 Y;
            public Vector2 Z;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(float m11, float m12,
                             float m21, float m22,
                             float m31, float m32)
            {
                X = Vector2.Create(m11, m12);
                Y = Vector2.Create(m21, m22);
                Z = Vector2.Create(m31, m32);
            }

            public static Impl Identity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    Impl result;

                    result.X = Vector2.UnitX;
                    result.Y = Vector2.UnitY;
                    result.Z = Vector2.Zero;

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
                    return Unsafe.Add(ref Unsafe.AsRef(in X), row)[column];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    if ((uint)row >= RowCount)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    Unsafe.Add(ref X, row)[column] = value;
                }
            }

            public readonly bool IsIdentity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (X == Vector2.UnitX)
                        && (Y == Vector2.UnitY)
                        && (Z == Vector2.Zero);
                }
            }

            public Vector2 Translation
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                readonly get
                {
                    return Z;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    Z = value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator +(in Impl left, in Impl right)
            {
                Impl result;

                result.X = left.X + right.X;
                result.Y = left.Y + right.Y;
                result.Z = left.Z + right.Z;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(in Impl left, in Impl right)
            {
                return (left.X == right.X)
                    && (left.Y == right.Y)
                    && (left.Z == right.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(in Impl left, in Impl right)
            {
                return (left.X != right.X)
                    || (left.Y != right.Y)
                    || (left.Z != right.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator *(in Impl left, in Impl right)
            {
                Impl result;

                result.X = Vector2.Create(
                    left.X.X * right.X.X + left.X.Y * right.Y.X,
                    left.X.X * right.X.Y + left.X.Y * right.Y.Y
                );
                result.Y = Vector2.Create(
                    left.Y.X * right.X.X + left.Y.Y * right.Y.X,
                    left.Y.X * right.X.Y + left.Y.Y * right.Y.Y
                );
                result.Z = Vector2.Create(
                    left.Z.X * right.X.X + left.Z.Y * right.Y.X + right.Z.X,
                    left.Z.X * right.X.Y + left.Z.Y * right.Y.Y + right.Z.Y
                );

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator *(in Impl left, float right)
            {
                Impl result;

                result.X = left.X * right;
                result.Y = left.Y * right;
                result.Z = left.Z * right;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator -(in Impl left, in Impl right)
            {
                Impl result;

                result.X = left.X - right.X;
                result.Y = left.Y - right.Y;
                result.Z = left.Z - right.Z;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl operator -(in Impl value)
            {
                Impl result;

                result.X = -value.X;
                result.Y = -value.Y;
                result.Z = -value.Z;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotation(float radians)
            {
                radians = float.Ieee754Remainder(radians, float.Tau);

                float c;
                float s;

                if (radians > -RotationEpsilon && radians < RotationEpsilon)
                {
                    // Exact case for zero rotation.
                    c = 1;
                    s = 0;
                }
                else if (radians > float.Pi / 2 - RotationEpsilon && radians < float.Pi / 2 + RotationEpsilon)
                {
                    // Exact case for 90 degree rotation.
                    c = 0;
                    s = 1;
                }
                else if (radians < -float.Pi + RotationEpsilon || radians > float.Pi - RotationEpsilon)
                {
                    // Exact case for 180 degree rotation.
                    c = -1;
                    s = 0;
                }
                else if (radians > -float.Pi / 2 - RotationEpsilon && radians < -float.Pi / 2 + RotationEpsilon)
                {
                    // Exact case for 270 degree rotation.
                    c = 0;
                    s = -1;
                }
                else
                {
                    // Arbitrary rotation.
                    (s, c) = float.SinCos(radians);
                }

                // [  c  s ]
                // [ -s  c ]
                // [  0  0 ]

                Impl result;

                result.X = Vector2.Create( c, s);
                result.Y = Vector2.Create(-s, c);
                result.Z = Vector2.Zero;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateRotation(float radians, Vector2 centerPoint)
            {
                radians = float.Ieee754Remainder(radians, float.Tau);

                float c, s;

                if (radians > -RotationEpsilon && radians < RotationEpsilon)
                {
                    // Exact case for zero rotation.
                    c = 1;
                    s = 0;
                }
                else if (radians > float.Pi / 2 - RotationEpsilon && radians < float.Pi / 2 + RotationEpsilon)
                {
                    // Exact case for 90 degree rotation.
                    c = 0;
                    s = 1;
                }
                else if (radians < -float.Pi + RotationEpsilon || radians > float.Pi - RotationEpsilon)
                {
                    // Exact case for 180 degree rotation.
                    c = -1;
                    s = 0;
                }
                else if (radians > -float.Pi / 2 - RotationEpsilon && radians < -float.Pi / 2 + RotationEpsilon)
                {
                    // Exact case for 270 degree rotation.
                    c = 0;
                    s = -1;
                }
                else
                {
                    // Arbitrary rotation.
                    (s, c) = float.SinCos(radians);
                }

                float x = centerPoint.X * (1 - c) + centerPoint.Y * s;
                float y = centerPoint.Y * (1 - c) - centerPoint.X * s;

                // [  c  s ]
                // [ -s  c ]
                // [  x  y ]

                Impl result;

                result.X = Vector2.Create( c, s);
                result.Y = Vector2.Create(-s, c);
                result.Z = Vector2.Create( x, y);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(Vector2 scales)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scales.X);
                result.Y = Vector2.Create(0, scales.Y);
                result.Z = Vector2.Zero;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scaleX);
                result.Y = Vector2.Create(0, scaleY);
                result.Z = Vector2.Zero;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scaleX, float scaleY, Vector2 centerPoint)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scaleX);
                result.Y = Vector2.Create(0, scaleY);
                result.Z = centerPoint * (Vector2.One - Vector2.Create(scaleX, scaleY));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(Vector2 scales, Vector2 centerPoint)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scales.X);
                result.Y = Vector2.Create(0, scales.Y);
                result.Z = centerPoint * (Vector2.One - scales);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scale);
                result.Y = Vector2.Create(0, scale);
                result.Z = Vector2.Zero;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateScale(float scale, Vector2 centerPoint)
            {
                Impl result;

                result.X = Vector2.CreateScalar(scale);
                result.Y = Vector2.Create(0, scale);
                result.Z = centerPoint * (Vector2.One - Vector2.Create(scale));

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateSkew(float radiansX, float radiansY)
            {
                Impl result;

                result.X = Vector2.Create(1, float.Tan(radiansY));
                result.Y = Vector2.Create(float.Tan(radiansX), 1);
                result.Z = Vector2.Zero;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
            {
                float xTan = float.Tan(radiansX);
                float yTan = float.Tan(radiansY);

                float tx = -centerPoint.Y * xTan;
                float ty = -centerPoint.X * yTan;

                Impl result;

                result.X = Vector2.Create(1, yTan);
                result.Y = Vector2.Create(xTan, 1);
                result.Z = Vector2.Create(tx, ty);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(Vector2 position)
            {
                Impl result;

                result.X = Vector2.UnitX;
                result.Y = Vector2.UnitY;
                result.Z = position;

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl CreateTranslation(float positionX, float positionY)
            {
                Impl result;

                result.X = Vector2.UnitX;
                result.Y = Vector2.UnitY;
                result.Z = Vector2.Create(positionX, positionY);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Invert(in Impl matrix, out Impl result)
            {
                float det = (matrix.X.X * matrix.Y.Y) - (matrix.Y.X * matrix.X.Y);

                if (float.Abs(det) < float.Epsilon)
                {
                    Vector2 vNaN = Vector2.Create(float.NaN);

                    result.X = vNaN;
                    result.Y = vNaN;
                    result.Z = vNaN;

                    return false;
                }

                float invDet = 1.0f / det;

                result.X = Vector2.Create(
                    +matrix.Y.Y * invDet,
                    -matrix.X.Y * invDet
                );
                result.Y = Vector2.Create(
                    -matrix.Y.X * invDet,
                    +matrix.X.X * invDet
                );
                result.Z = Vector2.Create(
                    (matrix.Y.X * matrix.Z.Y - matrix.Z.X * matrix.Y.Y) * invDet,
                    (matrix.Z.X * matrix.X.Y - matrix.X.X * matrix.Z.Y) * invDet
                );

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Impl Lerp(in Impl left, in Impl right, float amount)
            {
                Impl result;

                result.X = Vector2.Lerp(left.X, right.X, amount);
                result.Y = Vector2.Lerp(left.Y, right.Y, amount);
                result.Z = Vector2.Lerp(left.Z, right.Z, amount);

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override readonly bool Equals([NotNullWhen(true)] object? obj)
                => (obj is Matrix3x2 other) && Equals(in other.AsImpl());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Equals(in Impl other)
            {
                // This function needs to account for floating-point equality around NaN
                // and so must behave equivalently to the underlying float/double.Equals

                return X.Equals(other.X)
                    && Y.Equals(other.Y)
                    && Z.Equals(other.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly float GetDeterminant()
            {
                // There isn't actually any such thing as a determinant for a non-square matrix,
                // but this 3x2 type is really just an optimization of a 3x3 where we happen to
                // know the rightmost column is always (0, 0, 1). So we expand to 3x3 format:
                //
                //  [ X.X, X.Y, 0 ]
                //  [ Y.X, Y.Y, 0 ]
                //  [ Z.X, Z.Y, 1 ]
                //
                // Sum the diagonal products:
                //  (X.X * Y.Y * 1) + (X.Y * 0 * Z.X) + (0 * Y.X * Z.Y)
                //
                // Subtract the opposite diagonal products:
                //  (Z.X * Y.Y * 0) + (Z.Y * 0 * X.X) + (1 * Y.X * X.Y)
                //
                // Collapse out the constants and oh look, this is just a 2x2 determinant!

                return (X.X * Y.Y) - (Y.X * X.Y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

            readonly bool IEquatable<Impl>.Equals(Impl other) => Equals(in other);
        }
    }
}

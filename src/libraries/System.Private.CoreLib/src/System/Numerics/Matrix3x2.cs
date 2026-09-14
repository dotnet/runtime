// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    /// <summary>Represents a 3x2 matrix.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public partial struct Matrix3x2 : IEquatable<Matrix3x2>
    {
        private const int RowCount = 3;
        private const int ColumnCount = 2;

        private const float InverseEpsilon = 2.938737E-39f;
        private const float RotationEpsilon = 1.7453292E-05f; // 0.1% of a degree

        // In an ideal world, we'd have 3x Vector2 fields. However, Matrix3x2 was shipped with
        // 6x public float fields and as such we cannot change the "backing" fields without it being
        // a breaking change. Likewise, we cannot switch to using something like ExplicitLayout
        // without it pessimizing other parts of the JIT and still preventing things like field promotion.
        //
        // This nested Impl struct works around this problem by relying on the JIT treating same sizeof
        // value type bitcasts as a no-op. Effectively the entire implementation is here in this type
        // and the public facing Matrix3x2 just defers to it with simple reinterpret casts inserted
        // at the relevant points.

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Impl AsImpl() => ref Unsafe.As<Matrix3x2, Impl>(ref this);

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref readonly Impl AsROImpl() => ref Unsafe.As<Matrix3x2, Impl>(ref Unsafe.AsRef(in this));

        internal struct Impl
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Matrix3x2 AsM3x2() => ref Unsafe.As<Impl, Matrix3x2>(ref this);

            public Vector2 X;
            public Vector2 Y;
            public Vector2 Z;
        }

        /// <summary>The first element of the first row.</summary>
        /// <remarks>This element exists at index: <c>[0, 0]</c> and is part of row <see cref="X" />.</remarks>
        public float M11;

        /// <summary>The second element of the first row.</summary>
        /// <remarks>This element exists at index: <c>[0, 1]</c> and is part of row <see cref="X" />.</remarks>
        public float M12;

        /// <summary>The first element of the second row.</summary>
        /// <remarks>This element exists at index: <c>[1, 0]</c> and is part of row <see cref="Y" />.</remarks>
        public float M21;

        /// <summary>The second element of the second row.</summary>
        /// <remarks>This element exists at index: <c>[1, 1]</c> and is part of row <see cref="Y" />.</remarks>
        public float M22;

        /// <summary>The first element of the third row.</summary>
        /// <remarks>This element exists at index: <c>[2, 0]</c> and is part of row <see cref="Z" />.</remarks>
        public float M31;

        /// <summary>The second element of the third row.</summary>
        /// <remarks>This element exists at index: <c>[2, 1]</c> and is part of row <see cref="Z" />.</remarks>
        public float M32;

        /// <summary>Initializes a <see cref="Matrix3x2"/> using the specified elements.</summary>
        /// <param name="m11">The value to assign to <see cref="M11" />.</param>
        /// <param name="m12">The value to assign to <see cref="M12" />.</param>
        /// <param name="m21">The value to assign to <see cref="M21" />.</param>
        /// <param name="m22">The value to assign to <see cref="M22" />.</param>
        /// <param name="m31">The value to assign to <see cref="M31" />.</param>
        /// <param name="m32">The value to assign to <see cref="M32" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3x2(float m11, float m12,
                         float m21, float m22,
                         float m31, float m32)
        {
            this = Create(
                m11, m12,
                m21, m22,
                m31, m32
            );
        }

        /// <summary>Gets the multiplicative identity matrix.</summary>
        /// <value>The multiplicative identity matrix.</value>
        public static Matrix3x2 Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Create(Vector2.UnitX, Vector2.UnitY, Vector2.Zero);
        }

        /// <summary>Gets a value that indicates whether the current matrix is an identity matrix.</summary>
        /// <value><see langword="true" /> if the current matrix is an identity matrix; otherwise, <see langword="false" />.</value>
        public readonly bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref readonly Impl impl = ref AsROImpl();

                return (impl.X == Vector2.UnitX)
                    && (impl.Y == Vector2.UnitY)
                    && (impl.Z == Vector2.Zero);
            }
        }

        /// <summary>Gets or sets the translation component of this matrix.</summary>
        /// <remarks>The translation component is stored as <see cref="Z" />.</remarks>
        public Vector2 Translation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().Z;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().Z = value;
        }

        /// <summary>Gets or sets the first row of the matrix.</summary>
        /// <remarks>This row comprises <see cref="M11" /> and <see cref="M12" />; it exists at index: <c>[0]</c>.</remarks>
        public Vector2 X
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().X;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().X = value;
        }

        /// <summary>Gets or sets the second row of the matrix.</summary>
        /// <remarks>This row comprises <see cref="M21" /> and <see cref="M22" />; it exists at index: <c>[1]</c>.</remarks>
        public Vector2 Y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().Y;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().Y = value;
        }

        /// <summary>Gets or sets the third row of the matrix.</summary>
        /// <remarks>This row comprises <see cref="M31" /> and <see cref="M32" />; it exists at index: <c>[2]</c>.</remarks>
        public Vector2 Z
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().Z;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().Z = value;
        }

        /// <summary>Gets or sets the row at the specified index.</summary>
        /// <param name="row">The index of the row to get or set.</param>
        /// <returns>The row at index: [<paramref name="row" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="row" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).</exception>
        public Vector2 this[int row]
        {
            // When row is a known constant, we can use a switch to get
            // optimal codegen as we are likely coming from register.
            //
            // However, if either is non constant we're going to end up having
            // to touch memory so just directly compute the relevant index.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                ref readonly Impl impl = ref AsROImpl();

                if (RuntimeHelpers.IsKnownConstant(row))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            return impl.X;
                        }

                        case 1:
                        {
                            return impl.Y;
                        }

                        case 2:
                        {
                            return impl.Z;
                        }

                        default:
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException();
                            return default;
                        }
                    }
                }
                else
                {
                    if ((uint)row >= RowCount)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    return Unsafe.Add(ref Unsafe.AsRef(in impl).X, row);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref Impl impl = ref AsImpl();

                if (RuntimeHelpers.IsKnownConstant(row))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            impl.X = value;
                            break;
                        }

                        case 1:
                        {
                            impl.Y = value;
                            break;
                        }

                        case 2:
                        {
                            impl.Z = value;
                            break;
                        }

                        default:
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException();
                            break;
                        }
                    }
                }
                else
                {
                    if ((uint)row >= RowCount)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    Unsafe.Add(ref impl.X, row) = value;
                }
            }
        }

        /// <summary>Gets or sets the element at the specified row and column.</summary>
        /// <param name="row">The index of the row containing the element to get or set.</param>
        /// <param name="column">The index of the column containing the element to get or set.</param>
        /// <returns>The element at index: [<paramref name="row" />, <paramref name="column" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="row" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).
        /// -or-
        /// <paramref name="column" /> was less than zero or greater than or equal to the number of columns (<c>2</c>).
        /// </exception>
        public float this[int row, int column]
        {
            // When both row and column are known constants, we can use a switch to
            // get optimal codegen as we are likely coming from register.
            //
            // However, if either is non constant we're going to end up having to
            // touch memory so just directly compute the relevant index.
            //
            // The JIT will elide any dead code paths if only one of the inputs is constant.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                ref readonly Impl impl = ref AsROImpl();

                if (RuntimeHelpers.IsKnownConstant(row) && RuntimeHelpers.IsKnownConstant(column))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            return impl.X.GetElement(column);
                        }

                        case 1:
                        {
                            return impl.Y.GetElement(column);
                        }

                        case 2:
                        {
                            return impl.Z.GetElement(column);
                        }

                        default:
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException();
                            return default;
                        }
                    }
                }
                else
                {
                    if (((uint)row >= RowCount) || ((uint)column >= ColumnCount))
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    return Unsafe.Add(ref Unsafe.AsRef(in impl.X.X), (row * ColumnCount) + column);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref Impl impl = ref AsImpl();

                if (RuntimeHelpers.IsKnownConstant(row) && RuntimeHelpers.IsKnownConstant(column))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            impl.X = impl.X.WithElement(column, value);
                            break;
                        }

                        case 1:
                        {
                            impl.Y = impl.Y.WithElement(column, value);
                            break;
                        }

                        case 2:
                        {
                            impl.Z = impl.Z.WithElement(column, value);
                            break;
                        }

                        default:
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException();
                            break;
                        }
                    }
                }
                else
                {
                    if (((uint)row >= RowCount) || ((uint)column >= ColumnCount))
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }
                    Unsafe.Add(ref impl.X.X, (row * ColumnCount) + column) = value;
                }
            }
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the operation of the addition operator for <see cref="Matrix3x2" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator +(Matrix3x2 value1, Matrix3x2 value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();
            ref readonly Impl right = ref value2.AsROImpl();

            Impl result;

            result.X = left.X + right.X;
            result.Y = left.Y + right.Y;
            result.Z = left.Z + right.Z;

            return result.AsM3x2();
        }

        /// <summary>Returns a value that indicates whether the specified matrices are equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix3x2 value1, Matrix3x2 value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();
            ref readonly Impl right = ref value2.AsROImpl();

            return (left.X == right.X)
                && (left.Y == right.Y)
                && (left.Z == right.Z);
        }

        /// <summary>Returns a value that indicates whether the specified matrices are not equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix3x2 value1, Matrix3x2 value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();
            ref readonly Impl right = ref value2.AsROImpl();

            return (left.X != right.X)
                || (left.Y != right.Y)
                || (left.Z != right.Z);
        }

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        /// <remarks>The <see cref="Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator *(Matrix3x2 value1, Matrix3x2 value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();
            ref readonly Impl right = ref value2.AsROImpl();

            Impl result;

            result.X = Transform2x2(left.X.AsVector128Unsafe(), in right).AsVector2();
            result.Y = Transform2x2(left.Y.AsVector128Unsafe(), in right).AsVector2();
            result.Z = Vector2.Transform(left.Z.AsVector128Unsafe(), in right).AsVector2();

            return result.AsM3x2();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Vector128<float> Transform2x2(Vector128<float> position, in Matrix3x2.Impl matrix)
            {
                Vector128<float> result = matrix.X.AsVector128Unsafe() * position.GetElement(0);
                return Vector128.MultiplyAddEstimate(matrix.Y.AsVector128Unsafe(), Vector128.Create(position.GetElement(1)), result);
            }
        }

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        /// <remarks>The <see cref="Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x2" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator *(Matrix3x2 value1, float value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();

            Impl result;

            result.X = left.X * value2;
            result.Y = left.Y * value2;
            result.Z = left.Z * value2;

            return result.AsM3x2();
        }

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="Subtract" /> method defines the operation of the subtraction operator for <see cref="Matrix3x2" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator -(Matrix3x2 value1, Matrix3x2 value2)
        {
            ref readonly Impl left = ref value1.AsROImpl();
            ref readonly Impl right = ref value2.AsROImpl();

            Impl result;

            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;

            return result.AsM3x2();
        }

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        /// <altmember cref="Negate(Matrix3x2)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator -(Matrix3x2 value)
        {
            ref readonly Impl impl = ref value.AsROImpl();

            Impl result;

            result.X = -impl.X;
            result.Y = -impl.Y;
            result.Z = -impl.Z;

            return result.AsM3x2();
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Add(Matrix3x2 value1, Matrix3x2 value2) => value1 + value2;

        /// <summary>Creates a <see cref="Matrix3x2" /> whose 6 elements are set to the specified value.</summary>
        /// <param name="value">The value to assign to all 6 elements.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose 6 elements are set to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(float value)
        {
            Vector2 vector = Vector2.Create(value);

            Impl result;

            result.X = vector;
            result.Y = vector;
            result.Z = vector;

            return result.AsM3x2();
        }

        /// <summary>Creates a <see cref="Matrix3x2" /> whose 3 rows are set to the specified value.</summary>
        /// <param name="value">The value to assign to all 3 rows.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose 3 rows are set to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Vector2 value)
        {
            Impl result;

            result.X = value;
            result.Y = value;
            result.Z = value;

            return result.AsM3x2();
        }

        /// <summary>Creates a <see cref="Matrix3x2" /> from the specified rows.</summary>
        /// <param name="x">The value to assign to <see cref="X" />.</param>
        /// <param name="y">The value to assign to <see cref="Y" />.</param>
        /// <param name="z">The value to assign to <see cref="Z" />.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose rows are set to the specified values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Vector2 x, Vector2 y, Vector2 z)
        {
            Impl result;

            result.X = x;
            result.Y = y;
            result.Z = z;

            return result.AsM3x2();
        }

        /// <summary>Creates a <see cref="Matrix3x2" /> from the specified elements.</summary>
        /// <param name="m11">The value to assign to <see cref="M11" />.</param>
        /// <param name="m12">The value to assign to <see cref="M12" />.</param>
        /// <param name="m21">The value to assign to <see cref="M21" />.</param>
        /// <param name="m22">The value to assign to <see cref="M22" />.</param>
        /// <param name="m31">The value to assign to <see cref="M31" />.</param>
        /// <param name="m32">The value to assign to <see cref="M32" />.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose elements are set to the specified values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(float m11, float m12,
                                       float m21, float m22,
                                       float m31, float m32)
        {
            Impl result;

            result.X = Vector2.Create(m11, m12);
            result.Y = Vector2.Create(m21, m22);
            result.Z = Vector2.Create(m31, m32);

            return result.AsM3x2();
        }

        /// <summary>Creates a rotation matrix using the given rotation in radians.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateRotation(float radians)
        {
            radians = float.Ieee754Remainder(radians, float.Tau);

            float c;
            float s;

            if (radians is > -RotationEpsilon and < RotationEpsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians is > (float.Pi / 2 - RotationEpsilon) and < (float.Pi / 2 + RotationEpsilon))
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians is < (-float.Pi + RotationEpsilon) or > (float.Pi - RotationEpsilon))
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians is > (-float.Pi / 2 - RotationEpsilon) and < (-float.Pi / 2 + RotationEpsilon))
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

            result.X = Vector2.Create(c, s);
            result.Y = Vector2.Create(-s, c);
            result.Z = Vector2.Zero;

            return result.AsM3x2();
        }

        /// <summary>Creates a rotation matrix using the specified rotation in radians and a center point.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateRotation(float radians, Vector2 centerPoint)
        {
            radians = float.Ieee754Remainder(radians, float.Tau);

            float c, s;

            if (radians is > -RotationEpsilon and < RotationEpsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians is > (float.Pi / 2 - RotationEpsilon) and < (float.Pi / 2 + RotationEpsilon))
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians is < (-float.Pi + RotationEpsilon) or > (float.Pi - RotationEpsilon))
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians is > (-float.Pi / 2 - RotationEpsilon) and < (-float.Pi / 2 + RotationEpsilon))
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

            result.X = Vector2.Create(c, s);
            result.Y = Vector2.Create(-s, c);
            result.Z = Vector2.Create(x, y);

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix from the specified vector scale.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(Vector2 scales)
        {
            Impl result;

            result.X = Vector2.CreateScalar(scales.X);
            result.Y = Vector2.Create(0, scales.Y);
            result.Z = Vector2.Zero;

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix from the specified X and Y components.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(float xScale, float yScale)
        {
            Impl result;

            result.X = Vector2.CreateScalar(xScale);
            result.Y = Vector2.Create(0, yScale);
            result.Z = Vector2.Zero;

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix that is offset by a given center point.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(float xScale, float yScale, Vector2 centerPoint)
        {
            Impl result;

            result.X = Vector2.CreateScalar(xScale);
            result.Y = Vector2.Create(0, yScale);
            result.Z = centerPoint * (Vector2.One - Vector2.Create(xScale, yScale));

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix from the specified vector scale with an offset from the specified center point.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(Vector2 scales, Vector2 centerPoint)
        {
            Impl result;

            result.X = Vector2.CreateScalar(scales.X);
            result.Y = Vector2.Create(0, scales.Y);
            result.Z = centerPoint * (Vector2.One - scales);

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix that scales uniformly with the given scale.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(float scale)
        {
            Impl result;

            result.X = Vector2.CreateScalar(scale);
            result.Y = Vector2.Create(0, scale);
            result.Z = Vector2.Zero;

            return result.AsM3x2();
        }

        /// <summary>Creates a scaling matrix that scales uniformly with the specified scale with an offset from the specified center.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateScale(float scale, Vector2 centerPoint)
        {
            Impl result;

            result.X = Vector2.CreateScalar(scale);
            result.Y = Vector2.Create(0, scale);
            result.Z = centerPoint * (1.0f - scale);

            return result.AsM3x2();
        }

        /// <summary>Creates a skew matrix from the specified angles in radians.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>The skew matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
        {
            Impl result;

            result.X = Vector2.Create(1, float.Tan(radiansY));
            result.Y = Vector2.Create(float.Tan(radiansX), 1);
            result.Z = Vector2.Zero;

            return result.AsM3x2();
        }

        /// <summary>Creates a skew matrix from the specified angles in radians and a center point.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The skew matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
        {
            float xTan = float.Tan(radiansX);
            float yTan = float.Tan(radiansY);

            float tx = -centerPoint.Y * xTan;
            float ty = -centerPoint.X * yTan;

            Impl result;

            result.X = Vector2.Create(1, yTan);
            result.Y = Vector2.Create(xTan, 1);
            result.Z = Vector2.Create(tx, ty);

            return result.AsM3x2();
        }

        /// <summary>Creates a translation matrix from the specified 2-dimensional vector.</summary>
        /// <param name="position">The translation position.</param>
        /// <returns>The translation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateTranslation(Vector2 position)
        {
            Impl result;

            result.X = Vector2.UnitX;
            result.Y = Vector2.UnitY;
            result.Z = position;

            return result.AsM3x2();
        }

        /// <summary>Creates a translation matrix from the specified X and Y components.</summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>The translation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 CreateTranslation(float xPosition, float yPosition)
        {
            Impl result;

            result.X = Vector2.UnitX;
            result.Y = Vector2.UnitY;
            result.Z = Vector2.Create(xPosition, yPosition);

            return result.AsM3x2();
        }

        /// <summary>Tries to invert the specified matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">When this method returns, contains the inverted matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was converted successfully; otherwise,  <see langword="false" />.</returns>
        public static bool Invert(Matrix3x2 matrix, out Matrix3x2 result)
        {
            Unsafe.SkipInit(out result);
            ref Impl resultImpl = ref result.AsImpl();

            float det = (matrix.X.X * matrix.Y.Y) - (matrix.Y.X * matrix.X.Y);

            if (float.Abs(det) < InverseEpsilon)
            {
                Vector2 vNaN = Vector2.NaN;

                resultImpl.X = vNaN;
                resultImpl.Y = vNaN;
                resultImpl.Z = vNaN;

                return false;
            }

            float invDet = 1.0f / det;

            resultImpl.X = Vector2.Create(
                +matrix.Y.Y * invDet,
                -matrix.X.Y * invDet
            );
            resultImpl.Y = Vector2.Create(
                -matrix.Y.X * invDet,
                +matrix.X.X * invDet
            );
            resultImpl.Z = Vector2.Create(
                (matrix.Y.X * matrix.Z.Y - matrix.Z.X * matrix.Y.Y) * invDet,
                (matrix.Z.X * matrix.X.Y - matrix.X.X * matrix.Z.Y) * invDet
            );

            return true;
        }

        /// <summary>Performs a linear interpolation from one matrix to a second matrix based on a value that specifies the weighting of the second matrix.</summary>
        /// <param name="matrix1">The first matrix.</param>
        /// <param name="matrix2">The second matrix.</param>
        /// <param name="amount">The relative weighting of <paramref name="matrix2" />.</param>
        /// <returns>The interpolated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Lerp(Matrix3x2 matrix1, Matrix3x2 matrix2, float amount)
        {
            ref readonly Impl left = ref matrix1.AsROImpl();
            ref readonly Impl right = ref matrix2.AsROImpl();

            Impl result;

            result.X = Vector2.Lerp(left.X, right.X, amount);
            result.Y = Vector2.Lerp(left.Y, right.Y, amount);
            result.Z = Vector2.Lerp(left.Z, right.Z, amount);

            return result.AsM3x2();
        }

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, Matrix3x2 value2) => value1 * value2;

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, float value2) => value1 * value2;

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Negate(Matrix3x2 value) => -value;

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Subtract(Matrix3x2 value1, Matrix3x2 value2) => value1 - value2;

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Matrix3x2" /> object and the corresponding elements of each matrix are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            => (obj is Matrix3x2 other) && Equals(other);

        /// <summary>Returns a value that indicates whether this instance and another <see cref="Matrix3x2" /> are equal.</summary>
        /// <param name="other">The other matrix.</param>
        /// <returns><see langword="true" /> if the two matrices are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix3x2 other)
        {
            ref readonly Impl left = ref AsROImpl();
            ref readonly Impl right = ref other.AsROImpl();

            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            return left.X.Equals(right.X) &&
                   left.Y.Equals(right.Y) &&
                   left.Z.Equals(right.Z);
        }

        /// <summary>Calculates the determinant for this matrix.</summary>
        /// <returns>The determinant.</returns>
        /// <remarks>The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).</remarks>
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

            ref readonly Impl impl = ref AsROImpl();
            return (impl.X.X * impl.Y.Y) -
                   (impl.X.Y * impl.Y.X);
        }

        /// <summary>Gets the element at the specified row and column.</summary>
        /// <param name="row">The index of the row containing the element to get.</param>
        /// <param name="column">The index of the column containing the element to get.</param>
        /// <returns>The element at index: [<paramref name="row" />, <paramref name="column" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="row" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).
        /// -or-
        /// <paramref name="column" /> was less than zero or greater than or equal to the number of columns (<c>2</c>).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetElement(int row, int column) => this[row, column];

        /// <summary>Gets or sets the row at the specified index.</summary>
        /// <param name="index">The index of the row to get.</param>
        /// <returns>The row at index: [<paramref name="index" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 GetRow(int index) => this[index];

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            ref readonly Impl impl = ref AsROImpl();
            return HashCode.Combine(impl.X, impl.Y, impl.Z);
        }

        /// <summary>Returns a string that represents this matrix.</summary>
        /// <returns>The string representation of this matrix.</returns>
        /// <remarks>The numeric values in the returned string are formatted by using the conventions of the current culture. For example, for the en-US culture, the returned string might appear as <c>{ {M11:1.1 M12:1.2} {M21:2.1 M22:2.2} {M31:3.1 M32:3.2} }</c>.</remarks>
        public override readonly string ToString()
            => $"{{ {{M11:{M11} M12:{M12}}} {{M21:{M21} M22:{M22}}} {{M31:{M31} M32:{M32}}} }}";

        /// <summary>Creates a new <see cref="Matrix3x2"/> with the element at the specified row and column set to the given value and the remaining elements set to the same value as that in the current matrix.</summary>
        /// <param name="row">The index of the row containing the element to replace.</param>
        /// <param name="column">The index of the column containing the element to replace.</param>
        /// <param name="value">The value to assign to the element at index: [<paramref name="row"/>, <paramref name="column"/>].</param>
        /// <returns>A <see cref="Matrix3x2" /> with the value of the element at index: [<paramref name="row"/>, <paramref name="column"/>] set to <paramref name="value" /> and the remaining elements set to the same value as that in the current matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="row" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).
        /// -or-
        /// <paramref name="column" /> was less than zero or greater than or equal to the number of columns (<c>2</c>).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3x2 WithElement(int row, int column, float value)
        {
            Matrix3x2 result = this;
            result[row, column] = value;
            return result;
        }

        /// <summary>Creates a new <see cref="Matrix3x2"/> with the row at the specified index set to the given value and the remaining rows set to the same value as that in the current matrix.</summary>
        /// <param name="index">The index of the row to replace.</param>
        /// <param name="value">The value to assign to the row at index: [<paramref name="index"/>].</param>
        /// <returns>A <see cref="Matrix3x2" /> with the value of the row at index: [<paramref name="index"/>] set to <paramref name="value" /> and the remaining rows set to the same value as that in the current matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than or equal to the number of rows (<c>3</c>).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3x2 WithRow(int index, Vector2 value)
        {
            Matrix3x2 result = this;
            result[index] = value;
            return result;
        }
    }
}

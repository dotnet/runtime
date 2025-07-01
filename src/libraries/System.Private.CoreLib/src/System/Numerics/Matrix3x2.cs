// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

        // In an ideal world, we'd have 3x Vector2 fields. However, Matrix3x2 was shipped with
        // 6x public float fields and as such we cannot change the "backing" fields without it being
        // a breaking change. Likewise, we cannot switch to using something like ExplicitLayout
        // without it pessimizing other parts of the JIT and still preventing things like field promotion.
        //
        // This nested Impl struct works around this problem by relying on the JIT treating same sizeof
        // value type bitcasts as a no-op. Effectively the entire implementation is here in this type
        // and the public facing Matrix3x2 just defers to it with simple reinterpret casts inserted
        // at the relevant points.

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
            get => (X == Vector2.UnitX)
                && (Y == Vector2.UnitY)
                && (Z == Vector2.Zero);
        }

        /// <summary>Gets or sets the translation component of this matrix.</summary>
        /// <remarks>The translation component is stored as <see cref="Z" />.</remarks>
        public Vector2 Translation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => Z;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Z = value;
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
                if (RuntimeHelpers.IsKnownConstant(row))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            return X;
                        }

                        case 1:
                        {
                            return Y;
                        }

                        case 2:
                        {
                            return Z;
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
                    return Unsafe.Add(ref Unsafe.AsRef(in AsROImpl()).X, row);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (RuntimeHelpers.IsKnownConstant(row))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            X = value;
                            break;
                        }

                        case 1:
                        {
                            Y = value;
                            break;
                        }

                        case 2:
                        {
                            Z = value;
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
                    Unsafe.Add(ref AsImpl().X, row) = value;
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
                if (RuntimeHelpers.IsKnownConstant(row) && RuntimeHelpers.IsKnownConstant(column))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            return X.GetElement(column);
                        }

                        case 1:
                        {
                            return Y.GetElement(column);
                        }

                        case 2:
                        {
                            return Z.GetElement(column);
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
                    return Unsafe.Add(ref Unsafe.AsRef(in M11), (row * ColumnCount) + column);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (RuntimeHelpers.IsKnownConstant(row) && RuntimeHelpers.IsKnownConstant(column))
                {
                    switch (row)
                    {
                        case 0:
                        {
                            X = X.WithElement(column, value);
                            break;
                        }

                        case 1:
                        {
                            Y = Y.WithElement(column, value);
                            break;
                        }

                        case 2:
                        {
                            Z = Z.WithElement(column, value);
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
                    Unsafe.Add(ref M11, (row * ColumnCount) + column) = value;
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
            => (value1.AsImpl() + value2.AsImpl()).AsM3x2();

        /// <summary>Returns a value that indicates whether the specified matrices are equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix3x2 value1, Matrix3x2 value2)
            => value1.AsImpl() == value2.AsImpl();

        /// <summary>Returns a value that indicates whether the specified matrices are not equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix3x2 value1, Matrix3x2 value2)
            => value1.AsImpl() != value2.AsImpl();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        /// <remarks>The <see cref="Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator *(Matrix3x2 value1, Matrix3x2 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM3x2();

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        /// <remarks>The <see cref="Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x2" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator *(Matrix3x2 value1, float value2)
            => (value1.AsImpl() * value2).AsM3x2();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="Subtract" /> method defines the operation of the subtraction operator for <see cref="Matrix3x2" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator -(Matrix3x2 value1, Matrix3x2 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM3x2();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        /// <altmember cref="Negate(Matrix3x2)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 operator -(Matrix3x2 value)
            => (-value.AsImpl()).AsM3x2();

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Add(Matrix3x2 value1, Matrix3x2 value2)
            => (value1.AsImpl() + value2.AsImpl()).AsM3x2();

        /// <summary>Creates a <see cref="Matrix3x2" /> whose 6 elements are set to the specified value.</summary>
        /// <param name="value">The value to assign to all 6 elements.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose 6 elements are set to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(float value) => Create(Vector2.Create(value));

        /// <summary>Creates a <see cref="Matrix3x2" /> whose 3 rows are set to the specified value.</summary>
        /// <param name="value">The value to assign to all 3 rows.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose 3 rows are set to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Vector2 value) => Create(value, value, value);

        /// <summary>Creates a <see cref="Matrix3x2" /> from the specified rows.</summary>
        /// <param name="x">The value to assign to <see cref="X" />.</param>
        /// <param name="y">The value to assign to <see cref="Y" />.</param>
        /// <param name="z">The value to assign to <see cref="Z" />.</param>
        /// <returns>A <see cref="Matrix3x2" /> whose rows are set to the specified values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Vector2 x, Vector2 y, Vector2 z)
        {
            Unsafe.SkipInit(out Matrix3x2 result);

            result.X = x;
            result.Y = y;
            result.Z = z;

            return result;
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
                                       float m31, float m32) => Create(
            Vector2.Create(m11, m12),
            Vector2.Create(m21, m22),
            Vector2.Create(m31, m32)
        );

        /// <summary>Creates a rotation matrix using the given rotation in radians.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians)
            => Impl.CreateRotation(radians).AsM3x2();

        /// <summary>Creates a rotation matrix using the specified rotation in radians and a center point.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians, Vector2 centerPoint)
            => Impl.CreateRotation(radians, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified vector scale.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales)
            => Impl.CreateScale(scales).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified X and Y components.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale)
            => Impl.CreateScale(xScale, yScale).AsM3x2();

        /// <summary>Creates a scaling matrix that is offset by a given center point.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale, Vector2 centerPoint)
            => Impl.CreateScale(xScale, yScale, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified vector scale with an offset from the specified center point.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales, Vector2 centerPoint)
            => Impl.CreateScale(scales, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix that scales uniformly with the given scale.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale)
            => Impl.CreateScale(scale).AsM3x2();

        /// <summary>Creates a scaling matrix that scales uniformly with the specified scale with an offset from the specified center.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale, Vector2 centerPoint)
            => Impl.CreateScale(scale, centerPoint).AsM3x2();

        /// <summary>Creates a skew matrix from the specified angles in radians.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
            => Impl.CreateSkew(radiansX, radiansY).AsM3x2();

        /// <summary>Creates a skew matrix from the specified angles in radians and a center point.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
            => Impl.CreateSkew(radiansX, radiansY, centerPoint).AsM3x2();

        /// <summary>Creates a translation matrix from the specified 2-dimensional vector.</summary>
        /// <param name="position">The translation position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(Vector2 position)
            => Impl.CreateTranslation(position).AsM3x2();

        /// <summary>Creates a translation matrix from the specified X and Y components.</summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(float xPosition, float yPosition)
            => Impl.CreateTranslation(xPosition, yPosition).AsM3x2();

        /// <summary>Tries to invert the specified matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">When this method returns, contains the inverted matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was converted successfully; otherwise,  <see langword="false" />.</returns>
        public static bool Invert(Matrix3x2 matrix, out Matrix3x2 result)
        {
            Unsafe.SkipInit(out result);
            return Impl.Invert(in matrix.AsImpl(), out result.AsImpl());
        }

        /// <summary>Performs a linear interpolation from one matrix to a second matrix based on a value that specifies the weighting of the second matrix.</summary>
        /// <param name="matrix1">The first matrix.</param>
        /// <param name="matrix2">The second matrix.</param>
        /// <param name="amount">The relative weighting of <paramref name="matrix2" />.</param>
        /// <returns>The interpolated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Lerp(Matrix3x2 matrix1, Matrix3x2 matrix2, float amount)
            => Impl.Lerp(in matrix1.AsImpl(), in matrix2.AsImpl(), amount).AsM3x2();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, Matrix3x2 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM3x2();

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, float value2)
            => (value1.AsImpl() * value2).AsM3x2();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Negate(Matrix3x2 value)
            => (-value.AsImpl()).AsM3x2();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Subtract(Matrix3x2 value1, Matrix3x2 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM3x2();

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Matrix3x2" /> object and the corresponding elements of each matrix are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            => AsROImpl().Equals(obj);

        /// <summary>Returns a value that indicates whether this instance and another <see cref="Matrix3x2" /> are equal.</summary>
        /// <param name="other">The other matrix.</param>
        /// <returns><see langword="true" /> if the two matrices are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix3x2 other)
            => AsROImpl().Equals(in other.AsImpl());

        /// <summary>Calculates the determinant for this matrix.</summary>
        /// <returns>The determinant.</returns>
        /// <remarks>The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetDeterminant()
            => AsROImpl().GetDeterminant();

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
            => AsROImpl().GetHashCode();

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

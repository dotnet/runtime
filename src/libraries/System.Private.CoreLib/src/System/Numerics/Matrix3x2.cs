// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>Represents a 3x2 matrix.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>
    [Intrinsic]
    public struct Matrix3x2 : IEquatable<Matrix3x2>
    {
        private const float RotationEpsilon = 0.001f * MathF.PI / 180f;     // 0.1% of a degree

        private static readonly Matrix3x2 _identity = new Matrix3x2(
            1f, 0f,
            0f, 1f,
            0f, 0f
        );

        /// <summary>The first element of the first row.</summary>
        public float M11;

        /// <summary>The second element of the first row.</summary>
        public float M12;

        /// <summary>The first element of the second row.</summary>
        public float M21;

        /// <summary>The second element of the second row.</summary>
        public float M22;

        /// <summary>The first element of the third row.</summary>
        public float M31;

        /// <summary>The second element of the third row.</summary>
        public float M32;

        /// <summary>Creates a 3x2 matrix from the specified components.</summary>
        /// <param name="m11">The value to assign to the first element in the first row.</param>
        /// <param name="m12">The value to assign to the second element in the first row.</param>
        /// <param name="m21">The value to assign to the first element in the second row.</param>
        /// <param name="m22">The value to assign to the second element in the second row.</param>
        /// <param name="m31">The value to assign to the first element in the third row.</param>
        /// <param name="m32">The value to assign to the second element in the third row.</param>
        public Matrix3x2(float m11, float m12,
                         float m21, float m22,
                         float m31, float m32)
        {
            M11 = m11;
            M12 = m12;

            M21 = m21;
            M22 = m22;

            M31 = m31;
            M32 = m32;
        }

        /// <summary>Gets the multiplicative identity matrix.</summary>
        /// <value>The multiplicative identify matrix.</value>
        public static Matrix3x2 Identity
        {
            get => _identity;
        }

        /// <summary>Gets a value that indicates whether the current matrix is the identity matrix.</summary>
        /// <value><see langword="true" /> if the current matrix is the identity matrix; otherwise, <see langword="false" />.</value>
        public readonly bool IsIdentity
        {
            get => this == Identity;
        }

        /// <summary>Gets or sets the translation component of this matrix.</summary>
        /// <value>The translation component of the current instance.</value>
        public Vector2 Translation
        {
            readonly get => new Vector2(M31, M32);

            set
            {
                M31 = value.X;
                M32 = value.Y;
            }
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values.</returns>
        /// <remarks>The <see cref="System.Numerics.Matrix3x2.op_Addition" /> method defines the operation of the addition operator for <see cref="System.Numerics.Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator +(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 m;

            m.M11 = value1.M11 + value2.M11;
            m.M12 = value1.M12 + value2.M12;

            m.M21 = value1.M21 + value2.M21;
            m.M22 = value1.M22 + value2.M22;

            m.M31 = value1.M31 + value2.M31;
            m.M32 = value1.M32 + value2.M32;

            return m;
        }

        /// <summary>Returns a value that indicates whether the specified matrices are equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        public static bool operator ==(Matrix3x2 value1, Matrix3x2 value2)
        {
            // Check diagonal element first for early out.
            return (value1.M11 == value2.M11
                 && value1.M22 == value2.M22
                 && value1.M12 == value2.M12
                 && value1.M21 == value2.M21
                 && value1.M31 == value2.M31
                 && value1.M32 == value2.M32);
        }

        /// <summary>Returns a value that indicates whether the specified matrices are not equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(Matrix3x2 value1, Matrix3x2 value2)
        {
            return !(value1 == value2);
        }

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        /// <remarks>The <see cref="System.Numerics.Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="System.Numerics.Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator *(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 m;

            // First row
            m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21;
            m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22;

            // Second row
            m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21;
            m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22;

            // Third row
            m.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value2.M31;
            m.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value2.M32;

            return m;
        }

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        /// <remarks>The <see cref="System.Numerics.Matrix3x2.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="System.Numerics.Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator *(Matrix3x2 value1, float value2)
        {
            Matrix3x2 m;

            m.M11 = value1.M11 * value2;
            m.M12 = value1.M12 * value2;

            m.M21 = value1.M21 * value2;
            m.M22 = value1.M22 * value2;

            m.M31 = value1.M31 * value2;
            m.M32 = value1.M32 * value2;

            return m;
        }

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="System.Numerics.Matrix3x2.Subtract" /> method defines the operation of the subtraction operator for <see cref="System.Numerics.Matrix3x2" /> objects.</remarks>
        public static Matrix3x2 operator -(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 m;

            m.M11 = value1.M11 - value2.M11;
            m.M12 = value1.M12 - value2.M12;

            m.M21 = value1.M21 - value2.M21;
            m.M22 = value1.M22 - value2.M22;

            m.M31 = value1.M31 - value2.M31;
            m.M32 = value1.M32 - value2.M32;

            return m;
        }

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        /// <altmember cref="System.Numerics.Matrix3x2.Negate(System.Numerics.Matrix3x2)"/>
        public static Matrix3x2 operator -(Matrix3x2 value)
        {
            Matrix3x2 m;

            m.M11 = -value.M11;
            m.M12 = -value.M12;

            m.M21 = -value.M21;
            m.M22 = -value.M22;

            m.M31 = -value.M31;
            m.M32 = -value.M32;

            return m;
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Add(Matrix3x2 value1, Matrix3x2 value2)
        {
            return value1 + value2;
        }

        /// <summary>Creates a rotation matrix using the given rotation in radians.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians)
        {
            radians = MathF.IEEERemainder(radians, MathF.PI * 2);

            float c, s;

            if (radians > -RotationEpsilon && radians < RotationEpsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > MathF.PI / 2 - RotationEpsilon && radians < MathF.PI / 2 + RotationEpsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -MathF.PI + RotationEpsilon || radians > MathF.PI - RotationEpsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -MathF.PI / 2 - RotationEpsilon && radians < -MathF.PI / 2 + RotationEpsilon)
            {
                // Exact case for 270 degree rotation.
                c = 0;
                s = -1;
            }
            else
            {
                // Arbitrary rotation.
                c = MathF.Cos(radians);
                s = MathF.Sin(radians);
            }

            // [  c  s ]
            // [ -s  c ]
            // [  0  0 ]
            Matrix3x2 result = Identity;

            result.M11 = c;
            result.M12 = s;
            result.M21 = -s;
            result.M22 = c;

            return result;
        }

        /// <summary>Creates a rotation matrix using the specified rotation in radians and a center point.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians, Vector2 centerPoint)
        {
            Matrix3x2 result;

            radians = MathF.IEEERemainder(radians, MathF.PI * 2);

            float c, s;

            if (radians > -RotationEpsilon && radians < RotationEpsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > MathF.PI / 2 - RotationEpsilon && radians < MathF.PI / 2 + RotationEpsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -MathF.PI + RotationEpsilon || radians > MathF.PI - RotationEpsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -MathF.PI / 2 - RotationEpsilon && radians < -MathF.PI / 2 + RotationEpsilon)
            {
                // Exact case for 270 degree rotation.
                c = 0;
                s = -1;
            }
            else
            {
                // Arbitrary rotation.
                c = MathF.Cos(radians);
                s = MathF.Sin(radians);
            }

            float x = centerPoint.X * (1 - c) + centerPoint.Y * s;
            float y = centerPoint.Y * (1 - c) - centerPoint.X * s;

            // [  c  s ]
            // [ -s  c ]
            // [  x  y ]
            result.M11 = c;
            result.M12 = s;
            result.M21 = -s;
            result.M22 = c;
            result.M31 = x;
            result.M32 = y;

            return result;
        }

        /// <summary>Creates a scaling matrix from the specified vector scale.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales)
        {
            Matrix3x2 result = Identity;

            result.M11 = scales.X;
            result.M22 = scales.Y;

            return result;
        }

        /// <summary>Creates a scaling matrix from the specified X and Y components.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale)
        {
            Matrix3x2 result = Identity;

            result.M11 = xScale;
            result.M22 = yScale;

            return result;
        }

        /// <summary>Creates a scaling matrix that is offset by a given center point.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale, Vector2 centerPoint)
        {
            Matrix3x2 result = Identity;

            float tx = centerPoint.X * (1 - xScale);
            float ty = centerPoint.Y * (1 - yScale);

            result.M11 = xScale;
            result.M22 = yScale;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>Creates a scaling matrix from the specified vector scale with an offset from the specified center point.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales, Vector2 centerPoint)
        {
            Matrix3x2 result = Identity;

            float tx = centerPoint.X * (1 - scales.X);
            float ty = centerPoint.Y * (1 - scales.Y);

            result.M11 = scales.X;
            result.M22 = scales.Y;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>Creates a scaling matrix that scales uniformly with the given scale.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale)
        {
            Matrix3x2 result = Identity;

            result.M11 = scale;
            result.M22 = scale;

            return result;
        }

        /// <summary>Creates a scaling matrix that scales uniformly with the specified scale with an offset from the specified center.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale, Vector2 centerPoint)
        {
            Matrix3x2 result = Identity;

            float tx = centerPoint.X * (1 - scale);
            float ty = centerPoint.Y * (1 - scale);

            result.M11 = scale;
            result.M22 = scale;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>Creates a skew matrix from the specified angles in radians.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
        {
            Matrix3x2 result = Identity;

            float xTan = MathF.Tan(radiansX);
            float yTan = MathF.Tan(radiansY);

            result.M12 = yTan;
            result.M21 = xTan;

            return result;
        }

        /// <summary>Creates a skew matrix from the specified angles in radians and a center point.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
        {
            Matrix3x2 result = Identity;

            float xTan = MathF.Tan(radiansX);
            float yTan = MathF.Tan(radiansY);

            float tx = -centerPoint.Y * xTan;
            float ty = -centerPoint.X * yTan;

            result.M12 = yTan;
            result.M21 = xTan;

            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>Creates a translation matrix from the specified 2-dimensional vector.</summary>
        /// <param name="position">The translation position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(Vector2 position)
        {
            Matrix3x2 result = Identity;

            result.M31 = position.X;
            result.M32 = position.Y;

            return result;
        }

        /// <summary>Creates a translation matrix from the specified X and Y components.</summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(float xPosition, float yPosition)
        {
            Matrix3x2 result = Identity;

            result.M31 = xPosition;
            result.M32 = yPosition;

            return result;
        }

        /// <summary>Tries to invert the specified matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">When this method returns, contains the inverted matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was converted successfully; otherwise,  <see langword="false" />.</returns>
        public static bool Invert(Matrix3x2 matrix, out Matrix3x2 result)
        {
            float det = (matrix.M11 * matrix.M22) - (matrix.M21 * matrix.M12);

            if (MathF.Abs(det) < float.Epsilon)
            {
                result = new Matrix3x2(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN);
                return false;
            }

            float invDet = 1.0f / det;

            result.M11 = matrix.M22 * invDet;
            result.M12 = -matrix.M12 * invDet;

            result.M21 = -matrix.M21 * invDet;
            result.M22 = matrix.M11 * invDet;

            result.M31 = (matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22) * invDet;
            result.M32 = (matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32) * invDet;

            return true;
        }

        /// <summary>Performs a linear interpolation from one matrix to a second matrix based on a value that specifies the weighting of the second matrix.</summary>
        /// <param name="matrix1">The first matrix.</param>
        /// <param name="matrix2">The second matrix.</param>
        /// <param name="amount">The relative weighting of <paramref name="matrix2" />.</param>
        /// <returns>The interpolated matrix.</returns>
        public static Matrix3x2 Lerp(Matrix3x2 matrix1, Matrix3x2 matrix2, float amount)
        {
            Matrix3x2 result;

            // First row
            result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
            result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;

            // Second row
            result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
            result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;

            // Third row
            result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
            result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;

            return result;
        }

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, Matrix3x2 value2)
        {
            return value1 * value2;
        }

        /// <summary>Multiplies a matrix by a float to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Multiply(Matrix3x2 value1, float value2)
        {
            return value1 * value2;
        }

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Negate(Matrix3x2 value)
        {
            return -value;
        }

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Subtract(Matrix3x2 value1, Matrix3x2 value2)
        {
            return value1 - value2;
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Numerics.Matrix3x2" /> object and the corresponding elements of each matrix are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Matrix3x2 other) && Equals(other);
        }

        /// <summary>Returns a value that indicates whether this instance and another 3x2 matrix are equal.</summary>
        /// <param name="other">The other matrix.</param>
        /// <returns><see langword="true" /> if the two matrices are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        public readonly bool Equals(Matrix3x2 other)
        {
            return this == other;
        }

        /// <summary>Calculates the determinant for this matrix.</summary>
        /// <returns>The determinant.</returns>
        /// <remarks>The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).</remarks>
        public readonly float GetDeterminant()
        {
            // There isn't actually any such thing as a determinant for a non-square matrix,
            // but this 3x2 type is really just an optimization of a 3x3 where we happen to
            // know the rightmost column is always (0, 0, 1). So we expand to 3x3 format:
            //
            //  [ M11, M12, 0 ]
            //  [ M21, M22, 0 ]
            //  [ M31, M32, 1 ]
            //
            // Sum the diagonal products:
            //  (M11 * M22 * 1) + (M12 * 0 * M31) + (0 * M21 * M32)
            //
            // Subtract the opposite diagonal products:
            //  (M31 * M22 * 0) + (M32 * 0 * M11) + (1 * M21 * M12)
            //
            // Collapse out the constants and oh look, this is just a 2x2 determinant!

            return (M11 * M22) - (M21 * M12);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(M11, M12, M21, M22, M31, M32);
        }

        /// <summary>Returns a string that represents this matrix.</summary>
        /// <returns>The string representation of this matrix.</returns>
        /// <remarks>The numeric values in the returned string are formatted by using the conventions of the current culture. For example, for the en-US culture, the returned string might appear as <c>{ {M11:1.1 M12:1.2} {M21:2.1 M22:2.2} {M31:3.1 M32:3.2} }</c>.</remarks>
        public override readonly string ToString() =>
            $"{{ {{M11:{M11} M12:{M12}}} {{M21:{M21} M22:{M22}}} {{M31:{M31} M32:{M32}}} }}";
    }
}

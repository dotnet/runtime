// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating a 3x2 matrix.
    /// </summary>
    public struct Matrix3x2 : IEquatable<Matrix3x2>
    {
        #region Public Fields
        /// <summary>
        /// The first element of the first row
        /// </summary>
        public float M11;
        /// <summary>
        /// The second element of the first row
        /// </summary>
        public float M12;
        /// <summary>
        /// The first element of the second row
        /// </summary>
        public float M21;
        /// <summary>
        /// The second element of the second row
        /// </summary>
        public float M22;
        /// <summary>
        /// The first element of the third row
        /// </summary>
        public float M31;
        /// <summary>
        /// The second element of the third row
        /// </summary>
        public float M32;
        #endregion Public Fields

        private static readonly Matrix3x2 _identity = new Matrix3x2
        (
            1f, 0f,
            0f, 1f,
            0f, 0f
        );

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix3x2 Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return M11 == 1f && M22 == 1f && // Check diagonal element first for early out.
                                    M12 == 0f &&
                       M21 == 0f &&
                       M31 == 0f && M32 == 0f;
            }
        }

        /// <summary>
        /// Gets or sets the translation component of this matrix.
        /// </summary>
        public Vector2 Translation
        {
            get
            {
                return new Vector2(M31, M32);
            }

            set
            {
                M31 = value.X;
                M32 = value.Y;
            }
        }

        /// <summary>
        /// Constructs a Matrix3x2 from the given components.
        /// </summary>
        public Matrix3x2(float m11, float m12,
                         float m21, float m22,
                         float m31, float m32)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
            this.M31 = m31;
            this.M32 = m32;
        }

        /// <summary>
        /// Creates a translation matrix from the given vector.
        /// </summary>
        /// <param name="position">The translation position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(Vector2 position)
        {
            Matrix3x2 result;

            result.M11 = 1.0f;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1.0f;

            result.M31 = position.X;
            result.M32 = position.Y;

            return result;
        }

        /// <summary>
        /// Creates a translation matrix from the given X and Y components.
        /// </summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix3x2 CreateTranslation(float xPosition, float yPosition)
        {
            Matrix3x2 result;

            result.M11 = 1.0f;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1.0f;

            result.M31 = xPosition;
            result.M32 = yPosition;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix from the given X and Y components.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale)
        {
            Matrix3x2 result;

            result.M11 = xScale;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = yScale;
            result.M31 = 0.0f;
            result.M32 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix that is offset by a given center point.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float xScale, float yScale, Vector2 centerPoint)
        {
            Matrix3x2 result;

            float tx = centerPoint.X * (1 - xScale);
            float ty = centerPoint.Y * (1 - yScale);

            result.M11 = xScale;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = yScale;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales)
        {
            Matrix3x2 result;

            result.M11 = scales.X;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = scales.Y;
            result.M31 = 0.0f;
            result.M32 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale with an offset from the given center point.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(Vector2 scales, Vector2 centerPoint)
        {
            Matrix3x2 result;

            float tx = centerPoint.X * (1 - scales.X);
            float ty = centerPoint.Y * (1 - scales.Y);

            result.M11 = scales.X;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = scales.Y;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix that scales uniformly with the given scale.
        /// </summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale)
        {
            Matrix3x2 result;

            result.M11 = scale;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = scale;
            result.M31 = 0.0f;
            result.M32 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates a scale matrix that scales uniformly with the given scale with an offset from the given center.
        /// </summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x2 CreateScale(float scale, Vector2 centerPoint)
        {
            Matrix3x2 result;

            float tx = centerPoint.X * (1 - scale);
            float ty = centerPoint.Y * (1 - scale);

            result.M11 = scale;
            result.M12 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = scale;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>
        /// Creates a skew matrix from the given angles in radians.
        /// </summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>A skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
        {
            Matrix3x2 result;

            float xTan = MathF.Tan(radiansX);
            float yTan = MathF.Tan(radiansY);

            result.M11 = 1.0f;
            result.M12 = yTan;
            result.M21 = xTan;
            result.M22 = 1.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates a skew matrix from the given angles in radians and a center point.
        /// </summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A skew matrix.</returns>
        public static Matrix3x2 CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
        {
            Matrix3x2 result;

            float xTan = MathF.Tan(radiansX);
            float yTan = MathF.Tan(radiansY);

            float tx = -centerPoint.Y * xTan;
            float ty = -centerPoint.X * yTan;

            result.M11 = 1.0f;
            result.M12 = yTan;
            result.M21 = xTan;
            result.M22 = 1.0f;
            result.M31 = tx;
            result.M32 = ty;

            return result;
        }

        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians)
        {
            Matrix3x2 result;

            radians = MathF.IEEERemainder(radians, MathF.PI * 2);

            float c, s;

            const float epsilon = 0.001f * MathF.PI / 180f;     // 0.1% of a degree

            if (radians > -epsilon && radians < epsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > MathF.PI / 2 - epsilon && radians < MathF.PI / 2 + epsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -MathF.PI + epsilon || radians > MathF.PI - epsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -MathF.PI / 2 - epsilon && radians < -MathF.PI / 2 + epsilon)
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
            result.M11 = c;
            result.M12 = s;
            result.M21 = -s;
            result.M22 = c;
            result.M31 = 0.0f;
            result.M32 = 0.0f;

            return result;
        }

        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians and a center point.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix3x2 CreateRotation(float radians, Vector2 centerPoint)
        {
            Matrix3x2 result;

            radians = MathF.IEEERemainder(radians, MathF.PI * 2);

            float c, s;

            const float epsilon = 0.001f * MathF.PI / 180f;     // 0.1% of a degree

            if (radians > -epsilon && radians < epsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > MathF.PI / 2 - epsilon && radians < MathF.PI / 2 + epsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -MathF.PI + epsilon || radians > MathF.PI - epsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -MathF.PI / 2 - epsilon && radians < -MathF.PI / 2 + epsilon)
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

        /// <summary>
        /// Calculates the determinant for this matrix. 
        /// The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).
        /// </summary>
        /// <returns>The determinant.</returns>
        public float GetDeterminant()
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

        /// <summary>
        /// Attempts to invert the given matrix. If the operation succeeds, the inverted matrix is stored in the result parameter.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="result">The output matrix.</param>
        /// <returns>True if the operation succeeded, False otherwise.</returns>
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

        /// <summary>
        /// Linearly interpolates from matrix1 to matrix2, based on the third parameter.
        /// </summary>
        /// <param name="matrix1">The first source matrix.</param>
        /// <param name="matrix2">The second source matrix.</param>
        /// <param name="amount">The relative weighting of matrix2.</param>
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

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix3x2 Negate(Matrix3x2 value)
        {
            Matrix3x2 result;

            result.M11 = -value.M11;
            result.M12 = -value.M12;
            result.M21 = -value.M21;
            result.M22 = -value.M22;
            result.M31 = -value.M31;
            result.M32 = -value.M32;

            return result;
        }

        /// <summary>
        /// Adds each matrix element in value1 with its corresponding element in value2.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the summed values.</returns>
        public static Matrix3x2 Add(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 result;

            result.M11 = value1.M11 + value2.M11;
            result.M12 = value1.M12 + value2.M12;
            result.M21 = value1.M21 + value2.M21;
            result.M22 = value1.M22 + value2.M22;
            result.M31 = value1.M31 + value2.M31;
            result.M32 = value1.M32 + value2.M32;

            return result;
        }

        /// <summary>
        /// Subtracts each matrix element in value2 from its corresponding element in value1.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the resulting values.</returns>
        public static Matrix3x2 Subtract(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 result;

            result.M11 = value1.M11 - value2.M11;
            result.M12 = value1.M12 - value2.M12;
            result.M21 = value1.M21 - value2.M21;
            result.M22 = value1.M22 - value2.M22;
            result.M31 = value1.M31 - value2.M31;
            result.M32 = value1.M32 - value2.M32;

            return result;
        }

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix3x2 Multiply(Matrix3x2 value1, Matrix3x2 value2)
        {
            Matrix3x2 result;

            // First row
            result.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21;
            result.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22;

            // Second row
            result.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21;
            result.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22;

            // Third row
            result.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value2.M31;
            result.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value2.M32;

            return result;
        }

        /// <summary>
        /// Scales all elements in a matrix by the given scalar factor.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix3x2 Multiply(Matrix3x2 value1, float value2)
        {
            Matrix3x2 result;

            result.M11 = value1.M11 * value2;
            result.M12 = value1.M12 * value2;
            result.M21 = value1.M21 * value2;
            result.M22 = value1.M22 * value2;
            result.M31 = value1.M31 * value2;
            result.M32 = value1.M32 * value2;

            return result;
        }

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
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

        /// <summary>
        /// Adds each matrix element in value1 with its corresponding element in value2.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the summed values.</returns>
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

        /// <summary>
        /// Subtracts each matrix element in value2 from its corresponding element in value1.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the resulting values.</returns>
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

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
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

        /// <summary>
        /// Scales all elements in a matrix by the given scalar factor.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The resulting matrix.</returns>
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

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public static bool operator ==(Matrix3x2 value1, Matrix3x2 value2)
        {
            return (value1.M11 == value2.M11 && value1.M22 == value2.M22 && // Check diagonal element first for early out.
                                                value1.M12 == value2.M12 &&
                    value1.M21 == value2.M21 &&
                    value1.M31 == value2.M31 && value1.M32 == value2.M32);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are not equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are not equal; False if they are equal.</returns>
        public static bool operator !=(Matrix3x2 value1, Matrix3x2 value2)
        {
            return (value1.M11 != value2.M11 || value1.M12 != value2.M12 ||
                    value1.M21 != value2.M21 || value1.M22 != value2.M22 ||
                    value1.M31 != value2.M31 || value1.M32 != value2.M32);
        }

        /// <summary>
        /// Returns a boolean indicating whether the matrix is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The other matrix to test equality against.</param>
        /// <returns>True if this matrix is equal to other; False otherwise.</returns>
        public bool Equals(Matrix3x2 other)
        {
            return (M11 == other.M11 && M22 == other.M22 && // Check diagonal element first for early out.
                                        M12 == other.M12 &&
                    M21 == other.M21 &&
                    M31 == other.M31 && M32 == other.M32);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix3x2)
            {
                return Equals((Matrix3x2)obj);
            }

            return false;
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return String.Format(ci, "{{ {{M11:{0} M12:{1}}} {{M21:{2} M22:{3}}} {{M31:{4} M32:{5}}} }}",
                                 M11.ToString(ci), M12.ToString(ci),
                                 M21.ToString(ci), M22.ToString(ci),
                                 M31.ToString(ci), M32.ToString(ci));
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return unchecked(M11.GetHashCode() + M12.GetHashCode() +
                             M21.GetHashCode() + M22.GetHashCode() +
                             M31.GetHashCode() + M32.GetHashCode());
        }
    }
}

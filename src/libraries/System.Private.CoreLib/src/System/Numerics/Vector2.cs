// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Numerics
{
    /// <summary>A structure encapsulating two single precision floating point values and provides hardware accelerated methods.</summary>
    [Intrinsic]
    public partial struct Vector2 : IEquatable<Vector2>, IFormattable
    {
        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        /// <summary>Constructs a vector whose elements are all the single specified value.</summary>
        /// <param name="value">The element to fill the vector with.</param>
        [Intrinsic]
        public Vector2(float value) : this(value, value)
        {
        }

        /// <summary>Constructs a vector with the given individual elements.</summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        [Intrinsic]
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Returns the vector (0,0).</summary>
        public static Vector2 Zero
        {
            [Intrinsic]
            get => default;
        }

        /// <summary>Returns the vector (1,1).</summary>
        public static Vector2 One
        {
            [Intrinsic]
            get => new Vector2(1.0f);
        }

        /// <summary>Returns the vector (1,0).</summary>
        public static Vector2 UnitX
        {
            get => new Vector2(1.0f, 0.0f);
        }

        /// <summary>Returns the vector (0,1).</summary>
        public static Vector2 UnitY
        {
            get => new Vector2(0.0f, 1.0f);
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(
                left.X + right.X,
                left.Y + right.Y
            );
        }

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 left, Vector2 right)
        {
            return new Vector2(
                left.X / right.X,
                left.Y / right.Y
            );
        }

        /// <summary>Divides the vector by the given scalar.</summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 value1, float value2)
        {
            return value1 / new Vector2(value2);
        }

        /// <summary>Returns a boolean indicating whether the two given vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are equal; False otherwise.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 left, Vector2 right)
        {
            return (left.X == right.X)
                && (left.Y == right.Y);
        }

        /// <summary>Returns a boolean indicating whether the two given vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are not equal; False if they are equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return !(left == right);
        }

        /// <summary>Multiplies two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(
                left.X * right.X,
                left.Y * right.Y
            );
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, float right)
        {
            return left * new Vector2(right);
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float left, Vector2 right)
        {
            return right * left;
        }

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(
                left.X - right.X,
                left.Y - right.Y
            );
        }

        /// <summary>Negates a given vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 value)
        {
            return Zero - value;
        }

        /// <summary>Returns a vector whose elements are the absolute values of each of the source vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(Vector2 value)
        {
            return new Vector2(
                MathF.Abs(value.X),
                MathF.Abs(value.Y)
            );
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Add(Vector2 left, Vector2 right)
        {
            return left + right;
        }

        /// <summary>Restricts a vector between a min and max value.</summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
        {
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            return Min(Max(value1, min), max);
        }

        /// <summary>Returns the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector2 value1, Vector2 value2)
        {
            float distanceSquared = DistanceSquared(value1, value2);
            return MathF.Sqrt(distanceSquared);
        }

        /// <summary>Returns the Euclidean distance squared between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vector2 value1, Vector2 value2)
        {
            Vector2 difference = value1 - value2;
            return Dot(difference, difference);
        }

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Divide(Vector2 left, Vector2 right)
        {
            return left / right;
        }

        /// <summary>Divides the vector by the given scalar.</summary>
        /// <param name="left">The source vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Divide(Vector2 left, float divisor)
        {
            return left / divisor;
        }

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector2 value1, Vector2 value2)
        {
            return (value1.X * value2.X)
                 + (value1.Y * value2.Y);
        }

        /// <summary>Linearly interpolates between two vectors based on the given weighting.</summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
        {
            return (value1 * (1.0f - amount)) + (value2 * amount);
        }

        /// <summary>Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors</summary>
        /// <param name="value1">The first source vector</param>
        /// <param name="value2">The second source vector</param>
        /// <returns>The maximized vector</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(Vector2 value1, Vector2 value2)
        {
            return new Vector2(
                (value1.X > value2.X) ? value1.X : value2.X,
                (value1.Y > value2.Y) ? value1.Y : value2.Y
            );
        }

        /// <summary>Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.</summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <returns>The minimized vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(Vector2 value1, Vector2 value2)
        {
            return new Vector2(
                (value1.X < value2.X) ? value1.X : value2.X,
                (value1.Y < value2.Y) ? value1.Y : value2.Y
            );
        }

        /// <summary>Multiplies two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Multiply(Vector2 left, Vector2 right)
        {
            return left * right;
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Multiply(Vector2 left, float right)
        {
            return left * right;
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Multiply(float left, Vector2 right)
        {
            return left * right;
        }

        /// <summary>Negates a given vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Negate(Vector2 value)
        {
            return -value;
        }

        /// <summary>Returns a vector with the same direction as the given vector, but with a length of 1.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Normalize(Vector2 value)
        {
            return value / value.Length();
        }

        /// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            float dot = Dot(vector, normal);
            return vector - (2 * dot * normal);
        }

        /// <summary>Returns a vector whose elements are the square root of each of the source vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SquareRoot(Vector2 value)
        {
            return new Vector2(
                MathF.Sqrt(value.X),
                MathF.Sqrt(value.Y)
            );
        }

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Subtract(Vector2 left, Vector2 right)
        {
            return left - right;
        }

        /// <summary>Transforms a vector by the given matrix.</summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 position, Matrix3x2 matrix)
        {
            return new Vector2(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M31,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M32
            );
        }

        /// <summary>Transforms a vector by the given matrix.</summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 position, Matrix4x4 matrix)
        {
            return new Vector2(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42
            );
        }

        /// <summary>Transforms a vector by the given Quaternion rotation value.</summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 value, Quaternion rotation)
        {
            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;

            float wz2 = rotation.W * z2;
            float xx2 = rotation.X * x2;
            float xy2 = rotation.X * y2;
            float yy2 = rotation.Y * y2;
            float zz2 = rotation.Z * z2;

            return new Vector2(
                value.X * (1.0f - yy2 - zz2) + value.Y * (xy2 - wz2),
                value.X * (xy2 + wz2) + value.Y * (1.0f - xx2 - zz2)
            );
        }

        /// <summary>Transforms a vector normal by the given matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 TransformNormal(Vector2 normal, Matrix3x2 matrix)
        {
            return new Vector2(
                (normal.X * matrix.M11) + (normal.Y * matrix.M21),
                (normal.X * matrix.M12) + (normal.Y * matrix.M22)
            );
        }

        /// <summary>Transforms a vector normal by the given matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 TransformNormal(Vector2 normal, Matrix4x4 matrix)
        {
            return new Vector2(
                (normal.X * matrix.M11) + (normal.Y * matrix.M21),
                (normal.X * matrix.M12) + (normal.Y * matrix.M22)
            );
        }

        /// <summary>Copies the contents of the vector into the given array.</summary>
        /// <param name="array">The destination array.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(float[] array)
        {
            CopyTo(array, 0);
        }

        /// <summary>Copies the contents of the vector into the given array, starting from the given index.</summary>
        /// <exception cref="ArgumentNullException">If array is null.</exception>
        /// <exception cref="RankException">If array is multidimensional.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero.</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array or if there are not enough elements to copy.</exception>
        [Intrinsic]
        public readonly void CopyTo(float[] array, int index)
        {
            if (array is null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
            }

            if ((index < 0) || (index >= array.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.Format(SR.Arg_ArgumentOutOfRangeException, index));
            }

            if ((array.Length - index) < 2)
            {
                throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, index));
            }

            array[index] = X;
            array[index + 1] = Y;
        }

        /// <summary>Returns a boolean indicating whether the given Object is equal to this Vector2 instance.</summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this Vector2; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Vector2 other) && Equals(other);
        }

        /// <summary>Returns a boolean indicating whether the given Vector2 is equal to this Vector2 instance.</summary>
        /// <param name="other">The Vector2 to compare this instance to.</param>
        /// <returns>True if the other Vector2 is equal to this instance; False otherwise.</returns>
        [Intrinsic]
        public readonly bool Equals(Vector2 other)
        {
            return this == other;
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>Returns the length of the vector.</summary>
        /// <returns>The vector's length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Length()
        {
            float lengthSquared = LengthSquared();
            return MathF.Sqrt(lengthSquared);
        }

        /// <summary>Returns the length of the vector squared. This operation is cheaper than Length().</summary>
        /// <returns>The vector's length squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LengthSquared()
        {
            return Dot(this, this);
        }

        /// <summary>Returns a String representing this Vector2 instance.</summary>
        /// <returns>The string representation.</returns>
        public override readonly string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>Returns a String representing this Vector2 instance, using the specified format to format individual elements.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public readonly string ToString(string? format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>Returns a String representing this Vector2 instance, using the specified format to format individual elements and the given IFormatProvider.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            sb.Append(X.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(Y.ToString(format, formatProvider));
            sb.Append('>');
            return sb.ToString();
        }
    }
}

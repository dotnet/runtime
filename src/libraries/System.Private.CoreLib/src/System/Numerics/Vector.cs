// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>
    /// Contains various methods useful for creating, manipulating, combining, and converting generic vectors with one another.
    /// </summary>
    [Intrinsic]
    public static partial class Vector
    {
        /// <summary>
        /// Creates a new vector with elements selected between the two given source vectors, and based on a mask vector.
        /// </summary>
        /// <param name="condition">The integral mask vector used to drive selection.</param>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The new vector with elements selected based on the mask.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right)
        {
            return (Vector<float>)Vector<float>.ConditionalSelect((Vector<float>)condition, left, right);
        }

        /// <summary>
        /// Creates a new vector with elements selected between the two given source vectors, and based on a mask vector.
        /// </summary>
        /// <param name="condition">The integral mask vector used to drive selection.</param>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The new vector with elements selected based on the mask.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right)
        {
            return (Vector<double>)Vector<double>.ConditionalSelect((Vector<double>)condition, left, right);
        }

        /// <summary>
        /// Creates a new vector with elements selected between the two given source vectors, and based on a mask vector.
        /// </summary>
        /// <param name="condition">The mask vector used to drive selection.</param>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The new vector with elements selected based on the mask.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.ConditionalSelect(condition, left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left and right were equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Equals(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether elements in the left and right floating point vectors were equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.Equals(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left and right were equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.Equals(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether elements in the left and right floating point vectors were equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.Equals(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left and right were equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.Equals(left, right);
        }

        /// <summary>
        /// Returns a boolean indicating whether each pair of elements in the given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The first vector to compare.</param>
        /// <returns>True if all elements are equal; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left == right;
        }

        /// <summary>
        /// Returns a boolean indicating whether any single pair of elements in the given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any element pairs are equal; False if no element pairs are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return !Vector<T>.Equals(left, right).Equals(Vector<T>.Zero);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.LessThan(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were less than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.LessThan(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.LessThan(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were less than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.LessThan(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.LessThan(left, right);
        }

        /// <summary>
        /// Returns a boolean indicating whether all of the elements in left are less than their corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if all elements in left are less than their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThan(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>
        /// Returns a boolean indicating whether any element in left is less than its corresponding element in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any elements in left are less than their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThan(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.LessThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were less than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.LessThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.LessThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were less than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.LessThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were less than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.LessThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a boolean indicating whether all elements in left are less than or equal to their corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if all elements in left are less than or equal to their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThanOrEqual(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>
        /// Returns a boolean indicating whether any element in left is less than or equal to its corresponding element in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any elements in left are less than their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThanOrEqual(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.GreaterThan(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were greater than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.GreaterThan(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.GreaterThan(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were greater than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.GreaterThan(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.GreaterThan(left, right);
        }

        /// <summary>
        /// Returns a boolean indicating whether all elements in left are greater than the corresponding elements in right.
        /// elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if all elements in left are greater than their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThan(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>
        /// Returns a boolean indicating whether any element in left is greater than its corresponding element in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any elements in left are greater than their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThan(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.GreaterThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were greater than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.GreaterThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.GreaterThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements signal whether the elements in left were greater than or equal to their
        /// corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.GreaterThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns an integral vector whose elements signal whether the elements in left were greater than or equal to
        /// their corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resultant integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.GreaterThanOrEqual(left, right);
        }

        /// <summary>
        /// Returns a boolean indicating whether all of the elements in left are greater than or equal to
        /// their corresponding elements in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if all elements in left are greater than or equal to their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThanOrEqual(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>
        /// Returns a boolean indicating whether any element in left is greater than or equal to its corresponding element in right.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any elements in left are greater than or equal to their corresponding elements in right; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThanOrEqual(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        // Every operation must either be a JIT intrinsic or implemented over a JIT intrinsic
        // as a thin wrapper
        // Operations implemented over a JIT intrinsic should be inlined
        // Methods that do not have a <T> type parameter are recognized as intrinsics
        /// <summary>
        /// Returns whether or not vector operations are subject to hardware acceleration through JIT intrinsic support.
        /// </summary>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => false;
        }

        // Vector<T>
        // Basic Math
        // All Math operations for Vector<T> are aggressively inlined here

        /// <summary>
        /// Returns a new vector whose elements are the absolute values of the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Abs<T>(Vector<T> value) where T : struct
        {
            return Vector<T>.Abs(value);
        }

        /// <summary>
        /// Returns a new vector whose elements are the minimum of each pair of elements in the two given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The minimum vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Min<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Min(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements are the maximum of each pair of elements in the two given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The maximum vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Max<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Max(left, right);
        }

        // Specialized vector operations

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Dot(left, right);
        }

        /// <summary>
        /// Returns a new vector whose elements are the square roots of the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> SquareRoot<T>(Vector<T> value) where T : struct
        {
            return Vector<T>.SquareRoot(value);
        }

        /// <summary>
        /// Returns a new vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>
        /// The vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// If a value is equal to <see cref="float.NaN"/>, <see cref="float.NegativeInfinity"/> or <see cref="float.PositiveInfinity"/>, that value is returned.
        /// Note that this method returns a <see cref="float"/> instead of an integral type.
        /// </returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Ceiling(Vector<float> value)
        {
            return Vector<float>.Ceiling(value);
        }

        /// <summary>
        /// Returns a new vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>
        /// The vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// If a value is equal to <see cref="double.NaN"/>, <see cref="double.NegativeInfinity"/> or <see cref="double.PositiveInfinity"/>, that value is returned.
        /// Note that this method returns a <see cref="double"/> instead of an integral type.
        /// </returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Ceiling(Vector<double> value)
        {
            return Vector<double>.Ceiling(value);
        }

        /// <summary>
        /// Returns a new vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>
        /// The vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// If a value is equal to <see cref="float.NaN"/>, <see cref="float.NegativeInfinity"/> or <see cref="float.PositiveInfinity"/>, that value is returned.
        /// Note that this method returns a <see cref="float"/> instead of an integral type.
        /// </returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Floor(Vector<float> value)
        {
            return Vector<float>.Floor(value);
        }

        /// <summary>
        /// Returns a new vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>
        /// The vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// If a value is equal to <see cref="double.NaN"/>, <see cref="double.NegativeInfinity"/> or <see cref="double.PositiveInfinity"/>, that value is returned.
        /// Note that this method returns a <see cref="double"/> instead of an integral type.
        /// </returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Floor(Vector<double> value)
        {
            return Vector<double>.Floor(value);
        }

        /// <summary>
        /// Creates a new vector whose values are the sum of each pair of elements from the two given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left + right;
        }

        /// <summary>
        /// Creates a new vector whose values are the difference between each pairs of elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left - right;
        }

        /// <summary>
        /// Creates a new vector whose values are the product of each pair of elements from the two given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left * right;
        }

        /// <summary>
        /// Returns a new vector whose values are the values of the given vector each multiplied by a scalar value.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar factor.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, T right) where T : struct
        {
            return left * right;
        }

        /// <summary>
        /// Returns a new vector whose values are the values of the given vector each multiplied by a scalar value.
        /// </summary>
        /// <param name="left">The scalar factor.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(T left, Vector<T> right) where T : struct
        {
            return left * right;
        }

        /// <summary>
        /// Returns a new vector whose values are the result of dividing the first vector's elements
        /// by the corresponding elements in the second vector.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The divided vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left / right;
        }

        /// <summary>
        /// Returns a new vector whose elements are the given vector's elements negated.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Negate<T>(Vector<T> value) where T : struct
        {
            return -value;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-and operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left & right;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-or operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left | right;
        }

        /// <summary>
        /// Returns a new vector whose elements are obtained by taking the one's complement of the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The one's complement vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> OnesComplement<T>(Vector<T> value) where T : struct
        {
            return ~value;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-exclusive-or operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left ^ right;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-and-not operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left & ~right;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of unsigned bytes.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> AsVectorByte<T>(Vector<T> value) where T : struct
        {
            return (Vector<byte>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of signed bytes.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) where T : struct
        {
            return (Vector<sbyte>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of 16-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) where T : struct
        {
            return (Vector<ushort>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of signed 16-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> AsVectorInt16<T>(Vector<T> value) where T : struct
        {
            return (Vector<short>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of unsigned 32-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) where T : struct
        {
            return (Vector<uint>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of signed 32-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> AsVectorInt32<T>(Vector<T> value) where T : struct
        {
            return (Vector<int>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of unsigned 64-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) where T : struct
        {
            return (Vector<ulong>)value;
        }


        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of signed 64-bit integers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> AsVectorInt64<T>(Vector<T> value) where T : struct
        {
            return (Vector<long>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of 32-bit floating point numbers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> AsVectorSingle<T>(Vector<T> value) where T : struct
        {
            return (Vector<float>)value;
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of a vector of 64-bit floating point numbers.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> AsVectorDouble<T>(Vector<T> value) where T : struct
        {
            return (Vector<double>)value;
        }

        /// <summary>
        /// Widens a Vector{Byte} into two Vector{UInt16}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
        {
            int elements = Vector<byte>.Count;
            ushort* lowPtr = stackalloc ushort[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (ushort)source[i];
            }
            ushort* highPtr = stackalloc ushort[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (ushort)source[i + (elements / 2)];
            }

            low = *(Vector<ushort>*)lowPtr;
            high = *(Vector<ushort>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{UInt16} into two Vector{UInt32}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
        {
            int elements = Vector<ushort>.Count;
            uint* lowPtr = stackalloc uint[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (uint)source[i];
            }
            uint* highPtr = stackalloc uint[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (uint)source[i + (elements / 2)];
            }

            low = *(Vector<uint>*)lowPtr;
            high = *(Vector<uint>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{UInt32} into two Vector{UInt64}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
        {
            int elements = Vector<uint>.Count;
            ulong* lowPtr = stackalloc ulong[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (ulong)source[i];
            }
            ulong* highPtr = stackalloc ulong[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (ulong)source[i + (elements / 2)];
            }

            low = *(Vector<ulong>*)lowPtr;
            high = *(Vector<ulong>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{SByte} into two Vector{Int16}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
        {
            int elements = Vector<sbyte>.Count;
            short* lowPtr = stackalloc short[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (short)source[i];
            }
            short* highPtr = stackalloc short[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (short)source[i + (elements / 2)];
            }

            low = *(Vector<short>*)lowPtr;
            high = *(Vector<short>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{Int16} into two Vector{Int32}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [Intrinsic]
        public static unsafe void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
        {
            int elements = Vector<short>.Count;
            int* lowPtr = stackalloc int[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (int)source[i];
            }
            int* highPtr = stackalloc int[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (int)source[i + (elements / 2)];
            }

            low = *(Vector<int>*)lowPtr;
            high = *(Vector<int>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{Int32} into two Vector{Int64}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [Intrinsic]
        public static unsafe void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
        {
            int elements = Vector<int>.Count;
            long* lowPtr = stackalloc long[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (long)source[i];
            }
            long* highPtr = stackalloc long[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (long)source[i + (elements / 2)];
            }

            low = *(Vector<long>*)lowPtr;
            high = *(Vector<long>*)highPtr;
        }

        /// <summary>
        /// Widens a Vector{Single} into two Vector{Double}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [Intrinsic]
        public static unsafe void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
        {
            int elements = Vector<float>.Count;
            double* lowPtr = stackalloc double[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (double)source[i];
            }
            double* highPtr = stackalloc double[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (double)source[i + (elements / 2)];
            }

            low = *(Vector<double>*)lowPtr;
            high = *(Vector<double>*)highPtr;
        }

        /// <summary>
        /// Narrows two Vector{UInt16}'s into one Vector{Byte}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Byte} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
        {
            int elements = Vector<byte>.Count;
            byte* retPtr = stackalloc byte[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (byte)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (byte)high[i];
            }

            return *(Vector<byte>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{UInt32}'s into one Vector{UInt16}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{UInt16} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
        {
            int elements = Vector<ushort>.Count;
            ushort* retPtr = stackalloc ushort[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (ushort)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (ushort)high[i];
            }

            return *(Vector<ushort>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{UInt64}'s into one Vector{UInt32}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{UInt32} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
        {
            int elements = Vector<uint>.Count;
            uint* retPtr = stackalloc uint[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (uint)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (uint)high[i];
            }

            return *(Vector<uint>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{Int16}'s into one Vector{SByte}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{SByte} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
        {
            int elements = Vector<sbyte>.Count;
            sbyte* retPtr = stackalloc sbyte[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (sbyte)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (sbyte)high[i];
            }

            return *(Vector<sbyte>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{Int32}'s into one Vector{Int16}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Int16} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector<short> Narrow(Vector<int> low, Vector<int> high)
        {
            int elements = Vector<short>.Count;
            short* retPtr = stackalloc short[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (short)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (short)high[i];
            }

            return *(Vector<short>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{Int64}'s into one Vector{Int32}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Int32} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector<int> Narrow(Vector<long> low, Vector<long> high)
        {
            int elements = Vector<int>.Count;
            int* retPtr = stackalloc int[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (int)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (int)high[i];
            }

            return *(Vector<int>*)retPtr;
        }

        /// <summary>
        /// Narrows two Vector{Double}'s into one Vector{Single}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Single} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector<float> Narrow(Vector<double> low, Vector<double> high)
        {
            int elements = Vector<float>.Count;
            float* retPtr = stackalloc float[elements];
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i] = (float)low[i];
            }
            for (int i = 0; i < elements / 2; i++)
            {
                retPtr[i + (elements / 2)] = (float)high[i];
            }

            return *(Vector<float>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Int32} to a Vector{Single}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<float> ConvertToSingle(Vector<int> value)
        {
            int elements = Vector<float>.Count;
            float* retPtr = stackalloc float[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (float)value[i];
            }

            return *(Vector<float>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{UInt32} to a Vector{Single}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<float> ConvertToSingle(Vector<uint> value)
        {
            int elements = Vector<float>.Count;
            float* retPtr = stackalloc float[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (float)value[i];
            }

            return *(Vector<float>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Int64} to a Vector{Double}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<double> ConvertToDouble(Vector<long> value)
        {
            int elements = Vector<double>.Count;
            double* retPtr = stackalloc double[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (double)value[i];
            }

            return *(Vector<double>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{UInt64} to a Vector{Double}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value)
        {
            int elements = Vector<double>.Count;
            double* retPtr = stackalloc double[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (double)value[i];
            }

            return *(Vector<double>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Single} to a Vector{Int32}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value)
        {
            int elements = Vector<int>.Count;
            int* retPtr = stackalloc int[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (int)value[i];
            }

            return *(Vector<int>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Single} to a Vector{UInt32}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value)
        {
            int elements = Vector<uint>.Count;
            uint* retPtr = stackalloc uint[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (uint)value[i];
            }

            return *(Vector<uint>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Double} to a Vector{Int64}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value)
        {
            int elements = Vector<long>.Count;
            long* retPtr = stackalloc long[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (long)value[i];
            }

            return *(Vector<long>*)retPtr;
        }

        /// <summary>
        /// Converts a Vector{Double} to a Vector{UInt64}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value)
        {
            int elements = Vector<ulong>.Count;
            ulong* retPtr = stackalloc ulong[elements];
            for (int i = 0; i < elements; i++)
            {
                retPtr[i] = (ulong)value[i];
            }

            return *(Vector<ulong>*)retPtr;
        }

        [DoesNotReturn]
        internal static void ThrowInsufficientNumberOfElementsException(int requiredElementCount)
        {
            throw new IndexOutOfRangeException(SR.Format(SR.Arg_InsufficientNumberOfElements, requiredElementCount, "values"));
        }
    }
}

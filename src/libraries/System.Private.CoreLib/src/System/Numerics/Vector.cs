// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>Provides a collection of static convenience methods for creating, manipulating, combining, and converting generic vectors.</summary>
    [Intrinsic]
    public static partial class Vector
    {
        /// <summary>Creates a new single-precision vector with elements selected between two specified single-precision source vectors based on an integral mask vector.</summary>
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

        /// <summary>Creates a new double-precision vector with elements selected between two specified double-precision source vectors based on an integral mask vector.</summary>
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

        /// <summary>Creates a new vector of a specified type with elements selected between two specified source vectors of the same type based on an integral mask vector.</summary>
        /// <param name="condition">The integral mask vector used to drive selection.</param>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The new vector with elements selected based on the mask.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.ConditionalSelect(condition, left, right);
        }

        /// <summary>Returns a new vector of a specified type whose elements signal whether the elements in two specified vectors of the same type are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Equals(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in two specified single-precision vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.Equals(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in two specified integral vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.Equals(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in two specified double-precision vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.Equals(left, right);
        }

        /// <summary>Returns a new vector whose elements signal whether the elements in two specified long integer vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting long integer vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.Equals(left, right);
        }

        /// <summary>Returns a value that indicates whether each pair of elements in the given vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if all elements in <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left == right;
        }

        /// <summary>Returns a value that indicates whether any single pair of elements in the given vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if any element pair in <paramref name="left" /> and <paramref name="right" /> is equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return !Vector<T>.Equals(left, right).Equals(Vector<T>.Zero);
        }

        /// <summary>Returns a new vector of a specified type whose elements signal whether the elements in one vector are less than their corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.LessThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one single-precision vector are less than their corresponding elements in a second single-precision vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.LessThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one integral vector are less than their corresponding elements in a second integral vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.LessThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one double-precision floating-point vector are less than their corresponding elements in a second double-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.LessThan(left, right);
        }

        /// <summary>Returns a new long integer vector whose elements signal whether the elements in one long integer vector are less than their corresponding elements in a second long integer vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting long integer vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.LessThan(left, right);
        }

        /// <summary>Returns a value that indicates whether all of the elements in the first vector are less than their corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if all of the elements in <paramref name="left" /> are less than the corresponding elements in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThan(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>Returns a value that indicates whether any element in the first vector is less than the corresponding element in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if any element in <paramref name="left" /> is less than the corresponding element in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThan(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>Returns a new vector whose elements signal whether the elements in one vector are less than or equal to their corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.LessThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one single-precision floating-point vector are less than or equal to their corresponding elements in a second single-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.LessThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one integral vector are less than or equal to their corresponding elements in a second integral vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.LessThanOrEqual(left, right);
        }

        /// <summary>Returns a new long integer vector whose elements signal whether the elements in one long integer vector are less or equal to their corresponding elements in a second long integer vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting long integer vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.LessThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one double-precision floating-point vector are less than or equal to their corresponding elements in a second double-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.LessThanOrEqual(left, right);
        }

        /// <summary>Returns a value that indicates whether all elements in the first vector are less than or equal to their corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if all of the elements in <paramref name="left" /> are less than or equal to the corresponding elements in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThanOrEqual(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>Returns a value that indicates whether any element in the first vector is less than or equal to the corresponding element in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if any element in <paramref name="left" /> is less than or equal to the corresponding element in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.LessThanOrEqual(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>Returns a new vector whose elements signal whether the elements in one vector of a specified type are greater than their corresponding elements in the second vector of the same time.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.GreaterThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one single-precision floating-point vector are greater than their corresponding elements in a second single-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.GreaterThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one integral vector are greater than their corresponding elements in a second integral vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.GreaterThan(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one double-precision floating-point vector are greater than their corresponding elements in a second double-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.GreaterThan(left, right);
        }

        /// <summary>Returns a new long integer vector whose elements signal whether the elements in one long integer vector are greater than their corresponding elements in a second long integer vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting long integer vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.GreaterThan(left, right);
        }

        /// <summary>Returns a value that indicates whether all elements in the first vector are greater than the corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if all elements in <paramref name="left" /> are greater than the corresponding elements in <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThan(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>Returns a value that indicates whether any element in the first vector is greater than the corresponding element in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if any element in <paramref name="left" /> is greater than the corresponding element in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThan(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>Returns a new vector whose elements signal whether the elements in one vector of a specified type are greater than or equal to their corresponding elements in the second vector of the same type.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.GreaterThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one vector are greater than or equal to their corresponding elements in the single-precision floating-point second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
        {
            return (Vector<int>)Vector<float>.GreaterThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one integral vector are greater than or equal to their corresponding elements in the second integral vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
        {
            return Vector<int>.GreaterThanOrEqual(left, right);
        }

        /// <summary>Returns a new long integer vector whose elements signal whether the elements in one long integer vector are greater than or equal to their corresponding elements in the second long integer vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting long integer vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
        {
            return Vector<long>.GreaterThanOrEqual(left, right);
        }

        /// <summary>Returns a new integral vector whose elements signal whether the elements in one vector are greater than or equal to their corresponding elements in the second double-precision floating-point vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>The resulting integral vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
        {
            return (Vector<long>)Vector<double>.GreaterThanOrEqual(left, right);
        }

        /// <summary>Returns a value that indicates whether all elements in the first vector are greater than or equal to all the corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if all elements in <paramref name="left" /> are greater than or equal to the corresponding elements in <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThanOrEqual(left, right);
            return cond.Equals(Vector<int>.AllBitsSet);
        }

        /// <summary>Returns a value that indicates whether any element in the first vector is greater than or equal to the corresponding element in the second vector.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns><see langword="true" /> if any element in <paramref name="left" /> is greater than or equal to the corresponding element in <paramref name="right" />; otherwise,  <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            Vector<int> cond = (Vector<int>)Vector<T>.GreaterThanOrEqual(left, right);
            return !cond.Equals(Vector<int>.Zero);
        }

        /// <summary>Gets a value that indicates whether vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>Vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions and the RyiJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => false;
        }

        /// <summary>Returns a new vector whose elements are the absolute values of the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Abs<T>(Vector<T> value) where T : struct
        {
            return Vector<T>.Abs(value);
        }

        /// <summary>Returns a new vector whose elements are the minimum of each pair of elements in the two given vectors.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The minimum vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Min<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Min(left, right);
        }

        /// <summary>Returns a new vector whose elements are the maximum of each pair of elements in the two given vectors.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The maximum vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Max<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Max(left, right);
        }

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return Vector<T>.Dot(left, right);
        }

        /// <summary>Returns a new vector whose elements are the square roots of a specified vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> SquareRoot<T>(Vector<T> value) where T : struct
        {
            return Vector<T>.SquareRoot(value);
        }

        /// <summary>Returns a new vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// If a value is equal to <see cref="float.NaN" />, <see cref="float.NegativeInfinity" />, or <see cref="float.PositiveInfinity" />, that value is returned.</returns>
        /// <remarks>Note that this method returns a <see cref="float" /> instead of an integral type.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Ceiling(Vector<float> value)
        {
            return Vector<float>.Ceiling(value);
        }

        /// <summary>Returns a new vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The vector whose elements are the smallest integral values that are greater than or equal to the given vector's elements.
        /// If a value is equal to <see cref="double.NaN" />, <see cref="double.NegativeInfinity" />, or <see cref="double.PositiveInfinity" />, that value is returned.</returns>
        /// <remarks>Note that this method returns a <see cref="double" /> instead of an integral type.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Ceiling(Vector<double> value)
        {
            return Vector<double>.Ceiling(value);
        }

        /// <summary>Returns a new vector whose elements are the largest integral values that are less than or equal to the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// If a value is equal to <see cref="float.NaN" />, <see cref="float.NegativeInfinity" />, or <see cref="float.PositiveInfinity" />, that value is returned.</returns>
        /// <remarks>Note that this method returns a <see cref="float" /> instead of an integral type.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Floor(Vector<float> value)
        {
            return Vector<float>.Floor(value);
        }

        /// <summary>Returns a new vector whose elements are the largest integral values that are less than or equal to the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The vector whose elements are the largest integral values that are less than or equal to the given vector's elements.
        /// If a value is equal to <see cref="double.NaN" />, <see cref="double.NegativeInfinity" />, or <see cref="double.PositiveInfinity" />, that value is returned.</returns>
        /// <remarks>Note that this method returns a <see cref="double" /> instead of an integral type.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Floor(Vector<double> value)
        {
            return Vector<double>.Floor(value);
        }

        /// <summary>Returns a new vector whose values are the sum of each pair of elements from two given vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left + right;
        }

        /// <summary>Returns a new vector whose values are the difference between the elements in the second vector and their corresponding elements in the first vector.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left - right;
        }

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The element-wise product vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left * right;
        }

        /// <summary>Returns a new vector whose values are the values of a specified vector each multiplied by a scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, T right) where T : struct
        {
            return left * right;
        }

        /// <summary>Returns a new vector whose values are a scalar value multiplied by each of the values of a specified vector.</summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(T left, Vector<T> right) where T : struct
        {
            return left * right;
        }

        /// <summary>Returns a new vector whose values are the result of dividing the first vector's elements by the corresponding elements in the second vector.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The divided vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left / right;
        }

        /// <summary>Returns a new vector whose elements are the negation of the corresponding element in the specified vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Negate<T>(Vector<T> value) where T : struct
        {
            return -value;
        }

        /// <summary>Returns a new vector by performing a bitwise <see langword="And" /> operation on each pair of elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left & right;
        }

        /// <summary>Returns a new vector by performing a bitwise <see langword="Or" /> operation on each pair of elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left | right;
        }

        /// <summary>Returns a new vector whose elements are obtained by taking the one's complement of a specified vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> OnesComplement<T>(Vector<T> value) where T : struct
        {
            return ~value;
        }

        /// <summary>Returns a new vector by performing a bitwise exclusive Or (<see langword="XOr" />) operation on each pair of elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left ^ right;
        }

        /// <summary>Returns a new vector by performing a bitwise And Not operation on each pair of corresponding elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The resulting vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right) where T : struct
        {
            return left & ~right;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of unsigned bytes.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> AsVectorByte<T>(Vector<T> value) where T : struct
        {
            return (Vector<byte>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of signed bytes.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) where T : struct
        {
            return (Vector<sbyte>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of unsigned 16-bit integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) where T : struct
        {
            return (Vector<ushort>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of 16-bit integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> AsVectorInt16<T>(Vector<T> value) where T : struct
        {
            return (Vector<short>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of unsigned integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) where T : struct
        {
            return (Vector<uint>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> AsVectorInt32<T>(Vector<T> value) where T : struct
        {
            return (Vector<int>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of unsigned long integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) where T : struct
        {
            return (Vector<ulong>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of long integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> AsVectorInt64<T>(Vector<T> value) where T : struct
        {
            return (Vector<long>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a single-precision floating-point vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> AsVectorSingle<T>(Vector<T> value) where T : struct
        {
            return (Vector<float>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a double-precision floating-point vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> AsVectorDouble<T>(Vector<T> value) where T : struct
        {
            return (Vector<double>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of unsigned native integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value) where T : struct
        {
            return (Vector<nuint>)value;
        }

        /// <summary>Reinterprets the bits of a specified vector into those of a vector of native integers.</summary>
        /// <param name="value">The source vector.</param>
        /// <typeparam name="T">The vector type. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
        /// <returns>The reinterpreted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<nint> AsVectorNInt<T>(Vector<T> value) where T : struct
        {
            return (Vector<nint>)value;
        }

        /// <summary>Widens a <c>Vector&lt;Byte&gt;</c> into two <c>Vector&lt;UInt16&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;UInt16&gt;</c> into two <c>Vector&lt;UInt32&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;UInt32&gt;</c> into two <c>Vector&lt;UInt64&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;SByte&gt;</c> into two <c>Vector&lt;Int16&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;Int16&gt;</c> into two <c>Vector&lt;Int32&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;Int32&gt;</c> into two <c>Vector&lt;Int64&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Widens a <c>Vector&lt;Single&gt;</c> into two <c>Vector&lt;Double&gt;</c> instances.</summary>
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
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

        /// <summary>Narrows two <c>Vector&lt;UInt16&gt;</c> instances into one <c>Vector&lt;Byte&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;Byte&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;UInt32&gt;</c> instances into one <c>Vector&lt;UInt16&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;UInt16&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;UInt64&gt;</c> instances into one <c>Vector&lt;UInt32&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;UInt32&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;Int16&gt;</c> instances into one <c>Vector&lt;SByte&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;SByte&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;Int32&gt;</c> instances into one <c>Vector&lt;Int16&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;Int16&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;Int64&gt;</c> instances into one <c>Vector&lt;Int32&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;Int32&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Narrows two <c>Vector&lt;Double&gt;</c> instances into one <c>Vector&lt;Single&gt;</c>.</summary>
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A <c>Vector&lt;Single&gt;</c> containing elements narrowed from the source vectors.</returns>
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

        /// <summary>Converts a <c>Vector&lt;Int32&gt;</c> to a <c>Vector&lt;Single&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;UInt32&gt;</c> to a <c>Vector&lt;Single&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;Int64&gt;</c> to a <c>Vector&lt;Double&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;UInt64&gt;</c> to a <c>Vector&lt;Double&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;Single&gt;</c> to a <c>Vector&lt;Int32&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;Single&gt;</c> to a <c>Vector&lt;UInt32&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;Double&gt;</c> to a <c>Vector&lt;Int64&gt;</c>.</summary>
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

        /// <summary>Converts a <c>Vector&lt;Double&gt;</c> to a <c>Vector&lt;UInt64&gt;</c>.</summary>
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

        /// <summary>
        /// Reinterprets a <see cref="Vector{T}"/> as a <see cref="Vector{T}"/> of new type.
        /// </summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type to reinterpret the vector as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector"/> reinterpreted as a new <see cref="Vector{T}"/>.</returns>
        /// <exception cref="NotSupportedException">
        /// The type of <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> is not supported.
        /// </exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<TTo> As<TFrom, TTo>(this Vector<TFrom> vector)
            where TFrom : struct
            where TTo : struct
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TTo>();

            return Unsafe.As<Vector<TFrom>, Vector<TTo>>(ref vector);
        }
    }
}

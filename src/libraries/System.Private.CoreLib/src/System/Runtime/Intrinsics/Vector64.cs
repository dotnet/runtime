// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace System.Runtime.Intrinsics
{
    /// <summary>Provides a collection of static methods for creating, manipulating, and otherwise operating on 64-bit vectors.</summary>
    public static class Vector64
    {
        internal const int Size = 8;

        internal const int Alignment = 8;

        /// <summary>Gets a value that indicates whether 64-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 64-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>64-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 64-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => IsHardwareAccelerated;
        }

        /// <summary>Computes the absolute value of each element in a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector that will have its absolute value computed.</param>
        /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Abs<T>(Vector64<T> vector)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return vector;
            }
            else
            {
                Unsafe.SkipInit(out Vector64<T> result);

                for (int index = 0; index < Vector64<T>.Count; index++)
                {
                    T value = Scalar<T>.Abs(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Add<T>(Vector64<T> left, Vector64<T> right) => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> AndNot<T>(Vector64<T> left, Vector64<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = left._00 & ~right._00;

            return result;
        }

        /// <summary>Reinterprets a <see cref="Vector64{TFrom}" /> as a new <see cref="Vector64{TTo}" />.</summary>
        /// <typeparam name="TFrom">The type of the elements in the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the elements in the output vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{TTo}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<TTo> As<TFrom, TTo>(this Vector64<TFrom> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TTo>();

            return Unsafe.As<Vector64<TFrom>, Vector64<TTo>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Byte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<byte> AsByte<T>(this Vector64<T> vector) => vector.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Double}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> AsDouble<T>(this Vector64<T> vector) => vector.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<short> AsInt16<T>(this Vector64<T> vector) => vector.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<int> AsInt32<T>(this Vector64<T> vector) => vector.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<long> AsInt64<T>(this Vector64<T> vector) => vector.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nint> AsNInt<T>(this Vector64<T> vector) => vector.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nuint> AsNUInt<T>(this Vector64<T> vector) => vector.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{SByte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<sbyte> AsSByte<T>(this Vector64<T> vector) => vector.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Single}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> AsSingle<T>(this Vector64<T> vector) => vector.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ushort> AsUInt16<T>(this Vector64<T> vector) => vector.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector) => vector.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector) => vector.As<T, ulong>();

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> BitwiseAnd<T>(Vector64<T> left, Vector64<T> right) => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> BitwiseOr<T>(Vector64<T> left, Vector64<T> right) => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector64<T> Ceiling<T>(Vector64<T> vector)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(short))
             || (typeof(T) == typeof(int))
             || (typeof(T) == typeof(long))
             || (typeof(T) == typeof(nint))
             || (typeof(T) == typeof(nuint))
             || (typeof(T) == typeof(sbyte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong)))
            {
                return vector;
            }
            else
            {
                Unsafe.SkipInit(out Vector64<T> result);

                for (int index = 0; index < Vector64<T>.Count; index++)
                {
                    T value = Scalar<T>.Ceiling(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> Ceiling(Vector64<float> vector) => Ceiling<float>(vector);

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Ceiling(Vector64<double> vector) => Ceiling<double>(vector);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="condition" />, <paramref name="left" />, and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> ConditionalSelect<T>(Vector64<T> condition, Vector64<T> left, Vector64<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = (left._00 & condition._00) | (right._00 & ~condition._00);

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Int64}" /> to a <see cref="Vector64{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> ConvertToDouble(Vector64<long> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                double value = vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{UInt64}" /> to a <see cref="Vector64{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> ConvertToDouble(Vector64<ulong> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                double value = vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Single}" /> to a <see cref="Vector64{Int32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> ConvertToInt32(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<int> result);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                int value = (int)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Double}" /> to a <see cref="Vector64{Int64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> ConvertToInt64(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<long> result);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                long value = (long)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Int32}" /> to a <see cref="Vector64{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> ConvertToSingle(Vector64<int> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                float value = vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{UInt32}" /> to a <see cref="Vector64{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> ConvertToSingle(Vector64<uint> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                float value = vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Single}" /> to a <see cref="Vector64{UInt32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> ConvertToUInt32(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<uint> result);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                uint value = (uint)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Double}" /> to a <see cref="Vector64{UInt64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> ConvertToUInt64(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<ulong> result);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                ulong value = (ulong)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Copies a <see cref="Vector64{T}" /> to a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector64<T> vector, T[] destination)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (destination.Length < Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
        }

        /// <summary>Copies a <see cref="Vector64{T}" /> to a given array starting at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CopyTo<T>(this Vector64<T> vector, T[] destination, int startIndex)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
        }

        /// <summary>Copies a <see cref="Vector64{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector64<T> vector, Span<T> destination)
        {
            if (destination.Length < Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> Create<T>(T value)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi8</remarks>
        /// <returns>A new <see cref="Vector64{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> Create(byte value) => Create<byte>(value);

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> Create(double value) => Create<double>(value);

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi16</remarks>
        /// <returns>A new <see cref="Vector64{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> Create(short value) => Create<short>(value);

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi32</remarks>
        /// <returns>A new <see cref="Vector64{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> Create(int value) => Create<int>(value);

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> Create(long value) => Create<long>(value);

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nint> Create(nint value) => Create<nint>(value);

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nuint> Create(nuint value) => Create<nuint>(value);

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi8</remarks>
        /// <returns>A new <see cref="Vector64{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<sbyte> Create(sbyte value) => Create<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> Create(float value) => Create<float>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi16</remarks>
        /// <returns>A new <see cref="Vector64{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ushort> Create(ushort value) => Create<ushort>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi32</remarks>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> Create(uint value) => Create<uint>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> Create(ulong value) => Create<ulong>(value);

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector64{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Create<T>(T[] values)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (values.Length < Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref values[0]));
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Create<T>(T[] values, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((index < 0) || ((values.Length - index) < Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector64{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Create<T>(ReadOnlySpan<T> values)
        {
            if (values.Length < Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi8</remarks>
        /// <returns>A new <see cref="Vector64{Byte}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
        {
            byte* pResult = stackalloc byte[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };
            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi16</remarks>
        /// <returns>A new <see cref="Vector64{Int16}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> Create(short e0, short e1, short e2, short e3)
        {
            short* pResult = stackalloc short[4]
            {
                e0,
                e1,
                e2,
                e3,
            };
            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi32</remarks>
        /// <returns>A new <see cref="Vector64{Int32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> Create(int e0, int e1)
        {
            int* pResult = stackalloc int[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi8</remarks>
        /// <returns>A new <see cref="Vector64{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
        {
            sbyte* pResult = stackalloc sbyte[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };
            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> Create(float e0, float e1)
        {
            float* pResult = stackalloc float[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi16</remarks>
        /// <returns>A new <see cref="Vector64{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
        {
            ushort* pResult = stackalloc ushort[4]
            {
                e0,
                e1,
                e2,
                e3,
            };
            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi32</remarks>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> Create(uint e0, uint e1)
        {
            uint* pResult = stackalloc uint[2]
            {
                e0,
                e1,
            };
            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> CreateScalar<T>(T value)
        {
            Vector64<T> result = Vector64<T>.Zero;
            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> CreateScalar(byte value) => CreateScalar<byte>(value);

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> CreateScalar(double value) => CreateScalar<double>(value);

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> CreateScalar(short value) => CreateScalar<short>(value);

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> CreateScalar(int value) => CreateScalar<int>(value);

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> CreateScalar(long value) => CreateScalar<long>(value);

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nint> CreateScalar(nint value) => CreateScalar<nint>(value);

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nuint> CreateScalar(nuint value) => CreateScalar<nuint>(value);

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<sbyte> CreateScalar(sbyte value) => CreateScalar<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> CreateScalar(float value) => CreateScalar<float>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ushort> CreateScalar(ushort value) => CreateScalar<ushort>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> CreateScalar(uint value) => CreateScalar<uint>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> CreateScalar(ulong value) => CreateScalar<ulong>(value);

        /// <summary>Creates a new <see cref="Vector64{T}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> CreateScalarUnsafe<T>(T value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            Unsafe.SkipInit(out Vector64<T> result);

            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> CreateScalarUnsafe(byte value) => CreateScalarUnsafe<byte>(value);

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> CreateScalarUnsafe(double value) => CreateScalarUnsafe<double>(value);

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> CreateScalarUnsafe(short value) => CreateScalarUnsafe<short>(value);

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> CreateScalarUnsafe(int value) => CreateScalarUnsafe<int>(value);

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> CreateScalarUnsafe(long value) => CreateScalarUnsafe<long>(value);

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nint> CreateScalarUnsafe(nint value) => CreateScalarUnsafe<nint>(value);

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nuint> CreateScalarUnsafe(nuint value) => CreateScalarUnsafe<nuint>(value);

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<sbyte> CreateScalarUnsafe(sbyte value) => CreateScalarUnsafe<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> CreateScalarUnsafe(float value) => CreateScalarUnsafe<float>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ushort> CreateScalarUnsafe(ushort value) => CreateScalarUnsafe<ushort>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> CreateScalarUnsafe(uint value) => CreateScalarUnsafe<uint>(value);

        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> CreateScalarUnsafe(ulong value) => CreateScalarUnsafe<ulong>(value);

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Divide<T>(Vector64<T> left, Vector64<T> right) => left / right;

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Divide<T>(Vector64<T> left, T right) => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector64<T> left, Vector64<T> right)
        {
            T result = default!;

            // Doing this as pairs is important for floating-point determinism
            // This is because the underlying dpps instruction on x86/x64 will do this equivalently
            // and otherwise the software vs accelerated implementations may differ in returned result.

            if (Vector64<T>.Count != 1)
            {
                for (int index = 0; index < Vector64<T>.Count; index += 2)
                {
                    T value = Scalar<T>.Add(
                        Scalar<T>.Multiply(left.GetElementUnsafe(index + 0), right.GetElementUnsafe(index + 0)),
                        Scalar<T>.Multiply(left.GetElementUnsafe(index + 1), right.GetElementUnsafe(index + 1))
                    );

                    result = Scalar<T>.Add(result, value);
                }
            }
            else
            {
                result = Scalar<T>.Multiply(left.GetElementUnsafe(0), right.GetElementUnsafe(0));
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Equals<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector64<T> left, Vector64<T> right) => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Vector64<T> Exp<T>(Vector64<T> vector)
            where T : IExponentialFunctions<T>
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = T.Exp(vector.GetElement(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the exp of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its Exp computed.</param>
        /// <returns>A vector whose elements are the exp of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Exp(Vector64<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpDouble<Vector64<double>, Vector64<long>, Vector64<ulong>>(vector);
            }
            else
            {
                return Exp<double>(vector);
            }
        }

        /// <summary>Computes the exp of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its exp computed.</param>
        /// <returns>A vector whose elements are the exp of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> Exp(Vector64<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpSingle<Vector64<float>, Vector64<uint>, Vector64<double>, Vector64<ulong>>(vector);
            }
            else
            {
                return Exp<float>(vector);
            }
        }

        /// <summary>Extracts the most significant bit from each element in a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose elements should have their most significant bit extracted.</param>
        /// <returns>The packed most significant bits extracted from the elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ExtractMostSignificantBits<T>(this Vector64<T> vector)
        {
            uint result = 0;

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                uint value = Scalar<T>.ExtractMostSignificantBit(vector.GetElementUnsafe(index));
                result |= (value << index);
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector64<T> Floor<T>(Vector64<T> vector)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(short))
             || (typeof(T) == typeof(int))
             || (typeof(T) == typeof(long))
             || (typeof(T) == typeof(nint))
             || (typeof(T) == typeof(nuint))
             || (typeof(T) == typeof(sbyte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong)))
            {
                return vector;
            }
            else
            {
                Unsafe.SkipInit(out Vector64<T> result);

                for (int index = 0; index < Vector64<T>.Count; index++)
                {
                    T value = Scalar<T>.Floor(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> Floor(Vector64<float> vector) => Floor<float>(vector);

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Floor(Vector64<double> vector) => Floor<double>(vector);

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetElement<T>(this Vector64<T> vector, int index)
        {
            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> GreaterThan<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (!Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> GreaterThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (!Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> LessThan<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (!Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> LessThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (!Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                if (Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> Load<T>(T* source) => LoadUnsafe(ref *source);

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> LoadAligned<T>(T* source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if (((nuint)(source) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            return *(Vector64<T>*)source;
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> LoadAlignedNonTemporal<T>(T* source) => LoadAligned(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> LoadUnsafe<T>(ref readonly T source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in source));
            return Unsafe.ReadUnaligned<Vector64<T>>(in address);
        }

        /// <summary>Loads a vector from the given source and element offset.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source to which <paramref name="elementOffset" /> will be added before loading the vector.</param>
        /// <param name="elementOffset">The element offset from <paramref name="source" /> from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" /> plus <paramref name="elementOffset" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> LoadUnsafe<T>(ref readonly T source, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
            return Unsafe.ReadUnaligned<Vector64<T>>(in address);
        }

        internal static Vector64<T> Log<T>(Vector64<T> vector)
            where T : ILogarithmicFunctions<T>
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = T.Log(vector.GetElement(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the log of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its log computed.</param>
        /// <returns>A vector whose elements are the log of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Log(Vector64<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogDouble<Vector64<double>, Vector64<long>, Vector64<ulong>>(vector);
            }
            else
            {
                return Log<double>(vector);
            }
        }

        /// <summary>Computes the log of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its log computed.</param>
        /// <returns>A vector whose elements are the log of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> Log(Vector64<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogSingle<Vector64<float>, Vector64<int>, Vector64<uint>>(vector);
            }
            else
            {
                return Log<float>(vector);
            }
        }

        internal static Vector64<T> Log2<T>(Vector64<T> vector)
            where T : ILogarithmicFunctions<T>
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = T.Log2(vector.GetElement(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the log2 of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its log2 computed.</param>
        /// <returns>A vector whose elements are the log2 of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Log2(Vector64<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Double<Vector64<double>, Vector64<long>, Vector64<ulong>>(vector);
            }
            else
            {
                return Log2<double>(vector);
            }
        }

        /// <summary>Computes the log2 of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its log2 computed.</param>
        /// <returns>A vector whose elements are the log2 of the elements in <paramref name="vector" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<float> Log2(Vector64<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Single<Vector64<float>, Vector64<int>, Vector64<uint>>(vector);
            }
            else
            {
                return Log2<float>(vector);
            }
        }

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right"/> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Max<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the minimum of two vectors on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right"/> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Min<T>(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right"/> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(Vector64<T> left, Vector64<T> right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right"/> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(Vector64<T> left, T right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right"/> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(T left, Vector64<T> right) => left * right;

        /// <summary>Narrows two <see cref="Vector64{Double}"/> instances into one <see cref="Vector64{Single}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Single}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> Narrow(Vector64<double> lower, Vector64<double> upper)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                float value = (float)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<double>.Count; i < Vector64<float>.Count; i++)
            {
                float value = (float)upper.GetElementUnsafe(i - Vector64<double>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int16}"/> instances into one <see cref="Vector64{SByte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{SByte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<sbyte> Narrow(Vector64<short> lower, Vector64<short> upper)
        {
            Unsafe.SkipInit(out Vector64<sbyte> result);

            for (int i = 0; i < Vector64<short>.Count; i++)
            {
                sbyte value = (sbyte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<short>.Count; i < Vector64<sbyte>.Count; i++)
            {
                sbyte value = (sbyte)upper.GetElementUnsafe(i - Vector64<short>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int32}"/> instances into one <see cref="Vector64{Int16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Int16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> Narrow(Vector64<int> lower, Vector64<int> upper)
        {
            Unsafe.SkipInit(out Vector64<short> result);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                short value = (short)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<int>.Count; i < Vector64<short>.Count; i++)
            {
                short value = (short)upper.GetElementUnsafe(i - Vector64<int>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int64}"/> instances into one <see cref="Vector64{Int32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Int32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> Narrow(Vector64<long> lower, Vector64<long> upper)
        {
            Unsafe.SkipInit(out Vector64<int> result);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                int value = (int)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<long>.Count; i < Vector64<int>.Count; i++)
            {
                int value = (int)upper.GetElementUnsafe(i - Vector64<long>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt16}"/> instances into one <see cref="Vector64{Byte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Byte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> Narrow(Vector64<ushort> lower, Vector64<ushort> upper)
        {
            Unsafe.SkipInit(out Vector64<byte> result);

            for (int i = 0; i < Vector64<ushort>.Count; i++)
            {
                byte value = (byte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<ushort>.Count; i < Vector64<byte>.Count; i++)
            {
                byte value = (byte)upper.GetElementUnsafe(i - Vector64<ushort>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt32}"/> instances into one <see cref="Vector64{UInt16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{UInt16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ushort> Narrow(Vector64<uint> lower, Vector64<uint> upper)
        {
            Unsafe.SkipInit(out Vector64<ushort> result);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                ushort value = (ushort)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<uint>.Count; i < Vector64<ushort>.Count; i++)
            {
                ushort value = (ushort)upper.GetElementUnsafe(i - Vector64<uint>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt64}"/> instances into one <see cref="Vector64{UInt32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{UInt32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> Narrow(Vector64<ulong> lower, Vector64<ulong> upper)
        {
            Unsafe.SkipInit(out Vector64<uint> result);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                uint value = (uint)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<ulong>.Count; i < Vector64<uint>.Count; i++)
            {
                uint value = (uint)upper.GetElementUnsafe(i - Vector64<ulong>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Negates a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Negate<T>(Vector64<T> vector) => -vector;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> OnesComplement<T>(Vector64<T> vector) => ~vector;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector64<T> ShiftLeft<T>(Vector64<T> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<byte> ShiftLeft(Vector64<byte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<short> ShiftLeft(Vector64<short> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<int> ShiftLeft(Vector64<int> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<long> ShiftLeft(Vector64<long> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nint> ShiftLeft(Vector64<nint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nuint> ShiftLeft(Vector64<nuint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<sbyte> ShiftLeft(Vector64<sbyte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ushort> ShiftLeft(Vector64<ushort> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<uint> ShiftLeft(Vector64<uint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ulong> ShiftLeft(Vector64<ulong> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector64<T> ShiftRightArithmetic<T>(Vector64<T> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<short> ShiftRightArithmetic(Vector64<short> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<int> ShiftRightArithmetic(Vector64<int> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<long> ShiftRightArithmetic(Vector64<long> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nint> ShiftRightArithmetic(Vector64<nint> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<sbyte> ShiftRightArithmetic(Vector64<sbyte> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector64<T> ShiftRightLogical<T>(Vector64<T> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<byte> ShiftRightLogical(Vector64<byte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<short> ShiftRightLogical(Vector64<short> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<int> ShiftRightLogical(Vector64<int> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<long> ShiftRightLogical(Vector64<long> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nint> ShiftRightLogical(Vector64<nint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<nuint> ShiftRightLogical(Vector64<nuint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<sbyte> ShiftRightLogical(Vector64<sbyte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ushort> ShiftRightLogical(Vector64<ushort> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<uint> ShiftRightLogical(Vector64<uint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ulong> ShiftRightLogical(Vector64<ulong> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector64<byte> Shuffle(Vector64<byte> vector, Vector64<byte> indices)
        {
            Unsafe.SkipInit(out Vector64<byte> result);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                byte selectedIndex = indices.GetElementUnsafe(index);
                byte selectedValue = 0;

                if (selectedIndex < Vector64<byte>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<sbyte> Shuffle(Vector64<sbyte> vector, Vector64<sbyte> indices)
        {
            Unsafe.SkipInit(out Vector64<sbyte> result);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                byte selectedIndex = (byte)indices.GetElementUnsafe(index);
                sbyte selectedValue = 0;

                if (selectedIndex < Vector64<sbyte>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector64<short> Shuffle(Vector64<short> vector, Vector64<short> indices)
        {
            Unsafe.SkipInit(out Vector64<short> result);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                ushort selectedIndex = (ushort)indices.GetElementUnsafe(index);
                short selectedValue = 0;

                if (selectedIndex < Vector64<short>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<ushort> Shuffle(Vector64<ushort> vector, Vector64<ushort> indices)
        {
            Unsafe.SkipInit(out Vector64<ushort> result);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                ushort selectedIndex = indices.GetElementUnsafe(index);
                ushort selectedValue = 0;

                if (selectedIndex < Vector64<ushort>.Count)
                {
                    selectedValue = vector.GetElementUnsafe(selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector64<int> Shuffle(Vector64<int> vector, Vector64<int> indices)
        {
            Unsafe.SkipInit(out Vector64<int> result);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                int selectedValue = 0;

                if (selectedIndex < Vector64<int>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<uint> Shuffle(Vector64<uint> vector, Vector64<uint> indices)
        {
            Unsafe.SkipInit(out Vector64<uint> result);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                uint selectedIndex = indices.GetElementUnsafe(index);
                uint selectedValue = 0;

                if (selectedIndex < Vector64<uint>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector64<float> Shuffle(Vector64<float> vector, Vector64<int> indices)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                float selectedValue = 0;

                if (selectedIndex < Vector64<float>.Count)
                {
                    selectedValue = vector.GetElementUnsafe((int)selectedIndex);
                }
                result.SetElementUnsafe(index, selectedValue);
            }

            return result;
        }

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose square root is to be computed.</param>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Sqrt<T>(Vector64<T> vector)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.Sqrt(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Store<T>(this Vector64<T> source, T* destination) => source.StoreUnsafe(ref *destination);

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void StoreAligned<T>(this Vector64<T> source, T* destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if (((nuint)destination % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            *(Vector64<T>*)destination = source;
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void StoreAlignedNonTemporal<T>(this Vector64<T> source, T* destination) => source.StoreAligned(destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector64<T> source, ref T destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            ref byte address = ref Unsafe.As<T, byte>(ref destination);
            Unsafe.WriteUnaligned(ref address, source);
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector64<T> source, ref T destination, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Subtract<T>(Vector64<T> left, Vector64<T> right) => left - right;

        /// <summary>Computes the sum of all elements in a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose elements will be summed.</param>
        /// <returns>The sum of all elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T>(Vector64<T> vector)
        {
            T sum = default!;

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                sum = Scalar<T>.Add(sum, vector.GetElementUnsafe(index));
            }

            return sum;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToScalar<T>(this Vector64<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> ToVector128<T>(this Vector64<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Vector128<T> result = default;
            result.SetLowerUnsafe(vector);
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<T> ToVector128Unsafe<T>(this Vector64<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector128<T> result);
            result.SetLowerUnsafe(vector);
            return result;
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCopyTo<T>(this Vector64<T> vector, Span<T> destination)
        {
            if (destination.Length < Vector64<T>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
            return true;
        }

        /// <summary>Widens a <see cref="Vector64{Byte}" /> into two <see cref="Vector64{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<ushort> Lower, Vector64<ushort> Upper) Widen(Vector64<byte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Int16}" /> into two <see cref="Vector64{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<int> Lower, Vector64<int> Upper) Widen(Vector64<short> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Int32}" /> into two <see cref="Vector64{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<long> Lower, Vector64<long> Upper) Widen(Vector64<int> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{SByte}" /> into two <see cref="Vector64{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<short> Lower, Vector64<short> Upper) Widen(Vector64<sbyte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Single}" /> into two <see cref="Vector64{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<double> Lower, Vector64<double> Upper) Widen(Vector64<float> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{UInt16}" /> into two <see cref="Vector64{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<uint> Lower, Vector64<uint> Upper) Widen(Vector64<ushort> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{UInt32}" /> into two <see cref="Vector64{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (Vector64<ulong> Lower, Vector64<ulong> Upper) Widen(Vector64<uint> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens the lower half of a <see cref="Vector64{Byte}" /> into a <see cref="Vector64{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ushort> WidenLower(Vector64<byte> source)
        {
            Unsafe.SkipInit(out Vector64<ushort> lower);

            for (int i = 0; i < Vector64<ushort>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{Int16}" /> into a <see cref="Vector64{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> WidenLower(Vector64<short> source)
        {
            Unsafe.SkipInit(out Vector64<int> lower);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{Int32}" /> into a <see cref="Vector64{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> WidenLower(Vector64<int> source)
        {
            Unsafe.SkipInit(out Vector64<long> lower);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{SByte}" /> into a <see cref="Vector64{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> WidenLower(Vector64<sbyte> source)
        {
            Unsafe.SkipInit(out Vector64<short> lower);

            for (int i = 0; i < Vector64<short>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{Single}" /> into a <see cref="Vector64{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> WidenLower(Vector64<float> source)
        {
            Unsafe.SkipInit(out Vector64<double> lower);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{UInt16}" /> into a <see cref="Vector64{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> WidenLower(Vector64<ushort> source)
        {
            Unsafe.SkipInit(out Vector64<uint> lower);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see cref="Vector64{UInt32}" /> into a <see cref="Vector64{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> WidenLower(Vector64<uint> source)
        {
            Unsafe.SkipInit(out Vector64<ulong> lower);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{Byte}" /> into a <see cref="Vector64{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<ushort> WidenUpper(Vector64<byte> source)
        {
            Unsafe.SkipInit(out Vector64<ushort> upper);

            for (int i = Vector64<ushort>.Count; i < Vector64<byte>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<ushort>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{Int16}" /> into a <see cref="Vector64{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> WidenUpper(Vector64<short> source)
        {
            Unsafe.SkipInit(out Vector64<int> upper);

            for (int i = Vector64<int>.Count; i < Vector64<short>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<int>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{Int32}" /> into a <see cref="Vector64{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> WidenUpper(Vector64<int> source)
        {
            Unsafe.SkipInit(out Vector64<long> upper);

            for (int i = Vector64<long>.Count; i < Vector64<int>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<long>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{SByte}" /> into a <see cref="Vector64{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> WidenUpper(Vector64<sbyte> source)
        {
            Unsafe.SkipInit(out Vector64<short> upper);

            for (int i = Vector64<short>.Count; i < Vector64<sbyte>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<short>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{Single}" /> into a <see cref="Vector64{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> WidenUpper(Vector64<float> source)
        {
            Unsafe.SkipInit(out Vector64<double> upper);

            for (int i = Vector64<double>.Count; i < Vector64<float>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<double>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{UInt16}" /> into a <see cref="Vector64{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<uint> WidenUpper(Vector64<ushort> source)
        {
            Unsafe.SkipInit(out Vector64<uint> upper);

            for (int i = Vector64<uint>.Count; i < Vector64<ushort>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<uint>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see cref="Vector64{UInt32}" /> into a <see cref="Vector64{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<ulong> WidenUpper(Vector64<uint> source)
        {
            Unsafe.SkipInit(out Vector64<ulong> upper);

            for (int i = Vector64<ulong>.Count; i < Vector64<uint>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<ulong>.Count, value);
            }

            return upper;
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector64{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> WithElement<T>(this Vector64<T> vector, int index, T value)
        {
            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector64<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Xor<T>(Vector64<T> left, Vector64<T> right) => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector64<T> vector, int index)
        {
            Debug.Assert((index >= 0) && (index < Vector64<T>.Count));
            ref T address = ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in vector));
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector64<T> vector, int index, T value)
        {
            Debug.Assert((index >= 0) && (index < Vector64<T>.Count));
            ref T address = ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in vector));
            Unsafe.Add(ref address, index) = value;
        }
    }
}

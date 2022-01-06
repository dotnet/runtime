// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    public static class Vector64
    {
        internal const int Size = 8;

        /// <summary>Gets a value that indicates whether 64-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 64-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>64-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 64-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => false;
        }

        /// <summary>Computes the absolute value of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its absolute value computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Abs<T>(Vector64<T> vector)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return vector;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return vector;
            }
            else if (typeof(T) == typeof(uint))
            {
                return vector;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return vector;
            }
            else
            {
                return SoftwareFallback(vector);
            }

            static Vector64<T> SoftwareFallback(Vector64<T> vector)
            {
                Unsafe.SkipInit(out Vector64<T> result);

                for (int index = 0; index < Vector64<T>.Count; index++)
                {
                    var value = Scalar<T>.Abs(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Add<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> AndNot<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left & ~right;

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{U}" />.</summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<TTo> As<TFrom, TTo>(this Vector64<TFrom> vector)
            where TFrom : struct
            where TTo : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<TTo>();

            return Unsafe.As<Vector64<TFrom>, Vector64<TTo>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<byte> AsByte<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<double> AsDouble<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<short> AsInt16<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<int> AsInt32<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<long> AsInt64<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<nint> AsNInt<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<nuint> AsNUInt<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<sbyte> AsSByte<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<float> AsSingle<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<ushort> AsUInt16<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector)
            where T : struct => vector.As<T, ulong>();

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> BitwiseAnd<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> BitwiseOr<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        public static Vector64<float> Ceiling(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                var value = Scalar<float>.Ceiling(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Ceiling(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            var value = Scalar<double>.Ceiling(vector.GetElementUnsafe(0));
            result.SetElementUnsafe(0, value);

            return result;
        }

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> ConditionalSelect<T>(Vector64<T> condition, Vector64<T> left, Vector64<T> right)
            where T : struct => (left & condition) | (right & ~condition);

        /// <summary>Converts a <see cref="Vector64{Int64}" /> to a <see cref="Vector64{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector64<double> ConvertToDouble(Vector64<long> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                var value = (double)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{UInt64}" /> to a <see cref="Vector64{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<double> ConvertToDouble(Vector64<ulong> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                var value = (double)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Single}" /> to a <see cref="Vector64{Int32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector64<int> ConvertToInt32(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<int> result);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                var value = (int)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Double}" /> to a <see cref="Vector64{Int64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector64<long> ConvertToInt64(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<long> result);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                var value = (long)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Int32}" /> to a <see cref="Vector64{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector64<float> ConvertToSingle(Vector64<int> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                var value = (float)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{UInt32}" /> to a <see cref="Vector64{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<float> ConvertToSingle(Vector64<uint> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                var value = (float)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Single}" /> to a <see cref="Vector64{UInt32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<uint> ConvertToUInt32(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<uint> result);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                var value = (uint)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector64{Double}" /> to a <see cref="Vector64{UInt64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<ulong> ConvertToUInt64(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<ulong> result);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                var value = (ulong)vector.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Copies a <see cref="Vector64{T}" /> to a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        public static void CopyTo<T>(this Vector64<T> vector, T[] destination)
            where T : struct => vector.CopyTo(destination, startIndex: 0);

        /// <summary>Copies a <see cref="Vector64{T}" /> to a given array starting at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        public static unsafe void CopyTo<T>(this Vector64<T> vector, T[] destination, int startIndex)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if (destination is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
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
        public static void CopyTo<T>(this Vector64<T> vector, Span<T> destination)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if ((uint)destination.Length < (uint)Vector64<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<T> Create<T>(T value)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return Create((byte)(object)value).As<byte, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return Create((double)(object)value).As<double, T>();
            }
            else if (typeof(T) == typeof(short))
            {
                return Create((short)(object)value).As<short, T>();
            }
            else if (typeof(T) == typeof(int))
            {
                return Create((int)(object)value).As<int, T>();
            }
            else if (typeof(T) == typeof(long))
            {
                return Create((long)(object)value).As<long, T>();
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return Create((sbyte)(object)value).As<sbyte, T>();
            }
            else if (typeof(T) == typeof(float))
            {
                return Create((float)(object)value).As<float, T>();
            }
            else if (typeof(T) == typeof(ushort))
            {
                return Create((ushort)(object)value).As<ushort, T>();
            }
            else if (typeof(T) == typeof(uint))
            {
                return Create((uint)(object)value).As<uint, T>();
            }
            else if (typeof(T) == typeof(ulong))
            {
                return Create((ulong)(object)value).As<ulong, T>();
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi8</remarks>
        /// <returns>A new <see cref="Vector64{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<byte> Create(byte value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<byte> SoftwareFallback(byte value)
            {
                byte* pResult = stackalloc byte[8]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<double> Create(double value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<double> SoftwareFallback(double value)
            {
                return Unsafe.As<double, Vector64<double>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi16</remarks>
        /// <returns>A new <see cref="Vector64{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<short> Create(short value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<short> SoftwareFallback(short value)
            {
                short* pResult = stackalloc short[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi32</remarks>
        /// <returns>A new <see cref="Vector64{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<int> Create(int value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<int> SoftwareFallback(int value)
            {
                int* pResult = stackalloc int[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<long> Create(long value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<long> SoftwareFallback(long value)
            {
                return Unsafe.As<long, Vector64<long>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<nint> Create(nint value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<nint> SoftwareFallback(nint value)
            {
                if (Environment.Is64BitProcess)
                {
                    return Create((long)value).AsNInt();
                }
                else
                {
                    return Create((int)value).AsNInt();
                }
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<nuint> Create(nuint value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<nuint> SoftwareFallback(nuint value)
            {
                if (Environment.Is64BitProcess)
                {
                    return Create((ulong)value).AsNUInt();
                }
                else
                {
                    return Create((uint)value).AsNUInt();
                }
            }
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi8</remarks>
        /// <returns>A new <see cref="Vector64{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> Create(sbyte value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<sbyte> SoftwareFallback(sbyte value)
            {
                sbyte* pResult = stackalloc sbyte[8]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<float> Create(float value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<float> SoftwareFallback(float value)
            {
                float* pResult = stackalloc float[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi16</remarks>
        /// <returns>A new <see cref="Vector64{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> Create(ushort value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<ushort> SoftwareFallback(ushort value)
            {
                ushort* pResult = stackalloc ushort[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_set1_pi32</remarks>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<uint> SoftwareFallback(uint value)
            {
                uint* pResult = stackalloc uint[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector64<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<ulong> Create(ulong value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<ulong> SoftwareFallback(ulong value)
            {
                return Unsafe.As<ulong, Vector64<ulong>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector64{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        public static Vector64<T> Create<T>(T[] values)
            where T : struct => Create(values, index: 0);

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector64{T}.Count" />.</exception>
        public static Vector64<T> Create<T>(T[] values, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if (values is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((index < 0) || ((values.Length - index) < Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            return Unsafe.ReadUnaligned<Vector64<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector64{T}" /> with its elements set to the first <see cref="Vector64{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector64{T}.Count" />.</exception>
        public static Vector64<T> Create<T>(ReadOnlySpan<T> values)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

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
        public static unsafe Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1, e2, e3, e4, e5, e6, e7);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector64<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
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
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi16</remarks>
        /// <returns>A new <see cref="Vector64{Int16}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector64<short> Create(short e0, short e1, short e2, short e3)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1, e2, e3);
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector64<short> SoftwareFallback(short e0, short e1, short e2, short e3)
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
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi32</remarks>
        /// <returns>A new <see cref="Vector64{Int32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector64<int> Create(int e0, int e1)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1);
            }

            return SoftwareFallback(e0, e1);

            static Vector64<int> SoftwareFallback(int e0, int e1)
            {
                int* pResult = stackalloc int[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector64<int>>(pResult);
            }
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
        public static unsafe Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1, e2, e3, e4, e5, e6, e7);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector64<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
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
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        public static unsafe Vector64<float> Create(float e0, float e1)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1);
            }

            return SoftwareFallback(e0, e1);

            static Vector64<float> SoftwareFallback(float e0, float e1)
            {
                float* pResult = stackalloc float[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector64<float>>(pResult);
            }
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
        public static unsafe Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1, e2, e3);
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector64<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3)
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
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m64 _mm_setr_pi32</remarks>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint e0, uint e1)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(e0, e1);
            }

            return SoftwareFallback(e0, e1);

            static Vector64<uint> SoftwareFallback(uint e0, uint e1)
            {
                uint* pResult = stackalloc uint[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector64<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<byte> CreateScalar(byte value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<byte>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<byte> SoftwareFallback(byte value)
            {
                var result = Vector64<byte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<byte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<double> CreateScalar(double value)
        {
            if (AdvSimd.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<double> SoftwareFallback(double value)
            {
                return Unsafe.As<double, Vector64<double>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<short> CreateScalar(short value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<short>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<short> SoftwareFallback(short value)
            {
                var result = Vector64<short>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<short>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<int> CreateScalar(int value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<int>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<int> SoftwareFallback(int value)
            {
                var result = Vector64<int>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<int>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<long> CreateScalar(long value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<long> SoftwareFallback(long value)
            {
                return Unsafe.As<long, Vector64<long>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nint> CreateScalar(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return CreateScalar((long)value).AsNInt();
            }
            else
            {
                return CreateScalar((int)value).AsNInt();
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<nuint> CreateScalar(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return CreateScalar((ulong)value).AsNUInt();
            }
            else
            {
                return CreateScalar((uint)value).AsNUInt();
            }
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> CreateScalar(sbyte value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<sbyte>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<sbyte> SoftwareFallback(sbyte value)
            {
                var result = Vector64<sbyte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<sbyte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<float> CreateScalar(float value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<float>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<float> SoftwareFallback(float value)
            {
                var result = Vector64<float>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<float>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> CreateScalar(ushort value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<ushort>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<ushort> SoftwareFallback(ushort value)
            {
                var result = Vector64<ushort>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<ushort>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> CreateScalar(uint value)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.Insert(Vector64<uint>.Zero, 0, value);
            }

            return SoftwareFallback(value);

            static Vector64<uint> SoftwareFallback(uint value)
            {
                var result = Vector64<uint>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<uint>, byte>(ref result), value);
                return result;
            }
        }


        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<ulong> CreateScalar(ulong value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector64<ulong> SoftwareFallback(ulong value)
            {
                return Unsafe.As<ulong, Vector64<ulong>>(ref value);
            }
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector64<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            byte* pResult = stackalloc byte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector64<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            short* pResult = stackalloc short[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector64<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            int* pResult = stackalloc int[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector64<nint> CreateScalarUnsafe(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return Create((long)value).AsNInt();
            }
            else
            {
                return CreateScalarUnsafe((int)value).AsNInt();
            }
        }

        /// <summary>Creates a new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector64<nuint> CreateScalarUnsafe(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return Create((ulong)value).AsNUInt();
            }
            else
            {
                return CreateScalarUnsafe((uint)value).AsNUInt();
            }
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            sbyte* pResult = stackalloc sbyte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector64<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            float* pResult = stackalloc float[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ushort* pResult = stackalloc ushort[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            uint* pResult = stackalloc uint[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Divide<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static T Dot<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            T result = default;

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                var value = Scalar<T>.Multiply(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result = Scalar<T>.Add(result, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector64<T> Equals<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                var value = Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => Equals(left, right).As<T, ulong>() != Vector64<ulong>.Zero;

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        public static Vector64<float> Floor(Vector64<float> vector)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                var value = Scalar<float>.Floor(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<double> Floor(Vector64<double> vector)
        {
            Unsafe.SkipInit(out Vector64<double> result);

            var value = Scalar<double>.Floor(vector.GetElementUnsafe(0));
            result.SetElementUnsafe(0, value);

            return result;
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static T GetElement<T>(this Vector64<T> vector, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector64<T> GreaterThan<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => GreaterThan(left, right).As<T, ulong>() == Vector64<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => GreaterThan(left, right).As<T, ulong>() != Vector64<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector64<T> GreaterThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, ulong>() == Vector64<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, ulong>() != Vector64<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector64<T> LessThan<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => LessThan(left, right).As<T, ulong>() == Vector64<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => LessThan(left, right).As<T, ulong>() != Vector64<ulong>.Zero;

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector64<T> LessThanOrEqual<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                T value = Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, ulong>() == Vector64<ulong>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, ulong>() != Vector64<ulong>.Zero;

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector64<T> Max<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
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
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector64<T> Min<T>(Vector64<T> left, Vector64<T> right)
            where T : struct
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
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(Vector64<T> left, T right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Multiply<T>(T left, Vector64<T> right)
            where T : struct => left * right;

        /// <summary>Narrows two <see cref="Vector64{Double}"/> instances into one <see cref="Vector64{Single}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Single}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<float> Narrow(Vector64<double> lower, Vector64<double> upper)
        {
            Unsafe.SkipInit(out Vector64<float> result);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                var value = (float)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<double>.Count; i < Vector64<float>.Count; i++)
            {
                var value = (float)upper.GetElementUnsafe(i - Vector64<double>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int16}"/> instances into one <see cref="Vector64{SByte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{SByte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<sbyte> Narrow(Vector64<short> lower, Vector64<short> upper)
        {
            Unsafe.SkipInit(out Vector64<sbyte> result);

            for (int i = 0; i < Vector64<short>.Count; i++)
            {
                var value = (sbyte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<short>.Count; i < Vector64<sbyte>.Count; i++)
            {
                var value = (sbyte)upper.GetElementUnsafe(i - Vector64<short>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int32}"/> instances into one <see cref="Vector64{Int16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Int16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<short> Narrow(Vector64<int> lower, Vector64<int> upper)
        {
            Unsafe.SkipInit(out Vector64<short> result);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                var value = (short)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<int>.Count; i < Vector64<short>.Count; i++)
            {
                var value = (short)upper.GetElementUnsafe(i - Vector64<int>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{Int64}"/> instances into one <see cref="Vector64{Int32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Int32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        public static unsafe Vector64<int> Narrow(Vector64<long> lower, Vector64<long> upper)
        {
            Unsafe.SkipInit(out Vector64<int> result);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                var value = (int)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<long>.Count; i < Vector64<int>.Count; i++)
            {
                var value = (int)upper.GetElementUnsafe(i - Vector64<long>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt16}"/> instances into one <see cref="Vector64{Byte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{Byte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<byte> Narrow(Vector64<ushort> lower, Vector64<ushort> upper)
        {
            Unsafe.SkipInit(out Vector64<byte> result);

            for (int i = 0; i < Vector64<ushort>.Count; i++)
            {
                var value = (byte)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<ushort>.Count; i < Vector64<byte>.Count; i++)
            {
                var value = (byte)upper.GetElementUnsafe(i - Vector64<ushort>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt32}"/> instances into one <see cref="Vector64{UInt16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{UInt16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<ushort> Narrow(Vector64<uint> lower, Vector64<uint> upper)
        {
            Unsafe.SkipInit(out Vector64<ushort> result);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                var value = (ushort)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<uint>.Count; i < Vector64<ushort>.Count; i++)
            {
                var value = (ushort)upper.GetElementUnsafe(i - Vector64<uint>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector64{UInt64}"/> instances into one <see cref="Vector64{UInt32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector64{UInt32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector64<uint> Narrow(Vector64<ulong> lower, Vector64<ulong> upper)
        {
            Unsafe.SkipInit(out Vector64<uint> result);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                var value = (uint)lower.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector64<ulong>.Count; i < Vector64<uint>.Count; i++)
            {
                var value = (uint)upper.GetElementUnsafe(i - Vector64<ulong>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Negates a vector.</summary>
        /// <param name="vector">The vector to negate.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the negation of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Negate<T>(Vector64<T> vector)
            where T : struct => -vector;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> OnesComplement<T>(Vector64<T> vector)
            where T : struct => ~vector;

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <param name="vector">The vector whose square root is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        public static Vector64<T> Sqrt<T>(Vector64<T> vector)
            where T : struct
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Vector64<T>.Count; index++)
            {
                var value = Scalar<T>.Sqrt(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Subtract<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left - right;

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static T ToScalar<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<T> ToVector128<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Vector128<T> result = Vector128<T>.Zero;
            Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = vector;
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static unsafe Vector128<T> ToVector128Unsafe<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            byte* pResult = stackalloc byte[Vector128.Size];
            Unsafe.AsRef<Vector64<T>>(pResult) = vector;
            return Unsafe.AsRef<Vector128<T>>(pResult);
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was succesfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector64{T}.Count" />.</returns>
        public static bool TryCopyTo<T>(this Vector64<T> vector, Span<T> destination)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if ((uint)destination.Length < (uint)Vector64<T>.Count)
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
        public static unsafe (Vector64<ushort> Lower, Vector64<ushort> Upper) Widen(Vector64<byte> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Int16}" /> into two <see cref="Vector64{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector64<int> Lower, Vector64<int> Upper) Widen(Vector64<short> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Int32}" /> into two <see cref="Vector64{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector64<long> Lower, Vector64<long> Upper) Widen(Vector64<int> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{SByte}" /> into two <see cref="Vector64{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector64<short> Lower, Vector64<short> Upper) Widen(Vector64<sbyte> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{Single}" /> into two <see cref="Vector64{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        public static unsafe (Vector64<double> Lower, Vector64<double> Upper) Widen(Vector64<float> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{UInt16}" /> into two <see cref="Vector64{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector64<uint> Lower, Vector64<uint> Upper) Widen(Vector64<ushort> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector64{UInt32}" /> into two <see cref="Vector64{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        public static unsafe (Vector64<ulong> Lower, Vector64<ulong> Upper) Widen(Vector64<uint> source)
            => (WidenLower(source), WidenUpper(source));

        /// <summary>Creates a new <see cref="Vector64{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector64{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Vector64<T> WithElement<T>(this Vector64<T> vector, int index, T value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector64<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> Xor<T>(Vector64<T> left, Vector64<T> right)
            where T : struct => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector64<T> vector, int index)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector64<T>.Count));
            return Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in vector)), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector64<T> vector, int index, T value)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector64<T>.Count));
            Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in vector)), index) = value;
        }

        [Intrinsic]
        internal static Vector64<ushort> WidenLower(Vector64<byte> source)
        {
            Unsafe.SkipInit(out Vector64<ushort> lower);

            for (int i = 0; i < Vector64<ushort>.Count; i++)
            {
                var value = (ushort)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<int> WidenLower(Vector64<short> source)
        {
            Unsafe.SkipInit(out Vector64<int> lower);

            for (int i = 0; i < Vector64<int>.Count; i++)
            {
                var value = (int)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<long> WidenLower(Vector64<int> source)
        {
            Unsafe.SkipInit(out Vector64<long> lower);

            for (int i = 0; i < Vector64<long>.Count; i++)
            {
                var value = (long)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<short> WidenLower(Vector64<sbyte> source)
        {
            Unsafe.SkipInit(out Vector64<short> lower);

            for (int i = 0; i < Vector64<short>.Count; i++)
            {
                var value = (short)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<double> WidenLower(Vector64<float> source)
        {
            Unsafe.SkipInit(out Vector64<double> lower);

            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                var value = (double)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<uint> WidenLower(Vector64<ushort> source)
        {
            Unsafe.SkipInit(out Vector64<uint> lower);

            for (int i = 0; i < Vector64<uint>.Count; i++)
            {
                var value = (uint)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector64<ulong> WidenLower(Vector64<uint> source)
        {
            Unsafe.SkipInit(out Vector64<ulong> lower);

            for (int i = 0; i < Vector64<ulong>.Count; i++)
            {
                var value = (ulong)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static Vector64<ushort> WidenUpper(Vector64<byte> source)
        {
            Unsafe.SkipInit(out Vector64<ushort> upper);

            for (int i = Vector64<ushort>.Count; i < Vector64<byte>.Count; i++)
            {
                var value = (ushort)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<ushort>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<int> WidenUpper(Vector64<short> source)
        {
            Unsafe.SkipInit(out Vector64<int> upper);

            for (int i = Vector64<int>.Count; i < Vector64<short>.Count; i++)
            {
                var value = (int)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<int>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<long> WidenUpper(Vector64<int> source)
        {
            Unsafe.SkipInit(out Vector64<long> upper);

            for (int i = Vector64<long>.Count; i < Vector64<int>.Count; i++)
            {
                var value = (long)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<long>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<short> WidenUpper(Vector64<sbyte> source)
        {
            Unsafe.SkipInit(out Vector64<short> upper);

            for (int i = Vector64<short>.Count; i < Vector64<sbyte>.Count; i++)
            {
                var value = (short)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<short>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<double> WidenUpper(Vector64<float> source)
        {
            Unsafe.SkipInit(out Vector64<double> upper);

            for (int i = Vector64<double>.Count; i < Vector64<float>.Count; i++)
            {
                var value = (double)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<double>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<uint> WidenUpper(Vector64<ushort> source)
        {
            Unsafe.SkipInit(out Vector64<uint> upper);

            for (int i = Vector64<uint>.Count; i < Vector64<ushort>.Count; i++)
            {
                var value = (uint)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<uint>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector64<ulong> WidenUpper(Vector64<uint> source)
        {
            Unsafe.SkipInit(out Vector64<ulong> upper);

            for (int i = Vector64<ulong>.Count; i < Vector64<uint>.Count; i++)
            {
                var value = (ulong)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector64<ulong>.Count, value);
            }

            return upper;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Internal.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>Provides a collection of static convenience methods for creating, manipulating, combining, and converting generic vectors.</summary>
    [Intrinsic]
    public static partial class Vector
    {
        /// <summary>Gets a value that indicates whether vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>Vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => false;
        }

        /// <summary>Computes the absolute value of each element in a vector.</summary>
        /// <param name="value">The vector that will have its absolute value computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Abs<T>(Vector<T> value)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return value;
            }
            else if (typeof(T) == typeof(uint))
            {
                return value;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return value;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return value;
            }
            else
            {
                return SoftwareFallback(value);
            }

            static Vector<T> SoftwareFallback(Vector<T> value)
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    var element = Scalar<T>.Abs(value.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, element);
                }

                return result;
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Add<T>(Vector<T> left, Vector<T> right)
            where T : struct => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right)
            where T : struct => left & ~right;

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{U}" />.</summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
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

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> AsVectorByte<T>(Vector<T> value)
            where T : struct => value.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> AsVectorDouble<T>(Vector<T> value)
            where T : struct => value.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> AsVectorInt16<T>(Vector<T> value)
            where T : struct => value.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> AsVectorInt32<T>(Vector<T> value)
            where T : struct => value.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> AsVectorInt64<T>(Vector<T> value)
            where T : struct => value.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<nint> AsVectorNInt<T>(Vector<T> value)
            where T : struct => value.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value)
            where T : struct => value.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value)
            where T : struct => value.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> AsVectorSingle<T>(Vector<T> value)
            where T : struct => value.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value)
            where T : struct => value.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> AsVectorUInt32<T>(Vector<T> value)
            where T : struct => value.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value)
            where T : struct => value.As<T, ulong>();

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right)
            where T : struct => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right)
            where T : struct => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Ceiling(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                var element = Scalar<double>.Ceiling(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Ceiling(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                var element = Scalar<float>.Ceiling(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

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
        public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right)
            where T : struct => (left & condition) | (right & ~condition);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right)
            => ConditionalSelect(condition.As<int, float>(), left, right);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right)
            => ConditionalSelect(condition.As<long, double>(), left, right);

        /// <summary>Converts a <see cref="Vector{Int64}" /> to a <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector<double> ConvertToDouble(Vector<long> value)
        {
            if (Avx2.IsSupported)
            {
                Debug.Assert(Vector<double>.Count == Vector256<double>.Count);
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(Vector<double>.Count == Vector128<double>.Count);
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{UInt64}" /> to a <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value)
        {
            if (Avx2.IsSupported)
            {
                Debug.Assert(Vector<double>.Count == Vector256<double>.Count);
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(Vector<double>.Count == Vector128<double>.Count);
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{Int32}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<int> result);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                var element = (int)value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{Int64}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<long> result);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                var element = (long)value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector{Int32}" /> to a <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static unsafe Vector<float> ConvertToSingle(Vector<int> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int i = 0; i < Vector<float>.Count; i++)
            {
                var element = (float)value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector{UInt32}" /> to a <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector<float> ConvertToSingle(Vector<uint> value)
        {
            if (Avx2.IsSupported)
            {
                Debug.Assert(Vector<float>.Count == Vector256<float>.Count);
                return Vector256.ConvertToSingle(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(Vector<float>.Count == Vector128<float>.Count);
                return Vector128.ConvertToSingle(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{UInt32}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<uint> result);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                var element = (uint)value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{UInt64}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<ulong> result);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                var element = (ulong)value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right)
            where T : struct => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            T result = default;

            for (int index = 0; index < Vector<T>.Count; index++)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                var value = Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<double> left, Vector<double> right)
            => Equals<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<int> left, Vector<int> right)
            => Equals<int>(left, right);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> Equals(Vector<long> left, Vector<long> right)
            => Equals<long>(left, right);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Equals(Vector<float> left, Vector<float> right)
            => Equals<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector<T> left, Vector<T> right)
            where T : struct => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector<T> left, Vector<T> right)
            where T : struct => Equals(left, right).As<T, nuint>() != Vector<nuint>.Zero;


        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Floor(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                var element = Scalar<double>.Floor(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Floor(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                var element = Scalar<float>.Floor(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
            => GreaterThan<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
            => GreaterThan<int>(left, right);

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
            => GreaterThan<long>(left, right);

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
            => GreaterThan<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right)
            where T : struct => GreaterThan(left, right).As<T, nuint>() == Vector<nuint>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right)
            where T : struct => GreaterThan(left, right).As<T, nuint>() != Vector<nuint>.Zero;

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
            => GreaterThanOrEqual<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
            => GreaterThanOrEqual<int>(left, right);

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
            => GreaterThanOrEqual<long>(left, right);

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
            => GreaterThanOrEqual<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, nuint>() == Vector<nuint>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
            where T : struct => GreaterThanOrEqual(left, right).As<T, nuint>() != Vector<nuint>.Zero;

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
            => LessThan<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
            => LessThan<int>(left, right);

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
            => LessThan<long>(left, right);

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
            => LessThan<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector<T> left, Vector<T> right)
            where T : struct => LessThan(left, right).As<T, nuint>() == Vector<nuint>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector<T> left, Vector<T> right)
            where T : struct => LessThan(left, right).As<T, nuint>() != Vector<nuint>.Zero;

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default;
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
            => LessThanOrEqual<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
            => LessThanOrEqual<int>(left, right);

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
            => LessThanOrEqual<long>(left, right);

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
            => LessThanOrEqual<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, nuint>() == Vector<nuint>.AllBitsSet;

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
            where T : struct => LessThanOrEqual(left, right).As<T, nuint>() != Vector<nuint>.Zero;

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Max<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Min<T>(Vector<T> left, Vector<T> right)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(Vector<T> left, T right)
            where T : struct => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Multiply<T>(T left, Vector<T> right)
            where T : struct => left * right;

        /// <summary>Narrows two <see cref="Vector{Double}"/> instances into one <see cref="Vector{Single}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Single}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        public static unsafe Vector<float> Narrow(Vector<double> low, Vector<double> high)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                var value = (float)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<double>.Count; i < Vector<float>.Count; i++)
            {
                var value = (float)high.GetElementUnsafe(i - Vector<double>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{Int16}"/> instances into one <see cref="Vector{SByte}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{SByte}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
        {
            Unsafe.SkipInit(out Vector<sbyte> result);

            for (int i = 0; i < Vector<short>.Count; i++)
            {
                var value = (sbyte)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<short>.Count; i < Vector<sbyte>.Count; i++)
            {
                var value = (sbyte)high.GetElementUnsafe(i - Vector<short>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{Int32}"/> instances into one <see cref="Vector{Int16}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Int16}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        public static unsafe Vector<short> Narrow(Vector<int> low, Vector<int> high)
        {
            Unsafe.SkipInit(out Vector<short> result);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                var value = (short)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<int>.Count; i < Vector<short>.Count; i++)
            {
                var value = (short)high.GetElementUnsafe(i - Vector<int>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{Int64}"/> instances into one <see cref="Vector{Int32}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Int32}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        public static unsafe Vector<int> Narrow(Vector<long> low, Vector<long> high)
        {
            Unsafe.SkipInit(out Vector<int> result);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                var value = (int)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<long>.Count; i < Vector<int>.Count; i++)
            {
                var value = (int)high.GetElementUnsafe(i - Vector<long>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{UInt16}"/> instances into one <see cref="Vector{Byte}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Byte}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
        {
            Unsafe.SkipInit(out Vector<byte> result);

            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                var value = (byte)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<ushort>.Count; i < Vector<byte>.Count; i++)
            {
                var value = (byte)high.GetElementUnsafe(i - Vector<ushort>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{UInt32}"/> instances into one <see cref="Vector{UInt16}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{UInt16}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
        {
            Unsafe.SkipInit(out Vector<ushort> result);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                var value = (ushort)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<uint>.Count; i < Vector<ushort>.Count; i++)
            {
                var value = (ushort)high.GetElementUnsafe(i - Vector<uint>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see cref="Vector{UInt64}"/> instances into one <see cref="Vector{UInt32}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{UInt32}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
        {
            Unsafe.SkipInit(out Vector<uint> result);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                var value = (uint)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<ulong>.Count; i < Vector<uint>.Count; i++)
            {
                var value = (uint)high.GetElementUnsafe(i - Vector<ulong>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Negate<T>(Vector<T> value)
            where T : struct => -value;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> OnesComplement<T>(Vector<T> value)
            where T : struct => ~value;

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <param name="value">The vector whose square root is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> SquareRoot<T>(Vector<T> value)
            where T : struct
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                var element = Scalar<T>.Sqrt(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right)
            where T : struct => left - right;

        /// <summary>Widens a <see cref="Vector{Byte}" /> into two <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        public static unsafe void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Int16}" /> into two <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        public static unsafe void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Int32}" /> into two <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        public static unsafe void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{SByte}" /> into two <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        public static unsafe void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Single}" /> into two <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        public static unsafe void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{UInt16}" /> into two <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        public static unsafe void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{UInt32}" /> into two <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        [Intrinsic]
        public static unsafe void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right)
            where T : struct => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector<T> vector, int index)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector<T>.Count));
            return Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in vector)), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector<T> vector, int index, T value)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector<T>.Count));
            Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in vector)), index) = value;
        }

        [Intrinsic]
        internal static Vector<ushort> WidenLower(Vector<byte> source)
        {
            Unsafe.SkipInit(out Vector<ushort> lower);

            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                var value = (ushort)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<int> WidenLower(Vector<short> source)
        {
            Unsafe.SkipInit(out Vector<int> lower);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                var value = (int)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<long> WidenLower(Vector<int> source)
        {
            Unsafe.SkipInit(out Vector<long> lower);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                var value = (long)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<short> WidenLower(Vector<sbyte> source)
        {
            Unsafe.SkipInit(out Vector<short> lower);

            for (int i = 0; i < Vector<short>.Count; i++)
            {
                var value = (short)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<double> WidenLower(Vector<float> source)
        {
            Unsafe.SkipInit(out Vector<double> lower);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                var value = (double)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<uint> WidenLower(Vector<ushort> source)
        {
            Unsafe.SkipInit(out Vector<uint> lower);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                var value = (uint)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static unsafe Vector<ulong> WidenLower(Vector<uint> source)
        {
            Unsafe.SkipInit(out Vector<ulong> lower);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                var value = (ulong)source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        [Intrinsic]
        internal static Vector<ushort> WidenUpper(Vector<byte> source)
        {
            Unsafe.SkipInit(out Vector<ushort> upper);

            for (int i = Vector<ushort>.Count; i < Vector<byte>.Count; i++)
            {
                var value = (ushort)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<ushort>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<int> WidenUpper(Vector<short> source)
        {
            Unsafe.SkipInit(out Vector<int> upper);

            for (int i = Vector<int>.Count; i < Vector<short>.Count; i++)
            {
                var value = (int)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<int>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<long> WidenUpper(Vector<int> source)
        {
            Unsafe.SkipInit(out Vector<long> upper);

            for (int i = Vector<long>.Count; i < Vector<int>.Count; i++)
            {
                var value = (long)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<long>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<short> WidenUpper(Vector<sbyte> source)
        {
            Unsafe.SkipInit(out Vector<short> upper);

            for (int i = Vector<short>.Count; i < Vector<sbyte>.Count; i++)
            {
                var value = (short)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<short>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<double> WidenUpper(Vector<float> source)
        {
            Unsafe.SkipInit(out Vector<double> upper);

            for (int i = Vector<double>.Count; i < Vector<float>.Count; i++)
            {
                var value = (double)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<double>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<uint> WidenUpper(Vector<ushort> source)
        {
            Unsafe.SkipInit(out Vector<uint> upper);

            for (int i = Vector<uint>.Count; i < Vector<ushort>.Count; i++)
            {
                var value = (uint)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<uint>.Count, value);
            }

            return upper;
        }

        [Intrinsic]
        internal static unsafe Vector<ulong> WidenUpper(Vector<uint> source)
        {
            Unsafe.SkipInit(out Vector<ulong> upper);

            for (int i = Vector<ulong>.Count; i < Vector<uint>.Count; i++)
            {
                var value = (ulong)source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<ulong>.Count, value);
            }

            return upper;
        }

        /// <summary>
        /// Returns the sum of all elements inside the vector.
        /// </summary>
        [Intrinsic]
        public static T Sum<T>(Vector<T> value) where T : struct
        {
            T sum = default;

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                sum = Scalar<T>.Add(sum, value.GetElementUnsafe(index));
            }

            return sum;
        }
    }
}

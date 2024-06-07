// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

namespace System.Numerics
{
    /// <summary>Provides a collection of static methods for creating, manipulating, and otherwise operating on generic vectors.</summary>
    [Intrinsic]
    public static unsafe partial class Vector
    {
        internal static readonly nuint Alignment = (sizeof(Vector<byte>) == sizeof(Vector128<byte>)) ? (uint)(Vector128.Alignment) : (uint)(Vector256.Alignment);

        /// <summary>Gets a value that indicates whether vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>Vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => IsHardwareAccelerated;
        }

        /// <summary>Computes the absolute value of each element in a vector.</summary>
        /// <param name="value">The vector that will have its absolute value computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Abs<T>(Vector<T> value)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.Abs(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.Abs(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Abs(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.AndNot(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.AndNot(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.AndNot(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{U}" />.</summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<TTo> As<TFrom, TTo>(this Vector<TFrom> vector)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TTo>();

#if MONO
            return Unsafe.As<Vector<TFrom>, Vector<TTo>>(ref vector);
#else
            return Unsafe.BitCast<Vector<TFrom>, Vector<TTo>>(vector);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<byte> AsVectorByte<T>(Vector<T> value) => value.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<double> AsVectorDouble<T>(Vector<T> value) => value.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<short> AsVectorInt16<T>(Vector<T> value) => value.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<int> AsVectorInt32<T>(Vector<T> value) => value.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<long> AsVectorInt64<T>(Vector<T> value) => value.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<nint> AsVectorNInt<T>(Vector<T> value) => value.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value) => value.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) => value.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<float> AsVectorSingle<T>(Vector<T> value) => value.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) => value.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) => value.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) => value.As<T, ulong>();

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Ceiling(Vector<double> value)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.Ceiling(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.Ceiling(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.Ceiling(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Ceiling(Vector<float> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.Ceiling(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.Ceiling(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.Ceiling(value.AsVector128()).AsVector();
            }
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
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.ConditionalSelect(condition.AsVector512(), left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.ConditionalSelect(condition.AsVector256(), left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.ConditionalSelect(condition.AsVector128(), left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right) => ConditionalSelect(condition.As<int, float>(), left, right);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        [Intrinsic]
        public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right) => ConditionalSelect(condition.As<long, double>(), left, right);

        /// <summary>Converts a <see cref="Vector{Int64}" /> to a <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConvertToDouble(Vector<long> value)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.ConvertToDouble(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{UInt64}" /> to a <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConvertToDouble(Vector<ulong> value)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.ConvertToDouble(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{Int32}" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> ConvertToInt32(Vector<float> value)
        {
            if (sizeof(Vector<int>) == 64)
            {
                return Vector512.ConvertToInt32(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<int>) == 32)
            {
                return Vector256.ConvertToInt32(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<int>) == 16);
                return Vector128.ConvertToInt32(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{Int32}" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> ConvertToInt32Native(Vector<float> value)
        {
            if (sizeof(Vector<int>) == 64)
            {
                return Vector512.ConvertToInt32Native(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<int>) == 32)
            {
                return Vector256.ConvertToInt32Native(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<int>) == 16);
                return Vector128.ConvertToInt32Native(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{Int64}" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> ConvertToInt64(Vector<double> value)
        {
            if (sizeof(Vector<long>) == 64)
            {
                return Vector512.ConvertToInt64(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<long>) == 32)
            {
                return Vector256.ConvertToInt64(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<long>) == 16);
                return Vector128.ConvertToInt64(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{Int64}" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> ConvertToInt64Native(Vector<double> value)
        {
            if (sizeof(Vector<long>) == 64)
            {
                return Vector512.ConvertToInt64Native(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<long>) == 32)
            {
                return Vector256.ConvertToInt64Native(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<long>) == 16);
                return Vector128.ConvertToInt64Native(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Int32}" /> to a <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> ConvertToSingle(Vector<int> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.ConvertToSingle(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.ConvertToSingle(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.ConvertToSingle(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{UInt32}" /> to a <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> ConvertToSingle(Vector<uint> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.ConvertToSingle(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.ConvertToSingle(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.ConvertToSingle(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{UInt32}" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> ConvertToUInt32(Vector<float> value)
        {
            if (sizeof(Vector<uint>) == 64)
            {
                return Vector512.ConvertToUInt32(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == 32)
            {
                return Vector256.ConvertToUInt32(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == 16);
                return Vector128.ConvertToUInt32(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Single}" /> to a <see cref="Vector{UInt32}" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> ConvertToUInt32Native(Vector<float> value)
        {
            if (sizeof(Vector<uint>) == 64)
            {
                return Vector512.ConvertToUInt32Native(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == 32)
            {
                return Vector256.ConvertToUInt32Native(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == 16);
                return Vector128.ConvertToUInt32Native(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{UInt64}" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> ConvertToUInt64(Vector<double> value)
        {
            if (sizeof(Vector<ulong>) == 64)
            {
                return Vector512.ConvertToUInt64(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ulong>) == 32)
            {
                return Vector256.ConvertToUInt64(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ulong>) == 16);
                return Vector128.ConvertToUInt64(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see cref="Vector{Double}" /> to a <see cref="Vector{UInt64}" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> ConvertToUInt64Native(Vector<double> value)
        {
            if (sizeof(Vector<ulong>) == 64)
            {
                return Vector512.ConvertToUInt64Native(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ulong>) == 32)
            {
                return Vector256.ConvertToUInt64Native(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ulong>) == 16);
                return Vector128.ConvertToUInt64Native(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> instance where the elements begin at a specified value and which are spaced apart according to another specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="start">The value that element 0 will be initialized to.</param>
        /// <param name="step">The value that indicates how far apart each element should be from the previous.</param>
        /// <returns>A new <see cref="Vector{T}" /> instance with the first element initialized to <paramref name="start" /> and each subsequent element initialized to the the value of the previous element plus <paramref name="step" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> CreateSequence<T>(T start, T step)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.CreateSequence<T>(start, step).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.CreateSequence<T>(start, step).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.CreateSequence<T>(start, step).AsVector();
            }
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right) => left / right;

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Divide<T>(Vector<T> left, T right) => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.Dot(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.Dot(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Dot(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.Equals(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.Equals(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Equals(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector<long> Equals(Vector<double> left, Vector<double> right) => Equals<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector<int> Equals(Vector<int> left, Vector<int> right) => Equals<int>(left, right);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector<long> Equals(Vector<long> left, Vector<long> right) => Equals<long>(left, right);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        public static Vector<int> Equals(Vector<float> left, Vector<float> right) => Equals<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        public static bool EqualsAll<T>(Vector<T> left, Vector<T> right) => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.EqualsAny(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.EqualsAny(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.EqualsAny(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Floor(Vector<double> value)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.Floor(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.Floor(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.Floor(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Floor(Vector<float> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.Floor(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.Floor(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.Floor(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes (<paramref name="left"/> * <paramref name="right"/>) + <paramref name="addend"/>, rounded as one ternary operation.</summary>
        /// <param name="left">The vector to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The vector to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The vector to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>(<paramref name="left"/> * <paramref name="right"/>) + <paramref name="addend"/>, rounded as one ternary operation.</returns>
        /// <remarks>
        ///   <para>This computes (<paramref name="left"/> * <paramref name="right"/>) as if to infinite precision, adds <paramref name="addend" /> to that result as if to infinite precision, and finally rounds to the nearest representable value.</para>
        ///   <para>This differs from the non-fused sequence which would compute (<paramref name="left"/> * <paramref name="right"/>) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend" /> to the rounded result as if to infinite precision, and finally round to the nearest representable value.</para>
        /// </remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> FusedMultiplyAdd(Vector<double> left, Vector<double> right, Vector<double> addend)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.FusedMultiplyAdd(left.AsVector512(), right.AsVector512(), addend.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.FusedMultiplyAdd(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.FusedMultiplyAdd(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes (<paramref name="left"/> * <paramref name="right"/>) + <paramref name="addend"/>, rounded as one ternary operation.</summary>
        /// <param name="left">The vector to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The vector to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The vector to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>(<paramref name="left"/> * <paramref name="right"/>) + <paramref name="addend"/>, rounded as one ternary operation.</returns>
        /// <remarks>
        ///   <para>This computes (<paramref name="left"/> * <paramref name="right"/>) as if to infinite precision, adds <paramref name="addend" /> to that result as if to infinite precision, and finally rounds to the nearest representable value.</para>
        ///   <para>This differs from the non-fused sequence which would compute (<paramref name="left"/> * <paramref name="right"/>) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend" /> to the rounded result as if to infinite precision, and finally round to the nearest representable value.</para>
        /// </remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> FusedMultiplyAdd(Vector<float> left, Vector<float> right, Vector<float> addend)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.FusedMultiplyAdd(left.AsVector512(), right.AsVector512(), addend.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.FusedMultiplyAdd(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.FusedMultiplyAdd(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector();
            }
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetElement<T>(this Vector<T> vector, int index)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return vector.AsVector512().GetElement(index);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return vector.AsVector256().GetElement(index);
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return vector.AsVector128().GetElement(index);
            }
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThan(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThan(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThan(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right) => GreaterThan<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right) => GreaterThan<int>(left, right);

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right) => GreaterThan<long>(left, right);

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        [Intrinsic]
        public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right) => GreaterThan<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThanAll(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThanAll(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThanAll(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThanAny(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThanAny(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThanAny(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThanOrEqual(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThanOrEqual(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThanOrEqual(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right) => GreaterThanOrEqual<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right) => GreaterThanOrEqual<int>(left, right);

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right) => GreaterThanOrEqual<long>(left, right);

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        [Intrinsic]
        public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right) => GreaterThanOrEqual<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThanOrEqualAll(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThanOrEqualAll(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThanOrEqualAll(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.GreaterThanOrEqualAny(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.GreaterThanOrEqualAny(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.GreaterThanOrEqualAny(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThan(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThan(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThan(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector<long> LessThan(Vector<double> left, Vector<double> right) => LessThan<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector<int> LessThan(Vector<int> left, Vector<int> right) => LessThan<int>(left, right);

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector<long> LessThan(Vector<long> left, Vector<long> right) => LessThan<long>(left, right);

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        [Intrinsic]
        public static Vector<int> LessThan(Vector<float> left, Vector<float> right) => LessThan<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThanAll(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThanAll(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThanAll(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThanAny(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThanAny(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThanAny(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThanOrEqual(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThanOrEqual(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThanOrEqual(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right) => LessThanOrEqual<double>(left, right).As<double, long>();

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right) => LessThanOrEqual<int>(left, right);

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right) => LessThanOrEqual<long>(left, right);

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        [Intrinsic]
        public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right) => LessThanOrEqual<float>(left, right).As<float, int>();

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThanOrEqualAll(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThanOrEqualAll(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThanOrEqualAll(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.LessThanOrEqualAny(left.AsVector512(), right.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.LessThanOrEqualAny(left.AsVector256(), right.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.LessThanOrEqualAny(left.AsVector128(), right.AsVector128());
            }
        }

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<T> Load<T>(T* source) => LoadUnsafe(in *source);

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LoadAligned<T>(T* source)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.LoadAligned(source).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.LoadAligned(source).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.LoadAligned(source).AsVector();
            }
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<T> LoadAlignedNonTemporal<T>(T* source)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.LoadAlignedNonTemporal(source).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.LoadAlignedNonTemporal(source).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.LoadAlignedNonTemporal(source).AsVector();
            }
        }

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LoadUnsafe<T>(ref readonly T source)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.LoadUnsafe(in source).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.LoadUnsafe(in source).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.LoadUnsafe(in source).AsVector();
            }
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
        public static Vector<T> LoadUnsafe<T>(ref readonly T source, nuint elementOffset)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.LoadUnsafe(in source, elementOffset).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.LoadUnsafe(in source, elementOffset).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.LoadUnsafe(in source, elementOffset).AsVector();
            }
        }

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Max<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.Max(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.Max(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Max(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the minimum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Min<T>(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return Vector512.Min(left.AsVector512(), right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return Vector256.Min(left.AsVector256(), right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Min(left.AsVector128(), right.AsVector128()).AsVector();
            }
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Multiply<T>(Vector<T> left, T right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Multiply<T>(T left, Vector<T> right) => right * left;

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{double}, Vector128{double}, Vector128{double})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> MultiplyAddEstimate(Vector<double> left, Vector<double> right, Vector<double> addend)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.MultiplyAddEstimate(left.AsVector512(), right.AsVector512(), addend.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.MultiplyAddEstimate(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.MultiplyAddEstimate(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector();
            }
        }

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> MultiplyAddEstimate(Vector<float> left, Vector<float> right, Vector<float> addend)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.MultiplyAddEstimate(left.AsVector512(), right.AsVector512(), addend.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.MultiplyAddEstimate(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.MultiplyAddEstimate(left.AsVector128(), right.AsVector128(), addend.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{Double}"/> instances into one <see cref="Vector{Single}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Single}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Narrow(Vector<double> low, Vector<double> high)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{Int16}"/> instances into one <see cref="Vector{SByte}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{SByte}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
        {
            if (sizeof(Vector<sbyte>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<sbyte>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<sbyte>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{Int32}"/> instances into one <see cref="Vector{Int16}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Int16}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> Narrow(Vector<int> low, Vector<int> high)
        {
            if (sizeof(Vector<short>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<short>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<short>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{Int64}"/> instances into one <see cref="Vector{Int32}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Int32}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Narrow(Vector<long> low, Vector<long> high)
        {
            if (sizeof(Vector<int>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<int>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<int>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{UInt16}"/> instances into one <see cref="Vector{Byte}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{Byte}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
        {
            if (sizeof(Vector<byte>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<byte>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<byte>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{UInt32}"/> instances into one <see cref="Vector{UInt16}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{UInt16}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
        {
            if (sizeof(Vector<ushort>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ushort>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ushort>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Narrows two <see cref="Vector{UInt64}"/> instances into one <see cref="Vector{UInt32}" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector{UInt32}"/> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
        {
            if (sizeof(Vector<uint>) == 64)
            {
                return Vector512.Narrow(low.AsVector512(), high.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == 32)
            {
                return Vector256.Narrow(low.AsVector256(), high.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == 16);
                return Vector128.Narrow(low.AsVector128(), high.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> Negate<T>(Vector<T> value) => -value;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> OnesComplement<T>(Vector<T> value) => ~value;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<byte> ShiftLeft(Vector<byte> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<short> ShiftLeft(Vector<short> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<int> ShiftLeft(Vector<int> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<long> ShiftLeft(Vector<long> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<nint> ShiftLeft(Vector<nint> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<nuint> ShiftLeft(Vector<nuint> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<sbyte> ShiftLeft(Vector<sbyte> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ushort> ShiftLeft(Vector<ushort> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> ShiftLeft(Vector<uint> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> ShiftLeft(Vector<ulong> value, int shiftCount) => value << shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<short> ShiftRightArithmetic(Vector<short> value, int shiftCount) => value >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<int> ShiftRightArithmetic(Vector<int> value, int shiftCount) => value >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<long> ShiftRightArithmetic(Vector<long> value, int shiftCount) => value >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<nint> ShiftRightArithmetic(Vector<nint> value, int shiftCount) => value >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> value, int shiftCount) => value >> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<byte> ShiftRightLogical(Vector<byte> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<short> ShiftRightLogical(Vector<short> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<int> ShiftRightLogical(Vector<int> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<long> ShiftRightLogical(Vector<long> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        public static Vector<nint> ShiftRightLogical(Vector<nint> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<nuint> ShiftRightLogical(Vector<nuint> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<sbyte> ShiftRightLogical(Vector<sbyte> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ushort> ShiftRightLogical(Vector<ushort> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> ShiftRightLogical(Vector<uint> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> ShiftRightLogical(Vector<ulong> value, int shiftCount) => value >>> shiftCount;

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <param name="value">The vector whose square root is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> SquareRoot<T>(Vector<T> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.Sqrt(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.Sqrt(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Sqrt(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static void Store<T>(this Vector<T> source, T* destination) => source.StoreUnsafe(ref *destination);

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreAligned<T>(this Vector<T> source, T* destination)
        {
            if (sizeof(Vector<float>) == 64)
            {
                source.AsVector512().StoreAligned(destination);
            }
            else if (sizeof(Vector<float>) == 32)
            {
                source.AsVector256().StoreAligned(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                source.AsVector128().StoreAligned(destination);
            }
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static void StoreAlignedNonTemporal<T>(this Vector<T> source, T* destination)
        {
            if (sizeof(Vector<float>) == 64)
            {
                source.AsVector512().StoreAlignedNonTemporal(destination);
            }
            else if (sizeof(Vector<float>) == 32)
            {
                source.AsVector256().StoreAlignedNonTemporal(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                source.AsVector128().StoreAlignedNonTemporal(destination);
            }
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector<T> source, ref T destination)
        {
            if (sizeof(Vector<float>) == 64)
            {
                source.AsVector512().StoreUnsafe(ref destination);
            }
            else if (sizeof(Vector<float>) == 32)
            {
                source.AsVector256().StoreUnsafe(ref destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                source.AsVector128().StoreUnsafe(ref destination);
            }
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
        public static void StoreUnsafe<T>(this Vector<T> source, ref T destination, nuint elementOffset)
        {
            if (sizeof(Vector<float>) == 64)
            {
                source.AsVector512().StoreUnsafe(ref destination, elementOffset);
            }
            else if (sizeof(Vector<float>) == 32)
            {
                source.AsVector256().StoreUnsafe(ref destination, elementOffset);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                source.AsVector128().StoreUnsafe(ref destination, elementOffset);
            }
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right) => left - right;

        /// <summary>
        /// Returns the sum of all elements inside the vector.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T>(Vector<T> value)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return Vector512.Sum(value.AsVector512());
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return Vector256.Sum(value.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return Vector128.Sum(value.AsVector128());
            }
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToScalar<T>(this Vector<T> vector)
        {
            if (sizeof(Vector<float>) == 64)
            {
                return vector.AsVector512().ToScalar();
            }
            else if (sizeof(Vector<float>) == 32)
            {
                return vector.AsVector256().ToScalar();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return vector.AsVector128().ToScalar();
            }
        }

        /// <summary>Widens a <see cref="Vector{Byte}" /> into two <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Int16}" /> into two <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Int32}" /> into two <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{SByte}" /> into two <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{Single}" /> into two <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{UInt16}" /> into two <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see cref="Vector{UInt32}" /> into two <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens the lower half of a <see cref="Vector{Byte}" /> into a <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> WidenLower(Vector<byte> source)
        {
            if (sizeof(Vector<ushort>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ushort>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ushort>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{Int16}" /> into a <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> WidenLower(Vector<short> source)
        {
            if (sizeof(Vector<int>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<int>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<int>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{Int32}" /> into a <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> WidenLower(Vector<int> source)
        {
            if (sizeof(Vector<long>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<long>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<long>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{SByte}" /> into a <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> WidenLower(Vector<sbyte> source)
        {
            if (sizeof(Vector<short>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<short>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<short>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{Single}" /> into a <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> WidenLower(Vector<float> source)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{UInt16}" /> into a <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> WidenLower(Vector<ushort> source)
        {
            if (sizeof(Vector<uint>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the lower half of a <see cref="Vector{UInt32}" /> into a <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> WidenLower(Vector<uint> source)
        {
            if (sizeof(Vector<ulong>) == 64)
            {
                return Vector512.WidenLower(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ulong>) == 32)
            {
                return Vector256.WidenLower(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ulong>) == 16);
                return Vector128.WidenLower(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{Byte}" /> into a <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> WidenUpper(Vector<byte> source)
        {
            if (sizeof(Vector<ushort>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ushort>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ushort>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{Int16}" /> into a <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> WidenUpper(Vector<short> source)
        {
            if (sizeof(Vector<int>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<int>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<int>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{Int32}" /> into a <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> WidenUpper(Vector<int> source)
        {
            if (sizeof(Vector<long>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<long>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<long>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{SByte}" /> into a <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> WidenUpper(Vector<sbyte> source)
        {
            if (sizeof(Vector<short>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<short>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<short>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{Single}" /> into a <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> WidenUpper(Vector<float> source)
        {
            if (sizeof(Vector<double>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{UInt16}" /> into a <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> WidenUpper(Vector<ushort> source)
        {
            if (sizeof(Vector<uint>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Widens the upper half of a <see cref="Vector{UInt32}" /> into a <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> WidenUpper(Vector<uint> source)
        {
            if (sizeof(Vector<ulong>) == 64)
            {
                return Vector512.WidenUpper(source.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ulong>) == 32)
            {
                return Vector256.WidenUpper(source.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ulong>) == 16);
                return Vector128.WidenUpper(source.AsVector128()).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> WithElement<T>(this Vector<T> vector, int index, T value)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return vector.AsVector512().WithElement(index, value).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return vector.AsVector256().WithElement(index, value).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return vector.AsVector128().WithElement(index, value).AsVector();
            }
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) => left ^ right;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profitability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    // Many of the instance methods were moved to be extension methods as it results
    // in overall better codegen. This is because instance methods require the C# compiler
    // to generate extra locals as the `this` parameter has to be passed by reference.
    // Having them be extension methods means that the `this` parameter can be passed by
    // value instead, thus reducing the number of locals and helping prevent us from hitting
    // the internal inlining limits of the JIT.

    /// <summary>Provides a collection of static methods for creating, manipulating, and otherwise operting on 512-bit vectors.</summary>
    public static unsafe class Vector512
    {
        internal const int Size = 64;

#if TARGET_ARM
        internal const int Alignment = 8;
#elif TARGET_ARM64
        internal const int Alignment = 16;
#else
        internal const int Alignment = 64;
#endif

        /// <summary>Gets a value that indicates whether 512-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 512-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>512-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 512-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
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
        public static Vector512<T> Abs<T>(Vector512<T> vector)
        {
            return Create(
                Vector256.Abs(vector._lower),
                Vector256.Abs(vector._upper)
            );
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Add<T>(Vector512<T> left, Vector512<T> right) => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> AndNot<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.AndNot(left._lower, right._lower),
                Vector256.AndNot(left._upper, right._upper)
            );
        }

        /// <summary>Reinterprets a <see cref="Vector512{TFrom}" /> as a new <see cref="Vector512{TTo}" />.</summary>
        /// <typeparam name="TFrom">The type of the elements in the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the elements in the output vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{TTo}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<TTo> As<TFrom, TTo>(this Vector512<TFrom> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TTo>();

            return Unsafe.As<Vector512<TFrom>, Vector512<TTo>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Byte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> AsByte<T>(this Vector512<T> vector) => vector.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Double}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> AsDouble<T>(this Vector512<T> vector) => vector.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Int16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> AsInt16<T>(this Vector512<T> vector) => vector.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Int32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> AsInt32<T>(this Vector512<T> vector) => vector.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Int64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> AsInt64<T>(this Vector512<T> vector) => vector.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> AsNInt<T>(this Vector512<T> vector) => vector.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> AsNUInt<T>(this Vector512<T> vector) => vector.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{SByte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> AsSByte<T>(this Vector512<T> vector) => vector.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{Single}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> AsSingle<T>(this Vector512<T> vector) => vector.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> AsUInt16<T>(this Vector512<T> vector) => vector.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> AsUInt32<T>(this Vector512<T> vector) => vector.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> AsUInt64<T>(this Vector512<T> vector) => vector.As<T, ulong>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector512{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector512{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> AsVector512<T>(this Vector<T> value)
        {
            Debug.Assert(Vector512<T>.Count >= Vector<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            Vector512<T> result = default;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector512<T>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AsVector<T>(this Vector512<T> value)
        {
            Debug.Assert(Vector512<T>.Count >= Vector<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            ref byte address = ref Unsafe.As<Vector512<T>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector<T>>(ref address);
        }

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> BitwiseAnd<T>(Vector512<T> left, Vector512<T> right) => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> BitwiseOr<T>(Vector512<T> left, Vector512<T> right) => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<T> Ceiling<T>(Vector512<T> vector)
        {
            return Create(
                Vector256.Ceiling(vector._lower),
                Vector256.Ceiling(vector._upper)
            );
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Ceiling(Vector512<float> vector) => Ceiling<float>(vector);

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Ceiling(Vector512<double> vector) => Ceiling<double>(vector);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="condition" />, <paramref name="left" />, and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> ConditionalSelect<T>(Vector512<T> condition, Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.ConditionalSelect(condition._lower, left._lower, right._lower),
                Vector256.ConditionalSelect(condition._upper, left._upper, right._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Int64}" /> to a <see cref="Vector512{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> ConvertToDouble(Vector512<long> vector)
        {
            return Create(
                Vector256.ConvertToDouble(vector._lower),
                Vector256.ConvertToDouble(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{UInt64}" /> to a <see cref="Vector512{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> ConvertToDouble(Vector512<ulong> vector)
        {
            return Create(
                Vector256.ConvertToDouble(vector._lower),
                Vector256.ConvertToDouble(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Single}" /> to a <see cref="Vector512{Int32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> ConvertToInt32(Vector512<float> vector)
        {
            return Create(
                Vector256.ConvertToInt32(vector._lower),
                Vector256.ConvertToInt32(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Double}" /> to a <see cref="Vector512{Int64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> ConvertToInt64(Vector512<double> vector)
        {
            return Create(
                Vector256.ConvertToInt64(vector._lower),
                Vector256.ConvertToInt64(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Int32}" /> to a <see cref="Vector512{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> ConvertToSingle(Vector512<int> vector)
        {
            return Create(
                Vector256.ConvertToSingle(vector._lower),
                Vector256.ConvertToSingle(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{UInt32}" /> to a <see cref="Vector512{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> ConvertToSingle(Vector512<uint> vector)
        {
            return Create(
                Vector256.ConvertToSingle(vector._lower),
                Vector256.ConvertToSingle(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Single}" /> to a <see cref="Vector512{UInt32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> ConvertToUInt32(Vector512<float> vector)
        {
            return Create(
                Vector256.ConvertToUInt32(vector._lower),
                Vector256.ConvertToUInt32(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector512{Double}" /> to a <see cref="Vector512{UInt64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> ConvertToUInt64(Vector512<double> vector)
        {
            return Create(
                Vector256.ConvertToUInt64(vector._lower),
                Vector256.ConvertToUInt64(vector._upper)
            );
        }

        /// <summary>Copies a <see cref="Vector512{T}" /> to a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        public static void CopyTo<T>(this Vector512<T> vector, T[] destination)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (destination.Length < Vector512<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
        }

        /// <summary>Copies a <see cref="Vector512{T}" /> to a given array starting at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        public static void CopyTo<T>(this Vector512<T> vector, T[] destination, int startIndex)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Vector512<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
        }

        /// <summary>Copies a <see cref="Vector512{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which the <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        public static void CopyTo<T>(this Vector512<T> vector, Span<T> destination)
        {
            if ((uint)destination.Length < (uint)Vector512<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Create<T>(T value)
        {
            Vector256<T> vector = Vector256.Create(value);
            return Create(vector, vector);
        }

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi8</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> Create(byte value) => Create<byte>(value);

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512d _mm512_set1_pd</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Create(double value) => Create<double>(value);

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi16</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> Create(short value) => Create<short>(value);

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi32</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> Create(int value) => Create<int>(value);

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi64x</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> Create(long value) => Create<long>(value);

        /// <summary>Creates a new <see cref="Vector512{IntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{IntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> Create(nint value) => Create<nint>(value);

        /// <summary>Creates a new <see cref="Vector512{UIntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UIntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> Create(nuint value) => Create<nuint>(value);

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi8</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> Create(sbyte value) => Create<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512 _mm512_set1_ps</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Create(float value) => Create<float>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi16</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> Create(ushort value) => Create<ushort>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi32</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> Create(uint value) => Create<uint>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi64x</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> Create(ulong value) => Create<ulong>(value);

        /// <summary>Creates a new <see cref="Vector512{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with its elements set to the first <see cref="Vector512{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        public static Vector512<T> Create<T>(T[] values)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (values.Length < Vector512<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref values[0]));
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with its elements set to the first <see cref="Vector256{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        public static Vector512<T> Create<T>(T[] values, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((index < 0) || ((values.Length - index) < Vector512<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with its elements set to the first <see cref="Vector512{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector512{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Create<T>(ReadOnlySpan<T> values)
        {
            if (values.Length < Vector512<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            return Unsafe.ReadUnaligned<Vector512<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <param name="e32">The value that element 32 will be initialized to.</param>
        /// <param name="e33">The value that element 33 will be initialized to.</param>
        /// <param name="e34">The value that element 34 will be initialized to.</param>
        /// <param name="e35">The value that element 35 will be initialized to.</param>
        /// <param name="e36">The value that element 36 will be initialized to.</param>
        /// <param name="e37">The value that element 37 will be initialized to.</param>
        /// <param name="e38">The value that element 38 will be initialized to.</param>
        /// <param name="e39">The value that element 39 will be initialized to.</param>
        /// <param name="e40">The value that element 40 will be initialized to.</param>
        /// <param name="e41">The value that element 41 will be initialized to.</param>
        /// <param name="e42">The value that element 42 will be initialized to.</param>
        /// <param name="e43">The value that element 43 will be initialized to.</param>
        /// <param name="e44">The value that element 44 will be initialized to.</param>
        /// <param name="e45">The value that element 45 will be initialized to.</param>
        /// <param name="e46">The value that element 46 will be initialized to.</param>
        /// <param name="e47">The value that element 47 will be initialized to.</param>
        /// <param name="e48">The value that element 48 will be initialized to.</param>
        /// <param name="e49">The value that element 49 will be initialized to.</param>
        /// <param name="e50">The value that element 50 will be initialized to.</param>
        /// <param name="e51">The value that element 51 will be initialized to.</param>
        /// <param name="e52">The value that element 52 will be initialized to.</param>
        /// <param name="e53">The value that element 53 will be initialized to.</param>
        /// <param name="e54">The value that element 54 will be initialized to.</param>
        /// <param name="e55">The value that element 55 will be initialized to.</param>
        /// <param name="e56">The value that element 56 will be initialized to.</param>
        /// <param name="e57">The value that element 57 will be initialized to.</param>
        /// <param name="e58">The value that element 58 will be initialized to.</param>
        /// <param name="e59">The value that element 59 will be initialized to.</param>
        /// <param name="e60">The value that element 60 will be initialized to.</param>
        /// <param name="e61">The value that element 61 will be initialized to.</param>
        /// <param name="e62">The value that element 62 will be initialized to.</param>
        /// <param name="e63">The value that element 63 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Byte}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi8</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> Create(byte e0,  byte e1,  byte e2,  byte e3,  byte e4,  byte e5,  byte e6,  byte e7,  byte e8,  byte e9,  byte e10, byte e11, byte e12, byte e13, byte e14, byte e15,
                                             byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31,
                                             byte e32, byte e33, byte e34, byte e35, byte e36, byte e37, byte e38, byte e39, byte e40, byte e41, byte e42, byte e43, byte e44, byte e45, byte e46, byte e47,
                                             byte e48, byte e49, byte e50, byte e51, byte e52, byte e53, byte e54, byte e55, byte e56, byte e57, byte e58, byte e59, byte e60, byte e61, byte e62, byte e63)
        {
            return Create(
                Vector256.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31),
                Vector256.Create(e32, e33, e34, e35, e36, e37, e38, e39, e40, e41, e42, e43, e44, e45, e46, e47, e48, e49, e50, e51, e52, e53, e54, e55, e56, e57, e58, e59, e60, e61, e62, e63)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Double}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512d _mm512_setr_pd</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Create(double e0, double e1, double e2, double e3, double e4, double e5, double e6, double e7)
        {
            return Create(
                Vector256.Create(e0, e1, e2, e3),
                Vector256.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int16}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi16</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> Create(short e0,  short e1,  short e2,  short e3,  short e4,  short e5,  short e6,  short e7,  short e8,  short e9,  short e10, short e11, short e12, short e13, short e14, short e15,
                                              short e16, short e17, short e18, short e19, short e20, short e21, short e22, short e23, short e24, short e25, short e26, short e27, short e28, short e29, short e30, short e31)
        {
            return Create(
                Vector256.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15),
                Vector256.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int32}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi32</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7, int e8, int e9, int e10, int e11, int e12, int e13, int e14, int e15)
        {
            return Create(
                Vector256.Create(e0, e1, e2,  e3,  e4,  e5,  e6,  e7),
                Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int64}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi64x</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> Create(long e0, long e1, long e2, long e3, long e4, long e5, long e6, long e7)
        {
            return Create(
                Vector256.Create(e0, e1, e2, e3),
                Vector256.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <param name="e32">The value that element 32 will be initialized to.</param>
        /// <param name="e33">The value that element 33 will be initialized to.</param>
        /// <param name="e34">The value that element 34 will be initialized to.</param>
        /// <param name="e35">The value that element 35 will be initialized to.</param>
        /// <param name="e36">The value that element 36 will be initialized to.</param>
        /// <param name="e37">The value that element 37 will be initialized to.</param>
        /// <param name="e38">The value that element 38 will be initialized to.</param>
        /// <param name="e39">The value that element 39 will be initialized to.</param>
        /// <param name="e40">The value that element 40 will be initialized to.</param>
        /// <param name="e41">The value that element 41 will be initialized to.</param>
        /// <param name="e42">The value that element 42 will be initialized to.</param>
        /// <param name="e43">The value that element 43 will be initialized to.</param>
        /// <param name="e44">The value that element 44 will be initialized to.</param>
        /// <param name="e45">The value that element 45 will be initialized to.</param>
        /// <param name="e46">The value that element 46 will be initialized to.</param>
        /// <param name="e47">The value that element 47 will be initialized to.</param>
        /// <param name="e48">The value that element 48 will be initialized to.</param>
        /// <param name="e49">The value that element 49 will be initialized to.</param>
        /// <param name="e50">The value that element 50 will be initialized to.</param>
        /// <param name="e51">The value that element 51 will be initialized to.</param>
        /// <param name="e52">The value that element 52 will be initialized to.</param>
        /// <param name="e53">The value that element 53 will be initialized to.</param>
        /// <param name="e54">The value that element 54 will be initialized to.</param>
        /// <param name="e55">The value that element 55 will be initialized to.</param>
        /// <param name="e56">The value that element 56 will be initialized to.</param>
        /// <param name="e57">The value that element 57 will be initialized to.</param>
        /// <param name="e58">The value that element 58 will be initialized to.</param>
        /// <param name="e59">The value that element 59 will be initialized to.</param>
        /// <param name="e60">The value that element 60 will be initialized to.</param>
        /// <param name="e61">The value that element 61 will be initialized to.</param>
        /// <param name="e62">The value that element 62 will be initialized to.</param>
        /// <param name="e63">The value that element 63 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{SByte}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi8</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> Create(sbyte e0,  sbyte e1,  sbyte e2,  sbyte e3,  sbyte e4,  sbyte e5,  sbyte e6,  sbyte e7,  sbyte e8,  sbyte e9,  sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15,
                                              sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31,
                                              sbyte e32, sbyte e33, sbyte e34, sbyte e35, sbyte e36, sbyte e37, sbyte e38, sbyte e39, sbyte e40, sbyte e41, sbyte e42, sbyte e43, sbyte e44, sbyte e45, sbyte e46, sbyte e47,
                                              sbyte e48, sbyte e49, sbyte e50, sbyte e51, sbyte e52, sbyte e53, sbyte e54, sbyte e55, sbyte e56, sbyte e57, sbyte e58, sbyte e59, sbyte e60, sbyte e61, sbyte e62, sbyte e63)
        {
            return Create(
                Vector256.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31),
                Vector256.Create(e32, e33, e34, e35, e36, e37, e38, e39, e40, e41, e42, e43, e44, e45, e46, e47, e48, e49, e50, e51, e52, e53, e54, e55, e56, e57, e58, e59, e60, e61, e62, e63)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Single}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512 _mm512_setr_ps</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7, float e8, float e9, float e10, float e11, float e12, float e13, float e14, float e15)
        {
            return Create(
                Vector256.Create(e0, e1, e2,  e3,  e4,  e5,  e6,  e7),
                Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi16</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> Create(ushort e0,  ushort e1,  ushort e2,  ushort e3,  ushort e4,  ushort e5,  ushort e6,  ushort e7,  ushort e8,  ushort e9,  ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15,
                                               ushort e16, ushort e17, ushort e18, ushort e19, ushort e20, ushort e21, ushort e22, ushort e23, ushort e24, ushort e25, ushort e26, ushort e27, ushort e28, ushort e29, ushort e30, ushort e31)
        {
            return Create(
                Vector256.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15),
                Vector256.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi32</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7, uint e8, uint e9, uint e10, uint e11, uint e12, uint e13, uint e14, uint e15)
        {
            return Create(
                Vector256.Create(e0, e1, e2,  e3,  e4,  e5,  e6,  e7),
                Vector256.Create(e8, e9, e10, e11, e12, e13, e14, e15)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_epi64x</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3, ulong e4, ulong e5, ulong e6, ulong e7)
        {
            return Create(
                Vector256.Create(e0, e1, e2, e3),
                Vector256.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance from two <see cref="Vector256{T}" /> instances.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{T}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="lower" /> and <paramref name="upper" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Create<T>(Vector256<T> lower, Vector256<T> upper)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            Unsafe.SkipInit(out Vector512<T> result);

            result.SetLowerUnsafe(lower);
            result.SetUpperUnsafe(upper);

            return result;
        }

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance from two <see cref="Vector256{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> Create(Vector256<byte> lower, Vector256<byte> upper) => Create<byte>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance from two <see cref="Vector256{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512d _mm512_setr_m256d (__m256d lo, __m256d hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Create(Vector256<double> lower, Vector256<double> upper) => Create<double>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance from two <see cref="Vector256{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> Create(Vector256<short> lower, Vector256<short> upper) => Create<short>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance from two <see cref="Vector256{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_m256i (__m256i lo, __m256i hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> Create(Vector256<int> lower, Vector256<int> upper) => Create<int>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance from two <see cref="Vector256{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> Create(Vector256<long> lower, Vector256<long> upper) => Create<long>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{IntPtr}" /> instance from two <see cref="Vector256{IntPtr}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{IntPtr}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> Create(Vector256<nint> lower, Vector256<nint> upper) => Create<nint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{UIntPtr}" /> instance from two <see cref="Vector256{UIntPtr}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UIntPtr}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> Create(Vector256<nuint> lower, Vector256<nuint> upper) => Create<nuint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance from two <see cref="Vector256{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> Create(Vector256<sbyte> lower, Vector256<sbyte> upper) => Create<sbyte>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance from two <see cref="Vector256{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512 _mm512_setr_m256 (__m256 lo, __m256 hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Create(Vector256<float> lower, Vector256<float> upper) => Create<float>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance from two <see cref="Vector256{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> Create(Vector256<ushort> lower, Vector256<ushort> upper) => Create<ushort>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance from two <see cref="Vector256{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_setr_m256i (__m256i lo, __m256i hi)</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> Create(Vector256<uint> lower, Vector256<uint> upper) => Create<uint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance from two <see cref="Vector256{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 256-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 256-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> Create(Vector256<ulong> lower, Vector256<ulong> upper) => Create<ulong>(lower, upper);

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> CreateScalar<T>(T value) => Vector256.CreateScalar(value).ToVector512();

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> CreateScalar(byte value) => CreateScalar<byte>(value);

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> CreateScalar(double value) => CreateScalar<double>(value);

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> CreateScalar(short value) => CreateScalar<short>(value);

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> CreateScalar(int value) => CreateScalar<int>(value);

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> CreateScalar(long value) => CreateScalar<long>(value);

        /// <summary>Creates a new <see cref="Vector512{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> CreateScalar(nint value) => CreateScalar<nint>(value);

        /// <summary>Creates a new <see cref="Vector512{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> CreateScalar(nuint value) => CreateScalar<nuint>(value);

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> CreateScalar(sbyte value) => CreateScalar<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> CreateScalar(float value) => CreateScalar<float>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> CreateScalar(ushort value) => CreateScalar<ushort>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> CreateScalar(uint value) => CreateScalar<uint>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> CreateScalar(ulong value) => CreateScalar<ulong>(value);

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> CreateScalarUnsafe<T>(T value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            Unsafe.SkipInit(out Vector512<T> result);

            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> CreateScalarUnsafe(byte value) => CreateScalarUnsafe<byte>(value);

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> CreateScalarUnsafe(double value) => CreateScalarUnsafe<double>(value);

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> CreateScalarUnsafe(short value) => CreateScalarUnsafe<short>(value);

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> CreateScalarUnsafe(int value) => CreateScalarUnsafe<int>(value);

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> CreateScalarUnsafe(long value) => CreateScalarUnsafe<long>(value);

        /// <summary>Creates a new <see cref="Vector512{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> CreateScalarUnsafe(nint value) => CreateScalarUnsafe<nint>(value);

        /// <summary>Creates a new <see cref="Vector512{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> CreateScalarUnsafe(nuint value) => CreateScalarUnsafe<nuint>(value);

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> CreateScalarUnsafe(sbyte value) => CreateScalarUnsafe<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> CreateScalarUnsafe(float value) => CreateScalarUnsafe<float>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> CreateScalarUnsafe(ushort value) => CreateScalarUnsafe<ushort>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> CreateScalarUnsafe(uint value) => CreateScalarUnsafe<uint>(value);

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector512{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> CreateScalarUnsafe(ulong value) => CreateScalarUnsafe<ulong>(value);

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance where the elements begin at a specified value and which are spaced apart according to another specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="start">The value that element 0 will be initialized to.</param>
        /// <param name="step">The value that indicates how far apart each element should be from the previous.</param>
        /// <returns>A new <see cref="Vector512{T}" /> instance with the first element initialized to <paramref name="start" /> and each subsequent element initialized to the the value of the previous element plus <paramref name="step" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> CreateSequence<T>(T start, T step) => (Vector512<T>.Indices * step) + Create(start);

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Divide<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.Divide(left._lower, right._lower),
                Vector256.Divide(left._upper, right._upper)
            );
        }

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Divide<T>(Vector512<T> left, T right) => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector512<T> left, Vector512<T> right)
        {
            // Doing this as Dot(lower) + Dot(upper) is important for floating-point determinism
            // This is because the underlying dpps instruction on x86/x64 will do this equivalently
            // and otherwise the software vs accelerated implementations may differ in returned result.

            T result = Vector256.Dot(left._lower, right._lower);
            result = Scalar<T>.Add(result, Vector256.Dot(left._upper, right._upper));
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
        public static Vector512<T> Equals<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.Equals(left._lower, right._lower),
                Vector256.Equals(left._upper, right._upper)
            );
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAll<T>(Vector512<T> left, Vector512<T> right) => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.EqualsAny(left._lower, right._lower)
                || Vector256.EqualsAny(left._upper, right._upper);
        }

        /// <inheritdoc cref="Vector256.Exp(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Exp(Vector512<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpDouble<Vector512<double>, Vector512<long>, Vector512<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Exp(vector._lower),
                    Vector256.Exp(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector256.Exp(Vector256{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Exp(Vector512<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpSingle<Vector512<float>, Vector512<uint>, Vector512<double>, Vector512<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Exp(vector._lower),
                    Vector256.Exp(vector._upper)
                );
            }
        }

        /// <summary>Extracts the most significant bit from each element in a vector.</summary>
        /// <param name="vector">The vector whose elements should have their most significant bit extracted.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The packed most significant bits extracted from the elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ExtractMostSignificantBits<T>(this Vector512<T> vector)
        {
            ulong result = vector._lower.ExtractMostSignificantBits();
            result |= (ulong)(vector._upper.ExtractMostSignificantBits()) << Vector256<T>.Count;
            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<T> Floor<T>(Vector512<T> vector)
        {
            return Create(
                Vector256.Floor(vector._lower),
                Vector256.Floor(vector._upper)
            );
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Floor(Vector512<float> vector) => Floor<float>(vector);

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Floor(Vector512<double> vector) => Floor<double>(vector);

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetElement<T>(this Vector512<T> vector, int index)
        {
            if ((uint)(index) >= (uint)(Vector512<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Gets the value of the lower 256-bits as a new <see cref="Vector256{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 256-bits from.</param>
        /// <returns>The value of the lower 256-bits as a new <see cref="Vector256{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GetLower<T>(this Vector512<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            return vector._lower;
        }

        /// <summary>Gets the value of the upper 256-bits as a new <see cref="Vector256{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 256-bits from.</param>
        /// <returns>The value of the upper 256-bits as a new <see cref="Vector256{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GetUpper<T>(this Vector512<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            return vector._upper;
        }

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> GreaterThan<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.GreaterThan(left._lower, right._lower),
                Vector256.GreaterThan(left._upper, right._upper)
            );
        }

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAll<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.GreaterThanAll(left._lower, right._lower)
                && Vector256.GreaterThanAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.GreaterThanAny(left._lower, right._lower)
                || Vector256.GreaterThanAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> GreaterThanOrEqual<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.GreaterThanOrEqual(left._lower, right._lower),
                Vector256.GreaterThanOrEqual(left._upper, right._upper)
            );
        }

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAll<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.GreaterThanOrEqualAll(left._lower, right._lower)
                && Vector256.GreaterThanOrEqualAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.GreaterThanOrEqualAny(left._lower, right._lower)
                || Vector256.GreaterThanOrEqualAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> LessThan<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.LessThan(left._lower, right._lower),
                Vector256.LessThan(left._upper, right._upper)
            );
        }

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAll<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.LessThanAll(left._lower, right._lower)
                && Vector256.LessThanAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.LessThanAny(left._lower, right._lower)
                || Vector256.LessThanAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> LessThanOrEqual<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.LessThanOrEqual(left._lower, right._lower),
                Vector256.LessThanOrEqual(left._upper, right._upper)
            );
        }

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAll<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.LessThanOrEqualAll(left._lower, right._lower)
                && Vector256.LessThanOrEqualAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector512<T> left, Vector512<T> right)
        {
            return Vector256.LessThanOrEqualAny(left._lower, right._lower)
                || Vector256.LessThanOrEqualAny(left._upper, right._upper);
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
        public static Vector512<T> Load<T>(T* source) => LoadUnsafe(ref *source);

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> LoadAligned<T>(T* source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            if (((nuint)(source) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            return *(Vector512<T>*)(source);
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> LoadAlignedNonTemporal<T>(T* source) => LoadAligned(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> LoadUnsafe<T>(ref readonly T source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in source));
            return Unsafe.ReadUnaligned<Vector512<T>>(in address);
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
        public static Vector512<T> LoadUnsafe<T>(ref readonly T source, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
            return Unsafe.ReadUnaligned<Vector512<T>>(in address);
        }

        /// <summary>Loads a vector from the given source and reinterprets it as <see cref="ushort"/>.</summary>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<ushort> LoadUnsafe(ref char source) =>
            LoadUnsafe(ref Unsafe.As<char, ushort>(ref source));

        /// <summary>Loads a vector from the given source and element offset and reinterprets it as <see cref="ushort"/>.</summary>
        /// <param name="source">The source to which <paramref name="elementOffset" /> will be added before loading the vector.</param>
        /// <param name="elementOffset">The element offset from <paramref name="source" /> from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" /> plus <paramref name="elementOffset" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<ushort> LoadUnsafe(ref char source, nuint elementOffset) =>
            LoadUnsafe(ref Unsafe.As<char, ushort>(ref source), elementOffset);

        /// <inheritdoc cref="Vector256.Log(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Log(Vector512<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogDouble<Vector512<double>, Vector512<long>, Vector512<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Log(vector._lower),
                    Vector256.Log(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector256.Log(Vector256{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Log(Vector512<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogSingle<Vector512<float>, Vector512<int>, Vector512<uint>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Log(vector._lower),
                    Vector256.Log(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector256.Log2(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> Log2(Vector512<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Double<Vector512<double>, Vector512<long>, Vector512<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Log2(vector._lower),
                    Vector256.Log2(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector256.Log2(Vector256{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Log2(Vector512<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Single<Vector512<float>, Vector512<int>, Vector512<uint>>(vector);
            }
            else
            {
                return Create(
                    Vector256.Log2(vector._lower),
                    Vector256.Log2(vector._upper)
                );
            }
        }

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Max<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.Max(left._lower, right._lower),
                Vector256.Max(left._upper, right._upper)
            );
        }

        /// <summary>Computes the minimum of two vectors on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Min<T>(Vector512<T> left, Vector512<T> right)
        {
            return Create(
                Vector256.Min(left._lower, right._lower),
                Vector256.Min(left._upper, right._upper)
            );
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Multiply<T>(Vector512<T> left, Vector512<T> right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Multiply<T>(Vector512<T> left, T right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Multiply<T>(T left, Vector512<T> right) => left * right;

        /// <summary>Narrows two <see cref="Vector512{Double}"/> instances into one <see cref="Vector512{Single}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{Single}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> Narrow(Vector512<double> lower, Vector512<double> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{Int16}"/> instances into one <see cref="Vector512{SByte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{SByte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> Narrow(Vector512<short> lower, Vector512<short> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{Int32}"/> instances into one <see cref="Vector512{Int16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{Int16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> Narrow(Vector512<int> lower, Vector512<int> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{Int64}"/> instances into one <see cref="Vector512{Int32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{Int32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> Narrow(Vector512<long> lower, Vector512<long> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{UInt16}"/> instances into one <see cref="Vector512{Byte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{Byte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> Narrow(Vector512<ushort> lower, Vector512<ushort> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{UInt32}"/> instances into one <see cref="Vector512{UInt16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{UInt16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> Narrow(Vector512<uint> lower, Vector512<uint> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector512{UInt64}"/> instances into one <see cref="Vector512{UInt32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector512{UInt32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> Narrow(Vector512<ulong> lower, Vector512<ulong> upper)
        {
            return Create(
                Vector256.Narrow(lower._lower, lower._upper),
                Vector256.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Negates a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Negate<T>(Vector512<T> vector) => -vector;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> OnesComplement<T>(Vector512<T> vector)
        {
            return Create(
                Vector256.OnesComplement(vector._lower),
                Vector256.OnesComplement(vector._upper)
            );
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<T> ShiftLeft<T>(Vector512<T> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> ShiftLeft(Vector512<byte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> ShiftLeft(Vector512<short> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> ShiftLeft(Vector512<int> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> ShiftLeft(Vector512<long> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> ShiftLeft(Vector512<nint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> ShiftLeft(Vector512<nuint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> ShiftLeft(Vector512<sbyte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> ShiftLeft(Vector512<ushort> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> ShiftLeft(Vector512<uint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> ShiftLeft(Vector512<ulong> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<T> ShiftRightArithmetic<T>(Vector512<T> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> ShiftRightArithmetic(Vector512<short> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> ShiftRightArithmetic(Vector512<nint> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> ShiftRightArithmetic(Vector512<sbyte> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<T> ShiftRightLogical<T>(Vector512<T> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<byte> ShiftRightLogical(Vector512<byte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> ShiftRightLogical(Vector512<short> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> ShiftRightLogical(Vector512<int> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> ShiftRightLogical(Vector512<long> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nint> ShiftRightLogical(Vector512<nint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<nuint> ShiftRightLogical(Vector512<nuint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<sbyte> ShiftRightLogical(Vector512<sbyte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector512<byte> Shuffle(Vector512<byte> vector, Vector512<byte> indices)
        {
            Unsafe.SkipInit(out Vector512<byte> result);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                byte selectedIndex = indices.GetElementUnsafe(index);
                byte selectedValue = 0;

                if (selectedIndex < Vector512<byte>.Count)
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
        public static Vector512<sbyte> Shuffle(Vector512<sbyte> vector, Vector512<sbyte> indices)
        {
            Unsafe.SkipInit(out Vector512<sbyte> result);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                byte selectedIndex = (byte)indices.GetElementUnsafe(index);
                sbyte selectedValue = 0;

                if (selectedIndex < Vector512<sbyte>.Count)
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
        public static Vector512<short> Shuffle(Vector512<short> vector, Vector512<short> indices)
        {
            Unsafe.SkipInit(out Vector512<short> result);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                ushort selectedIndex = (ushort)indices.GetElementUnsafe(index);
                short selectedValue = 0;

                if (selectedIndex < Vector512<short>.Count)
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
        public static Vector512<ushort> Shuffle(Vector512<ushort> vector, Vector512<ushort> indices)
        {
            Unsafe.SkipInit(out Vector512<ushort> result);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                ushort selectedIndex = indices.GetElementUnsafe(index);
                ushort selectedValue = 0;

                if (selectedIndex < Vector512<ushort>.Count)
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
        public static Vector512<int> Shuffle(Vector512<int> vector, Vector512<int> indices)
        {
            Unsafe.SkipInit(out Vector512<int> result);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                int selectedValue = 0;

                if (selectedIndex < Vector512<int>.Count)
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
        public static Vector512<uint> Shuffle(Vector512<uint> vector, Vector512<uint> indices)
        {
            Unsafe.SkipInit(out Vector512<uint> result);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                uint selectedIndex = indices.GetElementUnsafe(index);
                uint selectedValue = 0;

                if (selectedIndex < Vector512<uint>.Count)
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
        public static Vector512<float> Shuffle(Vector512<float> vector, Vector512<int> indices)
        {
            Unsafe.SkipInit(out Vector512<float> result);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                float selectedValue = 0;

                if (selectedIndex < Vector512<float>.Count)
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
        public static Vector512<long> Shuffle(Vector512<long> vector, Vector512<long> indices)
        {
            Unsafe.SkipInit(out Vector512<long> result);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                long selectedValue = 0;

                if (selectedIndex < (uint)Vector512<long>.Count)
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
        public static Vector512<ulong> Shuffle(Vector512<ulong> vector, Vector512<ulong> indices)
        {
            Unsafe.SkipInit(out Vector512<ulong> result);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                ulong selectedIndex = indices.GetElementUnsafe(index);
                ulong selectedValue = 0;

                if (selectedIndex < (uint)Vector512<ulong>.Count)
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
        public static Vector512<double> Shuffle(Vector512<double> vector, Vector512<long> indices)
        {
            Unsafe.SkipInit(out Vector512<double> result);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                double selectedValue = 0;

                if (selectedIndex < (uint)Vector512<double>.Count)
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
        public static Vector512<T> Sqrt<T>(Vector512<T> vector)
        {
            return Create(
                Vector256.Sqrt(vector._lower),
                Vector256.Sqrt(vector._upper)
            );
        }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store<T>(this Vector512<T> source, T* destination) => source.StoreUnsafe(ref *destination);

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreAligned<T>(this Vector512<T> source, T* destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            if (((nuint)(destination) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            *(Vector512<T>*)(destination) = source;
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreAlignedNonTemporal<T>(this Vector512<T> source, T* destination) => source.StoreAligned(destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector512<T> source, ref T destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            ref byte address = ref Unsafe.As<T, byte>(ref destination);
            Unsafe.WriteUnaligned(ref address, source);
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector512<T> source, ref T destination, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> Subtract<T>(Vector512<T> left, Vector512<T> right) => left - right;

        /// <summary>Computes the sum of all elements in a vector.</summary>
        /// <param name="vector">The vector whose elements will be summed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of all elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T>(Vector512<T> vector)
        {
            // Doing this as Sum(lower) + Sum(upper) is important for floating-point determinism
            // This is because the underlying dpps instruction on x86/x64 will do this equivalently
            // and otherwise the software vs accelerated implementations may differ in returned result.

            T result = Vector256.Sum(vector._lower);
            result = Scalar<T>.Add(result, Vector256.Sum(vector._upper));
            return result;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToScalar<T>(this Vector512<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector512{T}.Count" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        public static bool TryCopyTo<T>(this Vector512<T> vector, Span<T> destination)
        {
            if ((uint)destination.Length < (uint)Vector512<T>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
            return true;
        }

        /// <summary>Widens a <see cref="Vector512{Byte}" /> into two <see cref="Vector512{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<ushort> Lower, Vector512<ushort> Upper) Widen(Vector512<byte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{Int16}" /> into two <see cref="Vector512{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<int> Lower, Vector512<int> Upper) Widen(Vector512<short> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{Int32}" /> into two <see cref="Vector512{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<long> Lower, Vector512<long> Upper) Widen(Vector512<int> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{SByte}" /> into two <see cref="Vector512{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<short> Lower, Vector512<short> Upper) Widen(Vector512<sbyte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{Single}" /> into two <see cref="Vector512{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<double> Lower, Vector512<double> Upper) Widen(Vector512<float> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{UInt16}" /> into two <see cref="Vector512{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<uint> Lower, Vector512<uint> Upper) Widen(Vector512<ushort> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector512{UInt32}" /> into two <see cref="Vector512{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector512<ulong> Lower, Vector512<ulong> Upper) Widen(Vector512<uint> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens the lower half of a <see cref="Vector512{Byte}" /> into a <see cref="Vector512{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> WidenLower(Vector512<byte> source)
        {
            Vector256<byte> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector512{Int16}" /> into a <see cref="Vector512{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> WidenLower(Vector512<short> source)
        {
            Vector256<short> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector512{Int32}" /> into a <see cref="Vector512{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> WidenLower(Vector512<int> source)
        {
            Vector256<int> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector512{SByte}" /> into a <see cref="Vector512{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> WidenLower(Vector512<sbyte> source)
        {
            Vector256<sbyte> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }
        /// <summary>Widens the lower half of a <see cref="Vector512{Single}" /> into a <see cref="Vector512{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> WidenLower(Vector512<float> source)
        {
            Vector256<float> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector512{UInt16}" /> into a <see cref="Vector512{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> WidenLower(Vector512<ushort> source)
        {
            Vector256<ushort> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector512{UInt32}" /> into a <see cref="Vector512{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> WidenLower(Vector512<uint> source)
        {
            Vector256<uint> lower = source._lower;

            return Create(
                Vector256.WidenLower(lower),
                Vector256.WidenUpper(lower)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{Byte}" /> into a <see cref="Vector512{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ushort> WidenUpper(Vector512<byte> source)
        {
            Vector256<byte> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{Int16}" /> into a <see cref="Vector512{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> WidenUpper(Vector512<short> source)
        {
            Vector256<short> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{Int32}" /> into a <see cref="Vector512{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<long> WidenUpper(Vector512<int> source)
        {
            Vector256<int> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{SByte}" /> into a <see cref="Vector512{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<short> WidenUpper(Vector512<sbyte> source)
        {
            Vector256<sbyte> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{Single}" /> into a <see cref="Vector512{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> WidenUpper(Vector512<float> source)
        {
            Vector256<float> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{UInt16}" /> into a <see cref="Vector512{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> WidenUpper(Vector512<ushort> source)
        {
            Vector256<ushort> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector512{UInt32}" /> into a <see cref="Vector512{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<ulong> WidenUpper(Vector512<uint> source)
        {
            Vector256<uint> upper = source._upper;

            return Create(
                Vector256.WidenLower(upper),
                Vector256.WidenUpper(upper)
            );
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector512{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector512<T> WithElement<T>(this Vector512<T> vector, int index, T value)
        {
            if ((uint)(index) >= (uint)(Vector512<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector512<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> with the lower 256-bits set to the specified value and the upper 256-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 256-bits from.</param>
        /// <param name="value">The value of the lower 256-bits as a <see cref="Vector256{T}" />.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with the lower 256-bits set to <paramref name="value" /> and the upper 256-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> WithLower<T>(this Vector512<T> vector, Vector256<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            Vector512<T> result = vector;
            result.SetLowerUnsafe(value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> with the upper 256-bits set to the specified value and the lower 256-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 256-bits from.</param>
        /// <param name="value">The value of the upper 256-bits as a <see cref="Vector256{T}" />.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with the upper 256-bits set to <paramref name="value" /> and the lower 256-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> WithUpper<T>(this Vector512<T> vector, Vector256<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            Vector512<T> result = vector;
            result.SetUpperUnsafe(value);
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
        public static Vector512<T> Xor<T>(Vector512<T> left, Vector512<T> right) => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector512<T> vector, int index)
        {
            Debug.Assert((index >= 0) && (index < Vector512<T>.Count));
            ref T address = ref Unsafe.As<Vector512<T>, T>(ref Unsafe.AsRef(in vector));
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector512<T> vector, int index, T value)
        {
            Debug.Assert((index >= 0) && (index < Vector512<T>.Count));
            ref T address = ref Unsafe.As<Vector512<T>, T>(ref Unsafe.AsRef(in vector));
            Unsafe.Add(ref address, index) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLowerUnsafe<T>(in this Vector512<T> vector, Vector256<T> value)
        {
            Unsafe.AsRef(in vector._lower) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetUpperUnsafe<T>(in this Vector512<T> vector, Vector256<T> value)
        {
            Unsafe.AsRef(in vector._upper) = value;
        }
    }
}

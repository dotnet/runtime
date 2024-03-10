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

    /// <summary>Provides a collection of static methods for creating, manipulating, and otherwise operating on 256-bit vectors.</summary>
    public static unsafe class Vector256
    {
        internal const int Size = 32;

#if TARGET_ARM
        internal const int Alignment = 8;
#elif TARGET_ARM64
        internal const int Alignment = 16;
#else
        internal const int Alignment = 32;
#endif

        /// <summary>Gets a value that indicates whether 256-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 256-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>256-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 256-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
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
        public static Vector256<T> Abs<T>(Vector256<T> vector)
        {
            return Create(
                Vector128.Abs(vector._lower),
                Vector128.Abs(vector._upper)
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
        public static Vector256<T> Add<T>(Vector256<T> left, Vector256<T> right) => left + right;

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> AndNot<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.AndNot(left._lower, right._lower),
                Vector128.AndNot(left._upper, right._upper)
            );
        }

        /// <summary>Reinterprets a <see cref="Vector256{TFrom}" /> as a new <see cref="Vector256{TTo}" />.</summary>
        /// <typeparam name="TFrom">The type of the elements in the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the elements in the output vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{TTo}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<TTo> As<TFrom, TTo>(this Vector256<TFrom> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<TTo>();

            return Unsafe.As<Vector256<TFrom>, Vector256<TTo>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Byte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> AsByte<T>(this Vector256<T> vector) => vector.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Double}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> AsDouble<T>(this Vector256<T> vector) => vector.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> AsInt16<T>(this Vector256<T> vector) => vector.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> AsInt32<T>(this Vector256<T> vector) => vector.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> AsInt64<T>(this Vector256<T> vector) => vector.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{IntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> AsNInt<T>(this Vector256<T> vector) => vector.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UIntPtr}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> AsNUInt<T>(this Vector256<T> vector) => vector.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{SByte}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> AsSByte<T>(this Vector256<T> vector) => vector.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Single}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> AsSingle<T>(this Vector256<T> vector) => vector.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> AsUInt16<T>(this Vector256<T> vector) => vector.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> AsUInt32<T>(this Vector256<T> vector) => vector.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> AsUInt64<T>(this Vector256<T> vector) => vector.As<T, ulong>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector256{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector256{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> AsVector256<T>(this Vector<T> value)
        {
            Debug.Assert(Vector256<T>.Count >= Vector<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            Vector256<T> result = default;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<T>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AsVector<T>(this Vector256<T> value)
        {
            Debug.Assert(Vector256<T>.Count >= Vector<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            ref byte address = ref Unsafe.As<Vector256<T>, byte>(ref value);
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
        public static Vector256<T> BitwiseAnd<T>(Vector256<T> left, Vector256<T> right) => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> BitwiseOr<T>(Vector256<T> left, Vector256<T> right) => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<T> Ceiling<T>(Vector256<T> vector)
        {
            return Create(
                Vector128.Ceiling(vector._lower),
                Vector128.Ceiling(vector._upper)
            );
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Ceiling(Vector256<float> vector) => Ceiling<float>(vector);

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Ceiling(Vector256<double> vector) => Ceiling<double>(vector);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="condition" />, <paramref name="left" />, and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> ConditionalSelect<T>(Vector256<T> condition, Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.ConditionalSelect(condition._lower, left._lower, right._lower),
                Vector128.ConditionalSelect(condition._upper, left._upper, right._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector256{Int64}" /> to a <see cref="Vector256{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> ConvertToDouble(Vector256<long> vector)
        {
            if (Avx2.IsSupported)
            {
                // Based on __m256d int64_to_double_fast_precise(const __m256i v)
                // from https://stackoverflow.com/a/41223013/12860347. CC BY-SA 4.0

                Vector256<int> lowerBits;

                lowerBits = vector.AsInt32();
                lowerBits = Avx2.Blend(lowerBits, Create(0x43300000_00000000).AsInt32(), 0b10101010);           // Blend the 32 lowest significant bits of vector with the bit representation of double(2^52)

                Vector256<long> upperBits = Avx2.ShiftRightLogical(vector, 32);                                             // Extract the 32 most significant bits of vector
                upperBits = Avx2.Xor(upperBits, Create(0x45300000_80000000));                                   // Flip the msb of upperBits and blend with the bit representation of double(2^84 + 2^63)

                Vector256<double> result = Avx.Subtract(upperBits.AsDouble(), Create(0x45300000_80100000).AsDouble());        // Compute in double precision: (upper - (2^84 + 2^63 + 2^52)) + lower
                return Avx.Add(result, lowerBits.AsDouble());
            }
            else
            {
                return Create(
                    Vector128.ConvertToDouble(vector._lower),
                    Vector128.ConvertToDouble(vector._upper)
                );
            }
        }

        /// <summary>Converts a <see cref="Vector256{UInt64}" /> to a <see cref="Vector256{Double}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> ConvertToDouble(Vector256<ulong> vector)
        {
            if (Avx2.IsSupported)
            {
                // Based on __m256d uint64_to_double_fast_precise(const __m256i v)
                // from https://stackoverflow.com/a/41223013/12860347. CC BY-SA 4.0

                Vector256<uint> lowerBits;

                lowerBits = vector.AsUInt32();
                lowerBits = Avx2.Blend(lowerBits, Create(0x43300000_00000000UL).AsUInt32(), 0b10101010);        // Blend the 32 lowest significant bits of vector with the bit representation of double(2^52)                                                 */

                Vector256<ulong> upperBits = Avx2.ShiftRightLogical(vector, 32);                                             // Extract the 32 most significant bits of vector
                upperBits = Avx2.Xor(upperBits, Create(0x45300000_00000000UL));                                 // Blend upperBits with the bit representation of double(2^84)

                Vector256<double> result = Avx.Subtract(upperBits.AsDouble(), Create(0x45300000_00100000UL).AsDouble());      // Compute in double precision: (upper - (2^84 + 2^52)) + lower
                return Avx.Add(result, lowerBits.AsDouble());
            }
            else
            {
                return Create(
                    Vector128.ConvertToDouble(vector._lower),
                    Vector128.ConvertToDouble(vector._upper)
                );
            }
        }

        /// <summary>Converts a <see cref="Vector256{Single}" /> to a <see cref="Vector256{Int32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> ConvertToInt32(Vector256<float> vector)
        {
            return Create(
                Vector128.ConvertToInt32(vector._lower),
                Vector128.ConvertToInt32(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector256{Double}" /> to a <see cref="Vector256{Int64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> ConvertToInt64(Vector256<double> vector)
        {
            return Create(
                Vector128.ConvertToInt64(vector._lower),
                Vector128.ConvertToInt64(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector256{Int32}" /> to a <see cref="Vector256{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> ConvertToSingle(Vector256<int> vector)
        {
            return Create(
                Vector128.ConvertToSingle(vector._lower),
                Vector128.ConvertToSingle(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector256{UInt32}" /> to a <see cref="Vector256{Single}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> ConvertToSingle(Vector256<uint> vector)
        {
            if (Avx2.IsSupported)
            {
                // This first bit of magic works because float can exactly represent integers up to 2^24
                //
                // This means everything between 0 and 2^16 (ushort.MaxValue + 1) are exact and so
                // converting each of the upper and lower halves will give an exact result

                Vector256<int> lowerBits = Avx2.And(vector, Create(0x0000FFFFU)).AsInt32();
                Vector256<int> upperBits = Avx2.ShiftRightLogical(vector, 16).AsInt32();

                Vector256<float> lower = Avx.ConvertToVector256Single(lowerBits);
                Vector256<float> upper = Avx.ConvertToVector256Single(upperBits);

                // This next bit of magic works because all multiples of 65536, at least up to 65535
                // are likewise exactly representable
                //
                // This means that scaling upper by 65536 gives us the exactly representable base value
                // and then the remaining lower value, which is likewise up to 65535 can be added on
                // giving us a result that will correctly round to the nearest representable value

                if (Fma.IsSupported)
                {
                    return Fma.MultiplyAdd(upper, Create(65536.0f), lower);
                }
                else
                {
                    Vector256<float> result = Avx.Multiply(upper, Create(65536.0f));
                    return Avx.Add(result, lower);
                }
            }
            else
            {
                return Create(
                    Vector128.ConvertToSingle(vector._lower),
                    Vector128.ConvertToSingle(vector._upper)
                );
            }
        }

        /// <summary>Converts a <see cref="Vector256{Single}" /> to a <see cref="Vector256{UInt32}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> ConvertToUInt32(Vector256<float> vector)
        {
            return Create(
                Vector128.ConvertToUInt32(vector._lower),
                Vector128.ConvertToUInt32(vector._upper)
            );
        }

        /// <summary>Converts a <see cref="Vector256{Double}" /> to a <see cref="Vector256{UInt64}" />.</summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> ConvertToUInt64(Vector256<double> vector)
        {
            return Create(
                Vector128.ConvertToUInt64(vector._lower),
                Vector128.ConvertToUInt64(vector._upper)
            );
        }

        /// <summary>Copies a <see cref="Vector256{T}" /> to a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector256<T> vector, T[] destination)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (destination.Length < Vector256<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), vector);
        }

        /// <summary>Copies a <see cref="Vector256{T}" /> to a given array starting at the specified index.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector256<T> vector, T[] destination, int startIndex)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Vector256<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), vector);
        }

        /// <summary>Copies a <see cref="Vector256{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which the <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector256<T> vector, Span<T> destination)
        {
            if (destination.Length < Vector256<T>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create<T>(T value)
        {
            Vector128<T> vector = Vector128.Create(value);
            return Create(vector, vector);
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi8</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> Create(byte value) => Create<byte>(value);

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256d _mm256_set1_pd</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Create(double value) => Create<double>(value);

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi16</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> Create(short value) => Create<short>(value);

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi32</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Create(int value) => Create<int>(value);

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi64x</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> Create(long value) => Create<long>(value);

        /// <summary>Creates a new <see cref="Vector256{IntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{IntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> Create(nint value) => Create<nint>(value);

        /// <summary>Creates a new <see cref="Vector256{UIntPtr}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UIntPtr}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> Create(nuint value) => Create<nuint>(value);

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi8</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> Create(sbyte value) => Create<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256 _mm256_set1_ps</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Create(float value) => Create<float>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi16</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> Create(ushort value) => Create<ushort>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi32</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> Create(uint value) => Create<uint>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_set1_epi64x</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> Create(ulong value) => Create<ulong>(value);

        /// <summary>Creates a new <see cref="Vector256{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with its elements set to the first <see cref="Vector256{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create<T>(T[] values)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (values.Length < Vector256<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref values[0]));
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> from a given array.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with its elements set to the first <see cref="Vector128{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create<T>(T[] values, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((index < 0) || ((values.Length - index) < Vector256<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with its elements set to the first <see cref="Vector256{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector256{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create<T>(ReadOnlySpan<T> values)
        {
            if (values.Length < Vector256<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector256{Byte}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi8</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> Create(byte e0,  byte e1,  byte e2,  byte e3,  byte e4,  byte e5,  byte e6,  byte e7,  byte e8,  byte e9,  byte e10, byte e11, byte e12, byte e13, byte e14, byte e15,
                                             byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
        {
            return Create(
                Vector128.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15),
                Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256d _mm256_setr_pd</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Create(double e0, double e1, double e2, double e3)
        {
            return Create(
                Vector128.Create(e0, e1),
                Vector128.Create(e2, e3)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector256{Int16}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi16</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
        {
            return Create(
                Vector128.Create(e0, e1, e2,  e3,  e4,  e5,  e6,  e7),
                Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi32</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
        {
            return Create(
                Vector128.Create(e0, e1, e2, e3),
                Vector128.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi64x</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> Create(long e0, long e1, long e2, long e3)
        {
            return Create(
                Vector128.Create(e0, e1),
                Vector128.Create(e2, e3)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector256{SByte}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi8</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> Create(sbyte e0,  sbyte e1,  sbyte e2,  sbyte e3,  sbyte e4,  sbyte e5,  sbyte e6,  sbyte e7,  sbyte e8,  sbyte e9,  sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15,
                                              sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
        {
            return Create(
                Vector128.Create(e0,  e1,  e2,  e3,  e4,  e5,  e6,  e7,  e8,  e9,  e10, e11, e12, e13, e14, e15),
                Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256 _mm256_setr_ps</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
        {
            return Create(
                Vector128.Create(e0, e1, e2, e3),
                Vector128.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector256{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi16</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
        {
            return Create(
                Vector128.Create(e0, e1, e2,  e3,  e4,  e5,  e6,  e7),
                Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi32</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
        {
            return Create(
                Vector128.Create(e0, e1, e2, e3),
                Vector128.Create(e4, e5, e6, e7)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_epi64x</remarks>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3)
        {
            return Create(
                Vector128.Create(e0, e1),
                Vector128.Create(e2, e3)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> instance from two <see cref="Vector128{T}" /> instances.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{T}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="lower" /> and <paramref name="upper" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create<T>(Vector128<T> lower, Vector128<T> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<T> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }
            else
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
                Unsafe.SkipInit(out Vector256<T> result);

                result.SetLowerUnsafe(lower);
                result.SetUpperUnsafe(upper);

                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance from two <see cref="Vector128{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> Create(Vector128<byte> lower, Vector128<byte> upper) => Create<byte>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance from two <see cref="Vector128{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256d _mm256_setr_m128d (__m128d lo, __m128d hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Create(Vector128<double> lower, Vector128<double> upper) => Create<double>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance from two <see cref="Vector128{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> Create(Vector128<short> lower, Vector128<short> upper) => Create<short>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance from two <see cref="Vector128{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_m128i (__m128i lo, __m128i hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Create(Vector128<int> lower, Vector128<int> upper) => Create<int>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance from two <see cref="Vector128{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> Create(Vector128<long> lower, Vector128<long> upper) => Create<long>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{IntPtr}" /> instance from two <see cref="Vector128{IntPtr}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{IntPtr}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> Create(Vector128<nint> lower, Vector128<nint> upper) => Create<nint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{UIntPtr}" /> instance from two <see cref="Vector128{UIntPtr}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UIntPtr}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> Create(Vector128<nuint> lower, Vector128<nuint> upper) => Create<nuint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance from two <see cref="Vector128{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> Create(Vector128<sbyte> lower, Vector128<sbyte> upper) => Create<sbyte>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance from two <see cref="Vector128{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256 _mm256_setr_m128 (__m128 lo, __m128 hi)</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Create(Vector128<float> lower, Vector128<float> upper) => Create<float>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance from two <see cref="Vector128{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> Create(Vector128<ushort> lower, Vector128<ushort> upper) => Create<ushort>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance from two <see cref="Vector128{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        /// <remarks>On x86, this method corresponds to __m256i _mm256_setr_m128i (__m128i lo, __m128i hi)</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> Create(Vector128<uint> lower, Vector128<uint> upper) => Create<uint>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance from two <see cref="Vector128{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> Create(Vector128<ulong> lower, Vector128<ulong> upper) => Create<ulong>(lower, upper);

        /// <summary>Creates a new <see cref="Vector256{T}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> CreateScalar<T>(T value) => Vector128.CreateScalar(value).ToVector256();

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> CreateScalar(byte value) => CreateScalar<byte>(value);

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> CreateScalar(double value) => CreateScalar<double>(value);

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> CreateScalar(short value) => CreateScalar<short>(value);

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> CreateScalar(int value) => CreateScalar<int>(value);

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> CreateScalar(long value) => CreateScalar<long>(value);

        /// <summary>Creates a new <see cref="Vector256{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> CreateScalar(nint value) => CreateScalar<nint>(value);

        /// <summary>Creates a new <see cref="Vector256{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> CreateScalar(nuint value) => CreateScalar<nuint>(value);

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> CreateScalar(sbyte value) => CreateScalar<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> CreateScalar(float value) => CreateScalar<float>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> CreateScalar(ushort value) => CreateScalar<ushort>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> CreateScalar(uint value) => CreateScalar<uint>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> CreateScalar(ulong value) => CreateScalar<ulong>(value);

        /// <summary>Creates a new <see cref="Vector256{T}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> CreateScalarUnsafe<T>(T value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            Unsafe.SkipInit(out Vector256<T> result);

            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> CreateScalarUnsafe(byte value) => CreateScalarUnsafe<byte>(value);

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> CreateScalarUnsafe(double value) => CreateScalarUnsafe<double>(value);

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> CreateScalarUnsafe(short value) => CreateScalarUnsafe<short>(value);

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> CreateScalarUnsafe(int value) => CreateScalarUnsafe<int>(value);

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> CreateScalarUnsafe(long value) => CreateScalarUnsafe<long>(value);

        /// <summary>Creates a new <see cref="Vector256{IntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{IntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> CreateScalarUnsafe(nint value) => CreateScalarUnsafe<nint>(value);

        /// <summary>Creates a new <see cref="Vector256{UIntPtr}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UIntPtr}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> CreateScalarUnsafe(nuint value) => CreateScalarUnsafe<nuint>(value);

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> CreateScalarUnsafe(sbyte value) => CreateScalarUnsafe<sbyte>(value);

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> CreateScalarUnsafe(float value) => CreateScalarUnsafe<float>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> CreateScalarUnsafe(ushort value) => CreateScalarUnsafe<ushort>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> CreateScalarUnsafe(uint value) => CreateScalarUnsafe<uint>(value);

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> CreateScalarUnsafe(ulong value) => CreateScalarUnsafe<ulong>(value);

        /// <summary>Creates a new <see cref="Vector256{T}" /> instance where the elements begin at a specified value and which are spaced apart according to another specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="start">The value that element 0 will be initialized to.</param>
        /// <param name="step">The value that indicates how far apart each element should be from the previous.</param>
        /// <returns>A new <see cref="Vector256{T}" /> instance with the first element initialized to <paramref name="start" /> and each subsequent element initialized to the the value of the previous element plus <paramref name="step" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> CreateSequence<T>(T start, T step) => (Vector256<T>.Indices * step) + Create(start);

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Divide<T>(Vector256<T> left, Vector256<T> right) => left / right;

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Divide<T>(Vector256<T> left, T right) => left / right;

        /// <summary>Computes the dot product of two vectors.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector that will be dotted with <paramref name="right" />.</param>
        /// <param name="right">The vector that will be dotted with <paramref name="left" />.</param>
        /// <returns>The dot product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Dot<T>(Vector256<T> left, Vector256<T> right)
        {
            // Doing this as Dot(lower) + Dot(upper) is important for floating-point determinism
            // This is because the underlying dpps instruction on x86/x64 will do this equivalently
            // and otherwise the software vs accelerated implementations may differ in returned result.

            T result = Vector128.Dot(left._lower, right._lower);
            result = Scalar<T>.Add(result, Vector128.Dot(left._upper, right._upper));
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
        public static Vector256<T> Equals<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.Equals(left._lower, right._lower),
                Vector128.Equals(left._upper, right._upper)
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
        public static bool EqualsAll<T>(Vector256<T> left, Vector256<T> right) => left == right;

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.EqualsAny(left._lower, right._lower)
                || Vector128.EqualsAny(left._upper, right._upper);
        }

        /// <inheritdoc cref="Vector128.Exp(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Exp(Vector256<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpDouble<Vector256<double>, Vector256<long>, Vector256<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Exp(vector._lower),
                    Vector128.Exp(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector128.Exp(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Exp(Vector256<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpSingle<Vector256<float>, Vector256<uint>, Vector256<double>, Vector256<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Exp(vector._lower),
                    Vector128.Exp(vector._upper)
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
        public static uint ExtractMostSignificantBits<T>(this Vector256<T> vector)
        {
            uint result = vector._lower.ExtractMostSignificantBits();
            result |= vector._upper.ExtractMostSignificantBits() << Vector128<T>.Count;
            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<T> Floor<T>(Vector256<T> vector)
        {
            return Create(
                Vector128.Floor(vector._lower),
                Vector128.Floor(vector._upper)
            );
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="MathF.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Floor(Vector256<float> vector) => Floor<float>(vector);

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        /// <seealso cref="Math.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Floor(Vector256<double> vector) => Floor<double>(vector);

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetElement<T>(this Vector256<T> vector, int index)
        {
            if ((uint)(index) >= (uint)(Vector256<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Gets the value of the lower 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 128-bits from.</param>
        /// <returns>The value of the lower 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GetLower<T>(this Vector256<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            return vector._lower;
        }

        /// <summary>Gets the value of the upper 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 128-bits from.</param>
        /// <returns>The value of the upper 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GetUpper<T>(this Vector256<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
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
        public static Vector256<T> GreaterThan<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.GreaterThan(left._lower, right._lower),
                Vector128.GreaterThan(left._upper, right._upper)
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
        public static bool GreaterThanAll<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.GreaterThanAll(left._lower, right._lower)
                && Vector128.GreaterThanAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanAny<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.GreaterThanAny(left._lower, right._lower)
                || Vector128.GreaterThanAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> GreaterThanOrEqual<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.GreaterThanOrEqual(left._lower, right._lower),
                Vector128.GreaterThanOrEqual(left._upper, right._upper)
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
        public static bool GreaterThanOrEqualAll<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.GreaterThanOrEqualAll(left._lower, right._lower)
                && Vector128.GreaterThanOrEqualAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqualAny<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.GreaterThanOrEqualAny(left._lower, right._lower)
                || Vector128.GreaterThanOrEqualAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> LessThan<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.LessThan(left._lower, right._lower),
                Vector128.LessThan(left._upper, right._upper)
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
        public static bool LessThanAll<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.LessThanAll(left._lower, right._lower)
                && Vector128.LessThanAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanAny<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.LessThanAny(left._lower, right._lower)
                || Vector128.LessThanAny(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> LessThanOrEqual<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.LessThanOrEqual(left._lower, right._lower),
                Vector128.LessThanOrEqual(left._upper, right._upper)
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
        public static bool LessThanOrEqualAll<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.LessThanOrEqualAll(left._lower, right._lower)
                && Vector128.LessThanOrEqualAll(left._upper, right._upper);
        }

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqualAny<T>(Vector256<T> left, Vector256<T> right)
        {
            return Vector128.LessThanOrEqualAny(left._lower, right._lower)
                || Vector128.LessThanOrEqualAny(left._upper, right._upper);
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
        public static Vector256<T> Load<T>(T* source) => LoadUnsafe(ref *source);

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> LoadAligned<T>(T* source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            if (((nuint)(source) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            return *(Vector256<T>*)(source);
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
        public static Vector256<T> LoadAlignedNonTemporal<T>(T* source) => LoadAligned(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> LoadUnsafe<T>(ref readonly T source)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in source));
            return Unsafe.ReadUnaligned<Vector256<T>>(in address);
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
        public static Vector256<T> LoadUnsafe<T>(ref readonly T source, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
            return Unsafe.ReadUnaligned<Vector256<T>>(in address);
        }

        /// <summary>Loads a vector from the given source and reinterprets it as <see cref="ushort"/>.</summary>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<ushort> LoadUnsafe(ref char source) =>
            LoadUnsafe(ref Unsafe.As<char, ushort>(ref source));

        /// <summary>Loads a vector from the given source and element offset and reinterprets it as <see cref="ushort"/>.</summary>
        /// <param name="source">The source to which <paramref name="elementOffset" /> will be added before loading the vector.</param>
        /// <param name="elementOffset">The element offset from <paramref name="source" /> from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" /> plus <paramref name="elementOffset" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<ushort> LoadUnsafe(ref char source, nuint elementOffset) =>
            LoadUnsafe(ref Unsafe.As<char, ushort>(ref source), elementOffset);

        /// <inheritdoc cref="Vector128.Log(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Log(Vector256<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogDouble<Vector256<double>, Vector256<long>, Vector256<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Log(vector._lower),
                    Vector128.Log(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector128.Log(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Log(Vector256<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogSingle<Vector256<float>, Vector256<int>, Vector256<uint>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Log(vector._lower),
                    Vector128.Log(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector128.Log2(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> Log2(Vector256<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Double<Vector256<double>, Vector256<long>, Vector256<ulong>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Log2(vector._lower),
                    Vector128.Log2(vector._upper)
                );
            }
        }

        /// <inheritdoc cref="Vector128.Log2(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Log2(Vector256<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Single<Vector256<float>, Vector256<int>, Vector256<uint>>(vector);
            }
            else
            {
                return Create(
                    Vector128.Log2(vector._lower),
                    Vector128.Log2(vector._upper)
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
        public static Vector256<T> Max<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.Max(left._lower, right._lower),
                Vector128.Max(left._upper, right._upper)
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
        public static Vector256<T> Min<T>(Vector256<T> left, Vector256<T> right)
        {
            return Create(
                Vector128.Min(left._lower, right._lower),
                Vector128.Min(left._upper, right._upper)
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
        public static Vector256<T> Multiply<T>(Vector256<T> left, Vector256<T> right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Multiply<T>(Vector256<T> left, T right) => left * right;

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="left" /> and <paramref name="right" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Multiply<T>(T left, Vector256<T> right) => left * right;

        /// <summary>Narrows two <see cref="Vector256{Double}"/> instances into one <see cref="Vector256{Single}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{Single}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Narrow(Vector256<double> lower, Vector256<double> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{Int16}"/> instances into one <see cref="Vector256{SByte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{SByte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> Narrow(Vector256<short> lower, Vector256<short> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{Int32}"/> instances into one <see cref="Vector256{Int16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{Int16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> Narrow(Vector256<int> lower, Vector256<int> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{Int64}"/> instances into one <see cref="Vector256{Int32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{Int32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Narrow(Vector256<long> lower, Vector256<long> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{UInt16}"/> instances into one <see cref="Vector256{Byte}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{Byte}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> Narrow(Vector256<ushort> lower, Vector256<ushort> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{UInt32}"/> instances into one <see cref="Vector256{UInt16}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{UInt16}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> Narrow(Vector256<uint> lower, Vector256<uint> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Narrows two <see cref="Vector256{UInt64}"/> instances into one <see cref="Vector256{UInt32}" />.</summary>
        /// <param name="lower">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="upper">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see cref="Vector256{UInt32}"/> containing elements narrowed from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> Narrow(Vector256<ulong> lower, Vector256<ulong> upper)
        {
            return Create(
                Vector128.Narrow(lower._lower, lower._upper),
                Vector128.Narrow(upper._lower, upper._upper)
            );
        }

        /// <summary>Negates a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Negate<T>(Vector256<T> vector) => -vector;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> OnesComplement<T>(Vector256<T> vector)
        {
            return Create(
                Vector128.OnesComplement(vector._lower),
                Vector128.OnesComplement(vector._upper)
            );
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<T> ShiftLeft<T>(Vector256<T> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> ShiftLeft(Vector256<byte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> ShiftLeft(Vector256<short> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> ShiftLeft(Vector256<int> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> ShiftLeft(Vector256<long> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> ShiftLeft(Vector256<nint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> ShiftLeft(Vector256<nuint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> ShiftLeft(Vector256<sbyte> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> ShiftLeft(Vector256<ushort> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> ShiftLeft(Vector256<uint> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> ShiftLeft(Vector256<ulong> vector, int shiftCount) => vector << shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<T> ShiftRightArithmetic<T>(Vector256<T> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> ShiftRightArithmetic(Vector256<nint> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> vector, int shiftCount) => vector >> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<T> ShiftRightLogical<T>(Vector256<T> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> ShiftRightLogical(Vector256<short> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> ShiftRightLogical(Vector256<int> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> ShiftRightLogical(Vector256<long> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nint> ShiftRightLogical(Vector256<nint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<nuint> ShiftRightLogical(Vector256<nuint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="vector">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> vector, int shiftCount) => vector >>> shiftCount;

        /// <summary>Creates a new vector by selecting values from an input vector using a set of indices.</summary>
        /// <param name="vector">The input vector from which values are selected.</param>
        /// <param name="indices">The per-element indices used to select a value from <paramref name="vector" />.</param>
        /// <returns>A new vector containing the values from <paramref name="vector" /> selected by the given <paramref name="indices" />.</returns>
        [Intrinsic]
        public static Vector256<byte> Shuffle(Vector256<byte> vector, Vector256<byte> indices)
        {
            Unsafe.SkipInit(out Vector256<byte> result);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                byte selectedIndex = indices.GetElementUnsafe(index);
                byte selectedValue = 0;

                if (selectedIndex < Vector256<byte>.Count)
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
        public static Vector256<sbyte> Shuffle(Vector256<sbyte> vector, Vector256<sbyte> indices)
        {
            Unsafe.SkipInit(out Vector256<sbyte> result);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                byte selectedIndex = (byte)indices.GetElementUnsafe(index);
                sbyte selectedValue = 0;

                if (selectedIndex < Vector256<sbyte>.Count)
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
        public static Vector256<short> Shuffle(Vector256<short> vector, Vector256<short> indices)
        {
            Unsafe.SkipInit(out Vector256<short> result);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                ushort selectedIndex = (ushort)indices.GetElementUnsafe(index);
                short selectedValue = 0;

                if (selectedIndex < Vector256<short>.Count)
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
        public static Vector256<ushort> Shuffle(Vector256<ushort> vector, Vector256<ushort> indices)
        {
            Unsafe.SkipInit(out Vector256<ushort> result);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                ushort selectedIndex = indices.GetElementUnsafe(index);
                ushort selectedValue = 0;

                if (selectedIndex < Vector256<ushort>.Count)
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
        public static Vector256<int> Shuffle(Vector256<int> vector, Vector256<int> indices)
        {
            Unsafe.SkipInit(out Vector256<int> result);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                int selectedValue = 0;

                if (selectedIndex < Vector256<int>.Count)
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
        public static Vector256<uint> Shuffle(Vector256<uint> vector, Vector256<uint> indices)
        {
            Unsafe.SkipInit(out Vector256<uint> result);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                uint selectedIndex = indices.GetElementUnsafe(index);
                uint selectedValue = 0;

                if (selectedIndex < Vector256<uint>.Count)
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
        public static Vector256<float> Shuffle(Vector256<float> vector, Vector256<int> indices)
        {
            Unsafe.SkipInit(out Vector256<float> result);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                uint selectedIndex = (uint)indices.GetElementUnsafe(index);
                float selectedValue = 0;

                if (selectedIndex < Vector256<float>.Count)
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
        public static Vector256<long> Shuffle(Vector256<long> vector, Vector256<long> indices)
        {
            Unsafe.SkipInit(out Vector256<long> result);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                long selectedValue = 0;

                if (selectedIndex < (uint)Vector256<long>.Count)
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
        public static Vector256<ulong> Shuffle(Vector256<ulong> vector, Vector256<ulong> indices)
        {
            Unsafe.SkipInit(out Vector256<ulong> result);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                ulong selectedIndex = indices.GetElementUnsafe(index);
                ulong selectedValue = 0;

                if (selectedIndex < (uint)Vector256<ulong>.Count)
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
        public static Vector256<double> Shuffle(Vector256<double> vector, Vector256<long> indices)
        {
            Unsafe.SkipInit(out Vector256<double> result);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                ulong selectedIndex = (ulong)indices.GetElementUnsafe(index);
                double selectedValue = 0;

                if (selectedIndex < (uint)Vector256<double>.Count)
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
        public static Vector256<T> Sqrt<T>(Vector256<T> vector)
        {
            return Create(
                Vector128.Sqrt(vector._lower),
                Vector128.Sqrt(vector._upper)
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
        public static void Store<T>(this Vector256<T> source, T* destination) => source.StoreUnsafe(ref *destination);

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreAligned<T>(this Vector256<T> source, T* destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            if (((nuint)(destination) % Alignment) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            *(Vector256<T>*)(destination) = source;
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
        public static void StoreAlignedNonTemporal<T>(this Vector256<T> source, T* destination) => source.StoreAligned(destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector256<T> source, ref T destination)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
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
        public static void StoreUnsafe<T>(this Vector256<T> source, ref T destination, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
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
        public static Vector256<T> Subtract<T>(Vector256<T> left, Vector256<T> right) => left - right;

        /// <summary>Computes the sum of all elements in a vector.</summary>
        /// <param name="vector">The vector whose elements will be summed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The sum of all elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T>(Vector256<T> vector)
        {
            // Doing this as Sum(lower) + Sum(upper) is important for floating-point determinism
            // This is because the underlying dpps instruction on x86/x64 will do this equivalently
            // and otherwise the software vs accelerated implementations may differ in returned result.

            T result = Vector128.Sum(vector._lower);
            result = Scalar<T>.Add(result, Vector128.Sum(vector._upper));
            return result;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToScalar<T>(this Vector256<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector512{T}" /> with the lower 256-bits set to the value of the given vector and the upper 256-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with the lower 256-bits set to the value of <paramref name="vector" /> and the upper 256-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector512<T> ToVector512<T>(this Vector256<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            Vector512<T> result = default;
            result.SetLowerUnsafe(vector);
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector512{T}" /> with the lower 256-bits set to the value of the given vector and the upper 256-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector512{T}" /> with the lower 256-bits set to the value of <paramref name="vector" /> and the upper 256-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static unsafe Vector512<T> ToVector512Unsafe<T>(this Vector256<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector512<T> result);
            result.SetLowerUnsafe(vector);
            return result;
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector256{T}.Count" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> and <paramref name="destination" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCopyTo<T>(this Vector256<T> vector, Span<T> destination)
        {
            if (destination.Length < Vector256<T>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), vector);
            return true;
        }

        /// <summary>Widens a <see cref="Vector256{Byte}" /> into two <see cref="Vector256{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<ushort> Lower, Vector256<ushort> Upper) Widen(Vector256<byte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{Int16}" /> into two <see cref="Vector256{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<int> Lower, Vector256<int> Upper) Widen(Vector256<short> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{Int32}" /> into two <see cref="Vector256{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<long> Lower, Vector256<long> Upper) Widen(Vector256<int> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{SByte}" /> into two <see cref="Vector256{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<short> Lower, Vector256<short> Upper) Widen(Vector256<sbyte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{Single}" /> into two <see cref="Vector256{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<double> Lower, Vector256<double> Upper) Widen(Vector256<float> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{UInt16}" /> into two <see cref="Vector256{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<uint> Lower, Vector256<uint> Upper) Widen(Vector256<ushort> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see cref="Vector256{UInt32}" /> into two <see cref="Vector256{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector256<ulong> Lower, Vector256<ulong> Upper) Widen(Vector256<uint> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens the lower half of a <see cref="Vector256{Byte}" /> into a <see cref="Vector256{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> WidenLower(Vector256<byte> source)
        {
            Vector128<byte> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector256{Int16}" /> into a <see cref="Vector256{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> WidenLower(Vector256<short> source)
        {
            Vector128<short> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector256{Int32}" /> into a <see cref="Vector256{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> WidenLower(Vector256<int> source)
        {
            Vector128<int> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector256{SByte}" /> into a <see cref="Vector256{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> WidenLower(Vector256<sbyte> source)
        {
            Vector128<sbyte> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }
        /// <summary>Widens the lower half of a <see cref="Vector256{Single}" /> into a <see cref="Vector256{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> WidenLower(Vector256<float> source)
        {
            Vector128<float> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector256{UInt16}" /> into a <see cref="Vector256{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> WidenLower(Vector256<ushort> source)
        {
            Vector128<ushort> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the lower half of a <see cref="Vector256{UInt32}" /> into a <see cref="Vector256{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> WidenLower(Vector256<uint> source)
        {
            Vector128<uint> lower = source._lower;

            return Create(
                Vector128.WidenLower(lower),
                Vector128.WidenUpper(lower)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{Byte}" /> into a <see cref="Vector256{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> WidenUpper(Vector256<byte> source)
        {
            Vector128<byte> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{Int16}" /> into a <see cref="Vector256{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> WidenUpper(Vector256<short> source)
        {
            Vector128<short> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{Int32}" /> into a <see cref="Vector256{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<long> WidenUpper(Vector256<int> source)
        {
            Vector128<int> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{SByte}" /> into a <see cref="Vector256{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<short> WidenUpper(Vector256<sbyte> source)
        {
            Vector128<sbyte> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{Single}" /> into a <see cref="Vector256{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> WidenUpper(Vector256<float> source)
        {
            Vector128<float> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{UInt16}" /> into a <see cref="Vector256{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> WidenUpper(Vector256<ushort> source)
        {
            Vector128<ushort> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Widens the upper half of a <see cref="Vector256{UInt32}" /> into a <see cref="Vector256{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> WidenUpper(Vector256<uint> source)
        {
            Vector128<uint> upper = source._upper;

            return Create(
                Vector128.WidenLower(upper),
                Vector128.WidenUpper(upper)
            );
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector256{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<T> WithElement<T>(this Vector256<T> vector, int index, T value)
        {
            if ((uint)(index) >= (uint)(Vector256<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector256<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the lower 128-bits set to the specified value and the upper 128-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 128-bits from.</param>
        /// <param name="value">The value of the lower 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to <paramref name="value" /> and the upper 128-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> WithLower<T>(this Vector256<T> vector, Vector128<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            Vector256<T> result = vector;
            result.SetLowerUnsafe(value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the upper 128-bits set to the specified value and the lower 128-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 128-bits from.</param>
        /// <param name="value">The value of the upper 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the upper 128-bits set to <paramref name="value" /> and the lower 128-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> WithUpper<T>(this Vector256<T> vector, Vector128<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

            Vector256<T> result = vector;
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
        public static Vector256<T> Xor<T>(Vector256<T> left, Vector256<T> right) => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector256<T> vector, int index)
        {
            Debug.Assert((index >= 0) && (index < Vector256<T>.Count));
            ref T address = ref Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(in vector));
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector256<T> vector, int index, T value)
        {
            Debug.Assert((index >= 0) && (index < Vector256<T>.Count));
            ref T address = ref Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(in vector));
            Unsafe.Add(ref address, index) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLowerUnsafe<T>(in this Vector256<T> vector, Vector128<T> value)
        {
            Unsafe.AsRef(in vector._lower) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetUpperUnsafe<T>(in this Vector256<T> vector, Vector128<T> value)
        {
            Unsafe.AsRef(in vector._upper) = value;
        }
    }
}

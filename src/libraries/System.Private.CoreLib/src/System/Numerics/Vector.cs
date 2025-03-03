// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics
{
    /// <summary>Provides a collection of static methods for creating, manipulating, and otherwise operating on generic vectors.</summary>
    [Intrinsic]
    public static unsafe partial class Vector
    {
        internal static int Alignment => sizeof(Vector<byte>);

        /// <summary>Gets a value that indicates whether vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>Vector operations are subject to hardware acceleration on systems that support single instruction, multiple data (SIMD) instructions and when the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
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
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return value;
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T element = Scalar<T>.Abs(value.GetElementUnsafe(index));
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
        [Intrinsic]
        public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) => left + right;

        /// <inheritdoc cref="Vector128.All{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool All<T>(Vector<T> vector, T value) => vector == Create(value);

        /// <inheritdoc cref="Vector128.AllWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return All(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return All(vector.As<T, long>(), -1);
            }
            else
            {
                return All(vector, Scalar<T>.AllBitsSet);
            }
        }

        /// <summary>Computes the bitwise-and of a given vector and the ones complement of another vector.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to that is ones-complemented before being bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and the ones-complement of <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right) => left & ~right;

        /// <inheritdoc cref="Vector128.Any{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<T>(Vector<T> vector, T value) => EqualsAny(vector, Create(value));

        /// <inheritdoc cref="Vector128.AnyWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Any(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return Any(vector.As<T, long>(), -1);
            }
            else
            {
                return Any(vector, Scalar<T>.AllBitsSet);
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

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Byte&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Byte&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<byte> AsVectorByte<T>(Vector<T> value) => value.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Double&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Double&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<double> AsVectorDouble<T>(Vector<T> value) => value.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Int16&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Int16&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<short> AsVectorInt16<T>(Vector<T> value) => value.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Int32&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Int32&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<int> AsVectorInt32<T>(Vector<T> value) => value.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Int64&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Int64&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<long> AsVectorInt64<T>(Vector<T> value) => value.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;IntPtr&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;IntPtr&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<nint> AsVectorNInt<T>(Vector<T> value) => value.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;UIntPtr&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;UIntPtr&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value) => value.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;SByte&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;SByte&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) => value.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;Single&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;Single&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<float> AsVectorSingle<T>(Vector<T> value) => value.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;UInt16&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;UInt16&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) => value.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;UInt32&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;UInt32&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) => value.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see langword="Vector&lt;UInt64&gt;" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector&lt;UInt64&gt;" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) => value.As<T, ulong>();

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) => left & right;

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) => left | right;

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> Ceiling<T>(Vector<T> vector)
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
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Ceiling(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="double.Ceiling(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Ceiling(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                double element = Scalar<double>.Ceiling(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Computes the ceiling of each element in a vector.</summary>
        /// <param name="value">The vector that will have its ceiling computed.</param>
        /// <returns>A vector whose elements are the ceiling of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="float.Ceiling(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Ceiling(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                float element = Scalar<float>.Ceiling(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Clamp{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        [Intrinsic]
        public static Vector<T> Clamp<T>(Vector<T> value, Vector<T> min, Vector<T> max)
        {
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            return Min(Max(value, min), max);
        }

        /// <inheritdoc cref="Vector128.ClampNative{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        [Intrinsic]
        public static Vector<T> ClampNative<T>(Vector<T> value, Vector<T> min, Vector<T> max)
        {
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            return MinNative(MaxNative(value, min), max);
        }

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <remarks>The returned vector is equivalent to <paramref name="condition" /> <c>?</c> <paramref name="left" /> <c>:</c> <paramref name="right" /> on a per-bit basis.</remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right) => (left & condition) | AndNot(right, condition);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <remarks>The returned vector is equivalent to <paramref name="condition" /> <c>?</c> <paramref name="left" /> <c>:</c> <paramref name="right" /> on a per-bit basis.</remarks>
        [Intrinsic]
        public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right) => ConditionalSelect(condition.As<int, float>(), left, right);

        /// <summary>Conditionally selects a value from two vectors on a bitwise basis.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <remarks>The returned vector is equivalent to <paramref name="condition" /> <c>?</c> <paramref name="left" /> <c>:</c> <paramref name="right" /> on a per-bit basis.</remarks>
        [Intrinsic]
        public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right) => ConditionalSelect(condition.As<long, double>(), left, right);

        /// <summary>Converts a <see langword="Vector&lt;Int64&gt;" /> to a <see langword="Vector&lt;Double&gt;" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConvertToDouble(Vector<long> value)
        {
            if (sizeof(Vector<double>) == sizeof(Vector512<double>))
            {
                return Vector512.ConvertToDouble(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == sizeof(Vector256<double>))
            {
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == sizeof(Vector128<double>));
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see langword="Vector&lt;UInt64&gt;" /> to a <see langword="Vector&lt;Double&gt;" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> ConvertToDouble(Vector<ulong> value)
        {
            if (sizeof(Vector<double>) == sizeof(Vector512<double>))
            {
                return Vector512.ConvertToDouble(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<double>) == sizeof(Vector256<double>))
            {
                return Vector256.ConvertToDouble(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<double>) == sizeof(Vector128<double>));
                return Vector128.ConvertToDouble(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see langword="Vector&lt;Single&gt;" /> to a <see langword="Vector&lt;Int32&gt;" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static Vector<int> ConvertToInt32(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<int> result);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                int element = float.ConvertToInteger<int>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Single&gt;" /> to a <see langword="Vector&lt;Int32&gt;" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static Vector<int> ConvertToInt32Native(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<int> result);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                int element = float.ConvertToIntegerNative<int>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Double&gt;" /> to a <see langword="Vector&lt;Int64&gt;" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static Vector<long> ConvertToInt64(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<long> result);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                long element = double.ConvertToInteger<long>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Double&gt;" /> to a <see langword="Vector&lt;Int64&gt;" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static Vector<long> ConvertToInt64Native(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<long> result);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                long element = double.ConvertToIntegerNative<long>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Int32&gt;" /> to a <see langword="Vector&lt;Single&gt;" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        public static Vector<float> ConvertToSingle(Vector<int> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int i = 0; i < Vector<float>.Count; i++)
            {
                float element = value.GetElementUnsafe(i);
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;UInt32&gt;" /> to a <see langword="Vector&lt;Single&gt;" />.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> ConvertToSingle(Vector<uint> value)
        {
            if (sizeof(Vector<float>) == sizeof(Vector512<float>))
            {
                return Vector512.ConvertToSingle(value.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<float>) == sizeof(Vector256<float>))
            {
                return Vector256.ConvertToSingle(value.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<float>) == sizeof(Vector128<float>));
                return Vector128.ConvertToSingle(value.AsVector128()).AsVector();
            }
        }

        /// <summary>Converts a <see langword="Vector&lt;Single&gt;" /> to a <see langword="Vector&lt;UInt32&gt;" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> ConvertToUInt32(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<uint> result);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                uint element = float.ConvertToInteger<uint>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Single&gt;" /> to a <see langword="Vector&lt;UInt32&gt;" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<uint> ConvertToUInt32Native(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<uint> result);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                uint element = float.ConvertToIntegerNative<uint>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Double&gt;" /> to a <see langword="Vector&lt;UInt64&gt;" /> using saturation on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> ConvertToUInt64(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<ulong> result);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                ulong element = double.ConvertToInteger<ulong>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        /// <summary>Converts a <see langword="Vector&lt;Double&gt;" /> to a <see langword="Vector&lt;UInt64&gt;" /> using platform specific behavior on overflow.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> ConvertToUInt64Native(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<ulong> result);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                ulong element = double.ConvertToIntegerNative<ulong>(value.GetElementUnsafe(i));
                result.SetElementUnsafe(i, element);
            }

            return result;
        }

        internal static Vector<T> Cos<T>(Vector<T> vector)
            where T : ITrigonometricFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Cos(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Cos(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Cos(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.CosDouble<Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return Cos<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Cos(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Cos(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.CosSingle<Vector<float>, Vector<int>, Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return Cos<float>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.CopySign{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> CopySign<T>(Vector<T> value, Vector<T> sign)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return value;
            }
            else if (IsHardwareAccelerated)
            {
                return VectorMath.CopySign<Vector<T>, T>(value, sign);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T element = Scalar<T>.CopySign(value.GetElementUnsafe(index), sign.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, element);
                }

                return result;
            }
        }



        /// <inheritdoc cref="Vector128.Count{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<T>(Vector<T> vector, T value)
        {
            if (sizeof(Vector<T>) == sizeof(Vector512<T>))
            {
                return Vector512.Count(vector.AsVector512(), value);
            }
            else if (sizeof(Vector<T>) == sizeof(Vector256<T>))
            {
                return Vector256.Count(vector.AsVector256(), value);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == sizeof(Vector128<T>));
                return Vector128.Count(vector.AsVector128(), value);
            }
        }

        /// <inheritdoc cref="Vector128.CountWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Count(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return Count(vector.As<T, long>(), -1);
            }
            else
            {
                return Count(vector, Scalar<T>.AllBitsSet);
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<T> Create<T>(T value)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given readonly span.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="values" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Create<T>(ReadOnlySpan<T> values)
        {
            if (values.Length < Vector<T>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }
            return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        internal static Vector<T> CreateScalar<T>(T value)
        {
            Vector<T> result = Vector<T>.Zero;
            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> CreateScalarUnsafe<T>(T value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<T> result);

            result.SetElementUnsafe(0, value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> instance where the elements begin at a specified value and which are spaced apart according to another specified value.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="start">The value that element 0 will be initialized to.</param>
        /// <param name="step">The value that indicates how far apart each element should be from the previous.</param>
        /// <returns>A new <see cref="Vector{T}" /> instance with the first element initialized to <paramref name="start" /> and each subsequent element initialized to the value of the previous element plus <paramref name="step" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> CreateSequence<T>(T start, T step) => (Vector<T>.Indices * step) + Create(start);

        internal static Vector<T> DegreesToRadians<T>(Vector<T> degrees)
            where T : ITrigonometricFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.DegreesToRadians(degrees.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.DegreesToRadians(Vector128{double})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> DegreesToRadians(Vector<double> degrees)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.DegreesToRadians<Vector<double>, double>(degrees);
            }
            else
            {
                return DegreesToRadians<double>(degrees);
            }
        }

        /// <inheritdoc cref="Vector128.DegreesToRadians(Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> DegreesToRadians(Vector<float> degrees)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.DegreesToRadians<Vector<float>, float>(degrees);
            }
            else
            {
                return DegreesToRadians<float>(degrees);
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
        public static T Dot<T>(Vector<T> left, Vector<T> right) => Sum(left * right);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Vector<T> Exp<T>(Vector<T> vector)
            where T : IExponentialFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Exp(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Exp(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Exp(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpDouble<Vector<double>, Vector<ulong>>(vector);
            }
            else
            {
                return Exp<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Exp(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Exp(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.ExpSingle<Vector<float>, Vector<uint>, Vector<double>, Vector<ulong>>(vector);
            }
            else
            {
                return Exp<float>(vector);
            }
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="vector">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> Floor<T>(Vector<T> vector)
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
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Floor(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="double.Floor(double)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Floor(Vector<double> value)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                double element = Scalar<double>.Floor(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Computes the floor of each element in a vector.</summary>
        /// <param name="value">The vector that will have its floor computed.</param>
        /// <returns>A vector whose elements are the floor of the elements in <paramref name="value" />.</returns>
        /// <seealso cref="float.Floor(float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Floor(Vector<float> value)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                float element = Scalar<float>.Floor(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Computes (<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" />, rounded as one ternary operation.</summary>
        /// <param name="left">The vector to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The vector to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The vector to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" />, rounded as one ternary operation.</returns>
        /// <remarks>
        ///   <para>This computes (<paramref name="left" /> * <paramref name="right" />) as if to infinite precision, adds <paramref name="addend" /> to that result as if to infinite precision, and finally rounds to the nearest representable value.</para>
        ///   <para>This differs from the non-fused sequence which would compute (<paramref name="left" /> * <paramref name="right" />) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend" /> to the rounded result as if to infinite precision, and finally round to the nearest representable value.</para>
        /// </remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> FusedMultiplyAdd(Vector<double> left, Vector<double> right, Vector<double> addend)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                double value = double.FusedMultiplyAdd(left.GetElementUnsafe(index), right.GetElementUnsafe(index), addend.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes (<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" />, rounded as one ternary operation.</summary>
        /// <param name="left">The vector to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The vector to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The vector to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" />, rounded as one ternary operation.</returns>
        /// <remarks>
        ///   <para>This computes (<paramref name="left" /> * <paramref name="right" />) as if to infinite precision, adds <paramref name="addend" /> to that result as if to infinite precision, and finally rounds to the nearest representable value.</para>
        ///   <para>This differs from the non-fused sequence which would compute (<paramref name="left" /> * <paramref name="right" />) as if to infinite precision, round the result to the nearest representable value, add <paramref name="addend" /> to the rounded result as if to infinite precision, and finally round to the nearest representable value.</para>
        /// </remarks>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> FusedMultiplyAdd(Vector<float> left, Vector<float> right, Vector<float> addend)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                float value = float.FusedMultiplyAdd(left.GetElementUnsafe(index), right.GetElementUnsafe(index), addend.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            if ((uint)(index) >= (uint)(Vector<T>.Count))
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (!Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
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
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (!Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (Scalar<T>.GreaterThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Vector<T> Hypot<T>(Vector<T> x, Vector<T> y)
            where T : IRootFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Hypot(x.GetElementUnsafe(index), y.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Hypot(Vector128{double}, Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Hypot(Vector<double> x, Vector<double> y)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.HypotDouble<Vector<double>, Vector<ulong>>(x, y);
            }
            else
            {
                return Hypot<double>(x, y);
            }
        }

        /// <inheritdoc cref="Vector128.Hypot(Vector128{float}, Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Hypot(Vector<float> x, Vector<float> y)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.HypotSingle<Vector<float>, Vector<double>>(x, y);
            }
            else
            {
                return Hypot<float>(x, y);
            }
        }

        /// <inheritdoc cref="Vector128.IndexOf{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(Vector<T> vector, T value)
        {
            if (sizeof(Vector<T>) == sizeof(Vector512<T>))
            {
                return Vector512.IndexOf(vector.AsVector512(), value);
            }
            else if (sizeof(Vector<T>) == sizeof(Vector256<T>))
            {
                return Vector256.IndexOf(vector.AsVector256(), value);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == sizeof(Vector128<T>));
                return Vector128.IndexOf(vector.AsVector128(), value);
            }
        }

        /// <inheritdoc cref="Vector128.IndexOfWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return IndexOf(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return IndexOf(vector.As<T, long>(), -1);
            }
            else
            {
                return IndexOf(vector, Scalar<T>.AllBitsSet);
            }
        }

        /// <inheritdoc cref="Vector128.IsEvenInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsEvenInteger<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return VectorMath.IsEvenIntegerSingle<Vector<float>, Vector<uint>>(vector.As<T, float>()).As<float, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return VectorMath.IsEvenIntegerDouble<Vector<double>, Vector<ulong>>(vector.As<T, double>()).As<double, T>();
            }
            return IsZero(vector & Vector<T>.One);
        }

        /// <inheritdoc cref="Vector128.IsFinite{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsFinite<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return ~IsZero(AndNot(Create<uint>(float.PositiveInfinityBits), vector.As<T, uint>())).As<uint, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return ~IsZero(AndNot(Create<ulong>(double.PositiveInfinityBits), vector.As<T, ulong>())).As<ulong, T>();
            }
            return Vector<T>.AllBitsSet;
        }

        /// <inheritdoc cref="Vector128.IsInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsInfinity<T>(Vector<T> vector)
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return IsPositiveInfinity(Abs(vector));
            }
            return Vector<T>.Zero;
        }

        /// <inheritdoc cref="Vector128.IsInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsInteger<T>(Vector<T> vector)
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return IsFinite(vector) & Equals(vector, Truncate(vector));
            }
            return Vector<T>.AllBitsSet;
        }

        /// <inheritdoc cref="Vector128.IsNaN{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsNaN<T>(Vector<T> vector)
        {
            if ((typeof(T) == typeof(float)) || (typeof(T) == typeof(double)))
            {
                return ~Equals(vector, vector);
            }
            return Vector<T>.Zero;
        }

        /// <inheritdoc cref="Vector128.IsNegative{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsNegative<T>(Vector<T> vector)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return Vector<T>.Zero;
            }
            else if (typeof(T) == typeof(float))
            {
                return LessThan(vector.As<T, int>(), Vector<int>.Zero).As<int, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return LessThan(vector.As<T, long>(), Vector<long>.Zero).As<long, T>();
            }
            else
            {
                return LessThan(vector, Vector<T>.Zero);
            }
        }

        /// <inheritdoc cref="Vector128.IsNegativeInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsNegativeInfinity<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Equals(vector, Create(float.NegativeInfinity).As<float, T>());
            }
            else if (typeof(T) == typeof(double))
            {
                return Equals(vector, Create(double.NegativeInfinity).As<double, T>());
            }
            return Vector<T>.Zero;
        }

        /// <inheritdoc cref="Vector128.IsNormal{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsNormal<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return LessThan(Abs(vector).As<T, uint>() - Create<uint>(float.SmallestNormalBits), Create<uint>(float.PositiveInfinityBits - float.SmallestNormalBits)).As<uint, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return LessThan(Abs(vector).As<T, ulong>() - Create<ulong>(double.SmallestNormalBits), Create<ulong>(double.PositiveInfinityBits - double.SmallestNormalBits)).As<ulong, T>();
            }
            return ~IsZero(vector);
        }

        /// <inheritdoc cref="Vector128.IsOddInteger{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsOddInteger<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return VectorMath.IsOddIntegerSingle<Vector<float>, Vector<uint>>(vector.As<T, float>()).As<float, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return VectorMath.IsOddIntegerDouble<Vector<double>, Vector<ulong>>(vector.As<T, double>()).As<double, T>();
            }
            return ~IsZero(vector & Vector<T>.One);
        }

        /// <inheritdoc cref="Vector128.IsPositive{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsPositive<T>(Vector<T> vector)
        {
            if ((typeof(T) == typeof(byte))
             || (typeof(T) == typeof(ushort))
             || (typeof(T) == typeof(uint))
             || (typeof(T) == typeof(ulong))
             || (typeof(T) == typeof(nuint)))
            {
                return Vector<T>.AllBitsSet;
            }
            else if (typeof(T) == typeof(float))
            {
                return GreaterThanOrEqual(vector.As<T, int>(), Vector<int>.Zero).As<int, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return GreaterThanOrEqual(vector.As<T, long>(), Vector<long>.Zero).As<long, T>();
            }
            else
            {
                return GreaterThanOrEqual(vector, Vector<T>.Zero);
            }
        }

        /// <inheritdoc cref="Vector128.IsPositiveInfinity{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsPositiveInfinity<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Equals(vector, Create(float.PositiveInfinity).As<float, T>());
            }
            else if (typeof(T) == typeof(double))
            {
                return Equals(vector, Create(double.PositiveInfinity).As<double, T>());
            }
            return Vector<T>.Zero;
        }

        /// <inheritdoc cref="Vector128.IsSubnormal{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsSubnormal<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return LessThan(Abs(vector).As<T, uint>() - Vector<uint>.One, Create<uint>(float.MaxTrailingSignificand)).As<uint, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return LessThan(Abs(vector).As<T, ulong>() - Vector<ulong>.One, Create<ulong>(double.MaxTrailingSignificand)).As<ulong, T>();
            }
            return Vector<T>.Zero;
        }

        /// <inheritdoc cref="Vector128.IsZero{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> IsZero<T>(Vector<T> vector) => Equals(vector, Vector<T>.Zero);

        /// <inheritdoc cref="Vector128.LastIndexOf{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(Vector<T> vector, T value)
        {
            if (sizeof(Vector<T>) == sizeof(Vector512<T>))
            {
                return Vector512.LastIndexOf(vector.AsVector512(), value);
            }
            else if (sizeof(Vector<T>) == sizeof(Vector256<T>))
            {
                return Vector256.LastIndexOf(vector.AsVector256(), value);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == sizeof(Vector128<T>));
                return Vector128.LastIndexOf(vector.AsVector128(), value);
            }
        }

        /// <inheritdoc cref="Vector128.LastIndexOfWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return LastIndexOf(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return LastIndexOf(vector.As<T, long>(), -1);
            }
            else
            {
                return LastIndexOf(vector, Scalar<T>.AllBitsSet);
            }
        }

        internal static Vector<T> Lerp<T>(Vector<T> x, Vector<T> y, Vector<T> amount)
            where T : IFloatingPointIeee754<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Lerp(x.GetElementUnsafe(index), y.GetElementUnsafe(index), amount.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Performs a linear interpolation between two vectors based on the given weighting.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of <paramref name="y" />.</param>
        /// <returns>The interpolated vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Lerp(Vector<double> x, Vector<double> y, Vector<double> amount)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Lerp<Vector<double>, double>(x, y, amount);
            }
            else
            {
                return Lerp<double>(x, y, amount);
            }
        }

        /// <summary>Performs a linear interpolation between two vectors based on the given weighting.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of <paramref name="y" />.</param>
        /// <returns>The interpolated vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Lerp(Vector<float> x, Vector<float> y, Vector<float> amount)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Lerp<Vector<float>, float>(x, y, amount);
            }
            else
            {
                return Lerp<float>(x, y, amount);
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
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (!Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
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
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? Scalar<T>.AllBitsSet : default!;
                result.SetElementUnsafe(index, value);
            }

            return result;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (!Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }

            return true;
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
            for (int index = 0; index < Vector<T>.Count; index++)
            {
                if (Scalar<T>.LessThanOrEqual(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<T> Load<T>(T* source) => LoadUnsafe(ref *source);

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
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (((nuint)(source) % (uint)(Alignment)) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            return *(Vector<T>*)source;
        }

        /// <summary>Loads a vector from the given aligned source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The aligned source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<T> LoadAlignedNonTemporal<T>(T* source) => LoadAligned(source);

        /// <summary>Loads a vector from the given source.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> LoadUnsafe<T>(ref readonly T source)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in source));
            return Unsafe.ReadUnaligned<Vector<T>>(in address);
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
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            ref readonly byte address = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
            return Unsafe.ReadUnaligned<Vector<T>>(in address);
        }

        internal static Vector<T> Log<T>(Vector<T> vector)
            where T : ILogarithmicFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Log(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Log(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Log(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogDouble<Vector<double>, Vector<long>, Vector<ulong>>(vector);
            }
            else
            {
                return Log<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Log(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Log(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.LogSingle<Vector<float>, Vector<int>, Vector<uint>>(vector);
            }
            else
            {
                return Log<float>(vector);
            }
        }

        internal static Vector<T> Log2<T>(Vector<T> vector)
            where T : ILogarithmicFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Log2(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Log2(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Log2(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Double<Vector<double>, Vector<long>, Vector<ulong>>(vector);
            }
            else
            {
                return Log2<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Log2(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Log2(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Log2Single<Vector<float>, Vector<int>, Vector<uint>>(vector);
            }
            else
            {
                return Log2<float>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Max{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Max<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Max<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Max(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MaxMagnitude{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MaxMagnitude<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MaxMagnitude<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MaxMagnitude(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MaxMagnitudeNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MaxMagnitudeNumber<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MaxMagnitudeNumber<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MaxMagnitudeNumber(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MaxNative{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MaxNative<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return ConditionalSelect(GreaterThan(left, right), left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.GreaterThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MaxNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MaxNumber<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MaxNumber<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MaxNumber(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.Min{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Min<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.Min<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Min(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MinMagnitude{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MinMagnitude<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MinMagnitude<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MinMagnitude(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MinMagnitudeNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MinMagnitudeNumber<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MinMagnitudeNumber<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MinMagnitudeNumber(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MinNative{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MinNative<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return ConditionalSelect(LessThan(left, right), left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.LessThan(left.GetElementUnsafe(index), right.GetElementUnsafe(index)) ? left.GetElementUnsafe(index) : right.GetElementUnsafe(index);
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.MinNumber{T}(Vector128{T}, Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> MinNumber<T>(Vector<T> left, Vector<T> right)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.MinNumber<Vector<T>, T>(left, right);
            }
            else
            {
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.MinNumber(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
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

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> MultiplyAddEstimate<T>(Vector<T> left, Vector<T> right, Vector<T> addend)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = Scalar<T>.MultiplyAddEstimate(left.GetElementUnsafe(index), right.GetElementUnsafe(index), addend.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{double}, Vector128{double}, Vector128{double})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> MultiplyAddEstimate(Vector<double> left, Vector<double> right, Vector<double> addend)
        {
            Unsafe.SkipInit(out Vector<double> result);

            for (int index = 0; index < Vector<double>.Count; index++)
            {
                double element = double.MultiplyAddEstimate(left.GetElementUnsafe(index), right.GetElementUnsafe(index), addend.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> MultiplyAddEstimate(Vector<float> left, Vector<float> right, Vector<float> addend)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int index = 0; index < Vector<float>.Count; index++)
            {
                float element = float.MultiplyAddEstimate(left.GetElementUnsafe(index), right.GetElementUnsafe(index), addend.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;Double&gt;" /> instances into one <see langword="Vector&lt;Single&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;Single&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Narrow(Vector<double> low, Vector<double> high)
        {
            Unsafe.SkipInit(out Vector<float> result);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                float value = (float)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<double>.Count; i < Vector<float>.Count; i++)
            {
                float value = (float)high.GetElementUnsafe(i - Vector<double>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;Int16&gt;" /> instances into one <see langword="Vector&lt;SByte&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;SByte&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
        {
            Unsafe.SkipInit(out Vector<sbyte> result);

            for (int i = 0; i < Vector<short>.Count; i++)
            {
                sbyte value = (sbyte)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<short>.Count; i < Vector<sbyte>.Count; i++)
            {
                sbyte value = (sbyte)high.GetElementUnsafe(i - Vector<short>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;Int32&gt;" /> instances into one <see langword="Vector&lt;Int16&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;Int16&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> Narrow(Vector<int> low, Vector<int> high)
        {
            Unsafe.SkipInit(out Vector<short> result);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                short value = (short)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<int>.Count; i < Vector<short>.Count; i++)
            {
                short value = (short)high.GetElementUnsafe(i - Vector<int>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;Int64&gt;" /> instances into one <see langword="Vector&lt;Int32&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;Int32&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> Narrow(Vector<long> low, Vector<long> high)
        {
            Unsafe.SkipInit(out Vector<int> result);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                int value = (int)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<long>.Count; i < Vector<int>.Count; i++)
            {
                int value = (int)high.GetElementUnsafe(i - Vector<long>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;UInt16&gt;" /> instances into one <see langword="Vector&lt;Byte&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;Byte&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
        {
            Unsafe.SkipInit(out Vector<byte> result);

            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                byte value = (byte)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<ushort>.Count; i < Vector<byte>.Count; i++)
            {
                byte value = (byte)high.GetElementUnsafe(i - Vector<ushort>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;UInt32&gt;" /> instances into one <see langword="Vector&lt;UInt16&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;UInt16&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
        {
            Unsafe.SkipInit(out Vector<ushort> result);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                ushort value = (ushort)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<uint>.Count; i < Vector<ushort>.Count; i++)
            {
                ushort value = (ushort)high.GetElementUnsafe(i - Vector<uint>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Narrows two <see langword="Vector&lt;UInt64&gt;" /> instances into one <see langword="Vector&lt;UInt32&gt;" />.</summary>
        /// <param name="low">The vector that will be narrowed to the lower half of the result vector.</param>
        /// <param name="high">The vector that will be narrowed to the upper half of the result vector.</param>
        /// <returns>A <see langword="Vector&lt;UInt32&gt;" /> containing elements narrowed from <paramref name="low" /> and <paramref name="high" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
        {
            Unsafe.SkipInit(out Vector<uint> result);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                uint value = (uint)low.GetElementUnsafe(i);
                result.SetElementUnsafe(i, value);
            }

            for (int i = Vector<ulong>.Count; i < Vector<uint>.Count; i++)
            {
                uint value = (uint)high.GetElementUnsafe(i - Vector<ulong>.Count);
                result.SetElementUnsafe(i, value);
            }

            return result;
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> Negate<T>(Vector<T> value) => -value;

        /// <inheritdoc cref="Vector128.None{T}(Vector128{T}, T)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None<T>(Vector<T> vector, T value) => !EqualsAny(vector, Create(value));

        /// <inheritdoc cref="Vector128.NoneWhereAllBitsSet{T}(Vector128{T})" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NoneWhereAllBitsSet<T>(Vector<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return None(vector.As<T, int>(), -1);
            }
            else if (typeof(T) == typeof(double))
            {
                return None(vector.As<T, long>(), -1);
            }
            else
            {
                return None(vector, Scalar<T>.AllBitsSet);
            }
        }

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> OnesComplement<T>(Vector<T> value) => ~value;

        internal static Vector<T> RadiansToDegrees<T>(Vector<T> radians)
            where T : ITrigonometricFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.RadiansToDegrees(radians.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.RadiansToDegrees(Vector128{double})" />
        [Intrinsic]
        public static Vector<double> RadiansToDegrees(Vector<double> radians)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.RadiansToDegrees<Vector<double>, double>(radians);
            }
            else
            {
                return RadiansToDegrees<double>(radians);
            }
        }

        /// <inheritdoc cref="Vector128.RadiansToDegrees(Vector128{float})" />
        [Intrinsic]
        public static Vector<float> RadiansToDegrees(Vector<float> radians)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.RadiansToDegrees<Vector<float>, float>(radians);
            }
            else
            {
                return RadiansToDegrees<float>(radians);
            }
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> Round<T>(Vector<T> vector)
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
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Round(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.Round(Vector128{double})" />
        [Intrinsic]
        public static Vector<double> Round(Vector<double> vector) => Round<double>(vector);

        /// <inheritdoc cref="Vector128.Round(Vector128{float})" />
        [Intrinsic]
        public static Vector<float> Round(Vector<float> vector) => Round<float>(vector);

        /// <inheritdoc cref="Vector128.Round(Vector128{double}, MidpointRounding)" />
        [Intrinsic]
        public static Vector<double> Round(Vector<double> vector, MidpointRounding mode) => VectorMath.RoundDouble(vector, mode);

        /// <inheritdoc cref="Vector128.Round(Vector128{float}, MidpointRounding)" />
        [Intrinsic]
        public static Vector<float> Round(Vector<float> vector, MidpointRounding mode) => VectorMath.RoundSingle(vector, mode);

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

        [Intrinsic]
        internal static Vector<uint> ShiftLeft(Vector<uint> vector, Vector<uint> shiftCount)
        {
            if (sizeof(Vector<uint>) == sizeof(Vector512<uint>))
            {
                return Vector512.ShiftLeft(vector.AsVector512(), shiftCount.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<uint>) == sizeof(Vector256<uint>))
            {
                return Vector256.ShiftLeft(vector.AsVector256(), shiftCount.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<uint>) == sizeof(Vector128<uint>));
                return Vector128.ShiftLeft(vector.AsVector128(), shiftCount.AsVector128()).AsVector();
            }
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector<ulong> ShiftLeft(Vector<ulong> value, int shiftCount) => value << shiftCount;

        [Intrinsic]
        internal static Vector<ulong> ShiftLeft(Vector<ulong> vector, Vector<ulong> shiftCount)
        {
            if (sizeof(Vector<ulong>) == sizeof(Vector512<ulong>))
            {
                return Vector512.ShiftLeft(vector.AsVector512(), shiftCount.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<ulong>) == sizeof(Vector256<ulong>))
            {
                return Vector256.ShiftLeft(vector.AsVector256(), shiftCount.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<ulong>) == sizeof(Vector128<ulong>));
                return Vector128.ShiftLeft(vector.AsVector128(), shiftCount.AsVector128()).AsVector();
            }
        }

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

        internal static Vector<T> Sin<T>(Vector<T> vector)
            where T : ITrigonometricFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T value = T.Sin(vector.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <inheritdoc cref="Vector128.Sin(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> Sin(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.SinDouble<Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return Sin<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.Sin(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> Sin(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.SinSingle<Vector<float>, Vector<int>, Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return Sin<float>(vector);
            }
        }

        internal static (Vector<T> Sin, Vector<T> Cos) SinCos<T>(Vector<T> vector)
            where T : ITrigonometricFunctions<T>
        {
            Unsafe.SkipInit(out Vector<T> sinResult);
            Unsafe.SkipInit(out Vector<T> cosResult);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                (T sinValue, T cosValue) = T.SinCos(vector.GetElementUnsafe(index));
                sinResult.SetElementUnsafe(index, sinValue);
                cosResult.SetElementUnsafe(index, cosValue);
            }

            return (sinResult, cosResult);
        }

        /// <inheritdoc cref="Vector128.SinCos(Vector128{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector<double> Sin, Vector<double> Cos) SinCos(Vector<double> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.SinCosDouble<Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return SinCos<double>(vector);
            }
        }

        /// <inheritdoc cref="Vector128.SinCos(Vector128{float})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector<float> Sin, Vector<float> Cos) SinCos(Vector<float> vector)
        {
            if (IsHardwareAccelerated)
            {
                return VectorMath.SinCosSingle<Vector<float>, Vector<int>, Vector<double>, Vector<long>>(vector);
            }
            else
            {
                return SinCos<float>(vector);
            }
        }

        /// <summary>Computes the square root of a vector on a per-element basis.</summary>
        /// <param name="value">The vector whose square root is to be computed.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A vector whose elements are the square root of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> SquareRoot<T>(Vector<T> value)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                T element = Scalar<T>.Sqrt(value.GetElementUnsafe(index));
                result.SetElementUnsafe(index, element);
            }

            return result;
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
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (((nuint)destination % (uint)(Alignment)) != 0)
            {
                ThrowHelper.ThrowAccessViolationException();
            }

            *(Vector<T>*)destination = source;
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static void StoreAlignedNonTemporal<T>(this Vector<T> source, T* destination) => source.StoreAligned(destination);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe<T>(this Vector<T> source, ref T destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
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
        public static void StoreUnsafe<T>(this Vector<T> source, ref T destination, nuint elementOffset)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination), source);
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
            T sum = default!;

            for (int index = 0; index < Vector<T>.Count; index++)
            {
                sum = Scalar<T>.Add(sum, value.GetElementUnsafe(index));
            }

            return sum;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static T ToScalar<T>(this Vector<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return vector.GetElementUnsafe(0);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<T> Truncate<T>(Vector<T> vector)
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
                Unsafe.SkipInit(out Vector<T> result);

                for (int index = 0; index < Vector<T>.Count; index++)
                {
                    T value = Scalar<T>.Truncate(vector.GetElementUnsafe(index));
                    result.SetElementUnsafe(index, value);
                }

                return result;
            }
        }

        /// <inheritdoc cref="Vector128.Truncate(Vector128{double})" />
        [Intrinsic]
        public static Vector<double> Truncate(Vector<double> vector) => Truncate<double>(vector);

        /// <inheritdoc cref="Vector128.Truncate(Vector128{float})" />
        [Intrinsic]
        public static Vector<float> Truncate(Vector<float> vector) => Truncate<float>(vector);

        /// <summary>Widens a <see langword="Vector&lt;Byte&gt;" /> into two <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<ushort> Lower, Vector<ushort> Upper) Widen(Vector<byte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;Int16&gt;" /> into two <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<int> Lower, Vector<int> Upper) Widen(Vector<short> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;Int32&gt;" /> into two <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<long> Lower, Vector<long> Upper) Widen(Vector<int> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;SByte&gt;" /> into two <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<short> Lower, Vector<short> Upper) Widen(Vector<sbyte> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;Single&gt;" /> into two <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<double> Lower, Vector<double> Upper) Widen(Vector<float> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;UInt16&gt;" /> into two <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<uint> Lower, Vector<uint> Upper) Widen(Vector<ushort> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;UInt32&gt;" /> into two <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A pair of vectors that contain the widened lower and upper halves of <paramref name="source" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (Vector<ulong> Lower, Vector<ulong> Upper) Widen(Vector<uint> source) => (WidenLower(source), WidenUpper(source));

        /// <summary>Widens a <see langword="Vector&lt;Byte&gt;" /> into two <see cref="Vector{UInt16} " />.</summary>
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

        /// <summary>Widens a <see langword="Vector&lt;Int16&gt;" /> into two <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see langword="Vector&lt;Int32&gt;" /> into two <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see langword="Vector&lt;SByte&gt;" /> into two <see cref="Vector{Int16} " />.</summary>
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

        /// <summary>Widens a <see langword="Vector&lt;Single&gt;" /> into two <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <param name="low">A vector that will contain the widened result of the lower half of <paramref name="source" />.</param>
        /// <param name="high">A vector that will contain the widened result of the upper half of <paramref name="source" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
        {
            low = WidenLower(source);
            high = WidenUpper(source);
        }

        /// <summary>Widens a <see langword="Vector&lt;UInt16&gt;" /> into two <see cref="Vector{UInt32} " />.</summary>
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

        /// <summary>Widens a <see langword="Vector&lt;UInt32&gt;" /> into two <see cref="Vector{UInt64} " />.</summary>
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

        /// <summary>Widens the lower half of a <see langword="Vector&lt;Byte&gt;" /> into a <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> WidenLower(Vector<byte> source)
        {
            Unsafe.SkipInit(out Vector<ushort> lower);

            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;Int16&gt;" /> into a <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> WidenLower(Vector<short> source)
        {
            Unsafe.SkipInit(out Vector<int> lower);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;Int32&gt;" /> into a <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> WidenLower(Vector<int> source)
        {
            Unsafe.SkipInit(out Vector<long> lower);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;SByte&gt;" /> into a <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> WidenLower(Vector<sbyte> source)
        {
            Unsafe.SkipInit(out Vector<short> lower);

            for (int i = 0; i < Vector<short>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;Single&gt;" /> into a <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> WidenLower(Vector<float> source)
        {
            Unsafe.SkipInit(out Vector<double> lower);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;UInt16&gt;" /> into a <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> WidenLower(Vector<ushort> source)
        {
            Unsafe.SkipInit(out Vector<uint> lower);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the lower half of a <see langword="Vector&lt;UInt32&gt;" /> into a <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened lower half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> WidenLower(Vector<uint> source)
        {
            Unsafe.SkipInit(out Vector<ulong> lower);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                lower.SetElementUnsafe(i, value);
            }

            return lower;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;Byte&gt;" /> into a <see cref="Vector{UInt16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ushort> WidenUpper(Vector<byte> source)
        {
            Unsafe.SkipInit(out Vector<ushort> upper);

            for (int i = Vector<ushort>.Count; i < Vector<byte>.Count; i++)
            {
                ushort value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<ushort>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;Int16&gt;" /> into a <see cref="Vector{Int32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<int> WidenUpper(Vector<short> source)
        {
            Unsafe.SkipInit(out Vector<int> upper);

            for (int i = Vector<int>.Count; i < Vector<short>.Count; i++)
            {
                int value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<int>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;Int32&gt;" /> into a <see cref="Vector{Int64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<long> WidenUpper(Vector<int> source)
        {
            Unsafe.SkipInit(out Vector<long> upper);

            for (int i = Vector<long>.Count; i < Vector<int>.Count; i++)
            {
                long value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<long>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;SByte&gt;" /> into a <see cref="Vector{Int16} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<short> WidenUpper(Vector<sbyte> source)
        {
            Unsafe.SkipInit(out Vector<short> upper);

            for (int i = Vector<short>.Count; i < Vector<sbyte>.Count; i++)
            {
                short value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<short>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;Single&gt;" /> into a <see cref="Vector{Double} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<double> WidenUpper(Vector<float> source)
        {
            Unsafe.SkipInit(out Vector<double> upper);

            for (int i = Vector<double>.Count; i < Vector<float>.Count; i++)
            {
                double value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<double>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;UInt16&gt;" /> into a <see cref="Vector{UInt32} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<uint> WidenUpper(Vector<ushort> source)
        {
            Unsafe.SkipInit(out Vector<uint> upper);

            for (int i = Vector<uint>.Count; i < Vector<ushort>.Count; i++)
            {
                uint value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<uint>.Count, value);
            }

            return upper;
        }

        /// <summary>Widens the upper half of a <see langword="Vector&lt;UInt32&gt;" /> into a <see cref="Vector{UInt64} " />.</summary>
        /// <param name="source">The vector whose elements are to be widened.</param>
        /// <returns>A vector that contain the widened upper half of <paramref name="source" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<ulong> WidenUpper(Vector<uint> source)
        {
            Unsafe.SkipInit(out Vector<ulong> upper);

            for (int i = Vector<ulong>.Count; i < Vector<uint>.Count; i++)
            {
                ulong value = source.GetElementUnsafe(i);
                upper.SetElementUnsafe(i - Vector<ulong>.Count, value);
            }

            return upper;
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
            if ((uint)(index) >= (uint)(Vector<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector<T> result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) => left ^ right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetElementUnsafe<T>(in this Vector<T> vector, int index)
        {
            Debug.Assert((index >= 0) && (index < Vector<T>.Count));
            ref T address = ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in vector));
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetElementUnsafe<T>(in this Vector<T> vector, int index, T value)
        {
            Debug.Assert((index >= 0) && (index < Vector<T>.Count));
            ref T address = ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in vector));
            Unsafe.Add(ref address, index) = value;
        }
    }
}

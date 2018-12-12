// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector256DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector256.Size)]
    public readonly struct Vector256<T> : IEquatable<Vector256<T>>, IFormattable
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/coreclr/issues/15694)
        private readonly ulong _00;
        private readonly ulong _01;
        private readonly ulong _02;
        private readonly ulong _03;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector256{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            get
            {
                ThrowIfUnsupportedType();
                return Vector256.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector256{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowIfUnsupportedType();
                return default;
            }
        }

        internal unsafe string DisplayString
        {
            get
            {
                if (IsSupported)
                {
                    return ToString();
                }
                else
                {
                    return SR.NotSupported_Type;
                }
            }
        }

        internal static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (typeof(T) == typeof(byte)) ||
                       (typeof(T) == typeof(sbyte)) ||
                       (typeof(T) == typeof(short)) ||
                       (typeof(T) == typeof(ushort)) ||
                       (typeof(T) == typeof(int)) ||
                       (typeof(T) == typeof(uint)) ||
                       (typeof(T) == typeof(long)) ||
                       (typeof(T) == typeof(ulong)) ||
                       (typeof(T) == typeof(float)) ||
                       (typeof(T) == typeof(double));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfUnsupportedType()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{U}" />.</summary>
        /// <typeparam name="U">The type of the vector the current instance should be reinterpreted as.</typeparam>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<U> As<U>() where U : struct
        {
            ThrowIfUnsupportedType();
            Vector256<U>.ThrowIfUnsupportedType();
            return Unsafe.As<Vector256<T>, Vector256<U>>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Byte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<byte> AsByte() => As<byte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Double}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<double> AsDouble() => As<double>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Int16}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<short> AsInt16() => As<short>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Int32}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<int> AsInt32() => As<int>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Int64}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<long> AsInt64() => As<long>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{SByte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector256<sbyte> AsSByte() => As<sbyte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{Single}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<float> AsSingle() => As<float>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{UInt16}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector256<ushort> AsUInt16() => As<ushort>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{UInt32}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector256<uint> AsUInt32() => As<uint>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector256{UInt64}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector256{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector256<ulong> AsUInt64() => As<ulong>();

        /// <summary>Determines whether the specified <see cref="Vector256{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector256{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public bool Equals(Vector256<T> other)
        {
            if (Avx.IsSupported)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> result = Avx.Compare(AsSingle(), other.AsSingle(), FloatComparisonMode.EqualOrderedNonSignaling);
                    return Avx.MoveMask(result) == 0b1111_1111; // We have one bit per element
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<double> result = Avx.Compare(AsDouble(), other.AsDouble(), FloatComparisonMode.EqualOrderedNonSignaling);
                    return Avx.MoveMask(result) == 0b1111; // We have one bit per element
                }
            }

            if (Avx2.IsSupported)
            {
                // Unlike float/double, there are no special values to consider
                // for integral types and we can just do a comparison that all
                // bytes are exactly the same.

                Debug.Assert((typeof(T) != typeof(float)) && (typeof(T) != typeof(double)));
                Vector256<byte> result = Avx2.CompareEqual(AsByte(), other.AsByte());
                return Avx2.MoveMask(result) == unchecked((int)(0b1111_1111_1111_1111_1111_1111_1111_1111)); // We have one bit per element
            }

            return SoftwareFallback(in this, other);

            bool SoftwareFallback(in Vector256<T> x, Vector256<T> y)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!((IEquatable<T>)(x.GetElement(i))).Equals(y.GetElement(i)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector256{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals(object obj)
        {
            return (obj is Vector256<T>) && Equals((Vector256<T>)(obj));
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public T GetElement(int index)
        {
            ThrowIfUnsupportedType();

            if ((uint)(index) >= (uint)(Count))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ref T e0 = ref Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(in this));
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the current instance.</summary>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the value to.</param>
        /// <returns>A <see cref="Vector256{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public Vector256<T> WithElement(int index, T value)
        {
            ThrowIfUnsupportedType();

            if ((uint)(index) >= (uint)(Count))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Vector256<T> result = this;
            ref T e0 = ref Unsafe.As<Vector256<T>, T>(ref result);
            Unsafe.Add(ref e0, index) = value;
            return result;
        }

        /// <summary>Gets the hash code for the instance.</summary>
        /// <returns>The hash code for the instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override int GetHashCode()
        {
            ThrowIfUnsupportedType();

            int hashCode = 0;

            for (int i = 0; i < Count; i++)
            {
                hashCode = HashCode.Combine(hashCode, GetElement(i).GetHashCode());
            }

            return hashCode;
        }

        /// <summary>Gets the value of the lower 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <returns>The value of the lower 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<T> GetLower()
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();
            return Unsafe.As<Vector256<T>, Vector128<T>>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the lower 128-bits set to the specified value and the lower 128-bits set to the same value as that in the current instance.</summary>
        /// <param name="value">The value of the lower 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the specified value and the lower 128-bits set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<T> WithLower(Vector128<T> value)
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.InsertVector128(AsByte(), value.AsByte(), 0).As<T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.InsertVector128(AsSingle(), value.AsSingle(), 0).As<T>();
            }

            return SoftwareFallback(in this, value);

            Vector256<T> SoftwareFallback(in Vector256<T> t, Vector128<T> x)
            {
                Vector256<T> result = t;
                Unsafe.As<Vector256<T>, Vector128<T>>(ref result) = x;
                return result;
            }
        }

        /// <summary>Gets the value of the upper 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <returns>The value of the upper 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector128<T> GetUpper()
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.ExtractVector128(AsByte(), 1).As<T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.ExtractVector128(AsSingle(), 1).As<T>();
            }

            return SoftwareFallback(in this);

            Vector128<T> SoftwareFallback(in Vector256<T> t)
            {
                ref Vector128<T> lower = ref Unsafe.As<Vector256<T>, Vector128<T>>(ref Unsafe.AsRef(in t));
                return Unsafe.Add(ref lower, 1);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the upper 128-bits set to the specified value and the upper 128-bits set to the same value as that in the current instance.</summary>
        /// <param name="value">The value of the upper 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the upper 128-bits set to the specified value and the upper 128-bits set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<T> WithUpper(Vector128<T> value)
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.InsertVector128(AsByte(), value.AsByte(), 1).As<T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.InsertVector128(AsSingle(), value.AsSingle(), 1).As<T>();
            }

            return SoftwareFallback(in this, value);

            Vector256<T> SoftwareFallback(in Vector256<T> t, Vector128<T> x)
            {
                Vector256<T> result = t;
                ref Vector128<T> lower = ref Unsafe.As<Vector256<T>, Vector128<T>>(ref result);
                Unsafe.Add(ref lower, 1) = x;
                return result;
            }
        }

        /// <summary>Converts the current instance to a scalar containing the value of the first element.</summary>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public T ToScalar()
        {
            ThrowIfUnsupportedType();
            return Unsafe.As<Vector256<T>, T>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Converts the current instance to an equivalent string representation.</summary>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override string ToString()
        {
            return ToString("G");
        }

        /// <summary>Converts the current instance to an equivalent string representation using the specified format.</summary>
        /// <param name="format">The format specifier used to format the individual elements of the current instance.</param>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public string ToString(string format)
        {
            return ToString(format, formatProvider: null);
        }

        /// <summary>Converts the current instance to an equivalent string representation using the specified format.</summary>
        /// <param name="format">The format specifier used to format the individual elements of the current instance.</param>
        /// <param name="formatProvider">The format provider used to format the individual elements of the current instance.</param>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            ThrowIfUnsupportedType();

            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            int lastElement = Count - 1;

            var sb = StringBuilderCache.Acquire();
            sb.Append('<');

            for (int i = 0; i < lastElement; i++)
            {
                sb.Append(((IFormattable)(GetElement(i))).ToString(format, formatProvider));
                sb.Append(separator);
                sb.Append(' ');
            }
            sb.Append(((IFormattable)(GetElement(lastElement))).ToString(format, formatProvider));

            sb.Append('>');
            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}

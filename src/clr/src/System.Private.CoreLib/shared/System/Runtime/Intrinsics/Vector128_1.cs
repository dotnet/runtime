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
    [DebuggerTypeProxy(typeof(Vector128DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector128.Size)]
    public readonly struct Vector128<T> : IEquatable<Vector128<T>>, IFormattable
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/coreclr/issues/15694)
        private readonly ulong _00;
        private readonly ulong _01;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector128{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            get
            {
                ThrowIfUnsupportedType();
                return Vector128.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> Zero
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

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{U}" />.</summary>
        /// <typeparam name="U">The type of the vector the current instance should be reinterpreted as.</typeparam>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector128<U> As<U>() where U : struct
        {
            ThrowIfUnsupportedType();
            Vector128<U>.ThrowIfUnsupportedType();
            return Unsafe.As<Vector128<T>, Vector128<U>>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Byte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<byte> AsByte() => As<byte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Double}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<double> AsDouble() => As<double>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Int16}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<short> AsInt16() => As<short>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Int32}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<int> AsInt32() => As<int>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Int64}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<long> AsInt64() => As<long>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{SByte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector128<sbyte> AsSByte() => As<sbyte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{Single}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector128<float> AsSingle() => As<float>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{UInt16}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector128<ushort> AsUInt16() => As<ushort>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{UInt32}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector128<uint> AsUInt32() => As<uint>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector128{UInt64}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector128{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public Vector128<ulong> AsUInt64() => As<ulong>();

        /// <summary>Determines whether the specified <see cref="Vector128{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector128{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector128<T> other)
        {
            ThrowIfUnsupportedType();

            if (Sse.IsSupported && (typeof(T) == typeof(float)))
            {
                Vector128<float> result = Sse.CompareEqual(AsSingle(), other.AsSingle());
                return Sse.MoveMask(result) == 0b1111; // We have one bit per element
            }

            if (Sse2.IsSupported)
            {
                if (typeof(T) == typeof(double))
                {
                    Vector128<double> result = Sse2.CompareEqual(AsDouble(), other.AsDouble());
                    return Sse2.MoveMask(result) == 0b11; // We have one bit per element
                }
                else
                {
                    // Unlike float/double, there are no special values to consider
                    // for integral types and we can just do a comparison that all
                    // bytes are exactly the same.

                    Debug.Assert((typeof(T) != typeof(float)) && (typeof(T) != typeof(double)));
                    Vector128<byte> result = Sse2.CompareEqual(AsByte(), other.AsByte());
                    return Sse2.MoveMask(result) == 0b1111_1111_1111_1111; // We have one bit per element
                }
            }

            return SoftwareFallback(in this, other);

            bool SoftwareFallback(in Vector128<T> x, Vector128<T> y)
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
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector128{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals(object obj)
        {
            return (obj is Vector128<T>) && Equals((Vector128<T>)(obj));
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

            ref T e0 = ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in this));
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the current instance.</summary>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the value to.</param>
        /// <returns>A <see cref="Vector128{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public Vector128<T> WithElement(int index, T value)
        {
            ThrowIfUnsupportedType();

            if ((uint)(index) >= (uint)(Count))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Vector128<T> result = this;
            ref T e0 = ref Unsafe.As<Vector128<T>, T>(ref result);
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

        /// <summary>Gets the value of the lower 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <returns>The value of the lower 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<T> GetLower()
        {
            ThrowIfUnsupportedType();
            Vector64<T>.ThrowIfUnsupportedType();
            return Unsafe.As<Vector128<T>, Vector64<T>>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the lower 64-bits set to the specified value and the upper 64-bits set to the same value as that in the current instance.</summary>
        /// <param name="value">The value of the lower 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the specified value and the upper 64-bits set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector128<T> WithLower(Vector64<T> value)
        {
            ThrowIfUnsupportedType();
            Vector64<T>.ThrowIfUnsupportedType();

            Vector128<T> result = this;
            Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = value;
            return result;
        }

        /// <summary>Gets the value of the upper 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <returns>The value of the upper 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<T> GetUpper()
        {
            ThrowIfUnsupportedType();
            Vector64<T>.ThrowIfUnsupportedType();

            ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref Unsafe.AsRef(in this));
            return Unsafe.Add(ref lower, 1);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the upper 64-bits set to the specified value and the upper 64-bits set to the same value as that in the current instance.</summary>
        /// <param name="value">The value of the upper 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the upper 64-bits set to the specified value and the upper 64-bits set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector128<T> WithUpper(Vector64<T> value)
        {
            ThrowIfUnsupportedType();
            Vector64<T>.ThrowIfUnsupportedType();

            Vector128<T> result = this;
            ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref result);
            Unsafe.Add(ref lower, 1) = value;
            return result;
        }

        /// <summary>Converts the current instance to a scalar containing the value of the first element.</summary>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public T ToScalar()
        {
            ThrowIfUnsupportedType();
            return Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in this));
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

        /// <summary>Converts the current instance to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the current instance and the upper 128-bits initialized to zero.</summary>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the current instance and the upper 128-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public Vector256<T> ToVector256()
        {
            ThrowIfUnsupportedType();
            Vector256<T>.ThrowIfUnsupportedType();

            Vector256<T> result = Vector256<T>.Zero;
            Unsafe.As<Vector256<T>, Vector128<T>>(ref result) = this;
            return result;
        }

        /// <summary>Converts the current instance to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the current instance and the upper 128-bits left uninitialized.</summary>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the current instance and the upper 128-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public unsafe Vector256<T> ToVector256Unsafe()
        {
            ThrowIfUnsupportedType();
            Vector256<T>.ThrowIfUnsupportedType();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[Vector256.Size];
            Unsafe.AsRef<Vector128<T>>(pResult) = this;
            return Unsafe.AsRef<Vector256<T>>(pResult);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector64DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector64.Size)]
    public readonly struct Vector64<T> where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/coreclr/issues/15694)
        private readonly ulong _00;

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> Zero
        {
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
                    var items = new T[ElementCount];
                    Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref items[0]), this);
                    return $"({string.Join(", ", items)})";
                }
                else
                {
                    return SR.NotSupported_Type;
                }
            }
        }

        internal static int ElementCount
        {
            get
            {
                ThrowIfUnsupportedType();
                return Vector64.Size / Unsafe.SizeOf<T>();
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

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{U}" />.</summary>
        /// <typeparam name="U">The type of the vector the current instance should be reinterpreted as.</typeparam>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector64<U> As<U>() where U : struct
        {
            ThrowIfUnsupportedType();
            Vector64<U>.ThrowIfUnsupportedType();
            return Unsafe.As<Vector64<T>, Vector64<U>>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{byte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<byte> AsByte() => As<byte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{double}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<double> AsDouble() => As<double>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{short}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{short}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<short> AsInt16() => As<short>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{int}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{int}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<int> AsInt32() => As<int>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{long}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{long}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<long> AsInt64() => As<long>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{sbyte}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{sbyte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        public Vector64<sbyte> AsSByte() => As<sbyte>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{float}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{float}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector64<float> AsSingle() => As<float>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{ushort}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{ushort}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        public Vector64<ushort> AsUInt16() => As<ushort>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{uint}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{uint}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        public Vector64<uint> AsUInt32() => As<uint>();

        /// <summary>Reinterprets the current instance as a new <see cref="Vector64{ulong}" />.</summary>
        /// <returns>The current instance reinterpreted as a new <see cref="Vector64{ulong}" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        public Vector64<ulong> AsUInt64() => As<ulong>();

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public T GetElement(int index)
        {
            ThrowIfUnsupportedType();

            if ((uint)(index) >= (uint)(ElementCount))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ref T e0 = ref Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in this));
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the current instance.</summary>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the value to.</param>
        /// <returns>A <see cref="Vector64{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public Vector64<T> WithElement(int index, T value)
        {
            ThrowIfUnsupportedType();

            if ((uint)(index) >= (uint)(ElementCount))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Vector64<T> result = this;
            ref T e0 = ref Unsafe.As<Vector64<T>, T>(ref result);
            Unsafe.Add(ref e0, index) = value;
            return result;
        }

        /// <summary>Converts the current instance to a scalar containing the value of the first element.</summary>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public T ToScalar()
        {
            ThrowIfUnsupportedType();
            return Unsafe.As<Vector64<T>, T>(ref Unsafe.AsRef(in this));
        }

        /// <summary>Converts the current instance to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the current instance and the upper 64-bits initialized to zero.</summary>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the current instance and the upper 64-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public Vector128<T> ToVector128()
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();

            Vector128<T> result = Vector128<T>.Zero;
            Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = this;
            return result;
        }

        /// <summary>Converts the current instance to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the current instance and the upper 64-bits left uninitialized.</summary>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the current instance and the upper 64-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public unsafe Vector128<T> ToVector128Unsafe()
        {
            ThrowIfUnsupportedType();
            Vector128<T>.ThrowIfUnsupportedType();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[Vector128.Size];
            Unsafe.AsRef<Vector64<T>>(pResult) = this;
            return Unsafe.AsRef<Vector128<T>>(pResult);
        }
    }
}

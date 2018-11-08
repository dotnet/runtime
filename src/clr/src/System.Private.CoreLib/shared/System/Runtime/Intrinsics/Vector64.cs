// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    public static class Vector64
    {
        internal const int Size = 8;

        /// <summary>Creates a new <see cref="Vector64{byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<byte> Create(byte value)
        {
            var pResult = stackalloc byte[8]
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

        /// <summary>Creates a new <see cref="Vector64{double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{double}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<double> Create(double value)
        {
            return Unsafe.As<double, Vector64<double>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{short}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{short}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<short> Create(short value)
        {
            var pResult = stackalloc short[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{int}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{int}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<int> Create(int value)
        {
            var pResult = stackalloc int[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{long}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{long}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<long> Create(long value)
        {
            return Unsafe.As<long, Vector64<long>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{sbyte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{sbyte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> Create(sbyte value)
        {
            var pResult = stackalloc sbyte[8]
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

        /// <summary>Creates a new <see cref="Vector64{float}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{float}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<float> Create(float value)
        {
            var pResult = stackalloc float[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{ushort}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{ushort}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> Create(ushort value)
        {
            var pResult = stackalloc ushort[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{uint}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{uint}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint value)
        {
            var pResult = stackalloc uint[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{ulong}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{ulong}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ulong> Create(ulong value)
        {
            return Unsafe.As<ulong, Vector64<ulong>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{byte}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
        {
            var pResult = stackalloc byte[8]
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

        /// <summary>Creates a new <see cref="Vector64{short}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{short}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<short> Create(short e0, short e1, short e2, short e3)
        {
            var pResult = stackalloc short[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{int}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{int}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<int> Create(int e0, int e1)
        {
            var pResult = stackalloc int[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{sbyte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{sbyte}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
        {
            var pResult = stackalloc sbyte[8]
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

        /// <summary>Creates a new <see cref="Vector64{float}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{float}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<float> Create(float e0, float e1)
        {
            var pResult = stackalloc float[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{ushort}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{ushort}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
        {
            var pResult = stackalloc ushort[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{uint}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{uint}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint e0, uint e1)
        {
            var pResult = stackalloc uint[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{byte}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<byte> CreateScalar(byte value)
        {
            var result = Vector64<byte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<byte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{short}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{short}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<short> CreateScalar(short value)
        {
            var result = Vector64<short>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<short>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{int}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{int}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<int> CreateScalar(int value)
        {
            var result = Vector64<int>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<int>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{sbyte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{sbyte}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> CreateScalar(sbyte value)
        {
            var result = Vector64<sbyte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<sbyte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{float}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{float}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<float> CreateScalar(float value)
        {
            var result = Vector64<float>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<float>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{ushort}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{ushort}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> CreateScalar(ushort value)
        {
            var result = Vector64<ushort>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<ushort>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{uint}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{uint}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> CreateScalar(uint value)
        {
            var result = Vector64<uint>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<uint>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{byte}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{short}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{short}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc short[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{int}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{int}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc int[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{sbyte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{sbyte}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc sbyte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{float}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{float}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc float[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{ushort}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{ushort}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ushort[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{uint}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{uint}" /> instance with the first element initialized to <see cref="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc uint[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }
    }
}

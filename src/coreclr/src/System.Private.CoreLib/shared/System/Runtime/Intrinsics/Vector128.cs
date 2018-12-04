// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    public static class Vector128
    {
        internal const int Size = 16;

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<byte> Create(byte value)
        {
            var pResult = stackalloc byte[16]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<double> Create(double value)
        {
            var pResult = stackalloc double[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<short> Create(short value)
        {
            var pResult = stackalloc short[8]
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

            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<int> Create(int value)
        {
            var pResult = stackalloc int[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<long> Create(long value)
        {
            var pResult = stackalloc long[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte value)
        {
            var pResult = stackalloc sbyte[16]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector128<float> Create(float value)
        {
            var pResult = stackalloc float[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort value)
        {
            var pResult = stackalloc ushort[8]
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

            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint value)
        {
            var pResult = stackalloc uint[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong value)
        {
            var pResult = stackalloc ulong[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector128{Byte}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
        {
            var pResult = stackalloc byte[16]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
                e8,
                e9,
                e10,
                e11,
                e12,
                e13,
                e14,
                e15,
            };

            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<double> Create(double e0, double e1)
        {
            var pResult = stackalloc double[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
        {
            var pResult = stackalloc short[8]
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

            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<int> Create(int e0, int e1, int e2, int e3)
        {
            var pResult = stackalloc int[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<long> Create(long e0, long e1)
        {
            var pResult = stackalloc long[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector128{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
        {
            var pResult = stackalloc sbyte[16]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
                e8,
                e9,
                e10,
                e11,
                e12,
                e13,
                e14,
                e15,
            };

            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector128<float> Create(float e0, float e1, float e2, float e3)
        {
            var pResult = stackalloc float[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
        {
            var pResult = stackalloc ushort[8]
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

            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint e0, uint e1, uint e2, uint e3)
        {
            var pResult = stackalloc uint[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong e0, ulong e1)
        {
            var pResult = stackalloc ulong[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance from two <see cref="Vector64{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
        {
            Vector128<byte> result128 = Vector128<byte>.Zero;

            ref Vector64<byte> result64 = ref Unsafe.As<Vector128<byte>, Vector64<byte>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance from two <see cref="Vector64{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<double> Create(Vector64<double> lower, Vector64<double> upper)
        {
            Vector128<double> result128 = Vector128<double>.Zero;

            ref Vector64<double> result64 = ref Unsafe.As<Vector128<double>, Vector64<double>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance from two <see cref="Vector64{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<short> Create(Vector64<short> lower, Vector64<short> upper)
        {
            Vector128<short> result128 = Vector128<short>.Zero;

            ref Vector64<short> result64 = ref Unsafe.As<Vector128<short>, Vector64<short>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance from two <see cref="Vector64{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<int> Create(Vector64<int> lower, Vector64<int> upper)
        {
            Vector128<int> result128 = Vector128<int>.Zero;

            ref Vector64<int> result64 = ref Unsafe.As<Vector128<int>, Vector64<int>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance from two <see cref="Vector64{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<long> Create(Vector64<long> lower, Vector64<long> upper)
        {
            Vector128<long> result128 = Vector128<long>.Zero;

            ref Vector64<long> result64 = ref Unsafe.As<Vector128<long>, Vector64<long>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance from two <see cref="Vector64{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(Vector64<sbyte> lower, Vector64<sbyte> upper)
        {
            Vector128<sbyte> result128 = Vector128<sbyte>.Zero;

            ref Vector64<sbyte> result64 = ref Unsafe.As<Vector128<sbyte>, Vector64<sbyte>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance from two <see cref="Vector64{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<float> Create(Vector64<float> lower, Vector64<float> upper)
        {
            Vector128<float> result128 = Vector128<float>.Zero;

            ref Vector64<float> result64 = ref Unsafe.As<Vector128<float>, Vector64<float>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance from two <see cref="Vector64{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(Vector64<ushort> lower, Vector64<ushort> upper)
        {
            Vector128<ushort> result128 = Vector128<ushort>.Zero;

            ref Vector64<ushort> result64 = ref Unsafe.As<Vector128<ushort>, Vector64<ushort>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance from two <see cref="Vector64{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(Vector64<uint> lower, Vector64<uint> upper)
        {
            Vector128<uint> result128 = Vector128<uint>.Zero;

            ref Vector64<uint> result64 = ref Unsafe.As<Vector128<uint>, Vector64<uint>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance from two <see cref="Vector64{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(Vector64<ulong> lower, Vector64<ulong> upper)
        {
            Vector128<ulong> result128 = Vector128<ulong>.Zero;

            ref Vector64<ulong> result64 = ref Unsafe.As<Vector128<ulong>, Vector64<ulong>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<byte> CreateScalar(byte value)
        {
            var result = Vector128<byte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<byte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<double> CreateScalar(double value)
        {
            var result = Vector128<double>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<short> CreateScalar(short value)
        {
            var result = Vector128<short>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<short>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<int> CreateScalar(int value)
        {
            var result = Vector128<int>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<long> CreateScalar(long value)
        {
            var result = Vector128<long>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<long>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalar(sbyte value)
        {
            var result = Vector128<sbyte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<sbyte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<float> CreateScalar(float value)
        {
            var result = Vector128<float>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalar(ushort value)
        {
            var result = Vector128<ushort>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalar(uint value)
        {
            var result = Vector128<uint>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalar(ulong value)
        {
            var result = Vector128<ulong>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ulong>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<double> CreateScalarUnsafe(double value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc double[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc short[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc int[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<long> CreateScalarUnsafe(long value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc long[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc sbyte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector128<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc float[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ushort[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc uint[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalarUnsafe(ulong value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ulong[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace TestVectorHelperApis
{
    public class Program
    {
        private static Vector128<int> s_evaluationOrderVector;

        [Fact]
        public static int TestEntryPoint()
        {
            bool fail = false;

            Vector128<byte> bytes128 = Vector128.Create((byte)1, (byte)2, (byte)3, (byte)2, (byte)4, (byte)2, (byte)5, (byte)2,
                                                        (byte)6, (byte)2, (byte)7, (byte)2, (byte)8, (byte)2, (byte)9, (byte)2);
            Vector64<byte> bytes64 = Vector64.Create((byte)1, (byte)2, (byte)3, (byte)2, (byte)4, (byte)2, (byte)5, (byte)2);

            fail |= CountByte128(bytes128, 2) != 8;
            fail |= IndexOfByte128(bytes128, 2) != 1;
            fail |= LastIndexOfByte128(bytes128, 2) != 15;
            fail |= NoneByte128(bytes128, 42) != true;
            fail |= AnyByte128(bytes128, 2) != true;
            fail |= AllByte128(Vector128.Create((byte)2), 2) != true;

            fail |= CountByte64(bytes64, 2) != 4;
            fail |= IndexOfByte64(bytes64, 2) != 1;
            fail |= LastIndexOfByte64(bytes64, 2) != 7;
            fail |= NoneByte64(bytes64, 42) != true;

            Vector512<byte> bytes512 = Vector512<byte>.Zero.WithElement(63, (byte)2);

            fail |= CountByte512(bytes512, 2) != 1;
            fail |= IndexOfByte512(bytes512, 2) != 63;
            fail |= LastIndexOfByte512(bytes512, 2) != 63;

            Vector128<int> allBits128 = Vector128.Create(-1, 0, -1, 7);
            Vector64<int> allBits64 = Vector64.Create(-1, 0);

            fail |= CountAllBitsSetInt128(allBits128) != 2;
            fail |= IndexOfAllBitsSetInt128(allBits128) != 0;
            fail |= LastIndexOfAllBitsSetInt128(allBits128) != 2;
            fail |= AnyAllBitsSetInt128(allBits128) != true;
            fail |= AllAllBitsSetInt128(Vector128<int>.AllBitsSet) != true;
            fail |= NoneAllBitsSetInt128(Vector128<int>.Zero) != true;

            fail |= CountAllBitsSetInt64(allBits64) != 1;
            fail |= IndexOfAllBitsSetInt64(allBits64) != 0;
            fail |= LastIndexOfAllBitsSetInt64(allBits64) != 0;
            fail |= NoneAllBitsSetInt64(Vector64<int>.Zero) != true;

            fail |= IndexOfEvaluationOrder() != 10;
            fail |= IndexOfWhereAllBitsSetEvaluationOrder() != 10;

            Vector128<float> floatBits = Vector128.Create(BitConverter.Int32BitsToSingle(-1), 0.0f, BitConverter.Int32BitsToSingle(-1), 1.0f);
            fail |= CountAllBitsSetFloat128(floatBits) != 2;
            fail |= IndexOfAllBitsSetFloat128(floatBits) != 0;
            fail |= NoneAllBitsSetFloat128(Vector128<float>.Zero) != true;

            Vector128<float> floatNaN128 = Vector128.Create(float.NaN);
            fail |= AllFloatNaN128(floatNaN128, float.NaN) != false;
            fail |= AnyFloatNaN128(floatNaN128, float.NaN) != false;
            fail |= NoneFloatNaN128(floatNaN128, float.NaN) != true;

            Vector64<float> floatNaN64 = Vector64.Create(float.NaN);
            fail |= AllFloatNaN64(floatNaN64, float.NaN) != false;
            fail |= AnyFloatNaN64(floatNaN64, float.NaN) != false;
            fail |= NoneFloatNaN64(floatNaN64, float.NaN) != true;

            Vector128<double> doubleNaN128 = Vector128.Create(double.NaN);
            fail |= AllDoubleNaN128(doubleNaN128, double.NaN) != false;
            fail |= AnyDoubleNaN128(doubleNaN128, double.NaN) != false;
            fail |= NoneDoubleNaN128(doubleNaN128, double.NaN) != true;

            Vector64<double> doubleNaN64 = Vector64.Create(double.NaN);
            fail |= AllDoubleNaN64(doubleNaN64, double.NaN) != false;
            fail |= AnyDoubleNaN64(doubleNaN64, double.NaN) != false;
            fail |= NoneDoubleNaN64(doubleNaN64, double.NaN) != true;

            return fail ? 101 : 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, #7
            // ARM64-FULL-LINE: addv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-NOT: AdvSimdExtractBitMask
            // X64-FULL-LINE: {{v?}}pcmpeqb {{.*}}
            // X64-FULL-LINE: {{v?}}pmovmskb {{.*}}
            return Vector128.Count(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: uminv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-NOT: AdvSimdExtractBitMask
            // X64-FULL-LINE: {{v?}}pcmpeqb {{.*}}
            // X64-FULL-LINE: {{v?}}pmovmskb {{.*}}
            return Vector128.IndexOf(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LastIndexOfByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-NOT: AdvSimdExtractBitMask
            // X64-FULL-LINE: {{v?}}pcmpeqb {{.*}}
            // X64-FULL-LINE: {{v?}}pmovmskb {{.*}}
            return Vector128.LastIndexOf(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umaxp {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector128.None(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umaxp {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector128.Any(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllByte128(Vector128<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-NOT: AdvSimdExtractBitMask
            return Vector128.All(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountByte64(Vector64<byte> vector, byte value)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, #7
            // ARM64-FULL-LINE: addv {{b[0-9]+}}, {{v[0-9]+}}.8b
            // ARM64-NOT: AdvSimdExtractBitMask
            return Vector64.Count(vector, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfByte64(Vector64<byte> vector, byte value) => Vector64.IndexOf(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LastIndexOfByte64(Vector64<byte> vector, byte value) => Vector64.LastIndexOf(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneByte64(Vector64<byte> vector, byte value) => Vector64.None(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountByte512(Vector512<byte> vector, byte value) => Vector512.Count(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfByte512(Vector512<byte> vector, byte value) => Vector512.IndexOf(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LastIndexOfByte512(Vector512<byte> vector, byte value) => Vector512.LastIndexOf(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountAllBitsSetInt128(Vector128<int> vector)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, #31
            // ARM64-FULL-LINE: addv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-NOT: AdvSimdExtractBitMask
            // X64-FULL-LINE: {{v?}}pcmpeqd {{.*}}
            // X64-FULL-LINE: {{v?}}movmskps {{.*}}
            return Vector128.CountWhereAllBitsSet(vector);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfAllBitsSetInt128(Vector128<int> vector)
        {
            // ARM64-FULL-LINE: cmeq {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: uminv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-NOT: AdvSimdExtractBitMask
            // X64-FULL-LINE: {{v?}}pcmpeqd {{.*}}
            // X64-FULL-LINE: {{v?}}movmskps {{.*}}
            return Vector128.IndexOfWhereAllBitsSet(vector);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LastIndexOfAllBitsSetInt128(Vector128<int> vector) => Vector128.LastIndexOfWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyAllBitsSetInt128(Vector128<int> vector) => Vector128.AnyWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllAllBitsSetInt128(Vector128<int> vector) => Vector128.AllWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneAllBitsSetInt128(Vector128<int> vector) => Vector128.NoneWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountAllBitsSetInt64(Vector64<int> vector) => Vector64.CountWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfAllBitsSetInt64(Vector64<int> vector) => Vector64.IndexOfWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LastIndexOfAllBitsSetInt64(Vector64<int> vector) => Vector64.LastIndexOfWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneAllBitsSetInt64(Vector64<int> vector) => Vector64.NoneWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SetEvaluationOrderVector(int value)
        {
            s_evaluationOrderVector = Vector128.Create(value);
            return 10;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfEvaluationOrder()
        {
            s_evaluationOrderVector = Vector128<int>.Zero;
            return SetEvaluationOrderVector(42) + Vector128.IndexOf(s_evaluationOrderVector, 42);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfWhereAllBitsSetEvaluationOrder()
        {
            s_evaluationOrderVector = Vector128<int>.Zero;
            return SetEvaluationOrderVector(-1) + Vector128.IndexOfWhereAllBitsSet(s_evaluationOrderVector);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountAllBitsSetFloat128(Vector128<float> vector) => Vector128.CountWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfAllBitsSetFloat128(Vector128<float> vector) => Vector128.IndexOfWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneAllBitsSetFloat128(Vector128<float> vector) => Vector128.NoneWhereAllBitsSet(vector);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllFloatNaN128(Vector128<float> vector, float value) => Vector128.All(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyFloatNaN128(Vector128<float> vector, float value) => Vector128.Any(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneFloatNaN128(Vector128<float> vector, float value) => Vector128.None(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllFloatNaN64(Vector64<float> vector, float value) => Vector64.All(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyFloatNaN64(Vector64<float> vector, float value) => Vector64.Any(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneFloatNaN64(Vector64<float> vector, float value) => Vector64.None(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllDoubleNaN128(Vector128<double> vector, double value) => Vector128.All(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyDoubleNaN128(Vector128<double> vector, double value) => Vector128.Any(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneDoubleNaN128(Vector128<double> vector, double value) => Vector128.None(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AllDoubleNaN64(Vector64<double> vector, double value) => Vector64.All(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyDoubleNaN64(Vector64<double> vector, double value) => Vector64.Any(vector, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneDoubleNaN64(Vector64<double> vector, double value) => Vector64.None(vector, value);
    }
}

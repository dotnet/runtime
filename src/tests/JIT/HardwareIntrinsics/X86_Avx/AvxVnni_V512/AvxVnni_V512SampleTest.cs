// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest._AvxVnni_V512
{
    public partial class Program
    {
        [Fact]
        public static void AvxVnni_V512SampleTest()
        {
            Vector512<int> addend = Vector512<int>.Zero;
            Vector512<byte> unsignedBytes = Vector512.Create((byte)1);
            Vector512<sbyte> signedBytes = Vector512.Create((sbyte)2);
            Vector512<short> words = Vector512.Create((short)3);

            if (!AvxVnni.V512.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxVnni.V512.MultiplyWideningAndAdd(addend, unsignedBytes, signedBytes));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, unsignedBytes, signedBytes));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxVnni.V512.MultiplyWideningAndAdd(addend, words, words));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, words, words));
                return;
            }

            Vector512<int> result = AvxVnni.V512.MultiplyWideningAndAdd(addend, unsignedBytes, signedBytes);

            // Each int32 lane should sum 4 byte*sbyte products: 4 * (1 * 2) = 8
            AssertAllLanesEqual(result, 8);

            Vector512<int> resultSat = AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, unsignedBytes, signedBytes);
            AssertAllLanesEqual(resultSat, 8);

            Vector512<int> wordResult = AvxVnni.V512.MultiplyWideningAndAdd(addend, words, words);

            // Each int32 lane sums 2 short*short products: 2 * (3 * 3) = 18
            AssertAllLanesEqual(wordResult, 18);

            Vector512<int> wordResultSat = AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, words, words);
            AssertAllLanesEqual(wordResultSat, 18);

            // Non-trivial pattern test: magnitudes stay small (-3..3 product range)
            // so the saturating intrinsic produces the same result as the non-saturating
            // one. The distinct positive/negative-saturation code paths for
            // VPDPBUSDS / VPDPWSSDS are exercised by the dedicated edge-case blocks below.
            byte[] leftBytes = new byte[Vector512<byte>.Count];
            sbyte[] rightBytes = new sbyte[Vector512<sbyte>.Count];
            int[] byteExpected = new int[Vector512<int>.Count];

            for (int index = 0; index < leftBytes.Length; index++)
            {
                leftBytes[index] = (byte)(index + 1);
                rightBytes[index] = (sbyte)((index % 7) - 3);
                byteExpected[index / 4] += leftBytes[index] * rightBytes[index];
            }

            Vector512<byte> byteLeft = LoadVector512(leftBytes);
            Vector512<sbyte> byteRight = LoadVector512(rightBytes);

            AssertLanesEqual(AvxVnni.V512.MultiplyWideningAndAdd(addend, byteLeft, byteRight), byteExpected);
            AssertLanesEqual(AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, byteLeft, byteRight), byteExpected);

            short[] leftWords = new short[Vector512<short>.Count];
            short[] rightWords = new short[Vector512<short>.Count];
            int[] wordExpected = new int[Vector512<int>.Count];

            for (int index = 0; index < leftWords.Length; index++)
            {
                leftWords[index] = (short)(index - 16);
                rightWords[index] = (short)((index % 5) - 2);
                wordExpected[index / 2] += leftWords[index] * rightWords[index];
            }

            Vector512<short> wordLeft = LoadVector512(leftWords);
            Vector512<short> wordRight = LoadVector512(rightWords);

            AssertLanesEqual(AvxVnni.V512.MultiplyWideningAndAdd(addend, wordLeft, wordRight), wordExpected);
            AssertLanesEqual(AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, wordLeft, wordRight), wordExpected);

            Vector512<int> saturatingAddend = Vector512.Create(int.MaxValue - 10);
            Vector512<int> byteSaturated = AvxVnni.V512.MultiplyWideningAndAddSaturate(
                saturatingAddend,
                Vector512.Create(byte.MaxValue),
                Vector512.Create(sbyte.MaxValue));
            AssertAllLanesEqual(byteSaturated, int.MaxValue);

            Vector512<int> wordSaturated = AvxVnni.V512.MultiplyWideningAndAddSaturate(
                saturatingAddend,
                Vector512.Create(short.MaxValue),
                Vector512.Create(short.MaxValue));
            AssertAllLanesEqual(wordSaturated, int.MaxValue);

            // Negative saturation: addend just above int.MinValue plus a large negative
            // product sum should clamp to int.MinValue (the distinct -ve code path for
            // VPDPBUSDS / VPDPWSSDS).
            Vector512<int> negSaturatingAddend = Vector512.Create(int.MinValue + 10);

            // byte form: 255 * -128 = -32640 per pair; 4 pairs per lane = -130560;
            // addend (int.MinValue + 10) + (-130560) underflows to int.MinValue.
            Vector512<int> byteSaturatedNeg = AvxVnni.V512.MultiplyWideningAndAddSaturate(
                negSaturatingAddend,
                Vector512.Create(byte.MaxValue),
                Vector512.Create(sbyte.MinValue));
            AssertAllLanesEqual(byteSaturatedNeg, int.MinValue);

            // word form: short.MinValue * short.MaxValue = -1073709056 per pair;
            // 2 pairs per lane = -2147418112; addend + that underflows to int.MinValue.
            Vector512<int> wordSaturatedNeg = AvxVnni.V512.MultiplyWideningAndAddSaturate(
                negSaturatingAddend,
                Vector512.Create(short.MinValue),
                Vector512.Create(short.MaxValue));
            AssertAllLanesEqual(wordSaturatedNeg, int.MinValue);
        }

        private static void AssertAllLanesEqual(Vector512<int> value, int expected)
        {
            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal(expected, value.GetElement(index));
            }
        }

        private static void AssertLanesEqual(Vector512<int> value, int[] expected)
        {
            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal(expected[index], value.GetElement(index));
            }
        }

        private static Vector512<T> LoadVector512<T>(T[] values)
            where T : unmanaged
        {
            return Vector512.Create(values);
        }
    }
}

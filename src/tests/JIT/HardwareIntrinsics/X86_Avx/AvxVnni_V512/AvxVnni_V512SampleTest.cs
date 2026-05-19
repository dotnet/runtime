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
        }

        private static void AssertAllLanesEqual(Vector512<int> value, int expected)
        {
            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal(expected, value.GetElement(index));
            }
        }
    }
}

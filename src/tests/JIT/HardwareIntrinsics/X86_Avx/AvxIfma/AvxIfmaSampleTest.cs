// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest._AvxIfma
{
    public partial class Program
    {
        [Fact]
        public static void AvxIfmaSampleTest()
        {
            Vector128<ulong> zero128 = Vector128<ulong>.Zero;
            Vector128<ulong> one128 = Vector128.Create((ulong)1);
            Vector128<ulong> forty_two128 = Vector128.Create((ulong)42);
            Vector128<ulong> big128 = Vector128.Create((ulong)(1UL << 26));

            Vector256<ulong> zero256 = Vector256<ulong>.Zero;
            Vector256<ulong> one256 = Vector256.Create((ulong)1);
            Vector256<ulong> forty_two256 = Vector256.Create((ulong)42);
            Vector256<ulong> big256 = Vector256.Create((ulong)(1UL << 26));

            if (!AvxIfma.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.MultiplyAdd52Low(zero128, one128, forty_two128));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.MultiplyAdd52High(zero128, big128, big128));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.MultiplyAdd52Low(zero256, one256, forty_two256));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.MultiplyAdd52High(zero256, big256, big256));
                return;
            }

            // V128: VPMADD52LUQ semantics
            //   dst[i] = addend[i] + ((left[i] & 0xF_FFFF_FFFF_FFFF) * (right[i] & 0xF_FFFF_FFFF_FFFF))[51:0]
            AssertAllLanesEqual128(AvxIfma.MultiplyAdd52Low(zero128, one128, forty_two128), 42UL);
            AssertAllLanesEqual128(AvxIfma.MultiplyAdd52High(zero128, big128, big128), 1UL);
            AssertAllLanesEqual128(AvxIfma.MultiplyAdd52High(zero128, one128, forty_two128), 0UL);

            // V256: same shape
            AssertAllLanesEqual256(AvxIfma.MultiplyAdd52Low(zero256, one256, forty_two256), 42UL);
            AssertAllLanesEqual256(AvxIfma.MultiplyAdd52High(zero256, big256, big256), 1UL);
            AssertAllLanesEqual256(AvxIfma.MultiplyAdd52High(zero256, one256, forty_two256), 0UL);

            // Addend accumulates
            Vector128<ulong> preloaded128 = Vector128.Create((ulong)1000);
            AssertAllLanesEqual128(AvxIfma.MultiplyAdd52Low(preloaded128, one128, forty_two128), 1042UL);

            Vector256<ulong> preloaded256 = Vector256.Create((ulong)1000);
            AssertAllLanesEqual256(AvxIfma.MultiplyAdd52Low(preloaded256, one256, forty_two256), 1042UL);

            // Only low 52 bits of source operands participate
            ulong topMask = 0xFFF0_0000_0000_0000UL;
            Vector128<ulong> leftMasked128 = Vector128.Create((ulong)1 | topMask);
            Vector128<ulong> rightMasked128 = Vector128.Create((ulong)42 | topMask);
            AssertAllLanesEqual128(AvxIfma.MultiplyAdd52Low(zero128, leftMasked128, rightMasked128), 42UL);

            Vector256<ulong> leftMasked256 = Vector256.Create((ulong)1 | topMask);
            Vector256<ulong> rightMasked256 = Vector256.Create((ulong)42 | topMask);
            AssertAllLanesEqual256(AvxIfma.MultiplyAdd52Low(zero256, leftMasked256, rightMasked256), 42UL);

            // Non-trivial lane-varying pattern on V256
            const ulong Mask52 = 0x000F_FFFF_FFFF_FFFFUL;
            ulong[] leftValues = new ulong[Vector256<ulong>.Count];
            ulong[] rightValues = new ulong[Vector256<ulong>.Count];
            ulong[] addendValues = new ulong[Vector256<ulong>.Count];
            ulong[] expectedLow = new ulong[Vector256<ulong>.Count];
            for (int index = 0; index < leftValues.Length; index++)
            {
                leftValues[index] = ((ulong)index + 1) * 1_000_003UL;
                rightValues[index] = ((ulong)index + 5) * 999_983UL;
                addendValues[index] = (ulong)index * 7UL;
                ulong product = (leftValues[index] & Mask52) * (rightValues[index] & Mask52);
                expectedLow[index] = addendValues[index] + (product & Mask52);
            }
            AssertLanesEqual256(
                AvxIfma.MultiplyAdd52Low(
                    Vector256.Create(addendValues),
                    Vector256.Create(leftValues),
                    Vector256.Create(rightValues)),
                expectedLow);
        }

        private static void AssertAllLanesEqual128(Vector128<ulong> value, ulong expected)
        {
            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal(expected, value.GetElement(index));
            }
        }

        private static void AssertAllLanesEqual256(Vector256<ulong> value, ulong expected)
        {
            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal(expected, value.GetElement(index));
            }
        }

        private static void AssertLanesEqual256(Vector256<ulong> value, ulong[] expected)
        {
            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal(expected[index], value.GetElement(index));
            }
        }
    }
}

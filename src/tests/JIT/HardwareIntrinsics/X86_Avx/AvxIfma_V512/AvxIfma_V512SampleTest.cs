// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest._AvxIfma_V512
{
    public partial class Program
    {
        [Fact]
        public static void AvxIfma_V512SampleTest()
        {
            Vector512<ulong> zero = Vector512<ulong>.Zero;
            Vector512<ulong> one = Vector512.Create((ulong)1);
            Vector512<ulong> forty_two = Vector512.Create((ulong)42);
            Vector512<ulong> big = Vector512.Create((ulong)(1UL << 26));

            if (!AvxIfma.V512.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.V512.MultiplyAdd52Low(zero, one, forty_two));
                Assert.Throws<PlatformNotSupportedException>(
                    () => AvxIfma.V512.MultiplyAdd52High(zero, big, big));
                return;
            }

            // VPMADD52LUQ semantics per Intel SDM:
            //   dst[i] = addend[i] + ((left[i] & 0x000F_FFFF_FFFF_FFFF)
            //                      *  (right[i] & 0x000F_FFFF_FFFF_FFFF))[51:0]
            //
            // With addend=0, left=1, right=42: result = (1 * 42) & 0xF_FFFF_FFFF_FFFF = 42
            Vector512<ulong> lowResult = AvxIfma.V512.MultiplyAdd52Low(zero, one, forty_two);
            AssertAllLanesEqual(lowResult, 42UL);

            // Addend accumulates: pre-loaded addend + product low
            Vector512<ulong> preloaded = Vector512.Create((ulong)1000);
            Vector512<ulong> accumulated = AvxIfma.V512.MultiplyAdd52Low(preloaded, one, forty_two);
            AssertAllLanesEqual(accumulated, 1042UL);

            // VPMADD52HUQ semantics:
            //   dst[i] = addend[i] + ((left[i] & 0x000F_FFFF_FFFF_FFFF)
            //                      *  (right[i] & 0x000F_FFFF_FFFF_FFFF))[103:52]
            //
            // With addend=0, left = 1<<26, right = 1<<26:
            //   product = 1<<52; high 52 bits of product = 1
            Vector512<ulong> highResult = AvxIfma.V512.MultiplyAdd52High(zero, big, big);
            AssertAllLanesEqual(highResult, 1UL);

            // High 52 bits of a smaller product are zero: 1 * 42 = 42 fits in low 52 bits,
            // so the "high" 52 bits are 0.
            Vector512<ulong> highZero = AvxIfma.V512.MultiplyAdd52High(zero, one, forty_two);
            AssertAllLanesEqual(highZero, 0UL);

            // Only the low 52 bits of the source operands participate — bits above 52
            // should be ignored. Set the top bits and confirm no change:
            ulong topMask = 0xFFF0_0000_0000_0000UL;
            Vector512<ulong> leftMasked = Vector512.Create((ulong)1 | topMask);
            Vector512<ulong> rightMasked = Vector512.Create((ulong)42 | topMask);
            Vector512<ulong> lowResultMasked = AvxIfma.V512.MultiplyAdd52Low(zero, leftMasked, rightMasked);
            AssertAllLanesEqual(lowResultMasked, 42UL);

            // Non-trivial lane-varying pattern:
            ulong[] leftValues = new ulong[Vector512<ulong>.Count];
            ulong[] rightValues = new ulong[Vector512<ulong>.Count];
            ulong[] addendValues = new ulong[Vector512<ulong>.Count];
            ulong[] expectedLow = new ulong[Vector512<ulong>.Count];

            const ulong Mask52 = 0x000F_FFFF_FFFF_FFFFUL;
            for (int index = 0; index < leftValues.Length; index++)
            {
                leftValues[index] = ((ulong)index + 1) * 1_000_003UL;
                rightValues[index] = ((ulong)index + 5) * 999_983UL;
                addendValues[index] = (ulong)index * 7UL;

                ulong product = (leftValues[index] & Mask52) * (rightValues[index] & Mask52);
                expectedLow[index] = addendValues[index] + (product & Mask52);
            }

            Vector512<ulong> addendVec = Vector512.Create(addendValues);
            Vector512<ulong> leftVec = Vector512.Create(leftValues);
            Vector512<ulong> rightVec = Vector512.Create(rightValues);

            Vector512<ulong> patternedLow = AvxIfma.V512.MultiplyAdd52Low(addendVec, leftVec, rightVec);
            AssertLanesEqual(patternedLow, expectedLow);
        }

        private static void AssertAllLanesEqual(Vector512<ulong> value, ulong expected)
        {
            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal(expected, value.GetElement(index));
            }
        }

        private static void AssertLanesEqual(Vector512<ulong> value, ulong[] expected)
        {
            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal(expected[index], value.GetElement(index));
            }
        }
    }
}

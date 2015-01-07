// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CreateSectionShortTests
    {
        [Fact]
        public void Test01()
        {

            BitVector32.Section section;                   // returned section
            Int16 maxValue = 0;        // int16 to be used as CreateSection() argument
            BitVector32 bv32;
            int expected = 0;

            // [] ArgumentException for argument < 1
            //-----------------------------------------------------------------

            maxValue = Int16.MinValue;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue); });

            maxValue = -1;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue); });

            maxValue = 0;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue); });

            // [] no problems for argument > 0
            //-----------------------------------------------------------------

            maxValue = 1;
            section = BitVector32.CreateSection(maxValue);
            if (section.Mask != maxValue || section.Offset != 0)
            {
                Assert.False(true, string.Format("  Error, CreateSection({0}) returned ({1}, {2})", maxValue, section.Mask, section.Offset));
            }

            maxValue = Int16.MaxValue;
            section = BitVector32.CreateSection(maxValue);
            if (section.Mask != maxValue || section.Offset != 0)
            {
                Assert.False(true, string.Format("  Error, CreateSection({0}) returned ({1}, {2})", maxValue, section.Mask, section.Offset));
            }

            // [] CreateSection for random maxValue
            //-----------------------------------------------------------------           

            // generate random previous max value
            int max = -55;
            System.Random random = new System.Random(max);
            max = random.Next(1, System.Int16.MaxValue);

            maxValue = (Int16)max;
            section = BitVector32.CreateSection(maxValue);
            expected = 0;           // to calculate number of bits required to accomodate maxValue
            while (maxValue > (int)System.Math.Pow(2, expected))
                expected++;
            if (section.Mask != (int)System.Math.Pow(2, expected) - 1 || section.Offset != 0)
            {
                Assert.False(true, string.Format("  Error, CreateSection({0}) returned ({1}, {2}) instead of expected ({3}, {4})",
                                maxValue, section.Mask, section.Offset, (int)System.Math.Pow(2, expected) - 1, 0));
            }


            // [] apply Section to the Vector
            //-----------------------------------------------------------------

            maxValue = 1;
            section = BitVector32.CreateSection(maxValue);
            bv32 = new BitVector32(maxValue);
            if (bv32[section] != maxValue)
            {
                Assert.False(true, string.Format("  Error, BitVector32[{0}] returned {1} instead of expected {2}", section.ToString(), bv32[section], maxValue));
            }

            expected = 0;
            bv32[section] = expected;
            if (bv32[section] != expected || bv32.Data != expected)
            {
                Assert.False(true, string.Format("  Error, failed to set section to {0}", expected));
            }

            expected = 1;
            bv32[section] = expected;
            if (bv32[section] != expected || bv32.Data != expected)
            {
                Assert.False(true, string.Format("  Error, failed to set section to {0}", expected));
            }

            maxValue = 6;
            section = BitVector32.CreateSection(maxValue);
            bv32 = new BitVector32(maxValue);
            if (bv32[section] != maxValue)
            {
                Assert.False(true, string.Format("  Error, BitVector32[{0}] returned {1} instead of expected {2}", section.ToString(), bv32[section], maxValue));
            }

            expected = 0;
            bv32[section] = expected;
            if (bv32[section] != expected || bv32.Data != expected)
            {
                Assert.False(true, string.Format("  Error, failed to set section to {0}", expected));
            }

            expected = 1;
            bv32[section] = expected;
            if (bv32[section] != expected || bv32.Data != expected)
            {
                Assert.False(true, string.Format("  Error, failed to set section to {0}", expected));
            }

            expected = 3;
            bv32[section] = expected;
            if (bv32[section] != expected || bv32.Data != expected)
            {
                Assert.False(true, string.Format("  Error, failed to set section to {0}", expected));
            }
        }
    }
}
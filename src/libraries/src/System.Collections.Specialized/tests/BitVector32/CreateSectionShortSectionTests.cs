// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CreateSectionShortSectionTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32.Section section;                   // returned section
            BitVector32.Section sectionArgument;
            Int16 maxValue = 0;        // int16 to be used as CreateSection() argument
            BitVector32 bv32;
            int expected = 0;

            // [] ArgumentException for argument < 1
            //-----------------------------------------------------------------
            sectionArgument = BitVector32.CreateSection(1);

            maxValue = Int16.MinValue;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue, sectionArgument); });

            maxValue = -1;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue, sectionArgument); });

            maxValue = 0;
            Assert.Throws<ArgumentException>(() => { section = BitVector32.CreateSection(maxValue, sectionArgument); });

            // [] no problems for argument > 0
            //-----------------------------------------------------------------

            maxValue = 1;
            section = BitVector32.CreateSection(maxValue, sectionArgument);
            if (section.Mask != maxValue || section.Offset != 1)
            {
                Assert.False(true, string.Format("  Error, returned ({1}, {2}) instead of expected ({0}, {3})", maxValue, section.Mask, section.Offset, 1));
            }

            sectionArgument = BitVector32.CreateSection(2);      // should give Mask = 3, offset = 0
            maxValue = 1;
            section = BitVector32.CreateSection(maxValue, sectionArgument);
            if (section.Mask != maxValue || section.Offset != 2)
            {
                Assert.False(true, string.Format("  Error, returned ({1}, {2}) instead of expected ({0}, {3})", maxValue, section.Mask, section.Offset, 2));
            }

            maxValue = 2;
            section = BitVector32.CreateSection(maxValue, sectionArgument);
            if (section.Mask != 3 || section.Offset != 2)
            {
                Assert.False(true, string.Format("  Error, returned ({1}, {2}) instead of expected ({0}, {3})", 3, section.Mask, section.Offset, 2));
            }


            sectionArgument = BitVector32.CreateSection(Int16.MaxValue);
            maxValue = Int16.MaxValue;
            expected = 15;                  //expected offset
            section = BitVector32.CreateSection(maxValue, sectionArgument);
            if (section.Mask != maxValue || section.Offset != expected)
            {
                Assert.False(true, string.Format("  Error, returned ({1}, {2}) instead of expected ({0}, {3})", maxValue, section.Mask, section.Offset, expected));
            }

            sectionArgument = section;
            expected = 30;                  //expected offset
            section = BitVector32.CreateSection(maxValue, sectionArgument);
            if (section.Mask != maxValue || section.Offset != expected)
            {
                Assert.False(true, string.Format("  Error, returned ({1}, {2}) instead of expected ({0}, {3})", maxValue, section.Mask, section.Offset, expected));
            }

            // [] linked list of sections with maxValue = 1
            //-----------------------------------------------------------------

            maxValue = 1;
            sectionArgument = BitVector32.CreateSection(maxValue);
            bv32 = new BitVector32(Int32.MaxValue);
            for (int i = 1; i < 32; i++)
            {
                section = BitVector32.CreateSection(maxValue, sectionArgument);
                if (section.Mask != maxValue || section.Offset != i)
                {
                    Assert.False(true, string.Format("  Error, returned ({0}, {1}) instead of ({2}, {3})", section.Mask, section.Offset, maxValue, i));
                }
                sectionArgument = section;

                // apply section to the vector
                expected = 1;
                if (i == 31)
                    expected = 0;
                if (bv32[section] != expected)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], expected, i));
                }

                bv32[section] = 0;             // set section value to 0
                if (bv32[section] != 0)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], 0, i));
                }

                expected = 1;
                bv32[section] = 1;               // set section value back to 1
                if (bv32[section] != expected)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], expected, i));
                }
            }

            // [] linked list of sections with maxValue = 3
            //-----------------------------------------------------------------

            maxValue = 3;
            sectionArgument = BitVector32.CreateSection(maxValue);
            bv32 = new BitVector32(Int32.MaxValue);
            for (int i = 2; i < 32; i += 2)
            {
                section = BitVector32.CreateSection(maxValue, sectionArgument);
                if (section.Mask != maxValue || section.Offset != i)
                {
                    Assert.False(true, string.Format("  Error, returned ({0}, {1}) instead of ({2}, {3})", section.Mask, section.Offset, maxValue, i));
                }
                sectionArgument = section;

                // apply section to the vector
                expected = 3;
                if (i == 30)
                    expected = 1;
                if (bv32[section] != expected)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], expected, i));
                }

                bv32[section] = 0;             // set section value to 0
                if (bv32[section] != 0)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], 0, i));
                }

                expected = 3;
                bv32[section] = expected;               // set section value back to 3
                if (bv32[section] != expected)
                {
                    Assert.False(true, string.Format("  Error, returned {0} instead of {1} ", bv32[section], expected, i));
                }
            }

            // [] set section with maxValue = 3 to out of range value 6
            //-----------------------------------------------------------------

            maxValue = 3;
            bv32 = new BitVector32(-1);
            sectionArgument = BitVector32.CreateSection(maxValue);
            section = BitVector32.CreateSection(maxValue, sectionArgument);
        }
    }
}
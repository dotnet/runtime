// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class SectionEqualsObjTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32.Section section1;
            BitVector32.Section section2;
            short maxValue = 0;
            System.Random random = new System.Random(-55);

            // [] two BitVectors that are the same - expected true
            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            if (!section1.Equals(section1))
            {
                Assert.False(true, string.Format("Error, section is not equal to itself"));
            }

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            section2 = BitVector32.CreateSection(maxValue);
            if (!section1.Equals(section2))
            {
                Assert.False(true, string.Format("Error, different sections with same maxvalue are not equal"));
            }

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            maxValue = (Int16)(maxValue / 2);
            section2 = BitVector32.CreateSection(maxValue);
            if (section1.Equals(section2))
            {
                Assert.False(true, string.Format("Error, sections with different maxvalues are equal"));
            }

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            section2 = BitVector32.CreateSection(maxValue, section1);
            if (section1.Equals(section2))
            {
                Assert.False(true, string.Format("Error, two different sections are equal"));
            }

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            maxValue = (Int16)(maxValue / 2);
            section2 = BitVector32.CreateSection(maxValue, section1);
            if (section1.Equals(section2))
            {
                Assert.False(true, string.Format("Error, two different sections are equal"));
            }

            if (section1.Equals(null))
            {
                Assert.False(true, string.Format("Error, section and null are equal"));
            }

            section1 = BitVector32.CreateSection(maxValue);
            if (section1.Equals(maxValue))
            {
                Assert.False(true, string.Format("Error, section and non-section-object are equal"));
            }
        }
    }
}
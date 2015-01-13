// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class SectionGetHashCodeTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32.Section section1;
            BitVector32.Section section2;
            int code1 = 0;              // HashCode of section1
            int code2 = 0;              // HashCode of section2
            short maxValue = 0;
            System.Random random = new System.Random(-55);

            // [] two BitVectors that are the same should return the same HashCode
            //-----------------------------------------------------------------

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            code1 = section1.GetHashCode();
            code2 = section1.GetHashCode();
            if (code1 != code2)
            {
                Assert.False(true, string.Format("Error, HashCodes of the same section: {0} != {1}", code1, code2));
            }

            maxValue = (Int16)random.Next(1, Int16.MaxValue);
            section1 = BitVector32.CreateSection(maxValue);
            section2 = BitVector32.CreateSection(maxValue);
            code1 = section1.GetHashCode();
            code2 = section2.GetHashCode();
            if (code1 != code2)
            {
                Assert.False(true, string.Format("Error, HashCodes of different sections with same maxvalue: {0} != {1}", code1, code2));
            }
        }
    }
}
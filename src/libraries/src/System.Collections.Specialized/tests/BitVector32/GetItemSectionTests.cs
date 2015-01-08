// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetItemSectionTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            int data = 0;
            // create sections to test
            BitVector32.Section[] sect = new BitVector32.Section[6];
            sect[0] = BitVector32.CreateSection(1);        // 1x0 section  - 1st bit
            sect[1] = BitVector32.CreateSection(1, sect[0]);        // 1x1 section - 2nd bit
            sect[2] = BitVector32.CreateSection(7);        // 7x0 section  - first 3 bits
            sect[3] = BitVector32.CreateSection(Int16.MaxValue);        // Int16.MaxValuex0 section
            sect[4] = BitVector32.CreateSection(Int16.MaxValue, sect[3]);        // Int16.MaxValuex15 section
            sect[5] = BitVector32.CreateSection(1, sect[4]);
            sect[5] = BitVector32.CreateSection(1, sect[5]);        // sign bit section    - last sign bit

            // array of expected sections values for all 1's in vector
            int[] values =
            {
                1,                  //   0
                1,                  //   1
                7,                  //   2
                Int16.MaxValue,     //   3
                Int16.MaxValue,     //   4
                1                   //   5 - value of sign bit may differ based on Data sign
            };

            // [] BitVector is constructed as expected and sections return expected values
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data = {0} after default ctor", bv32.Data));
            }

            // loop through sections
            // all values should return 0
            for (int i = 0; i < sect.Length; i++)
            {
                if (bv32[sect[i]] != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, bv32[sect[i]], 0));
                }
            }

            //
            //   (-1)
            //   
            data = -1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through sections
            // all values should correspond to 'values'-array elements 
            for (int i = 0; i < sect.Length; i++)
            {
                if (bv32[sect[i]] != values[i])
                {
                    string temp = "Err" + i;
                    if (i == sect.Length - 1)
                        temp += "section for last bit";
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], values[i]));
                }
            }

            //
            //  Int32.MaxValue
            //
            data = Int32.MaxValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            values[values.Length - 1] = 0;
            // loop through sections
            // all values should correspond to 'values'-array elements 
            for (int i = 0; i < sect.Length; i++)
            {
                if (bv32[sect[i]] != values[i])
                {
                    string temp = "Err" + i;
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], values[i]));
                }
            }

            //
            //    Int32.MinValue
            //
            data = Int32.MinValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            ClearArray(values);
            values[values.Length - 1] = 1;
            // loop through sections
            // all values should correspond to 'values'-array elements 
            for (int i = 0; i < sect.Length; i++)
            {
                if (bv32[sect[i]] != values[i])
                {
                    string temp = "Err" + i;
                    if (i == sect.Length - 1)
                        temp += "section for last bit";
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], values[i]));
                }
            }

            //
            //    1
            //
            data = 1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            ClearArray(values);
            values[0] = 1;
            values[2] = 1;
            values[3] = 1;
            // loop through sections
            // all values should correspond to 'values'-array elements 
            for (int i = 0; i < sect.Length; i++)
            {
                if (bv32[sect[i]] != values[i])
                {
                    string temp = "Err" + i;
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], values[i]));
                }
            }
        }

        private void ClearArray(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = 0;
            }
        }
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class SetItemSectionIntTests
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
            sect[3] = BitVector32.CreateSection(Int16.MaxValue);        // Int16.MaxValue x 0 section
            sect[4] = BitVector32.CreateSection(Int16.MaxValue, sect[3]);        // Int16.MaxValue x 15 section
            sect[5] = BitVector32.CreateSection(1, sect[4]);
            sect[5] = BitVector32.CreateSection(1, sect[5]);        // sign bit section    - last sign bit

            // [] BitVector is constructed as expected and sections return expected values
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data = {0} after default ctor", bv32.Data));
            }

            // loop through sections
            // set all to max
            for (int i = 0; i < sect.Length; i++)
            {
                bv32[sect[i]] = (int)sect[i].Mask;
                if (bv32[sect[i]] != (int)sect[i].Mask)
                {
                    string temp = "Err" + i;
                    if (i == sect.Length - 1)
                        temp += "section for last bit";
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], (int)sect[i].Mask));
                }
            }

            // loop through sections
            // set all to 0
            for (int i = 0; i < sect.Length; i++)
            {
                bv32[sect[i]] = 0;
                if (bv32[sect[i]] != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, bv32[sect[i]], 0));
                }
            }

            // loop through sections
            // set all to 1
            for (int i = 0; i < sect.Length; i++)
            {
                bv32[sect[i]] = 1;
                if (bv32[sect[i]] != 1)
                {
                    string temp = "Err" + i;
                    if (i == sect.Length - 1)
                        temp += "section for last bit";
                    Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], 1));
                }
            }

            // loop through sections
            // set to (max/2) where possible
            for (int i = 0; i < sect.Length; i++)
            {
                if (((int)sect[i].Mask) > 3)
                {
                    data = ((int)sect[i].Mask) / 2;
                    bv32[sect[i]] = data;
                    if (bv32[sect[i]] != data)
                    {
                        string temp = "Err" + i;
                        Assert.False(true, string.Format("{0} returned {1} instead of {2}", temp, bv32[sect[i]], data));
                    }
                }
            }
            // loop through sections
            // set all to out-of-range: (-1)
            //
            // There will be no exception
            // in debug builds assertion is thrown (uncatchable)
            // will skip out-of-range cases in debug build
            //
#if !DEBUG
            for (int i = 0; i < sect.Length; i++)
            {

                bv32[sect[i]] = -1;
                if (bv32[sect[i]] != sect[i].Mask)
                {
                    string temp = "Err" + i;
                    Assert.False(true, temp + ": didn't set section to " + sect[i].Mask);
                }
            }

            // loop through sections
            // set all to out-of-range: Max + 1
            for (int i = 0; i < sect.Length; i++)
            {
                int exp = (int)(sect[i].Mask) + 1;
                bv32[sect[i]] = exp;
                exp = sect[i].Mask & exp;
                if (bv32[sect[i]] != exp)
                {
                    string temp = "Err" + i;
                    Assert.False(true, temp + ": didn't set section to " + exp);
                }
            }
#endif
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
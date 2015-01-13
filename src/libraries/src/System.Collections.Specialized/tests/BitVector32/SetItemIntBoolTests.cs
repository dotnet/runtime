// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class SetItemIntBoolTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            int data = 0;

            // array of positions to get bit for
            int[] bits =
            {
                0,                  //  bits[0] = 0
                1,                  //  bits[1] = 1
                2,                  //  bits[2] = 2
                3,                  //  bits[3] = 3
                7,                  //  bits[4] = 7
                15,                 //  bits[5] = 15
                16,                 //  bits[6] = 16
                Int16.MaxValue,     //  bits[7] = Int16.MaxValue
                Int32.MaxValue - 1, //  bits[8] = Int32.MaxValue - 1
                Int32.MinValue,     //  bits[9] = Int32.MinValue
                Int16.MinValue,     //  bits[10] = Int16.MinValue
                -1                  //  bits[11] = -1
            };

            // [] BitVector is constructed as expected and bit flags are as expected
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            if (bv32.Data != 0)
            {
                Assert.False(true, string.Format("Error, Data = {0} after default ctor", bv32.Data));
            }

            // loop through bits for vector (0)
            // set to true, verify Data, set to false
            bool expected = false;
            for (int i = 0; i < bits.Length; i++)
            {
                bv32[bits[i]] = true;
                if (!bv32[bits[i]])
                {
                    if (i == 9 || i == 10 || i == 11)
                        continue;
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], true, bits[i]));
                }
                if (bv32.Data != bits[i])
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, bits[i]));
                }

                bv32[bits[i]] = false;
                if (i == 0)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], expected, bits[i]));
                }

                if (bv32.Data != 0)
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, 0));
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

            // loop through bits for vector (-1)
            // set to false, verify Data, set to true
            for (int i = 0; i < bits.Length; i++)
            {
                bv32[bits[i]] = false;
                if (i == 0)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], expected, bits[i]));
                }

                if (bv32.Data != ~bits[i])
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, ~bits[i]));
                }

                bv32[bits[i]] = true;
                if (!bv32[bits[i]])
                {
                    if (i == 9 || i == 10 || i == 11)
                        continue;
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], true, bits[i]));
                }
                if (bv32.Data != data)
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, data));
                }
            }


            //
            //   loop through different vectors
            //

            // loop through bits
            // set given bit flag to false, verify Data, set to true
            for (int i = 0; i < bits.Length; i++)
            {
                int ind = i + 3;
                data = bits[i];
                bv32 = new BitVector32(data);
                if (bv32.Data != data)
                {
                    Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data, ind));
                }

                bv32[bits[i]] = false;
                if (i == 0)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], expected, bits[i], ind));
                }

                if (bv32.Data != 0)
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, 0, ind));
                }

                bv32[bits[i]] = true;
                if (!bv32[bits[i]])
                {
                    if (i == 9 || i == 10 || i == 11)
                        continue;
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], true, bits[i], ind));
                }

                if (bv32.Data != data)
                {
                    Assert.False(true, string.Format("Error, Data returned {1} instead of {2}", i, bv32.Data, data, ind));
                }
            }
        }
    }
}
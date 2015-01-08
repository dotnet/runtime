// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetItemIntTests
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

            // loop through bits
            // all values should return false except 0
            bool expected = false;
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == 0)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    Assert.False(true, string.Format("Error, bit[{3}]: returned {1} instead of {2}", i, bv32[bits[i]], expected, bits[i]));
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

            // loop through bits
            // all values should return true 
            expected = true;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }

            //
            //   Int32.MaxValue
            //   
            data = Int32.MaxValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // positive values should return true, negative should return false 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == (bits.Length - 1) || i == (bits.Length - 2) || i == (bits.Length - 3))
                    expected = false;
                else
                    expected = true;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }

            //
            //   Int32.MinValue
            //   
            data = Int32.MinValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except 0, 9 = true 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == 0 || i == 9)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }


            //
            //   1
            //   
            data = 1;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except 0, 1 = true 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == 0 || i == 1)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }


            //
            //  2
            //   
            data = 2;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except 0, 2 = true 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == 0 || i == 2)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }

            //
            //   3
            //   
            data = 3;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except 0, 1, 2, 3 = true 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i < 4)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }

            //
            //   7
            //   
            data = 7;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except 0, 1, 2, 3, 7 = true 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i < 5)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }

            //
            //   Int16.MaxValue
            //   
            data = Int16.MaxValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return true, except for last 3 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i < 8)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Err" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }


            //
            //   Int16.MinValue
            //   
            data = Int16.MinValue;
            bv32 = new BitVector32(data);
            if (bv32.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} ", bv32.Data));
            }

            // loop through bits
            // all values should return false, except for 0, 9, 10 
            for (int i = 0; i < bits.Length; i++)
            {
                if (i == 0 || i == 9 || i == 10)
                    expected = true;
                else
                    expected = false;
                if (bv32[bits[i]] != expected)
                {
                    string temp = "Error" + i;
                    Assert.False(true, string.Format("{0}: bit[{3}]: returned {1} instead of {2}", temp, bv32[bits[i]], expected, bits[i]));
                }
            }
        }
    }
}
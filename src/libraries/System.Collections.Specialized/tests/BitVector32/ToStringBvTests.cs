// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class ToStringBvTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            BitVector32 bv32_1;       // extra BitVector32 - for comparison
            string str = "";              // result of ToString() for bv32
            string str_1 = "";                    // result of ToString() for bv32_1
            int data = 0;

            // [] two BitVectors that are the same should return the same string
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            bv32_1 = new BitVector32();
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two default structs: \"{0}\" != \"{1}\"", str, str_1));
            }


            // generate random data value
            data = -55;
            System.Random random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two equal vectors: \"{0}\" != \"{1}\"", str, str_1));
            }

            bv32 = new BitVector32(data);
            if (data < Int32.MaxValue)
                data++;
            else
                data--;
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) == 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two different vectors: \"{0}\" == \"{1}\"", str, str_1));
            }

            bv32 = new BitVector32();
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of the same default struct: \"{0}\" != \"{1}\"", str, str_1));
            }

            bv32 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of the same vector: \"{0}\" != \"{1}\"", str, str_1));
            }

            data = 0;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two {2}-vectors: \"{0}\" != \"{1}\"", str, str_1, data));
            }

            data = 1;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two {2}-vectors: \"{0}\" != \"{1}\"", str, str_1, data));
            }

            data = -1;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two {2}-vectors: \"{0}\" != \"{1}\"", str, str_1, data));
            }

            data = Int32.MaxValue;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two {2}-vectors: \"{0}\" != \"{1}\"", str, str_1, data));
            }

            data = Int32.MinValue;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            str_1 = BitVector32.ToString(bv32_1);
            if (String.Compare(str, str_1) != 0)
            {
                Assert.False(true, string.Format("Error, ToString() of two {2}-vectors: \"{0}\" != \"{1}\"", str, str_1, data));
            }


            // generate random data value
            data = -55;
            random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bv32 = new BitVector32(data);
            str = BitVector32.ToString(bv32);
            if (str.IndexOf("BitVector32") == -1)
            {
                Assert.False(true, string.Format("Error, ToString() doesn't contain \"BitVector32\""));
            }
        }
    }
}
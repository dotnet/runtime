// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetHashCodeTests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            BitVector32 bv32_1;       // extra BitVector32 - for comparison 
            int code = 0;              // HashCode of bv32
            int code_1 = 0;                    // HashCode of bv32_1
            int data = 0;

            // [] two BitVectors that are the same should return the same HashCode
            //-----------------------------------------------------------------

            bv32 = new BitVector32();
            bv32_1 = new BitVector32();
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two default structs: {0} != {1}", code, code_1));
            }


            // generate random data value
            data = -55;
            System.Random random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two equal vectors: {0} != {1}", code, code_1));
            }

            bv32 = new BitVector32(data);
            if (data < Int32.MaxValue)
                data++;
            else
                data--;
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code == code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two different vectors: {0} == {1}", code, code_1));
            }

            bv32 = new BitVector32();
            code = bv32.GetHashCode();
            code_1 = bv32.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes the same default struct: {0} != {1}", code, code_1));
            }

            bv32 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes the same vector: {0} != {1}", code, code_1));
            }

            data = 0;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two {2}-vectors: {0} != {1}", code, code_1, data));
            }

            data = 1;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two {2}-vectors: {0} != {1}", code, code_1, data));
            }

            data = -1;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two {2}-vectors: {0} != {1}", code, code_1, data));
            }

            data = Int32.MaxValue;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two {2}-vectors: {0} != {1}", code, code_1, data));
            }

            data = Int32.MinValue;
            bv32 = new BitVector32(data);
            bv32_1 = new BitVector32(data);
            code = bv32.GetHashCode();
            code_1 = bv32_1.GetHashCode();
            if (code != code_1)
            {
                Assert.False(true, string.Format("Error, HashCodes of two {2}-vectors: {0} != {1}", code, code_1, data));
            }
        }
    }
}
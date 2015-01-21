// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorBitvector32Tests
    {
        [Fact]
        public void Test01()
        {
            BitVector32 bv32;
            BitVector32 bvExtra;
            int data = 0;

            // [] BitVector is constructed as expected
            //-----------------------------------------------------------------

            data = 0;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = 1;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = -1;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = 2;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = 10;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = 99;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = -9;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = System.Int32.MinValue;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = System.Int32.MaxValue;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = System.Int32.MinValue / 2;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            data = System.Int32.MaxValue / 2;
            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }

            // generate random data value
            data = -55;
            System.Random random = new System.Random(data);
            data = random.Next(System.Int32.MinValue, System.Int32.MaxValue);

            bvExtra = new BitVector32(data);
            if (bvExtra.Data != data)
            {
                Assert.False(true, string.Format("Error, Data = {0} after ctor({1})", bvExtra.Data, data));
            }
            bv32 = new BitVector32(bvExtra);
            if (bv32.Data != bvExtra.Data)
            {
                Assert.False(true, string.Format("Error, Data: returned: {0} , expected: {1}", bv32.Data, bvExtra.Data));
            }
        }
    }
}
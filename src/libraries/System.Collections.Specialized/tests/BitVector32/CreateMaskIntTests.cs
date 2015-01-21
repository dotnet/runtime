// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CreateMaskIntTests
    {
        [Fact]
        public void Test01()
        {
            int mask = 0;                   // returned mask
            int maskArgument = 0;        // mask to be used as CreateMask() argument
            int iter = 0;               //number of iteration (for list of masks)
            int expected = 0;              // expected mask

            // [] Mask is constructed as expected
            //-----------------------------------------------------------------

            maskArgument = BitVector32.CreateMask();
            expected = (int)System.Math.Pow(2, iter);
            if (maskArgument != expected)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", maskArgument, expected));
            }

            iter++;
            mask = BitVector32.CreateMask(maskArgument);
            expected = (int)System.Math.Pow(2, iter);
            if (mask != expected)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", mask, expected));
            }

            int cnt = 50;                 // number of attempts to create same mask
            for (int i = 0; i < cnt; i++)
            {
                mask = BitVector32.CreateMask(maskArgument);
                if (mask != expected)
                {
                    string err = "Error" + (i + 1).ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", mask, expected));
                }
            }

            iter++;
            maskArgument = BitVector32.CreateMask(BitVector32.CreateMask());
            expected = (int)System.Math.Pow(2, iter);
            mask = BitVector32.CreateMask(maskArgument);
            if (mask != expected)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", mask, expected));
            }

            for (int i = 0; i < cnt; i++)
            {
                mask = BitVector32.CreateMask(maskArgument);
                if (mask != expected)
                {
                    string err = "Error" + (i + 1).ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", mask, expected));
                }
            }

            iter++;
            maskArgument = BitVector32.CreateMask(BitVector32.CreateMask(BitVector32.CreateMask()));
            expected = (int)System.Math.Pow(2, iter);
            mask = BitVector32.CreateMask(maskArgument);
            if (mask != expected)
            {
                Assert.False(true, string.Format("Error, Created Mask: {0} , expected: {1}", mask, expected));
            }

            for (int i = 0; i < cnt; i++)
            {
                mask = BitVector32.CreateMask(maskArgument);
                if (mask != expected)
                {
                    string err = "Error" + (i + 1).ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", mask, expected));
                }
            }

            // [] circular list with masks
            //-----------------------------------------------------------------

            maskArgument = BitVector32.CreateMask();
            mask = 0;
            for (iter = 1; iter < 32; iter++)
            {
                mask = BitVector32.CreateMask(maskArgument);
                expected = (int)(1 << iter);
                if (mask != expected)
                {
                    string err = "Error" + iter.ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", maskArgument, expected));
                }
                maskArgument = mask;
            }

            maskArgument = Int32.MaxValue;
            mask = 0;
            for (iter = 1; iter < 32; iter++)
            {
                mask = BitVector32.CreateMask(maskArgument);
                expected = (-1) * (int)(1 << iter);
                if (mask != expected)
                {
                    string err = "Error" + iter.ToString();
                    Assert.False(true, string.Format(err + ", Created Mask: {0} , expected: {1}", maskArgument, expected));
                }
                maskArgument = mask;
            }


            maskArgument = 0;
            mask = 0;
            expected = (int)System.Math.Pow(2, maskArgument);
            mask = BitVector32.CreateMask(maskArgument);
            if (mask != expected)
            {
                Assert.False(true, string.Format("Error, CreatedMask() returned: {0} , expected: {1}", mask, expected));
            }


            // [] random argument-mask
            //-----------------------------------------------------------------

            // generate random previous mask value
            maskArgument = -55;
            System.Random random = new System.Random(maskArgument);
            maskArgument = random.Next(System.Int32.MinValue / 2 + 1, System.Int32.MaxValue / 2 - 1);

            mask = 0;
            if (maskArgument == 0)
                expected = (int)System.Math.Pow(2, maskArgument);
            else
                expected = maskArgument * 2;
            mask = BitVector32.CreateMask(maskArgument);
            if (mask != expected)
            {
                Assert.False(true, string.Format("Error, CreatedMask() returned: {0} , expected: {1}", mask, expected));
            }

            // [] out or range argument-mask
            //-----------------------------------------------------------------

            maskArgument = Int32.MinValue;
            mask = 0;
            Assert.Throws<InvalidOperationException>(() => { mask = BitVector32.CreateMask(maskArgument); });
        }
    }
}
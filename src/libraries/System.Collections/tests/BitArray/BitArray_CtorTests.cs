// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace BitArrayTests
{
    public class BitArray_CtorTests
    {
        /// <summary>
        /// Test that BitArray(int) is constructed with right size and all bits set to false
        /// When constructed all values should be set to false
        /// </summary>
        [Fact]
        public static void BitArray_CtorIntTest()
        {
            int arraySize = 40;
            int i = 0;

            BitArray bitArray = new BitArray(arraySize);

            for (i = 0; i < 40; i++)
                if (bitArray.Get(i))
                    break;

            Assert.Equal(i, arraySize); //"Err_1! i should be equal to size"
            Assert.Equal(bitArray.Length, arraySize); //"Err_2! ba1.Length should be equal to size"

            arraySize = 200;
            bitArray = new BitArray(arraySize);

            for (i = 0; i < 200; i++)
                if (bitArray.Get(i))
                    break;

            Assert.Equal(i, arraySize); //"Err_3! i should be equal to size"
            Assert.Equal(bitArray.Length, arraySize); //"Err_4! ba1.Length should be equal to size"
        }

        /// <summary>
        /// Test that BitArray(Boolean[]) is constructed with right values
        /// </summary>
        [Fact]
        public static void BitArray_CtorBoolArrTest()
        {
            BitArray bitArr1;
            Boolean[] bolArr1;

            int arraySize;

            // Create a bitarray with bool[] and check the results
            arraySize = 10;
            int[] valuesArray = new int[] { 5, 1, 8, 0, 1, 2, 7, 3, 3, 6 };

            bolArr1 = new Boolean[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                if (valuesArray[i] > 5)
                    bolArr1[i] = true;
                else
                    bolArr1[i] = false;
            }
            bitArr1 = new BitArray(bolArr1);

            Assert.Equal(bitArr1.Length, arraySize); //"Err_5! bitArr1.Length should be equal to arraySize"

            for (int i = 0; i < bitArr1.Length; i++)
            {
                if (bolArr1[i] != bitArr1[i])
                {
                    Assert.Equal(bolArr1[i], bitArr1[i]); //"Err_6! bolArr1[i] should be equal to bitArr1[i]"
                }
            }
        }


        /// <summary>
        /// Test that BitArray(BitArray) is constructed with right values
        /// </summary>
        [Fact]
        public static void BitArray_CtorBitArrayTest()
        {
            // Creating bit array with 10 values that have all 1s
            BitArray ba1 = null;
            BitArray bits = null;
            int[] values = null;
            int i;

            values = new int[10];
            for (i = 0; i < 10; i++)
                values[i] = unchecked((int)0xffffffff);

            bits = new BitArray(values);
            ba1 = new BitArray(bits);

            Assert.Equal(ba1.Length, 320); //"Err_7! ba1.Length should be equal to 320"

            for (i = 0; i < 320; i++)
            {
                Assert.True(ba1.Get(i)); //"Err_8! array should contain all 1s"
            }

            // Creating BitArray with 10 values that have all 0s
            values = new int[10];
            for (i = 0; i < 10; i++)
                values[i] = 0;

            bits = new BitArray(values);
            ba1 = new BitArray(bits);

            Assert.Equal(ba1.Length, 320); //"Err_9! ba1.Length should be equal to 320"

            for (i = 0; i < 320; i++)
            {
                Assert.False(ba1.Get(i)); //"Err_10! array should contain all 0s"
            }


            // Creating BitArray with 10 values that have zeros and ones
            values = new int[10];
            for (i = 0; i < 10; i++)
                values[i] = unchecked((int)0xaaaaaaaa);

            bits = new BitArray(values);
            ba1 = new BitArray(bits);

            Assert.Equal(ba1.Length, 320); //"Err_11! ba1.Length should be equal to 320"

            for (i = 0; i < 320; i++)
            {
                if (i % 2 == 1)
                    Assert.True(ba1.Get(i)); //"Err_12! index should contain 1"
                else
                    Assert.False(ba1.Get(i)); //"Err_13! index should contain 0"
            }
        }

        /// <summary>
        /// Test that we get ArgumentOutOfRangeException when length is less than zero     
        /// </summary>
        [Fact]
        public static void BitArray_CtorIntTest_Negative()
        {
            int i = -1;
            Assert.Throws<ArgumentOutOfRangeException>(delegate { new BitArray(i); }); //"Err_14! wrong exception thrown."
        }

        /// <summary>
        /// Test that we get ArgumentNullException when argument is null
        /// </summary>
        [Fact]
        public static void BitArray_CtorBitArrayTest_Negative()
        {
            BitArray bits = null;
            Assert.Throws<ArgumentNullException>(delegate { new BitArray(bits); }); //"Err_15! wrong exception thrown."
        }

        /// <summary>
        /// Test that we get ArgumentNullException when argument is null
        /// </summary>
        [Fact]
        public static void BitArray_CtorBoolArrayTest_Negative()
        {
            Boolean[] boolArray = null;
            Assert.Throws<ArgumentNullException>(delegate { new BitArray(boolArray); }); //"Err_16! wrong exception thrown."
        }
    }
}

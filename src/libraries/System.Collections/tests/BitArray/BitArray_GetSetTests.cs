// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;
using BitArrayTests.BitArray_BitArray_GetSetTests;

namespace BitArrayTests
{
    namespace BitArray_BitArray_GetSetTests
    {
        public class BitArray_OperatorsTests
        {
            /// <summary>
            /// Test BitArray.Get 
            /// </summary>
            [Fact]
            public static void BitArray_GetTest()
            {
                BitArray ba2 = new BitArray(6, false);

                ba2.Set(0, true);
                ba2.Set(1, false);
                ba2.Set(2, true);
                ba2.Set(5, true);

                Assert.True(ba2.Get(0)); //"Err_1! Expected ba4.Get(0) to be true"
                Assert.False(ba2.Get(1)); //"Err_2! Expected ba4.Get(1) to be false"
                Assert.True(ba2.Get(2)); //"Err_3! Expected ba4.Get(2) to be true"
                Assert.False(ba2.Get(3)); //"Err_4! Expected ba4.Get(3) to be false"
                Assert.False(ba2.Get(4)); //"Err_5! Expected ba4.Get(4) to be false"
                Assert.True(ba2.Get(5)); //"Err_6! Expected ba4.Get(5) to be true"
            }

            /// <summary>
            /// Test BitArray.Set 
            /// </summary>
            [Fact]
            public static void BitArray_SetTest()
            {
                // []  Set true to true, true to false
                // []  Set false to false and false to true is covered in BitArray_GetTest() above
                BitArray ba2 = new BitArray(6, true);

                ba2.Set(0, true);
                ba2.Set(1, false);
                ba2.Set(2, true);
                ba2.Set(5, true);

                Assert.True(ba2.Get(0)); //"Err_7! Expected ba4.Get(0) to be true"
                Assert.False(ba2.Get(1)); //"Err_8! Expected ba4.Get(1) to be false"
                Assert.True(ba2.Get(2)); //"Err_9! Expected ba4.Get(2) to be true"
            }


            /// <summary>
            /// Test BitArray.Set 
            /// </summary>
            [Fact]
            public static void BitArray_SetAllTest()
            {
                BitArray ba2 = new BitArray(6, false);

                Assert.False(ba2.Get(0)); //"Err_10! Expected ba4.Get(0) to be false"
                Assert.False(ba2.Get(5)); //"Err_11! Expected ba4.Get(1) to be false"

                // false to true
                ba2.SetAll(true);

                Assert.True(ba2.Get(0)); //"Err_12! Expected ba4.Get(0) to be true"
                Assert.True(ba2.Get(5)); //"Err_13! Expected ba4.Get(1) to be true"


                // false to false
                ba2.SetAll(false);

                Assert.False(ba2.Get(0)); //"Err_14! Expected ba4.Get(0) to be false"
                Assert.False(ba2.Get(5)); //"Err_15! Expected ba4.Get(1) to be false"

                ba2 = new BitArray(6, true);

                Assert.True(ba2.Get(0)); //"Err_16! Expected ba4.Get(0) to be true"
                Assert.True(ba2.Get(5)); //"Err_17! Expected ba4.Get(1) to be true"

                // true to true
                ba2.SetAll(true);

                Assert.True(ba2.Get(0)); //"Err_18! Expected ba4.Get(0) to be true"
                Assert.True(ba2.Get(5)); //"Err_19! Expected ba4.Get(1) to be true"

                // true to false
                ba2.SetAll(false);

                Assert.False(ba2.Get(0)); //"Err_20! Expected ba4.Get(0) to be false"
                Assert.False(ba2.Get(5)); //"Err_21! Expected ba4.Get(1) to be false"

                // []  Size stress.
                int size = 0x1000F;
                ba2 = new BitArray(size, true);

                Assert.True(ba2.Get(0)); //"Err_22! Expected ba4.Get(0) to be true"
                Assert.True(ba2.Get(size - 1)); //"Err_23! Expected ba4.Get(size-1) to be true"

                ba2.SetAll(false);

                Assert.False(ba2.Get(0)); //"Err_24! Expected ba4.Get(0) to be false"
                Assert.False(ba2.Get(size - 1)); //"Err_25! Expected ba4.Get(size-1) to be false"
            }


            /// <summary>
            /// Test BitArray.GetEnumerator 
            /// </summary>
            [Fact]
            public static void BitArray_GetEnumeratorTest()
            {
                int size = 10;

                Boolean[] bolArr1 = new Boolean[size];

                for (int i = 0; i < size; i++)
                {
                    if (i > 5)
                        bolArr1[i] = true;
                    else
                        bolArr1[i] = false;
                }

                BitArray bitArr1 = new BitArray(bolArr1);
                IEnumerator ienm1 = bitArr1.GetEnumerator();

                int iCount = 0;

                while (ienm1.MoveNext())
                {
                    Assert.Equal((Boolean)ienm1.Current, bolArr1[iCount++]); //"Err_26! wrong value returned"
                }

                ienm1.Reset();
                iCount = 0;
                while (ienm1.MoveNext())
                {
                    Assert.Equal((Boolean)ienm1.Current, bolArr1[iCount++]); //"Err_27! wrong value returned"
                }
            }

            /// <summary>
            /// Test BitArray.set_Length
            /// </summary>
            [Fact]
            public static void BitArray_SetLengthTest()
            {
                // []  Standard increase of length.
                BitArray ba2 = null;

                int size = 16;
                ba2 = new BitArray(size, true);

                ba2.Length = size * 3;

                // If Length is set to a value that is greater than Count, the new elements are set to false.
                Assert.False(ba2.Get(size * 2)); //"Err_28! Expected ba2.Get(size * 2) to be false"


                ba2 = new BitArray(size, true);

                ba2.Length = size / 2;

                Assert.True(ba2.Get((size / 2) - 2)); //"Err_29! Expected ba2.Get(size * 2) to be true"


                size = 16384;
                ba2 = new BitArray(size);

                for (int i = 0; i < size; i++)
                {
                    ba2[i] = 0 == i % 2;
                }

                ba2.Length = 256;

                for (int i = 0; i < 256; i++)
                {
                    Assert.Equal(ba2[i], (0 == i % 2)); //"Err_30! Expected values to be equal"
                }

                Assert.Equal(ba2.Length, 256); //"Err_31! Expected values to be equal"

                // [A2]  Show original bit values are reset by decreasing-then-increasing size over them.
                ba2.Length = 0;
                ba2.Length = size;

                Assert.False(ba2.Get(size - 1)); //"Err_32! Expected ba2.Get(size-1) to be false"
            }

            /// <summary>
            /// Test BitArray.get_Length
            /// </summary>
            [Fact]
            public static void BitArray_GetLengthTest()
            {
                // []  Standard.
                int size = 6;
                BitArray ba2 = new BitArray(size, false);

                Assert.Equal(ba2.Length, size); //"Err_33! values are not equal"

                // []  Boundary.
                size = 0;
                ba2 = new BitArray(size, false);

                Assert.Equal(ba2.Length, size); //"Err_34! values are not equal"

                // []  Size stress.
                size = 0x1000F;
                ba2 = new BitArray(size, false);

                Assert.Equal(ba2.Length, size); //"Err_35! values are not equal"
            }

            /// <summary>
            /// Get negative test
            /// </summary>
            [Fact]
            public static void BitArray_GetTest_Negative()
            {
                BitArray ba = new BitArray(6, false);

                // index is less than zero
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba.Get(-3); }); //"Err_36! wrong exception thrown."

                // index is greater than or equal to the number of elements in the BitArray. 
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba.Get(10); }); //"Err_37! wrong exception thrown."
            }

            /// <summary>
            /// Set negative test
            /// </summary>
            [Fact]
            public static void BitArray_SetTest_Negative()
            {
                BitArray ba = new BitArray(6, false);

                // index is less than zero
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba.Set(-3, false); }); //"Err_38! wrong exception thrown."

                // index is greater than or equal to the number of elements in the BitArray. 
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba.Set(10, false); }); //"Err_39! wrong exception thrown."
            }

            /// <summary>
            /// GetEnumerator negative test
            /// </summary>
            [Fact]
            public static void BitArray_GetEnumeratorTest_Negative()
            {
                int size = 10;

                Boolean[] bolArr1 = new Boolean[size];

                for (int i = 0; i < size; i++)
                {
                    if (i > 5)
                        bolArr1[i] = true;
                    else
                        bolArr1[i] = false;
                }

                BitArray bitArr1 = new BitArray(bolArr1);
                IEnumerator ienm1 = bitArr1.GetEnumerator();

                // test that initially enumerator is positioned before the first element in the collection --> Current will be undefined
                Assert.Throws<InvalidOperationException>(delegate { Object obj = ienm1.Current; }); //"Err_40! wrong exception thrown."

                // get to the end of the collection
                while (ienm1.MoveNext()) ;

                // test that after MoveNext() returns false (i.e. we are at the end) enumerator is positioned after the last element in the collection --> Current will be undefined
                Assert.Throws<InvalidOperationException>(delegate { Object obj = ienm1.Current; }); //"Err_41! wrong exception thrown."


                //[] we will change the underlying BitArray and see the effect
                ienm1.Reset();
                ienm1.MoveNext();
                bitArr1[0] = false;

                // we do not throw exception when getting Current
                Object obj2 = ienm1.Current;

                // test that the enumerator is not valid after modifying collection
                Assert.Throws<InvalidOperationException>(delegate { ienm1.MoveNext(); }); //"Err_42! wrong exception thrown."
                Assert.Throws<InvalidOperationException>(delegate { ienm1.Reset(); }); //"Err_43! wrong exception thrown."
            }

            /// <summary>
            /// BitArray.set_Length negative test
            /// </summary>
            [Fact]
            public static void BitArray_SetLengthTest_Negative()
            {
                // []  decrease of length.
                int size = 16;
                BitArray ba2 = new BitArray(size, true);
                ba2.Length = size / 2;

                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba2.Get(size); }); //"Err_44! wrong exception thrown."


                // []  LARGE decrease of length.
                // Our implementation does not actually shrink the size of the array unless there is a decrease greater then 256 * 4 * 8 (8192)bits
                size = 16384;
                ba2 = new BitArray(size);

                for (int i = 0; i < size; i++)
                {
                    ba2[i] = 0 == i % 2;
                }

                ba2.Length = 256;

                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba2.Get(265); }); //"Err_45! wrong exception thrown."


                // [A1]  Zero length and negative length, Exception.
                ba2.Length = 0;
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba2.Get(0); }); //"Err_46! wrong exception thrown."
                Assert.Throws<ArgumentOutOfRangeException>(delegate { ba2.Length = -5; }); //"Err_47! wrong exception thrown."
            }

            /// <summary>
            /// BitArray.get_Length negative test
            /// </summary>
            [Fact]
            public static void BitArray_GetLengthTest_Negative()
            {
                // []  ArgumentException, less than zero.
                int size = -3;

                Assert.Throws<ArgumentOutOfRangeException>(delegate { new BitArray(size, false); }); //"Err_48! wrong exception thrown."
            }
        }
    }
}

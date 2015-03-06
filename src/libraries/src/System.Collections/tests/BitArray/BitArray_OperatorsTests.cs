// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;
using BitArrayTests.BitArray_BitArray_OperatorsTests;

namespace BitArrayTests
{
    public enum Operator { Xor, Or, And };
    namespace BitArray_BitArray_OperatorsTests
    {
        public class BitArray_OperatorsTests
        {
            /// <summary>
            /// Test BitArray.And Operator
            /// </summary>
            [Fact]
            public static void BitArray_AndTest()
            {
                BitArray_Helper(Operator.And);

                // [] Test to make sure that 0 sized bit arrays can be And-ed

                BitArray b1 = new BitArray(0);
                BitArray b2 = new BitArray(0);

                b1.And(b2);
            }

            /// <summary>
            /// Test BitArray.Not Operator
            /// </summary>
            [Fact]
            public static void BitArray_NotTest()
            {
                // []  Standard cases 

                BitArray ba2 = null;
                BitArray ba4 = null;

                ba2 = new BitArray(6, false);

                ba2.Set(0, true);
                ba2.Set(1, true);

                ba4 = ba2.Not();

                for (int i = 0; i < ba4.Length; i++)
                {
                    if (i <= 1)
                        Assert.False(ba4.Get(i)); //"Err_2! ba4.Get(" + i + ") should be false"
                    else
                        Assert.True(ba4.Get(i)); //"Err_3! ba4.Get(" + i + ") should be true"
                }



                // []  Size stress cases.
                ba2 = new BitArray(0x1000F, false);

                ba2.Set(0, true);
                ba2.Set(1, true);

                ba2.Set(0x10000, true);
                ba2.Set(0x10001, true);

                ba4 = ba2.Not();

                Assert.False(ba4.Get(1)); //"Err_4! ba4.Get(1) should be false"
                Assert.True(ba4.Get(2)); //"Err_5! ba4.Get(2) should be true"

                for (int i = 0x10000; i < ba2.Length; i++)
                {
                    if (i <= 0x10001)
                        Assert.False(ba4.Get(i)); //"Err_6! ba4.Get(" + i + ") should be false"
                    else
                        Assert.True(ba4.Get(i)); //"Err_7! ba4.Get(" + i + ") should be true"
                }
            }

            /// <summary>
            /// Test BitArray.Or Operator
            /// </summary>
            [Fact]
            public static void BitArray_OrTest()
            {
                BitArray_Helper(Operator.Or);
            }

            /// <summary>
            /// Test BitArray.Xor Operator
            /// </summary>
            [Fact]
            public static void BitArray_XorTest()
            {
                BitArray_Helper(Operator.Xor);
            }

            public static void BitArray_Helper(Operator op)
            {
                BitArray ba2 = null;
                BitArray ba3 = null;
                BitArray ba4 = null;

                // []  Standard cases (1,1) (1,0) (0,0).
                ba2 = new BitArray(6, false);
                ba3 = new BitArray(6, false);

                ba2.Set(0, true);
                ba2.Set(1, true);

                ba3.Set(1, true);
                ba3.Set(2, true);

                switch (op)
                {
                    case Operator.Xor:
                        ba4 = ba2.Xor(ba3);
                        Assert.True(ba4.Get(0)); //"Err_8! Expected ba4.Get(0) to be true"
                        Assert.False(ba4.Get(1)); //"Err_9! Expected ba4.Get(1) to be false"
                        Assert.True(ba4.Get(2)); //"Err_10! Expected ba4.Get(2) to be true"
                        Assert.False(ba4.Get(4)); //"Err_11! Expected ba4.Get(4) to be false"
                        break;

                    case Operator.And:
                        ba4 = ba2.And(ba3);
                        Assert.False(ba4.Get(0)); //"Err_12! Expected ba4.Get(0) to be false"
                        Assert.True(ba4.Get(1)); //"Err_13! Expected ba4.Get(1) to be true"
                        Assert.False(ba4.Get(2)); //"Err_14! Expected ba4.Get(2) to be false"
                        Assert.False(ba4.Get(4)); //"Err_15! Expected ba4.Get(4) to be false"
                        break;

                    case Operator.Or:
                        ba4 = ba2.Or(ba3);
                        Assert.True(ba4.Get(0)); //"Err_16! Expected ba4.Get(0) to be true"
                        Assert.True(ba4.Get(1)); //"Err_17! Expected ba4.Get(1) to be true"
                        Assert.True(ba4.Get(2)); //"Err_18! Expected ba4.Get(2) to be true"
                        Assert.False(ba4.Get(4)); //"Err_19! Expected ba4.Get(4) to be false"
                        break;
                }


                // []  Size stress cases.
                ba2 = new BitArray(0x1000F, false);
                ba3 = new BitArray(0x1000F, false);

                ba2.Set(0x10000, true); // The bit for 1 (2^0).
                ba2.Set(0x10001, true); // The bit for 2 (2^1).

                ba3.Set(0x10001, true); // The bit for 2 (2^1).

                switch (op)
                {
                    case Operator.Xor:
                        ba4 = ba2.Xor(ba3);
                        Assert.True(ba4.Get(0x10000)); //"Err_20! Expected ba4.Get(0x10000) to be true"
                        Assert.False(ba4.Get(0x10001)); //"Err_21! Expected ba4.Get(0x10001) to be false"
                        Assert.False(ba4.Get(0x10002)); //"Err_22! Expected ba4.Get(0x10002) to be false"
                        Assert.False(ba4.Get(0x10004)); //"Err_23! Expected ba4.Get(0x10004) to be false"
                        break;

                    case Operator.And:
                        ba4 = ba2.And(ba3);
                        Assert.False(ba4.Get(0x10000)); //"Err_24! Expected ba4.Get(0x10000) to be false"
                        Assert.True(ba4.Get(0x10001)); //"Err_25! Expected ba4.Get(0x10001) to be true"
                        Assert.False(ba4.Get(0x10002)); //"Err_26! Expected ba4.Get(0x10002) to be false"
                        Assert.False(ba4.Get(0x10004)); //"Err_27! Expected ba4.Get(0x10004) to be false"
                        break;

                    case Operator.Or:
                        ba4 = ba2.Or(ba3);
                        Assert.True(ba4.Get(0x10000)); //"Err_28! Expected ba4.Get(0x10000) to be true"
                        Assert.True(ba4.Get(0x10001)); //"Err_29! Expected ba4.Get(0x10001) to be true"
                        Assert.False(ba4.Get(0x10002)); //"Err_30! Expected ba4.Get(0x10002) to be false"
                        Assert.False(ba4.Get(0x10004)); //"Err_31! Expected ba4.Get(0x10004) to be false"
                        break;
                }
            }

            /// <summary>
            /// And negative test
            /// </summary>
            [Fact]
            public static void BitArray_AndTest_Negative()
            {
                // []  ArgumentException, length of arrays is different
                BitArray ba2 = new BitArray(11, false);
                BitArray ba3 = new BitArray(6, false);

                Assert.Throws<ArgumentException>(delegate { ba2.And(ba3); }); //"Err_32! wrong exception thrown."
                Assert.Throws<ArgumentException>(delegate { ba3.And(ba2); }); //"Err_33! wrong exception thrown."


                // []  ArgumentNullException, null.
                ba2 = new BitArray(6, false);
                ba3 = null;

                Assert.Throws<ArgumentNullException>(delegate { ba2.And(ba3); }); //"Err_34! wrong exception thrown."
            }

            /// <summary>
            /// Or negative test
            /// </summary>
            [Fact]
            public static void BitArray_OrTest_Negative()
            {
                // []  ArgumentException, length of arrays is different
                BitArray ba2 = new BitArray(11, false);
                BitArray ba3 = new BitArray(6, false);

                Assert.Throws<ArgumentException>(delegate { ba2.Or(ba3); }); //"Err_35! wrong exception thrown."

                // []  ArgumentNullException, null.
                ba2 = new BitArray(6, false);
                ba3 = null;

                Assert.Throws<ArgumentNullException>(delegate { ba2.Or(ba3); }); //"Err_36! wrong exception thrown."
            }

            /// <summary>
            /// Xor negative test
            /// </summary>
            [Fact]
            public static void BitArray_XorTest_Negative()
            {
                // []  ArgumentException, length of arrays is different
                BitArray ba2 = new BitArray(11, false);
                BitArray ba3 = new BitArray(6, false);

                Assert.Throws<ArgumentException>(delegate { ba2.Xor(ba3); }); //"Err_37! wrong exception thrown."

                // []  ArgumentNullException, null.
                ba2 = new BitArray(6, false);
                ba3 = null;

                Assert.Throws<ArgumentNullException>(delegate { ba2.Xor(ba3); }); //"Err_38! wrong exception thrown."
            }
        }
    }
}

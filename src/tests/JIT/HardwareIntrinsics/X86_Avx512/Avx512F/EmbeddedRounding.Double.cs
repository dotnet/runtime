// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512F
{
    public partial class Program
    {
        [Fact]
        public static unsafe void AddEmbeddedRounding()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[8] {0x3fe0000000000000, 0x3fe0000000000000, 0x3fdfffffffffffff, 0x3fe0000000000000,
                                                                   0xbfe0000000000000, 0xbfe0000000000000, 0xbfe0000000000000, 0xbfe0000000000001};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[8] {0x3fe0000000000001, 0x3fe0000000000001, 0x3fe0000000000000, 0x3fe0000000000000,
                                                                   0xbfe0000000000000, 0xbfe0000000000000, 0xbfdfffffffffffff, 0xbfe0000000000000};
            ulong[] answerTable_ToZero = new ulong[8] {0x3fe0000000000000, 0x3fe0000000000000, 0x3fdfffffffffffff, 0x3fe0000000000000,
                                                       0xbfe0000000000000, 0xbfe0000000000000, 0xbfdfffffffffffff, 0xbfe0000000000000};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 0.05 , 0.10, 0.15, 0.20, -0.25, -0.30, -0.35, -0.40 }, new double[8] { 0.45 , 0.40, 0.35, 0.30, -0.25, -0.20, -0.15, -0.10 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on double with ToZero:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }
                }
            }

            Assert.Equal(1, testResult);
        }
    }
}

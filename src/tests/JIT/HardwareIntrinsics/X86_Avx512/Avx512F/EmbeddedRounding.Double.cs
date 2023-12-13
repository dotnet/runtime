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
        public static unsafe void ConvertToInt32EmbeddedRounding_Double()
        {
            int testResult = 1;
            int answerTable_ToNegativeInfinity = -1;
            int answerTable_ToPositiveInfinity  = 0;
            int answerTable_ToZero = 0;
            if (Avx512F.IsSupported)
            {
                Vector128<double> inputVec = Vector128.Create(-0.45, -0.45);
                int res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on double with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on double with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on double with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertToUInt32EmbeddedRounding_Double()
        {
            int testResult = 1;
            uint answerTable_ToNegativeInfinity = 4294967295;
            uint answerTable_ToPositiveInfinity  = 0;
            uint answerTable_ToZero = 0;
            if (Avx512F.IsSupported)
            {
                Vector128<double> inputVec = Vector128.Create(-0.45, -0.45);
                uint res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on double with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on double with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on double with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertToInt64EmbeddedRounding_Double()
        {
            int testResult = 1;
            long answerTable_ToNegativeInfinity = -1;
            long answerTable_ToPositiveInfinity  = 0;
            long answerTable_ToZero = 0;
            if (Avx512F.X64.IsSupported)
            {
                Vector128<double> inputVec = Vector128.Create(-0.45, -0.45);
                long res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on double with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on double with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on double with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertToUInt64EmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong answerTable_ToNegativeInfinity = 18446744073709551615;
            ulong answerTable_ToPositiveInfinity  = 0;
            ulong answerTable_ToZero = 0;
            if (Avx512F.X64.IsSupported)
            {
                Vector128<double> inputVec = Vector128.Create(-0.45, -0.45);
                ulong res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on double with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on double with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on double with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertScalarToVector128DoubleInt64EmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x402e000000000000, 0xbff0000000000000};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x402e000000000000, 0xbff0000000000000};
            ulong[] answerTable_ToZero = new ulong[2] {0x402e000000000000, 0xbff0000000000000};     
            if (Avx512F.X64.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { -1.0f, -1.0f}, new double[2]))
                {
                    var upper = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    long value = 15;
                    var vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on Int64 input with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on Int64 input with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on Int64 input with ToZero:");
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

        [Fact]
        public static unsafe void ConvertScalarToVector128DoubleUInt64EmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x402e000000000000, 0xbff0000000000000};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x402e000000000000, 0xbff0000000000000};
            ulong[] answerTable_ToZero = new ulong[2] {0x402e000000000000, 0xbff0000000000000};    
            if (Avx512F.X64.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { -1.0f, -1.0f}, new double[2]))
                {
                    var upper = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    ulong value = 15;
                    var vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on UInt64 input with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on UInt64 input with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Double(upper, value, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Double Embedded rounding failed on UInt64 input with ToZero:");
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

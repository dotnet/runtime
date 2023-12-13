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
        public static unsafe void ConvertToInt32EmbeddedRounding_Single()
        {
            int testResult = 1;
            int answerTable_ToNegativeInfinity = -1;
            int answerTable_ToPositiveInfinity  = 0;
            int answerTable_ToZero = 0;
            if (Avx512F.IsSupported)
            {
                Vector128<float> inputVec = Vector128.Create(-0.45f, -0.45f, -0.45f, -0.45f);
                int res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToInt32(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToInt32 Embedded rounding failed on float with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertToUInt32EmbeddedRounding_Single()
        {
            int testResult = 1;
            uint answerTable_ToNegativeInfinity = 4294967295;
            uint answerTable_ToPositiveInfinity  = 0;
            uint answerTable_ToZero = 0;
            if (Avx512F.IsSupported)
            {
                Vector128<float> inputVec = Vector128.Create(-0.45f, -0.45f, -0.45f, -0.45f);
                uint res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on float with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on float with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.ConvertToUInt32(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToUInt32 Embedded rounding failed on float with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertScalarToVector128SingleInt32EmbeddedRounding_Single()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};
            uint[] answerTable_ToPositiveInfinity  = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};
            uint[] answerTable_ToZero = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};     
            if (Avx512F.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { -1.0f, -1.0f, -1.0f, -1.0f }, new float[4]))
                {
                    var upper = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    int value = 15;
                    var vd3 = Avx512F.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int32 input with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int32 input with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int32 input with ToZero:");
                            foreach (var item in floatTable.outArray)
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
        public static unsafe void ConvertToInt64EmbeddedRounding_Single()
        {
            int testResult = 1;
            long answerTable_ToNegativeInfinity = -1;
            long answerTable_ToPositiveInfinity  = 0;
            long answerTable_ToZero = 0;
            if (Avx512F.X64.IsSupported)
            {
                Vector128<float> inputVec = Vector128.Create(-0.45f, -0.45f, -0.45f, -0.45f);
                long res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on float with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on float with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToInt64(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToInt64 Embedded rounding failed on float with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

                [Fact]
        public static unsafe void ConvertToUInt64EmbeddedRounding_Single()
        {
            int testResult = 1;
            ulong answerTable_ToNegativeInfinity = 18446744073709551615;
            ulong answerTable_ToPositiveInfinity  = 0;
            ulong answerTable_ToZero = 0;
            if (Avx512F.X64.IsSupported)
            {
                Vector128<float> inputVec = Vector128.Create(-0.45f, -0.45f, -0.45f, -0.45f);
                ulong res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToNegativeInfinity);

                if (res != answerTable_ToNegativeInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on float with ToNegativeInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToPositiveInfinity);

                if (res != answerTable_ToPositiveInfinity)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on float with ToPositiveInfinity:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }

                res = Avx512F.X64.ConvertToUInt64(inputVec, FloatRoundingMode.ToZero);

                if (res != answerTable_ToZero)
                {
                    Console.WriteLine("Avx512 ConvertToUInt64 Embedded rounding failed on float with ToZero:");
                    Console.Write(res);
                    Console.WriteLine();
                    Assert.Fail("");
                }
            }
            Assert.Equal(1, testResult);
        }

        [Fact]
        public static unsafe void ConvertScalarToVector128SingleInt64EmbeddedRounding_Single()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000,};
            uint[] answerTable_ToPositiveInfinity  = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};
            uint[] answerTable_ToZero = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};     
            if (Avx512F.X64.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { -1.0f, -1.0f, -1.0f, -1.0f }, new float[4]))
                {
                    var upper = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    long value = 15;
                    var vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int64 input with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int64 input with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on Int64 input with ToZero:");
                            foreach (var item in floatTable.outArray)
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
        public static unsafe void ConvertScalarToVector128SingleUInt64EmbeddedRounding_Single()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000,};
            uint[] answerTable_ToPositiveInfinity  = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};
            uint[] answerTable_ToZero = new uint[4] {0x41700000, 0xbf800000, 0xbf800000, 0xbf800000};     
            if (Avx512F.X64.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { -1.0f, -1.0f, -1.0f, -1.0f }, new float[4]))
                {
                    var upper = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    ulong value = 15;
                    var vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on UInt64 input with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on UInt64 input with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.X64.ConvertScalarToVector128Single(upper, value, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 ConvertScalarToVector128Single Embedded rounding failed on UInt64 input with ToZero:");
                            foreach (var item in floatTable.outArray)
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

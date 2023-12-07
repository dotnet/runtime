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
        public static unsafe void AddEmbeddedRounding_Float()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[16] {0x3effffff,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,
                                                                  0xbf000000,0xbf000001,0xbf000000,0xbf000001,0xbf000000,0xbf000001,0xbf000000,0xbf000001};
            uint[] answerTable_ToPositiveInfinity  = new uint[16] {0x3f000000,0x3f000001,0x3f000000,0x3f000001,0x3f000000,0x3f000001,0x3f000000,0x3f000001,
                                                                  0xbeffffff,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000};
            uint[] answerTable_ToZero = new uint[16] {0x3effffff,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,0x3f000000,
                                                     0xbeffffff,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000,0xbf000000};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[16] { 0.05f , 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f, -0.05f, -0.10f, -0.15f, -0.20f, -0.25f, -0.30f, -0.35f, -0.40f }, 
                                                                                        new float[16] { 0.45f , 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, -0.45f, -0.40f, -0.35f, -0.30f, -0.25f, -0.20f, -0.15f, -0.10f }, 
                                                                                        new float[16]))
                {

                    var vd1 = Unsafe.Read<Vector512<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on float with ToZero:");
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
        public static unsafe void DivideEmbeddedRounding_Float()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[16] {0x3de38e39,0x3e800000,0x3edb6db7,0x3f2aaaaa,0x3f800000,0x3fc00000,0x40155554,0x40800000,
                                                                  0x3de38e39,0x3e800000,0x3edb6db7,0x3f2aaaaa,0x3f800000,0x3fc00000,0x40155554,0x40800000};
            uint[] answerTable_ToPositiveInfinity  = new uint[16] {0x3de38e3a,0x3e800000,0x3edb6db8,0x3f2aaaab,0x3f800000,0x3fc00001,0x40155555,0x40800000,
                                                                   0x3de38e3a,0x3e800000,0x3edb6db8,0x3f2aaaab,0x3f800000,0x3fc00001,0x40155555,0x40800000};
            uint[] answerTable_ToZero = new uint[16] {0x3de38e39,0x3e800000,0x3edb6db7,0x3f2aaaaa,0x3f800000,0x3fc00000,0x40155554,0x40800000,
                                                      0x3de38e39,0x3e800000,0x3edb6db7,0x3f2aaaaa,0x3f800000,0x3fc00000,0x40155554,0x40800000};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[16] { 0.05f , 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f, -0.05f, -0.10f, -0.15f, -0.20f, -0.25f, -0.30f, -0.35f, -0.40f }, 
                                                                                        new float[16] { 0.45f , 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, -0.45f, -0.40f, -0.35f, -0.30f, -0.25f, -0.20f, -0.15f, -0.10f }, 
                                                                                        new float[16]))
                {

                    var vd1 = Unsafe.Read<Vector512<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on float with ToZero:");
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
        public static unsafe void MultiplyEmbeddedRounding_Float()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[16] {0x3cb851eb,0x3d23d70a,0x3d570a3d,0x3d75c290,0x3d800000,0x3d75c290,0x3d570a3d,0x3d23d70a,
                                                                  0x3cb851eb,0x3d23d70a,0x3d570a3d,0x3d75c290,0x3d800000,0x3d75c290,0x3d570a3d,0x3d23d70a};
            uint[] answerTable_ToPositiveInfinity  = new uint[16] {0x3cb851ec,0x3d23d70b,0x3d570a3e,0x3d75c291,0x3d800000,0x3d75c291,0x3d570a3e,0x3d23d70b,
                                                                   0x3cb851ec,0x3d23d70b,0x3d570a3e,0x3d75c291,0x3d800000,0x3d75c291,0x3d570a3e,0x3d23d70b};
            uint[] answerTable_ToZero = new uint[16] {0x3cb851eb,0x3d23d70a,0x3d570a3d,0x3d75c290,0x3d800000,0x3d75c290,0x3d570a3d,0x3d23d70a,
                                                      0x3cb851eb,0x3d23d70a,0x3d570a3d,0x3d75c290,0x3d800000,0x3d75c290,0x3d570a3d,0x3d23d70a};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[16] { 0.05f , 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f, -0.05f, -0.10f, -0.15f, -0.20f, -0.25f, -0.30f, -0.35f, -0.40f }, 
                                                                                        new float[16] { 0.45f , 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, -0.45f, -0.40f, -0.35f, -0.30f, -0.25f, -0.20f, -0.15f, -0.10f }, 
                                                                                        new float[16]))
                {

                    var vd1 = Unsafe.Read<Vector512<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on float with ToZero:");
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
        public static unsafe void SubtractEmbeddedRounding_Float()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[16] {0xbecccccd,0xbe99999a,0xbe4ccccc,0xbdccccce,0x80000000,0x3dccccce,0x3e4ccccc,0x3e999999,
                                                                  0x3ecccccc,0x3e999999,0x3e4ccccc,0x3dccccce,0x80000000,0xbdccccce,0xbe4ccccc,0xbe99999a};
            uint[] answerTable_ToPositiveInfinity  = new uint[16] {0xbecccccc,0xbe999999,0xbe4ccccc,0xbdccccce,0x0,0x3dccccce,0x3e4ccccc,0x3e99999a,
                                                                   0x3ecccccd,0x3e99999a,0x3e4ccccc,0x3dccccce,0x0,0xbdccccce,0xbe4ccccc,0xbe999999};
            uint[] answerTable_ToZero = new uint[16] {0xbecccccc,0xbe999999,0xbe4ccccc,0xbdccccce,0x0,0x3dccccce,0x3e4ccccc,0x3e999999,
                                                      0x3ecccccc,0x3e999999,0x3e4ccccc,0x3dccccce,0x0,0xbdccccce,0xbe4ccccc,0xbe999999};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[16] { 0.05f , 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f, -0.05f, -0.10f, -0.15f, -0.20f, -0.25f, -0.30f, -0.35f, -0.40f }, 
                                                                                        new float[16] { 0.45f , 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, -0.45f, -0.40f, -0.35f, -0.30f, -0.25f, -0.20f, -0.15f, -0.10f }, 
                                                                                        new float[16]))
                {

                    var vd1 = Unsafe.Read<Vector512<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on float with ToZero:");
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
        public static unsafe void SqrtEmbeddedRounding_Float()
        {
            int testResult = 1;
            uint[] answerTable_ToNegativeInfinity = new uint[16] {0x3e64f92e,0x3ea1e89b,0x3ec64bf7,0x3ee4f92e,0x3f000000,0x3f0c378b,0x3f17739e,0x3f21e89b,
                                                                  0x3f2bbae2,0x3f3504f3,0x3f3ddacc,0x3f464bf7,0x3f4e64cf,0x3f562f59,0x3f5db3d7,0x3f64f92e};
            uint[] answerTable_ToPositiveInfinity  = new uint[16] {0x3e64f92f,0x3ea1e89c,0x3ec64bf8,0x3ee4f92f,0x3f000000,0x3f0c378c,0x3f17739f,0x3f21e89c,
                                                                   0x3f2bbae3,0x3f3504f4,0x3f3ddacd,0x3f464bf8,0x3f4e64d0,0x3f562f5a,0x3f5db3d8,0x3f64f92f};
            uint[] answerTable_ToZero = new uint[16] {0x3e64f92e,0x3ea1e89b,0x3ec64bf7,0x3ee4f92e,0x3f000000,0x3f0c378b,0x3f17739e,0x3f21e89b,
                                                      0x3f2bbae2,0x3f3504f3,0x3f3ddacc,0x3f464bf7,0x3f4e64cf,0x3f562f59,0x3f5db3d7,0x3f64f92e};

            if (Avx512F.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[16] { 0.05f , 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f, 0.45f , 0.50f, 0.55f, 0.60f, 0.65f, 0.70f, 0.75f, 0.80f}, 
                                                                          new float[16]))
                {

                    var vd1 = Unsafe.Read<Vector512<float>>(floatTable.inArrayPtr);
                    var vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd2);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd2);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd2);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on float with ToZero:");
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
        public static unsafe void AddScalarEmbeddedRounding_Float()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x3e999999, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x3e99999a, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0x3e999999, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[2] { -0.05f, 0 }, new float[2] { 0.35f, 0 }, new float[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on float with ToZero:");
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
        public static unsafe void DivideScalarEmbeddedRounding_Float()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbe124925, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbe124924, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbe124924, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[2] { -0.05f, 0 }, new float[2] { 0.35f, 0 }, new float[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on float with ToZero:");
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
        public static unsafe void MultiplyScalarEmbeddedRounding_Float()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbc8f5c29, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbc8f5c28, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbc8f5c28, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[2] { -0.05f, 0 }, new float[2] { 0.35f, 0 }, new float[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on float with ToZero:");
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
        public static unsafe void SubtractScalarEmbeddedRounding_Float()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbecccccd, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbecccccc, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbecccccc, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[2] { -0.05f, 0 }, new float[2] { 0.35f, 0 }, new float[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on float with ToZero:");
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
        public static unsafe void SqrtScalarEmbeddedRounding_Float()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x3f17739e, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x3f17739f, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0x3f17739e, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[2] { 0.05f, 0 }, new float[2] { 0.35f, 0 }, new float[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on float with ToNegativeInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on float with ToPositiveInfinity:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(floatTable.outArrayPtr, vd3);

                    for (int i = 0; i < floatTable.outArray.Length; i++)
                    {
                        if (BitConverter.SingleToUInt32Bits(floatTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on float with ToZero:");
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

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
        public static unsafe void AddEmbeddedRounding_Double()
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

        [Fact]
        public static unsafe void DivideEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[8] {0x3fbc71c71c71c71c, 0x3fd0000000000000, 0x3fdb6db6db6db6db, 0x3fe5555555555555,
                                                                   0x3ff0000000000000, 0x3ff7ffffffffffff, 0x4002aaaaaaaaaaaa, 0x4010000000000000};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[8] {0x3fbc71c71c71c71d, 0x3fd0000000000000, 0x3fdb6db6db6db6dc, 0x3fe5555555555556,
                                                                    0x3ff0000000000000, 0x3ff8000000000000, 0x4002aaaaaaaaaaab, 0x4010000000000000};
            ulong[] answerTable_ToZero = new ulong[8] {0x3fbc71c71c71c71c, 0x3fd0000000000000, 0x3fdb6db6db6db6db, 0x3fe5555555555555,
                                                       0x3ff0000000000000, 0x3ff7ffffffffffff, 0x4002aaaaaaaaaaaa, 0x4010000000000000};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 0.05 , 0.10, 0.15, 0.20, -0.25, -0.30, -0.35, -0.40 }, new double[8] { 0.45 , 0.40, 0.35, 0.30, -0.25, -0.20, -0.15, -0.10 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Divide(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Divide Embedded rounding failed on double with ToZero:");
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
        public static unsafe void MultiplyEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[8] {0x3f970a3d70a3d70a, 0x3fa47ae147ae147b, 0x3faae147ae147ae0, 0x3faeb851eb851eb8,
                                                                   0x3fb0000000000000, 0x3faeb851eb851eb8, 0x3faae147ae147ae0, 0x3fa47ae147ae147b};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[8] {0x3f970a3d70a3d70b, 0x3fa47ae147ae147c, 0x3faae147ae147ae1, 0x3faeb851eb851eb9,
                                                                    0x3fb0000000000000, 0x3faeb851eb851eb9, 0x3faae147ae147ae1, 0x3fa47ae147ae147c};
            ulong[] answerTable_ToZero = new ulong[8] {0x3f970a3d70a3d70a, 0x3fa47ae147ae147b, 0x3faae147ae147ae0, 0x3faeb851eb851eb8,
                                                       0x3fb0000000000000, 0x3faeb851eb851eb8, 0x3faae147ae147ae0, 0x3fa47ae147ae147b};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 0.05 , 0.10, 0.15, 0.20, -0.25, -0.30, -0.35, -0.40 }, new double[8] { 0.45 , 0.40, 0.35, 0.30, -0.25, -0.20, -0.15, -0.10 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Multiply(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Multiply Embedded rounding failed on double with ToZero:");
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
        public static unsafe void SubtractEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[8] {0xbfd999999999999a,0xbfd3333333333334,0xbfc9999999999999,0xbfb9999999999998,
                                                                   0x8000000000000000,0xbfb9999999999998,0xbfc9999999999999,0xbfd3333333333334};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[8] {0xbfd9999999999999,0xbfd3333333333333,0xbfc9999999999999,0xbfb9999999999998,
                                                                    0x0,0xbfb9999999999998,0xbfc9999999999999,0xbfd3333333333333};
            ulong[] answerTable_ToZero = new ulong[8] {0xbfd9999999999999,0xbfd3333333333333,0xbfc9999999999999,0xbfb9999999999998,
                                                        0x0,0xbfb9999999999998,0xbfc9999999999999,0xbfd3333333333333};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 0.05 , 0.10, 0.15, 0.20, -0.25, -0.30, -0.35, -0.40 }, new double[8] { 0.45 , 0.40, 0.35, 0.30, -0.25, -0.20, -0.15, -0.10 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.Subtract(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Subtract Embedded rounding failed on double with ToZero:");
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
        public static unsafe void SqrtEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[8] {0x3fcc9f25c5bfedd9,0x3fd43d136248490f,0x3fd8c97ef43f7247,0x3fdc9f25c5bfedd9,
                                                                   0x3fe0000000000000,0x3fe186f174f88472,0x3fe2ee73dadc9b56,0x3fe43d136248490f};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[8] {0x3fcc9f25c5bfedda,0x3fd43d1362484910,0x3fd8c97ef43f7248,0x3fdc9f25c5bfedda,
                                                                    0x3fe0000000000000,0x3fe186f174f88473,0x3fe2ee73dadc9b57,0x3fe43d1362484910};
            ulong[] answerTable_ToZero = new ulong[8] {0x3fcc9f25c5bfedd9,0x3fd43d136248490f,0x3fd8c97ef43f7247,0x3fdc9f25c5bfedd9,
                                                       0x3fe0000000000000,0x3fe186f174f88472,0x3fe2ee73dadc9b56,0x3fe43d136248490f};

            if (Avx512F.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[8] { 0.05, 0.10, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArrayPtr);
                    var vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd2);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd2);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd2 = Avx512F.Sqrt(vd1, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd2);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 Sqrt Embedded rounding failed on double with ToZero:");
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
        public static unsafe void AddScalarEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x3fd3333333333332, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x3fd3333333333333, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0x3fd3333333333332, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { -0.05, 0 }, new double[2] { 0.35, 0 }, new double[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.AddScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 AddScalar Embedded rounding failed on double with ToZero:");
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
        public static unsafe void DivideScalarEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbfc2492492492493, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbfc2492492492492, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbfc2492492492492, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { -0.05, 0 }, new double[2] { 0.35, 0 }, new double[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.DivideScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 DivideScalar Embedded rounding failed on double with ToZero:");
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
        public static unsafe void MultiplyScalarEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbf91eb851eb851ec, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbf91eb851eb851eb, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbf91eb851eb851eb, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { -0.05, 0 }, new double[2] { 0.35, 0 }, new double[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.MultiplyScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 MultiplyScalar Embedded rounding failed on double with ToZero:");
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
        public static unsafe void SubtractScalarEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0xbfd999999999999a, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0xbfd9999999999999, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0xbfd9999999999999, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { -0.05, 0 }, new double[2] { 0.35, 0 }, new double[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SubtractScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 SubtractScalar Embedded rounding failed on double with ToZero:");
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
        public static unsafe void SqrtScalarEmbeddedRounding_Double()
        {
            int testResult = 1;
            ulong[] answerTable_ToNegativeInfinity = new ulong[2] {0x3fe2ee73dadc9b56, 0};
            ulong[] answerTable_ToPositiveInfinity  = new ulong[2] {0x3fe2ee73dadc9b57, 0};
            ulong[] answerTable_ToZero = new ulong[2] {0x3fe2ee73dadc9b56, 0};

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { 0.05, 0 }, new double[2] { 0.35, 0 }, new double[2]))
                {

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToNegativeInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToNegativeInfinity[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on double with ToNegativeInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToPositiveInfinity);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToPositiveInfinity[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on double with ToPositiveInfinity:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    vd3 = Avx512F.SqrtScalar(vd1, vd2, FloatRoundingMode.ToZero);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToUInt64Bits(doubleTable.outArray[i]) != answerTable_ToZero[i])
                        {
                            Console.WriteLine("Avx512 SqrtScalar Embedded rounding failed on double with ToZero:");
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

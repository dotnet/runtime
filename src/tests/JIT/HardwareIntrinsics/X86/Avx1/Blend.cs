// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.Avx1
{
    public partial class Program
    {

        [Fact]
        public static unsafe void Blend()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new float[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new float[8]))
                {
                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector256<float>>(floatTable.inArray2Ptr);

                    // SDDD SDDD
                    var vf3 = Avx.Blend(vf1, vf2, 1);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == y[0]) && (z[1] == x[1]) &&
                                                             (z[2] == x[2]) && (z[3] == x[3]) &&
                                                             (z[4] == x[4]) && (z[5] == x[5]) &&
                                                             (z[6] == x[6]) && (z[7] == x[7])))
                    {
                        Console.WriteLine("0Avx Blend failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // DSDD DDDD
                    vf3 = Avx.Blend(vf1, vf2, 2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == y[1]) &&
                                                             (z[2] == x[2]) && (z[3] == x[3]) &&
                                                             (z[4] == x[4]) && (z[5] == x[5]) &&
                                                             (z[6] == x[6]) && (z[7] == x[7])))
                    {
                        Console.WriteLine("Avx Blend failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // DDSD DDDD
                    vf3 = Avx.Blend(vf1, vf2, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == x[1]) &&
                                                             (z[2] == y[2]) && (z[3] == x[3]) &&
                                                             (z[4] == x[4]) && (z[5] == x[5]) &&
                                                             (z[6] == x[6]) && (z[7] == x[7])))
                    {
                        Console.WriteLine("Avx Blend failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // SDSD SDSD
                    vf3 = Avx.Blend(vf1, vf2, 85);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == y[0]) && (z[1] == x[1]) &&
                                                             (z[2] == y[2]) && (z[3] == x[3]) &&
                                                             (z[4] == y[4]) && (z[5] == x[5]) &&
                                                             (z[6] == y[6]) && (z[7] == x[7])))
                    {
                        Console.WriteLine("Avx Blend failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                    
                    // SDDD DDDD
                    vf3 = (Vector256<float>)typeof(Avx).GetMethod(nameof(Avx.Blend), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(1) });
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == y[0]) && (z[1] == x[1]) &&
                                                             (z[2] == x[2]) && (z[3] == x[3]) &&
                                                             (z[4] == x[4]) && (z[5] == x[5]) &&
                                                             (z[6] == x[6]) && (z[7] == x[7])))
                    {
                        Console.WriteLine("Avx Blend failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[4] { 1, -5, 100, 0 }, new double[4] { 22, -1, -50, 0 }, new double[4]))
                {
                    var vf1 = Unsafe.Read<Vector256<double>>(doubleTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector256<double>>(doubleTable.inArray2Ptr);

                    // DD DD
                    var vf3 = Avx.Blend(vf1, vf2, 0);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == x[1]) &&
                                                              (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("Avx Blend failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // SD DD
                    vf3 = Avx.Blend(vf1, vf2, 1);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => (z[0] == y[0]) && (z[1] == x[1]) &&
                                                              (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("Avx Blend failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // DS DD
                    vf3 = Avx.Blend(vf1, vf2, 2);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == y[1]) &&
                                                              (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("Avx Blend failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // SS DD
                    vf3 = Avx.Blend(vf1, vf2, 51);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => (z[0] == y[0]) && (z[1] == y[1]) &&
                                                              (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("Avx Blend failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // DD DD
                    vf3 = (Vector256<double>)typeof(Avx).GetMethod(nameof(Avx.Blend), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(0) });
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == x[1]) &&
                                                              (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("Avx Blend failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}

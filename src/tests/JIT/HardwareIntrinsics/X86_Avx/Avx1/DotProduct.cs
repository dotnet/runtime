// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Linq;
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
        public static unsafe void DotProduct()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new float[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new float[8]))
                {
                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector256<float>>(floatTable.inArray2Ptr);

                    var vf3 = Avx.DotProduct(vf1, vf2, 255);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]) + (x[3] * y[3]))))
                    {
                        Console.WriteLine("Avx DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Avx.DotProduct(vf1, vf2, 127);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]))))
                    {
                        Console.WriteLine("Avx DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Avx.DotProduct(vf1, vf2, 63);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == ((x[0] * y[0]) + (x[1] * y[1])))))
                    {
                        Console.WriteLine("3 Avx DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Avx.DotProduct(vf1, vf2, 85);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z[0] == ((x[0] * y[0]) + (x[2] * y[2])) &&
                                                             z[2] == ((x[0] * y[0]) + (x[2] * y[2])) &&
                                                             z[4] == ((x[4] * y[4]) + (x[6] * y[6])) &&
                                                             z[6] == ((x[4] * y[4]) + (x[6] * y[6])) &&
                                                             z[1] == 0 && z[3] == 0 &&
                                                             z[5] == 0 && z[7] == 0))
                    {
                        Console.WriteLine("Avx DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = (Vector256<float>)typeof(Avx).GetMethod(nameof(Avx.DotProduct), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(255) });
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]) + (x[3] * y[3]))))
                    {
                        Console.WriteLine("Avx DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
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

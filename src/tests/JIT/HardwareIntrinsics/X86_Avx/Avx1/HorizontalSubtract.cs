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
        public static unsafe void HorizontalSubtract()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[8] {22, -1, -50, 0, 22, -1, -50, 0 }, new float[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new float[8]))
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[4] { 1, -5, 100, 0 }, new double[4] { 22, -1, -50, 0 }, new double[4]))
                {

                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector256<float>>(floatTable.inArray2Ptr);
                    var vf3 = Avx.HorizontalSubtract(vf1, vf2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((left, right, result) => 
                                                              (left[0] - left[1] == result[0]) && (right[0] - right[1]  == result[2]) &&
                                                              (left[2] - left[3] == result[1]) && (right[2] - right[3]  == result[3]) &&
                                                              (left[4] - left[5] == result[4]) && (right[4] - right[5]  == result[6]) &&
                                                              (left[6] - left[7] == result[5]) && (right[6] - right[7]  == result[7])))
                    {               
                        Console.WriteLine("Avx HorizontalAdd failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                    
                    var vd1 = Unsafe.Read<Vector256<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector256<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx.HorizontalSubtract(vd1, vd2);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    if (!doubleTable.CheckResult((left, right, result) => 
                                                              (left[0] - left[1] == result[0]) && (right[0] - right[1]  == result[1]) &&
                                                              (left[2] - left[3] == result[2]) && (right[2] - right[3]  == result[3])))
                    {
                        Console.WriteLine("Avx HorizontalAdd failed on double:");
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

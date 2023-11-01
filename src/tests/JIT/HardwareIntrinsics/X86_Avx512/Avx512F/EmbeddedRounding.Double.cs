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

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new double[8] { 1, 1, 50, 0, 1, 1, 50, 0 }, new double[8]))
                {

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.Add(vd1, vd2, FloatRoundingMode.ToEven);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);
                    
                    for (int i = 0; i < doubleTable.outArray.Length; i++)
                    {
                        if (BitConverter.DoubleToInt64Bits(doubleTable.outArray[i]) != BitConverter.DoubleToInt64Bits(doubleTable.inArray1[i] + doubleTable.inArray2[i]))
                        {
                            Console.WriteLine("Avx512 Add Embedded rounding failed on double:");
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

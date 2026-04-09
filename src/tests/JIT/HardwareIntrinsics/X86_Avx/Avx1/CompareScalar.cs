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
        public static unsafe void CompareScalar()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new float[8] { 22, -5, -50, 0, 22, -1, -50, 0 }, new float[8]))
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[4] { 1, -5, 100, 0 }, new double[4] { 1, 1, 50, 0 }, new double[4]))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vf3 = Avx.CompareScalar(vf1, vf2, FloatComparisonMode.OrderedEqualNonSignaling);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx.CompareScalar(vd1, vd2, FloatComparisonMode.OrderedEqualNonSignaling);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);


                    if (BitConverter.SingleToInt32Bits(floatTable.outArray[0]) != (floatTable.inArray1[0] == floatTable.inArray2[0] ? -1 : 0))
                    {
                        Console.WriteLine("Avx CompareScalar failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        Assert.Fail("");
                    }
                    for (int i = 1; i < 4; i++)
                    {
                        if (floatTable.outArray[i] != floatTable.inArray1[i])
                        {
                            Console.WriteLine("Avx CompareScalar failed on float:");
                            foreach (var item in floatTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }
                    
                    
                    if (BitConverter.DoubleToInt64Bits(doubleTable.outArray[0]) != (doubleTable.inArray1[0] == doubleTable.inArray2[0] ? -1 : 0))
                    {
                        Console.WriteLine("Avx CompareScalar failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        Assert.Fail("");
                    }
                    for (int i = 1; i < 2; i++)
                    {
                        if (doubleTable.outArray[i] != doubleTable.inArray1[i])
                        {
                            Console.WriteLine("Avx CompareScalar failed on double:");
                            foreach (var item in doubleTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    try 
                    {
                        var ve = Avx.CompareScalar(vf1, vf2, (FloatComparisonMode)32);
                        Unsafe.Write(floatTable.outArrayPtr, ve);
                        Console.WriteLine("Avx CompareScalar failed on float with out-of-range argument:");
                        Assert.Fail("");
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        testResult = Pass;
                    }

                    try 
                    {
                        var ve = Avx.CompareScalar(vd1, vd2, (FloatComparisonMode)32);
                        Unsafe.Write(floatTable.outArrayPtr, ve);
                        Console.WriteLine("Avx CompareScalar failed on double with out-of-range argument:");
                        Assert.Fail("");
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        testResult = Pass;
                    }

                    try 
                    {
                        var ve = typeof(Avx).GetMethod(nameof(Avx.CompareScalar), new Type[] { typeof(Vector128<Single>), typeof(Vector128<Single>), typeof(FloatComparisonMode) })
                                     .Invoke(null, new object[] {vf1, vf2, (FloatComparisonMode)32});
                        Console.WriteLine("Indirect-calling Avx CompareScalar failed on float with out-of-range argument:");
                        Assert.Fail("");
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        if (e.InnerException is ArgumentOutOfRangeException)
                        {
                            testResult = Pass;
                        }
                        else
                        {
                            Console.WriteLine("Indirect-calling Avx CompareScalar failed on float with out-of-range argument:");
                            Assert.Fail("");
                        }
                    }

                    try 
                    {
                        var ve = typeof(Avx).GetMethod(nameof(Avx.CompareScalar), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>), typeof(FloatComparisonMode) })
                                     .Invoke(null, new object[] {vd1, vd2, (FloatComparisonMode)32});
                        Console.WriteLine("Indirect-calling Avx CompareScalar failed on double with out-of-range argument:");
                        Assert.Fail("");
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        if (e.InnerException is ArgumentOutOfRangeException)
                        {
                            testResult = Pass;
                        }
                        else
                        {
                            Console.WriteLine("Indirect-calling Avx CompareScalar failed on double with out-of-range argument:");
                            Assert.Fail("");
                        }
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}

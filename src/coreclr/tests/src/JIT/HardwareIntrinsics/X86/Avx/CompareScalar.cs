// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new float[8] { 22, -5, -50, 0, 22, -1, -50, 0 }, new float[8]))
                using (TestTable<double> doubleTable = new TestTable<double>(new double[4] { 1, -5, 100, 0 }, new double[4] { 1, 1, 50, 0 }, new double[4]))
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
                        return Fail;
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
                            return Fail;
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
                        return Fail;
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
                            return Fail;
                        }
                    }

                    try 
                    {
                        var ve = Avx.CompareScalar(vf1, vf2, (FloatComparisonMode)32);
                        Unsafe.Write(floatTable.outArrayPtr, ve);
                        Console.WriteLine("Avx CompareScalar failed on float with out-of-range argument:");
                        return Fail;
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
                        return Fail;
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
                        return Fail;
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
                            return Fail;
                        }
                    }

                    try 
                    {
                        var ve = typeof(Avx).GetMethod(nameof(Avx.CompareScalar), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>), typeof(FloatComparisonMode) })
                                     .Invoke(null, new object[] {vd1, vd2, (FloatComparisonMode)32});
                        Console.WriteLine("Indirect-calling Avx CompareScalar failed on double with out-of-range argument:");
                        return Fail;
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
                            return Fail;
                        }
                    }
                }
            }

            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] inArray1;
            public T[] inArray2;
            public T[] outArray;

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            GCHandle outHandle;
            public TestTable(T[] a, T[] b, T[] c)
            {
                this.inArray1 = a;
                this.inArray2 = b;
                this.outArray = c;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, T, T, bool> check)
            {
                for (int i = 0; i < inArray1.Length; i++)
                {
                    if (!check(inArray1[i], inArray2[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }
        }

    }
}
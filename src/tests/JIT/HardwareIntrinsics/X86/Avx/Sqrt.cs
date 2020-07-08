// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0}, new float[8]))
                using (TestTable<double> doubleTable = new TestTable<double>(new double[4] { 1, -5, 100, 0 }, new double[4]))
                {

                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArrayPtr);
                    var vf2 = Avx.Sqrt(vf1);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);
                    
                    var vd1 = Unsafe.Read<Vector256<double>>(doubleTable.inArrayPtr);             
                    var vd2 = Avx.Sqrt(vd1);
                    Unsafe.Write(doubleTable.outArrayPtr, vd2);
                    
                    if (!floatTable.CheckResult((x, y) => {
                        var expected = MathF.Sqrt(x);
                        return (expected == y)
                            || (float.IsNaN(expected) && float.IsNaN(y));
                    }))
                    {
                        Console.WriteLine("Avx Sqrt failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                    
                    if (!doubleTable.CheckResult((x, y) => {
                        var expected = Math.Sqrt(x);
                        return (expected == y)
                            || (double.IsNaN(expected) && double.IsNaN(y));
                    }))
                    {
                        Console.WriteLine("Avx Sqrt failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }


            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] inArray;
            public T[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle outHandle;
            public TestTable(T[] a, T[] b)
            {
                this.inArray = a;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < inArray.Length; i++)
                {
                    if (!check(inArray[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    }
}

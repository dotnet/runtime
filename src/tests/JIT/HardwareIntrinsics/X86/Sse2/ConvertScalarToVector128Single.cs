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

            if (Sse2.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { -11, 7 }, new double[2]))
                {
                    var vd0 = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var vf2 = Sse2.ConvertScalarToVector128Single(vf1, vd0);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (!floatTable.CheckResult((x, y) => (y[0] == -11)
                                                       && (y[1] == x[1]) && (y[2] == x[2]) && (y[3] == x[3])))
                    {
                        Console.WriteLine("SSE ConvertScalarToVector128Single failed on float:");
                        foreach (var item in floatTable.outArray)
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
            public bool CheckResult(Func<T[], T[], bool> check)
            {
                return check(inArray, outArray);
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    }
}

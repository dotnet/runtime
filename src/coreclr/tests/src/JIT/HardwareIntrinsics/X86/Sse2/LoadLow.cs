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

            if (Sse2.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2] { 22, -1 }, new double[2]))
                {
                    var vf1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vf2 = Sse2.LoadLow(vf1, (double*)(doubleTable.inArray2Ptr));
                    Unsafe.Write(doubleTable.outArrayPtr, vf2);

                    if (!doubleTable.CheckResult((x, y, z) => z[0] == y[0] && z[1] == x[1]))
                    {
                        Console.WriteLine("SSE2 LoadLow failed on double:");
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
            public bool CheckResult(Func<T[], T[], T[], bool> check)
            {
                return check(inArray1, inArray2, outArray);
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

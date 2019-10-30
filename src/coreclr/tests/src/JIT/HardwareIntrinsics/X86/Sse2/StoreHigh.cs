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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    Sse2.StoreHigh((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => y[0] == x[1] && y[1] == 0))
                    {
                        Console.WriteLine("SSE2 StoreHigh failed on double:");
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

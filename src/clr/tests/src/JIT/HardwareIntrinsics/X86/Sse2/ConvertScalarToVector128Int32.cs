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
                using (TestTable<int> intTable = new TestTable<int>(new int[4] { 1, -5, 7, 19 }, new int[4]))
                {
                    var vf1 = Unsafe.Read<Vector128<int>>(intTable.inArrayPtr);
                    var vf2 = Sse2.ConvertScalarToVector128Int32(5);
                    Unsafe.Write(intTable.outArrayPtr, vf2);

                    if (!intTable.CheckResult((x, y) => (y[0] == 5)
                                                       && (y[1] == 0) && (y[2] == 0) && (y[3] == 0)))
                    {
                        Console.WriteLine("SSE2 ConvertScalarToVector128Int32 failed on int:");
                        foreach (var item in intTable.outArray)
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

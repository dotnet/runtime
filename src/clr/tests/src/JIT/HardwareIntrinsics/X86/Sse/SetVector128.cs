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

            if (Sse.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { float.NaN, float.NaN, float.NaN, float.NaN }))
                {
                    var vf1 = Sse.SetVector128(1, -5, 100, 0);
                    Unsafe.Write(floatTable.outArrayPtr, vf1);

                    if (!floatTable.CheckResult((x) => (x[0] == 0) && (x[1] == 100) &&
                                                       (x[2] == -5) && (x[3] == 1)))
                    {
                        Console.WriteLine("SSE SetVector128 failed on float:");
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
            public T[] outArray;

            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle outHandle;
            public TestTable(T[] a)
            {
                this.outArray = a;

                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T[], bool> check)
            {
                return check(outArray);
            }

            public void Dispose()
            {
                outHandle.Free();
            }
        }

    }
}

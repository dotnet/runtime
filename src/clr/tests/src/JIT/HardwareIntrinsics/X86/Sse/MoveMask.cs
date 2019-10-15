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
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var res = Sse.MoveMask(vf1);

                    if (res != 0b0010)
                    {
                        Console.WriteLine("SSE MoveMask failed on float:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }
            }


            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] inArray1;
            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            GCHandle inHandle1;

            public TestTable(T[] a)
            {
                this.inArray1 = a;
                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            }

            public void Dispose()
            {
                inHandle1.Free();
            }
        }

    }
}

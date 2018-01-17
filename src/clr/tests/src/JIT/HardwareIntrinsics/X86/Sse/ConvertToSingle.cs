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
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var f2 = Sse.ConvertToSingle(vf1);

                    if (f2 != floatTable.inArray[0])
                    {
                        Console.WriteLine("SSE ConvertToSingle failed on float:");
                        Console.WriteLine(f2);
                        testResult = Fail;
                    }
                }
            }

            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] inArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            public TestTable(T[] a)
            {
                this.inArray = a;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
            }

            public void Dispose()
            {
                inHandle.Free();
            }
        }

    }
}

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

            if (Sse.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var i2 = Sse.ConvertToInt32(vf1);

                    if (i2 != ((int)floatTable.inArray[0]))
                    {
                        Console.WriteLine("SSE ConvertToInt32 failed on float:");
                        Console.WriteLine(i2);
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

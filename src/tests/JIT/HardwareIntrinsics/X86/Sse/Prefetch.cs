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
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 3 }))
                {
                    try
                    {
                        Sse.Prefetch0(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.Prefetch1(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.Prefetch2(floatTable.inArrayPtr);
                    }
                    catch
                    {
                        testResult = Fail;
                    }

                    try
                    {
                        Sse.PrefetchNonTemporal(floatTable.inArrayPtr);
                    }
                    catch
                    {
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

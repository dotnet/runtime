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
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 22, -5, 100, 0 }, new float[4] { 1, -1, -50, 3 }))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);

                    if (!Sse.CompareGreaterThanOrEqualUnorderedScalar(vf1, vf2))
                    {
                        Console.WriteLine("SSE CompareGreaterThanOrEqualUnorderedScalar failed positive test");
                        testResult = Fail;
                    }
                }

                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4] { 22, -1, -50, 3 }))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);

                    if (Sse.CompareGreaterThanOrEqualUnorderedScalar(vf1, vf2))
                    {
                        Console.WriteLine("SSE CompareGreaterThanOrEqualUnorderedScalar failed negative test");
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

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            public TestTable(T[] a, T[] b)
            {
                this.inArray1 = a;
                this.inArray2 = b;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
            }

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
            }
        }

    }
}

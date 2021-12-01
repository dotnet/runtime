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

            if (Avx.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }))
                {

                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArray1Ptr);
                    var res = Avx.MoveMask(vf1);

                    if (res != 0b00100010)
                    {
                        Console.WriteLine("Avx MoveMask failed on float:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }

                using (TestTable<double> doubleTable = new TestTable<double>(new double[4] { 1, -5, 1, -5 }))
                {

                    var vf1 = Unsafe.Read<Vector256<double>>(doubleTable.inArray1Ptr);
                    var res = Avx.MoveMask(vf1);

                    if (res != 0b1010)
                    {
                        Console.WriteLine("Avx MoveMask failed on double:");
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

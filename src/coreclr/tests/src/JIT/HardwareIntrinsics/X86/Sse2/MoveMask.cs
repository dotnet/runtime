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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }))
                {

                    var vf1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var res = Sse2.MoveMask(vf1);

                    if (res != 0b0010)
                    {
                        Console.WriteLine("SSE MoveMask failed on double:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> byteTable = new TestTable<byte>(new byte[16] { 255, 2, 0, 80, 0, 7, 0, 1, 2, 7, 80, 0, 123, 127, 5, 255 }))
                {

                    var vf1 = Unsafe.Read<Vector128<byte>>(byteTable.inArray1Ptr);
                    var res = Sse2.MoveMask(vf1);

                    if (res != 0b1000000000000001)
                    {
                        Console.WriteLine("SSE MoveMask failed on byte:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[16] { -1, 2, 0, 6, 0, 7, 111, 1, 2, 55, 80, 0, 11, 127, 5, -9 }))
                {

                    var vf1 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray1Ptr);
                    var res = Sse2.MoveMask(vf1);

                    if (res != 0b1000000000000001)
                    {
                        Console.WriteLine("SSE MoveMask failed on sbyte:");
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

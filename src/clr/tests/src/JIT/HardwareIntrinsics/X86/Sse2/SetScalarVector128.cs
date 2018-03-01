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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { double.NaN, double.NaN, }))
                {
                    var vf1 = Sse2.SetScalarVector128(3);
                    Unsafe.Write(doubleTable.outArrayPtr, vf1);

                    if (!doubleTable.CheckResult((x) => (x[0] == 3) && (BitConverter.DoubleToInt64Bits(x[1]) == 0)))
                    {
                        Console.WriteLine("SSE2 SetScalarVector128 failed on double:");
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

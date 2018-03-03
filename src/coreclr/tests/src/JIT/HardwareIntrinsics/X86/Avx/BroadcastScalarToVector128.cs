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

            if (Avx.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new float[4]))
                {
                    var vf = Avx.BroadcastScalarToVector128((float*)(floatTable.inArrayPtr));
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y)))
                    {
                        Console.WriteLine("AVX BroadcastScalarToVector128 failed on float:");
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
            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < outArray.Length; i++)
                {
                    if (!check(inArray[0], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    }
}

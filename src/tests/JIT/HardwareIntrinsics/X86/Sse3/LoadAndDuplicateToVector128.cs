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

            if (Sse3.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[4]))
                {
                    var vf = Sse3.LoadAndDuplicateToVector128((double*)(doubleTable.inArrayPtr));
                    Unsafe.Write(doubleTable.outArrayPtr, vf);

                    if (BitConverter.DoubleToInt64Bits(doubleTable.inArray[0]) != BitConverter.DoubleToInt64Bits(doubleTable.outArray[0]) || 
                        BitConverter.DoubleToInt64Bits(doubleTable.inArray[0]) != BitConverter.DoubleToInt64Bits(doubleTable.outArray[1]))
                    {
                        Console.WriteLine("Sse3 LoadAndDuplicateToVector128 failed on double:");
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
                for (int i = 0; i < inArray.Length; i++)
                {
                    if (!check(inArray[i], outArray[i]))
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

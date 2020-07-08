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
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                {
                    var vf1 = Sse.LoadVector128((float*)(floatTable.inArrayPtr));
                    var vf2 = Sse3.MoveHighAndDuplicate(vf1);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (BitConverter.SingleToInt32Bits(floatTable.inArray[1]) != BitConverter.SingleToInt32Bits(floatTable.outArray[0]) || 
                        BitConverter.SingleToInt32Bits(floatTable.inArray[1]) != BitConverter.SingleToInt32Bits(floatTable.outArray[1]) ||
                        BitConverter.SingleToInt32Bits(floatTable.inArray[3]) != BitConverter.SingleToInt32Bits(floatTable.outArray[2]) ||
                        BitConverter.SingleToInt32Bits(floatTable.inArray[3]) != BitConverter.SingleToInt32Bits(floatTable.outArray[3]))
                    {
                        Console.WriteLine("Sse3 MoveHighAndDuplicate failed on float:");
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

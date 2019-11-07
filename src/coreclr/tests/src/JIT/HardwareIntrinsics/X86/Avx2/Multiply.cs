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

            if (Avx2.IsSupported)
            {
                using (TestTable<int, int, long> intTable = new TestTable<int, int, long>(new int[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new int[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new long[4]))
                using (TestTable<uint, uint, ulong> uintTable = new TestTable<uint, uint, ulong>(new uint[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new ulong[4]))
                {

                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.Multiply(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector256<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector256<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.Multiply(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if (intTable.inArray1[i * 2] * intTable.inArray2[i * 2] != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 Multiply failed on int:");
                            foreach (var item in intTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            return Fail;
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if (uintTable.inArray1[i * 2] * uintTable.inArray2[i * 2] != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 Multiply failed on uint:");
                            foreach (var item in uintTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            return Fail;
                        }
                    }
                }
            }

            return testResult;
        }

        public unsafe struct TestTable<T1, T2, T3> : IDisposable where T1 : struct where T2 : struct where T3 : struct
        {
            public T1[] inArray1;
            public T2[] inArray2;
            public T3[] outArray;

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            GCHandle outHandle;
            public TestTable(T1[] a, T2[] b, T3[] c)
            {
                this.inArray1 = a;
                this.inArray2 = b;
                this.outArray = c;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T1, T2, T3, bool> check)
            {
                for (int i = 0; i < inArray1.Length; i++)
                {
                    if (!check(inArray1[i], inArray2[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }
        }

    }
}
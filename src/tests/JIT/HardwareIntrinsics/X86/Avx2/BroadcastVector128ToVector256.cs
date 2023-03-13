// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void BroadcastVector128ToVector256()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                using (TestTable_Broadcast<int> intTable = new TestTable_Broadcast<int>(new int[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new int[8]))
                {
                    var vf = Avx2.BroadcastVector128ToVector256((int*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx2 BroadcastVector128ToVector256 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable_Broadcast<uint> uintTable = new TestTable_Broadcast<uint>(new uint[8] { 1, 5, 100, 0, 1, 2, 3, 4 }, new uint[8]))
                {
                    var vf = Avx2.BroadcastVector128ToVector256((uint*)(uintTable.inArrayPtr));
                    Unsafe.Write(uintTable.outArrayPtr, vf);

                    if (!uintTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx2 BroadcastVector128ToVector256 failed on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable_Broadcast<long> longTable = new TestTable_Broadcast<long>(new long[4] { 1, -5, 100, 0}, new long[4]))
                {
                    var vf = Avx2.BroadcastVector128ToVector256((long*)(longTable.inArrayPtr));
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx2 BroadcastVector128ToVector256 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable_Broadcast<ulong> ulongTable = new TestTable_Broadcast<ulong>(new ulong[4] { 1, 5, 100, 0}, new ulong[4]))
                {
                    var vf = Avx2.BroadcastVector128ToVector256((ulong*)(ulongTable.inArrayPtr));
                    Unsafe.Write(ulongTable.outArrayPtr, vf);

                    if (!ulongTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx2 BroadcastVector128ToVector256 failed on ulong:");
                        foreach (var item in ulongTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }
            Assert.Equal(Pass, testResult);
        }

        public unsafe struct TestTable_Broadcast<T> : IDisposable where T : struct
        {
            public T[] inArray;
            public T[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle outHandle;
            public TestTable_Broadcast(T[] a, T[] b)
            {
                this.inArray = a;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < outArray.Length/2; i++)
                {
                    if (!check(inArray[i], outArray[i]))
                    {
                        return false;
                    }
                }
                for (int i = outArray.Length/2; i < outArray.Length; i++)
                {
                    if (!check(inArray[i - outArray.Length/2], outArray[i]))
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

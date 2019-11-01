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

            if (Sse3.IsSupported)
            {
                using (TestTable<int> intTable = new TestTable<int>(new int[4] { 1, -5, 100, 0 }, new int[4]))
                {
                    var vf = Sse3.LoadDquVector128((int*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<uint> intTable = new TestTable<uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4]))
                {
                    var vf = Sse3.LoadDquVector128((uint*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> intTable = new TestTable<long>(new long[2] { 1, -5 }, new long[2]))
                {
                    var vf = Sse3.LoadDquVector128((long*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on long:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ulong> intTable = new TestTable<ulong>(new ulong[2] { 1, 5 }, new ulong[2]))
                {
                    var vf = Sse3.LoadDquVector128((ulong*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on ulong:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> intTable = new TestTable<short>(new short[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new short[8]))
                {
                    var vf = Sse3.LoadDquVector128((short*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on short:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> intTable = new TestTable<ushort>(new ushort[8] { 1, 5, 100, 0, 1, 2, 3, 4 }, new ushort[8]))
                {
                    var vf = Sse3.LoadDquVector128((ushort*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on ushort:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> intTable = new TestTable<byte>(new byte[16] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new byte[16]))
                {
                    var vf = Sse3.LoadDquVector128((byte*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on byte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> intTable = new TestTable<sbyte>(new sbyte[16] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new sbyte[16]))
                {
                    var vf = Sse3.LoadDquVector128((sbyte*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse3 LoadDquVector128 failed on sbyte:");
                        foreach (var item in intTable.outArray)
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

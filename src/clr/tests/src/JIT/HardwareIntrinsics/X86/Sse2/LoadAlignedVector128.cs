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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((double*)(doubleTable.inArrayPtr));
                    Unsafe.Write(doubleTable.outArrayPtr, vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<int> intTable = new TestTable<int>(new int[4] { 1, -5, 100, 0 }, new int[4]))
                {
                    var vf = Sse2.LoadAlignedVector128((int*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> longTable = new TestTable<long>(new long[2] { 1, -5 }, new long[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((long*)(longTable.inArrayPtr));
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<uint> uintTable = new TestTable<uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4]))
                {
                    var vf = Sse2.LoadAlignedVector128((uint*)(uintTable.inArrayPtr));
                    Unsafe.Write(uintTable.outArrayPtr, vf);

                    if (!uintTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ulong> ulongTable = new TestTable<ulong>(new ulong[2] { 1, 5 }, new ulong[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((ulong*)(ulongTable.inArrayPtr));
                    Unsafe.Write(ulongTable.outArrayPtr, vf);

                    if (!ulongTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on ulong:");
                        foreach (var item in ulongTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> shortTable = new TestTable<short>(new short[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new short[8]))
                {
                    var vf = Sse2.LoadAlignedVector128((short*)(shortTable.inArrayPtr));
                    Unsafe.Write(shortTable.outArrayPtr, vf);

                    if (!shortTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on short:");
                        foreach (var item in shortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> ushortTable = new TestTable<ushort>(new ushort[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[8]))
                {
                    var vf = Sse2.LoadAlignedVector128((ushort*)(ushortTable.inArrayPtr));
                    Unsafe.Write(ushortTable.outArrayPtr, vf);

                    if (!ushortTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on ushort:");
                        foreach (var item in ushortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[16]))
                {
                    var vf = Sse2.LoadAlignedVector128((sbyte*)(sbyteTable.inArrayPtr));
                    Unsafe.Write(sbyteTable.outArrayPtr, vf);

                    if (!sbyteTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> byteTable = new TestTable<byte>(new byte[16] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[16]))
                {
                    var vf = Sse2.LoadAlignedVector128((byte*)(byteTable.inArrayPtr));
                    Unsafe.Write(byteTable.outArrayPtr, vf);

                    if (!byteTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on byte:");
                        foreach (var item in byteTable.outArray)
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
            private byte[] inArray;
            public T[] outArray;

            private GCHandle inHandle;
            private GCHandle outHandle;

            private byte simdSize;

            public TestTable(T[] a, T[] b)
            {
                this.inArray = new byte[32];
                this.outArray = b;

                this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.simdSize = 16;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<T, byte>(ref a[0]), this.simdSize);
            }

            public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), simdSize);
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < outArray.Length; i++)
                {
                    if (!check(Unsafe.Add<T>(ref Unsafe.AsRef<T>(inArrayPtr), i), outArray[i]))
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

            private static unsafe void* Align(byte* buffer, byte expectedAlignment)
            {
                // Compute how bad the misalignment is, which is at most (expectedAlignment - 1).
                // Then subtract that from the expectedAlignment and add it to the original address
                // to compute the aligned address.

                var misalignment = expectedAlignment - ((ulong)(buffer) % expectedAlignment);
                return (void*)(buffer + misalignment);
            }
        }
    }
}

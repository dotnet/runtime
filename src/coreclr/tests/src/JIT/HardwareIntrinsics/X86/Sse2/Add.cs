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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2] { 22, -1 }, new double[2]))
                using (TestTable<int> intTable = new TestTable<int>(new int[4] { 1, -5, 100, 0 }, new int[4] { 22, -1, -50, 0 }, new int[4]))
                using (TestTable<long> longTable = new TestTable<long>(new long[2] { 1, -5 }, new long[2] { 22, -1 }, new long[2]))
                using (TestTable<uint> uintTable = new TestTable<uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4] { 22, 1, 50, 0 }, new uint[4]))
                using (TestTable<ulong> ulongTable = new TestTable<ulong>(new ulong[2] { 1, 5 }, new ulong[2] { 22, 1 }, new ulong[2]))
                using (TestTable<short> shortTable = new TestTable<short>(new short[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new short[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new short[8]))
                using (TestTable<ushort> ushortTable = new TestTable<ushort>(new ushort[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new ushort[8]))
                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new sbyte[16]))
                using (TestTable<byte> byteTable = new TestTable<byte>(new byte[16] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[16] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new byte[16]))
                {
                    var vd1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Sse2.Add(vd1, vd2);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    if (!doubleTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd3 = (Vector128<double>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vd1.GetType(), vd2.GetType() }).Invoke(null, new object[] { vd1, vd2 });
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    if (!doubleTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vi1 = Unsafe.Read<Vector128<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector128<int>>(intTable.inArray2Ptr);
                    var vi3 = Sse2.Add(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    if (!intTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vi3 = (Vector128<int>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vi1.GetType(), vi2.GetType() }).Invoke(null, new object[] { vi1, vi2 });
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    if (!intTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vl1 = Unsafe.Read<Vector128<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector128<long>>(longTable.inArray2Ptr);
                    var vl3 = Sse2.Add(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    if (!longTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vl3 = (Vector128<long>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vl1.GetType(), vl2.GetType() }).Invoke(null, new object[] { vl1, vl2 });
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    if (!longTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vui1 = Unsafe.Read<Vector128<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector128<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Sse2.Add(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    if (!uintTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vui3 = (Vector128<uint>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vui1.GetType(), vui2.GetType() }).Invoke(null, new object[] { vui1, vui2 });
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    if (!uintTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vul1 = Unsafe.Read<Vector128<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector128<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Sse2.Add(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);

                    if (!ulongTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on ulong:");
                        foreach (var item in ulongTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vul3 = (Vector128<ulong>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vul1.GetType(), vul2.GetType() }).Invoke(null, new object[] { vul1, vul2 });
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);

                    if (!ulongTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on ulong:");
                        foreach (var item in ulongTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vs1 = Unsafe.Read<Vector128<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector128<short>>(shortTable.inArray2Ptr);
                    var vs3 = Sse2.Add(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    if (!shortTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on short:");
                        foreach (var item in shortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vs3 = (Vector128<short>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vs1.GetType(), vs2.GetType() }).Invoke(null, new object[] { vs1, vs2 });
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    if (!shortTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on short:");
                        foreach (var item in shortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vus1 = Unsafe.Read<Vector128<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector128<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Sse2.Add(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);

                    if (!ushortTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on ushort:");
                        foreach (var item in ushortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vus3 = (Vector128<ushort>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vus1.GetType(), vus2.GetType() }).Invoke(null, new object[] { vus1, vus2 });
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);

                    if (!ushortTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on ushort:");
                        foreach (var item in ushortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vsb1 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Sse2.Add(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    if (!sbyteTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vsb3 = (Vector128<sbyte>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vsb1.GetType(), vsb2.GetType() }).Invoke(null, new object[] { vsb1, vsb2 });
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    if (!sbyteTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vb1 = Unsafe.Read<Vector128<byte>>(byteTable.inArray1Ptr);
                    var vb2 = Unsafe.Read<Vector128<byte>>(byteTable.inArray2Ptr);
                    var vb3 = Sse2.Add(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    if (!byteTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed on byte:");
                        foreach (var item in byteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vb3 = (Vector128<byte>)typeof(Sse2).GetMethod(nameof(Sse2.Add), new Type[] { vb1.GetType(), vb2.GetType() }).Invoke(null, new object[] { vb1, vb2 });
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    if (!byteTable.CheckResult((x, y, z) => x + y == z))
                    {
                        Console.WriteLine("SSE2 Add failed via reflection on byte:");
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
            public T[] inArray1;
            public T[] inArray2;
            public T[] outArray;

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            GCHandle outHandle;
            public TestTable(T[] a, T[] b, T[] c)
            {
                this.inArray1 = a;
                this.inArray2 = b;
                this.outArray = c;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, T, T, bool> check)
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

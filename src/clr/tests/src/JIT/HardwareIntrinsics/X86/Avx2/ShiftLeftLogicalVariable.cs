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
                using (TestTable<int, uint, int> intTable = new TestTable<int, uint, int>(new int[8] { 1, 5, 100, 0, 1, 5, 100, 0}, new uint[8] { 2, 1, 50, 0, 22, 1, 50, 0 }, new int[8]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[8] { 2, 1, 50, 0, 22, 1, 50, 0 }, new uint[8]))
                using (TestTable<long, ulong, long> longTable = new TestTable<long, ulong, long>(new long[4] { 1, -5, 100, 0 }, new ulong[4] { 2, 1, 500, 0}, new long[4]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[4] { 1, 5, 100, 0 }, new ulong[4] { 2, 1, 500, 0 }, new ulong[4]))
                {   
                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<uint>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.ShiftLeftLogicalVariable(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector256<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector256<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.ShiftLeftLogicalVariable(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl1 = Unsafe.Read<Vector256<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector256<ulong>>(longTable.inArray2Ptr);
                    var vl3 = Avx2.ShiftLeftLogicalVariable(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul1 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Avx2.ShiftLeftLogicalVariable(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                        
                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if ((intTable.inArray2[i] > 31 ? 0 : intTable.inArray1[i] << (int)intTable.inArray2[i]) != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", intTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", intTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", intTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if ((uintTable.inArray2[i] > 31 ? 0 : (int)uintTable.inArray1[i] << (int)uintTable.inArray2[i]) != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", uintTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", uintTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", uintTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < longTable.outArray.Length; i++)
                    {
                        if ((longTable.inArray2[i] > 63 ? 0 : (int)longTable.inArray1[i] << (int)longTable.inArray2[i]) != (int)longTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", longTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", longTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", longTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < ulongTable.outArray.Length; i++)
                    {
                        if ((ulongTable.inArray2[i] > 63 ? 0 : (int)ulongTable.inArray1[i] << (int)ulongTable.inArray2[i]) != (int)ulongTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", ulongTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ulongTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ulongTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                }

                using (TestTable<int, uint, int> intTable = new TestTable<int, uint, int>(new int[8] { 1, 5, 100, 0, 1, 5, 100, 0}, new uint[8] { 2, 1, 50, 0, 22, 1, 50, 0 }, new int[8]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[8] { 2, 1, 50, 0, 22, 1, 50, 0 }, new uint[8]))
                using (TestTable<long, ulong, long> longTable = new TestTable<long, ulong, long>(new long[4] { 1, -5, 100, 0 }, new ulong[4] { 2, 1, 500, 0}, new long[4]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[4] { 1, 5, 100, 0 }, new ulong[4] { 2, 1, 500, 0 }, new ulong[4]))
                {   
                    var vi3 = Avx2.ShiftLeftLogicalVariable(Avx.LoadVector256((int*)intTable.inArray1Ptr), Avx.LoadVector256((uint*)intTable.inArray2Ptr));
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui3 = Avx2.ShiftLeftLogicalVariable(Avx.LoadVector256((uint*)uintTable.inArray1Ptr), Avx.LoadVector256((uint*)uintTable.inArray2Ptr));
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl3 = Avx2.ShiftLeftLogicalVariable(Avx.LoadVector256((long*)longTable.inArray1Ptr), Avx.LoadVector256((ulong*)longTable.inArray2Ptr));
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul3 = Avx2.ShiftLeftLogicalVariable(Avx.LoadVector256((ulong*)ulongTable.inArray1Ptr), Avx.LoadVector256((ulong*)ulongTable.inArray2Ptr));
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                        
                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if ((intTable.inArray2[i] > 31 ? 0 : intTable.inArray1[i] << (int)intTable.inArray2[i]) != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", intTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", intTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", intTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if ((uintTable.inArray2[i] > 31 ? 0 : (int)uintTable.inArray1[i] << (int)uintTable.inArray2[i]) != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", uintTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", uintTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", uintTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < longTable.outArray.Length; i++)
                    {
                        if ((longTable.inArray2[i] > 63 ? 0 : (int)longTable.inArray1[i] << (int)longTable.inArray2[i]) != (int)longTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", longTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", longTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", longTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < ulongTable.outArray.Length; i++)
                    {
                        if ((ulongTable.inArray2[i] > 63 ? 0 : (int)ulongTable.inArray1[i] << (int)ulongTable.inArray2[i]) != (int)ulongTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", ulongTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ulongTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ulongTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                }

                using (TestTable<int, uint, int> intTable = new TestTable<int, uint, int>(new int[4] { 1, 5, 100, 0}, new uint[4] { 2, 1, 50, 0 }, new int[4]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4] { 2, 1, 50, 0 }, new uint[4]))
                using (TestTable<long, ulong, long> longTable = new TestTable<long, ulong, long>(new long[2] { 1, -5 }, new ulong[2] { 2, 1 }, new long[2]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[2] { 1, 5 }, new ulong[2] { 2, 500 }, new ulong[2]))
                {   
                    var vi1 = Unsafe.Read<Vector128<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector128<uint>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.ShiftLeftLogicalVariable(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector128<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector128<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.ShiftLeftLogicalVariable(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl1 = Unsafe.Read<Vector128<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector128<ulong>>(longTable.inArray2Ptr);
                    var vl3 = Avx2.ShiftLeftLogicalVariable(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul1 = Unsafe.Read<Vector128<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector128<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Avx2.ShiftLeftLogicalVariable(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                        
                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if ((intTable.inArray2[i] > 31 ? 0 : intTable.inArray1[i] << (int)intTable.inArray2[i]) != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", intTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", intTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", intTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if ((uintTable.inArray2[i] > 31 ? 0 : (int)uintTable.inArray1[i] << (int)uintTable.inArray2[i]) != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", uintTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", uintTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", uintTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < longTable.outArray.Length; i++)
                    {
                        if ((longTable.inArray2[i] > 63 ? 0 : (int)longTable.inArray1[i] << (int)longTable.inArray2[i]) != (int)longTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", longTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", longTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", longTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < ulongTable.outArray.Length; i++)
                    {
                        if ((ulongTable.inArray2[i] > 63 ? 0 : (int)ulongTable.inArray1[i] << (int)ulongTable.inArray2[i]) != (int)ulongTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", ulongTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ulongTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ulongTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                }

                using (TestTable<int, uint, int> intTable = new TestTable<int, uint, int>(new int[4] { 1, 5, 100, 0 }, new uint[4] { 2, 1, 50, 0 }, new int[4]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4] { 2, 1, 50, 0 }, new uint[4]))
                using (TestTable<long, ulong, long> longTable = new TestTable<long, ulong, long>(new long[2] { 1, -5 }, new ulong[2] { 2, 1 }, new long[2]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[2] { 1, 5 }, new ulong[2] { 2, 500 }, new ulong[2]))
                {   
                    var vi3 = Avx2.ShiftLeftLogicalVariable(Sse2.LoadVector128((int*)intTable.inArray1Ptr), Sse2.LoadVector128((uint*)intTable.inArray2Ptr));
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui3 = Avx2.ShiftLeftLogicalVariable(Sse2.LoadVector128((uint*)uintTable.inArray1Ptr), Sse2.LoadVector128((uint*)uintTable.inArray2Ptr));
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl3 = Avx2.ShiftLeftLogicalVariable(Sse2.LoadVector128((long*)longTable.inArray1Ptr), Sse2.LoadVector128((ulong*)longTable.inArray2Ptr));
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul3 = Avx2.ShiftLeftLogicalVariable(Sse2.LoadVector128((ulong*)ulongTable.inArray1Ptr), Sse2.LoadVector128((ulong*)ulongTable.inArray2Ptr));
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                        
                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if ((intTable.inArray2[i] > 31 ? 0 : intTable.inArray1[i] << (int)intTable.inArray2[i]) != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", intTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", intTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", intTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if ((uintTable.inArray2[i] > 31 ? 0 : (int)uintTable.inArray1[i] << (int)uintTable.inArray2[i]) != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", uintTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", uintTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", uintTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < longTable.outArray.Length; i++)
                    {
                        if ((longTable.inArray2[i] > 63 ? 0 : (int)longTable.inArray1[i] << (int)longTable.inArray2[i]) != (int)longTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", longTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", longTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", longTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    }

                    for (int i = 0; i < ulongTable.outArray.Length; i++)
                    {
                        if ((ulongTable.inArray2[i] > 63 ? 0 : (int)ulongTable.inArray1[i] << (int)ulongTable.inArray2[i]) != (int)ulongTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 ShiftLeftLogicalVariable Vector128 failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", ulongTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ulongTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ulongTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
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
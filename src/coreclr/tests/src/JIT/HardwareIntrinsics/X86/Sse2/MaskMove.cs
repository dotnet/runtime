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
                using (TestTable<byte> byteTable = new TestTable<byte>(new byte[16] { 255, 2, 0, 80, 0, 7, 0, 1, 2, 7, 80, 0, 123, 127, 5, 255 }, new byte[16] { 255, 0, 255, 0, 255, 0, 255, 0, 0, 255, 0, 255, 0, 255, 0, 255 }, new byte[16]))
                {
                    Unsafe.Write(byteTable.outArrayPtr, Vector128<byte>.Zero);

                    var vf1 = Unsafe.Read<Vector128<byte>>(byteTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<byte>>(byteTable.inArray2Ptr);
                    Sse2.MaskMove(vf1, vf2, (byte*)(byteTable.outArrayPtr));

                    if (!byteTable.CheckResult((left, right, result) => result == (((right & 128) != 0) ? left : 0)))
                    {
                        Console.WriteLine("SSE MaskMove failed on byte:");
                        foreach (var item in byteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[16] { -1, 2, 0, 6, 0, 7, 111, 1, 2, 55, 80, 0, 11, 127, 5, -9 }, new sbyte[16] { -1, 0, -1, 0, -1, 0, -1, 0, 0, -1, 0, -1, 0, -1, 0, -1 }, new sbyte[16]))
                {
                    Unsafe.Write(sbyteTable.outArrayPtr, Vector128<sbyte>.Zero);

                    var vf1 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray2Ptr);
                    Sse2.MaskMove(vf1, vf2, (sbyte*)(sbyteTable.outArrayPtr));

                    if (!sbyteTable.CheckResult((left, right, result) => result == (((right & -128) != 0) ? left : 0)))
                    {
                        Console.WriteLine("SSE MaskMove failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
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

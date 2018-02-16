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

            if (Ssse3.IsSupported)
            {
                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[16] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 }, new sbyte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, new sbyte[16]))
                {
                    var vf1 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<sbyte>>(sbyteTable.inArray2Ptr);

                    var vf3 = Ssse3.AlignRight(vf1, vf2, 27);
                    Unsafe.Write(sbyteTable.outArrayPtr, vf3);

                    if (!sbyteTable.CheckResult((x, y, z) => (z[00] == 27) && (z[01] == 28) && (z[02] == 29) && (z[03] == 30) && 
                                                             (z[04] == 31) && (z[05] == 00) && (z[06] == 00) && (z[07] == 00) &&
                                                             (z[08] == 00) && (z[09] == 00) && (z[10] == 00) && (z[11] == 00) && 
                                                             (z[12] == 00) && (z[13] == 00) && (z[14] == 00) && (z[15] == 00)))
                    {
                        Console.WriteLine("SSE AlignRight failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Ssse3.AlignRight(vf1, vf2, 5);
                    Unsafe.Write(sbyteTable.outArrayPtr, vf3);

                    if (!sbyteTable.CheckResult((x, y, z) => (z[00] == 05) && (z[01] == 06) && (z[02] == 07) && (z[03] == 08) && 
                                                             (z[04] == 09) && (z[05] == 10) && (z[06] == 11) && (z[07] == 12) &&
                                                             (z[08] == 13) && (z[09] == 14) && (z[10] == 15) && (z[11] == 16) && 
                                                             (z[12] == 17) && (z[13] == 18) && (z[14] == 19) && (z[15] == 20)))
                    {
                        Console.WriteLine("SSE AlignRight failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Ssse3.AlignRight(vf1, vf2, 250);
                    Unsafe.Write(sbyteTable.outArrayPtr, vf3);
                    
                    if (!sbyteTable.CheckResult((x, y, z) => (z[00] == 00) && (z[01] == 00) && (z[02] == 00) && (z[03] == 00) && 
                                                             (z[04] == 00) && (z[05] == 00) && (z[06] == 00) && (z[07] == 00) &&
                                                             (z[08] == 00) && (z[09] == 00) && (z[10] == 00) && (z[11] == 00) && 
                                                             (z[12] == 00) && (z[13] == 00) && (z[14] == 00) && (z[15] == 00)))
                    {
                        Console.WriteLine("SSE AlignRight failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Ssse3.AlignRight(vf1, vf2, 228);
                    Unsafe.Write(sbyteTable.outArrayPtr, vf3);

                    if (!sbyteTable.CheckResult((x, y, z) => (z[00] == 00) && (z[01] == 00) && (z[02] == 00) && (z[03] == 00) && 
                                                             (z[04] == 00) && (z[05] == 00) && (z[06] == 00) && (z[07] == 00) &&
                                                             (z[08] == 00) && (z[09] == 00) && (z[10] == 00) && (z[11] == 00) && 
                                                             (z[12] == 00) && (z[13] == 00) && (z[14] == 00) && (z[15] == 00)))
                    {
                        Console.WriteLine("SSE AlignRight failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = (Vector128<sbyte>)typeof(Ssse3).GetMethod(nameof(Ssse3.AlignRight), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(27) });
                    Unsafe.Write(sbyteTable.outArrayPtr, vf3);

                    if (!sbyteTable.CheckResult((x, y, z) => (z[00] == 27) && (z[01] == 28) && (z[02] == 29) && (z[03] == 30) && 
                                                             (z[04] == 31) && (z[05] == 00) && (z[06] == 00) && (z[07] == 00) &&
                                                             (z[08] == 00) && (z[09] == 00) && (z[10] == 00) && (z[11] == 00) && 
                                                             (z[12] == 00) && (z[13] == 00) && (z[14] == 00) && (z[15] == 00)))
                    {
                        Console.WriteLine("SSE AlignRight failed on sbyte:");
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
            public bool CheckResult(Func<T[], T[], T[], bool> check)
            {
                return check(inArray1, inArray2, outArray);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Linq;
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

            if (Sse41.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4] { 22, -1, -50, 0 }, new float[4]))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);

                    var vf3 = Sse41.DotProduct(vf1, vf2, 255);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]) + (x[3] * y[3]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 127);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 63);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == ((x[0] * y[0]) + (x[1] * y[1])))))
                    {
                        Console.WriteLine("3 SSE41 DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 85);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z[0] == ((x[0] * y[0]) + (x[2] * y[2])) &&
                                                             z[2] == ((x[0] * y[0]) + (x[2] * y[2])) &&
                                                             z[1] == 0 && z[3] == 0))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = (Vector128<float>)typeof(Sse41).GetMethod(nameof(Sse41.DotProduct), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(255) });
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]) +
                                                                                       (x[2] * y[2]) + (x[3] * y[3]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2] { 22, -1 }, new double[2]))
                {
                    var vf1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<double>>(doubleTable.inArray2Ptr);

                    var vf3 = Sse41.DotProduct(vf1, vf2, 51);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 19);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 17);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => z[0] == (x[0] * y[0]) &&
                                                              z[1] == 0))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = Sse41.DotProduct(vf1, vf2, 33);
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => z[0] == (x[1] * y[1]) &&
                                                              z[1] == 0))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf3 = (Vector128<double>)typeof(Sse41).GetMethod(nameof(Sse41.DotProduct), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(51) });
                    Unsafe.Write(doubleTable.outArrayPtr, vf3);

                    if (!doubleTable.CheckResult((x, y, z) => z.All(result => result == (x[0] * y[0]) + (x[1] * y[1]))))
                    {
                        Console.WriteLine("SSE41 DotProduct failed on double:");
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

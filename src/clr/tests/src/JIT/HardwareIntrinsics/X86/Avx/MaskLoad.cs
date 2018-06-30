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

            if (Avx.IsSupported)
            {
                using (TestTable<float, uint> floatTable = new TestTable<float, uint>(new float[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new uint[8] { uint.MaxValue, uint.MaxValue, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0 }, new float[8]))
                {
                    Vector256<float> vf = Avx.MaskLoad((float*)(floatTable.inArrayPtr), Avx.LoadVector256((float*)(floatTable.maskArrayPtr)));
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, m, y) => m == uint.MaxValue ? BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y) : BitConverter.SingleToInt32Bits(y) == 0))
                    {
                        Console.WriteLine("AVX MaskLoad failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<double, ulong> doubleTable = new TestTable<double, ulong>(new double[4] { 1, -5, 100, 0}, new ulong[4] { 0, ulong.MaxValue, ulong.MaxValue, 0}, new double[4]))
                {
                    Vector256<double> vf = Avx.MaskLoad((double*)(doubleTable.inArrayPtr), Avx.LoadVector256((double*)(doubleTable.maskArrayPtr)));
                    Unsafe.Write(doubleTable.outArrayPtr, vf);

                    if (!doubleTable.CheckResult((x, m, y) =>  m == ulong.MaxValue ? BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y) : BitConverter.DoubleToInt64Bits(y) == 0))
                    {
                        Console.WriteLine("AVX MaskLoad failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<float, uint> floatTable = new TestTable<float, uint>(new float[4] { 1, -5, 100, 0 }, new uint[4] { uint.MaxValue, 0, 0, uint.MaxValue }, new float[4]))
                {
                    Vector128<float> vf = Avx.MaskLoad((float*)(floatTable.inArrayPtr), Sse.LoadVector128((float*)(floatTable.maskArrayPtr)));
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, m, y) => m == uint.MaxValue ? BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y) : BitConverter.SingleToInt32Bits(y) == 0))
                    {
                        Console.WriteLine("AVX MaskLoad failed on Vector128 float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<double, ulong> doubleTable = new TestTable<double, ulong>(new double[2] { 1, -5}, new ulong[2] { 0, ulong.MaxValue}, new double[2]))
                {
                    Vector128<double> vf = Avx.MaskLoad((double*)(doubleTable.inArrayPtr), Sse2.LoadVector128((double*)(doubleTable.maskArrayPtr)));
                    Unsafe.Write(doubleTable.outArrayPtr, vf);

                    if (!doubleTable.CheckResult((x, m, y) =>  m == ulong.MaxValue ? BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y) : BitConverter.DoubleToInt64Bits(y) == 0))
                    {
                        Console.WriteLine("AVX MaskLoad failed on double:");
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

        public unsafe struct TestTable<T, U> : IDisposable where T : struct
        {
            public T[] inArray;
            public U[] maskArray;
            public T[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* maskArrayPtr => maskHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle maskHandle;
            GCHandle outHandle;
            public TestTable(T[] a, U[] mask, T[] b)
            {
                this.inArray = a;
                this.maskArray = mask;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                maskHandle = GCHandle.Alloc(maskArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, U, T, bool> check)
            {
                for (int i = 0; i < inArray.Length; i++)
                {
                    if (!check(inArray[i], maskArray[i], outArray[i]))
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

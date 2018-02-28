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
    // that it is intentionally designed to be a struct type that meets 
    // the generic constraint but is not supported by any intrinsics
    struct Num
    {
        public int a;
    }

    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable<float, int> floatTable = new TestTable<float, int>(new float[4] { 1, float.NaN, float.PositiveInfinity, float.NegativeInfinity }, new int[4]))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var vf2 = Sse.StaticCast<float, int>(vf1);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == y))
                    {
                        Console.WriteLine("SSE StaticCast failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // the successful path is the one that catches the exception, so that does nothing, 
                    // it is the fall-through path that's the error path
                    try
                    {
                        var v = Sse.StaticCast<float, Num>(vf1);
                        Unsafe.Write(floatTable.outArrayPtr, v);
                        Console.WriteLine("SSE StaticCast failed on target type test:");
                        testResult = Fail;
                    }
                    catch (System.NotSupportedException)
                    {
                    }

                    // the successful path is the one that catches the exception, so that does nothing, 
                    // it is the fall-through path that's the error path
                    try
                    {
                        var v = TestSrcType();
                        Unsafe.Write(floatTable.outArrayPtr, v);
                        Console.WriteLine("SSE StaticCast failed on source type test:");
                        testResult = Fail;
                    }
                    catch (System.NotSupportedException)
                    {
                    }
                }
            }
            
            return testResult;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Vector128<int> TestSrcType()
        {
            Vector128<Num> v1 = new Vector128<Num>();
            Vector128<Num> v2 = new Vector128<Num>();
            return Sse2.Add(Sse.StaticCast<Num, int>(v1), Sse.StaticCast<Num, int>(v2));
        }

        public unsafe struct TestTable<T, U> : IDisposable where T : struct where U : struct
        {
            public T[] inArray;
            public U[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle outHandle;
            public TestTable(T[] a, U[] b)
            {
                this.inArray = a;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, U, bool> check)
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

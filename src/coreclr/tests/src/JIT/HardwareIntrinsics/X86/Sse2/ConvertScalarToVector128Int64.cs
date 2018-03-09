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
                using (TestTable<long> longTable = new TestTable<long>(new long[2] { 1, -5 }, new long[2]))
                {
                    if (Environment.Is64BitProcess)
                    {
                        var vd = Sse2.ConvertScalarToVector128Int64((long)-5);
                        Unsafe.Write(longTable.outArrayPtr, vd);

                        if (!longTable.CheckResult((x, y) => (y[0] == -5) && (y[1] == 0)))
                        {
                            Console.WriteLine("SSE2 ConvertScalarToVector128Int32 failed on long:");
                            foreach (var item in longTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            testResult = Fail;
                        }
                    }
                    else
                    {
                        try
                        {
                            var vd = Sse2.ConvertScalarToVector128Int64(-5l);
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.ConvertScalarToVector128Int64)} failed: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {

                        }
                        catch(Exception ex)
                        {
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.ConvertScalarToVector128Int64)}-{ex} failed: expected PlatformNotSupportedException exception.");                            
                        }
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
            public bool CheckResult(Func<T[], T[], bool> check)
            {
                return check(inArray, outArray);
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    }
}

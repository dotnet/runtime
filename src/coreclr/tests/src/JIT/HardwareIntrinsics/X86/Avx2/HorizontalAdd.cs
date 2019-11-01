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
                using (TestTable<short, short, short> shortTable = new TestTable<short, short, short>(new short[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new short[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0}, new short[16]))
                using (TestTable<int, int, int> intTable = new TestTable<int, int, int>(new int[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new int[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new int[8]))
                {
                    var vs1 = Unsafe.Read<Vector256<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector256<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx2.HorizontalAdd(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.HorizontalAdd(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);
                    
                    if ((shortTable.inArray1[1] + shortTable.inArray1[0] != shortTable.outArray[0]) || 
                        (shortTable.inArray1[3] + shortTable.inArray1[2] != shortTable.outArray[1]) ||
                        (shortTable.inArray1[5] + shortTable.inArray1[4] != shortTable.outArray[2]) ||
                        (shortTable.inArray1[7] + shortTable.inArray1[6] != shortTable.outArray[3]) ||
                        (shortTable.inArray2[1] + shortTable.inArray2[0] != shortTable.outArray[4]) || 
                        (shortTable.inArray2[3] + shortTable.inArray2[2] != shortTable.outArray[5]) ||
                        (shortTable.inArray2[5] + shortTable.inArray2[4] != shortTable.outArray[6]) ||
                        (shortTable.inArray2[7] + shortTable.inArray2[6] != shortTable.outArray[7]) ||
                        (shortTable.inArray1[9] + shortTable.inArray1[8] != shortTable.outArray[8]) || 
                        (shortTable.inArray1[11] + shortTable.inArray1[10] !=shortTable.outArray[9]) ||
                        (shortTable.inArray1[13] + shortTable.inArray1[12] !=shortTable.outArray[10]) ||
                        (shortTable.inArray1[15] + shortTable.inArray1[14] !=shortTable.outArray[11]) ||
                        (shortTable.inArray2[9] + shortTable.inArray2[8]   !=shortTable.outArray[12]) || 
                        (shortTable.inArray2[11] + shortTable.inArray2[10] !=shortTable.outArray[13]) ||
                        (shortTable.inArray2[13] + shortTable.inArray2[12] !=shortTable.outArray[14]) ||
                        (shortTable.inArray2[15] + shortTable.inArray2[14] !=shortTable.outArray[15]))
                        {
                            Console.WriteLine("AVX2 HorizontalAdd failed on short:");
           
                            testResult = Fail;
            
                        }      
                    
                    if ((intTable.inArray1[1] + intTable.inArray1[0] != intTable.outArray[0]) || 
                        (intTable.inArray1[3] + intTable.inArray1[2] != intTable.outArray[1]) ||
                        (intTable.inArray2[1] + intTable.inArray2[0] != intTable.outArray[2]) ||
                        (intTable.inArray2[3] + intTable.inArray2[2] != intTable.outArray[3]) ||
                        (intTable.inArray1[5] + intTable.inArray1[4] != intTable.outArray[4]) || 
                        (intTable.inArray1[7] + intTable.inArray1[6] != intTable.outArray[5]) ||
                        (intTable.inArray2[5] + intTable.inArray2[4] != intTable.outArray[6]) ||
                        (intTable.inArray2[7] + intTable.inArray2[6] != intTable.outArray[7])) 

                        {
                            Console.WriteLine("AVX2 HorizontalAdd failed on int:");
           
                            testResult = Fail;
            
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
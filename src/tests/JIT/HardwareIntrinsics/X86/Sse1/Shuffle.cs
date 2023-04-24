// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Sse1
{
    public partial class Program
    {
        [Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/75767", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
        [Fact]
        public static unsafe void Shuffle()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[4] { 1, -5, 100, 0 }, new float[4] { 22, -1, -50, 0 }, new float[4]))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);

                    // XYZW
                    var vf3 = Sse.Shuffle(vf1, vf2, 27);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[3]) && (z[1] == x[2]) &&
                                                             (z[2] == y[1]) && (z[3] == y[0])))
                    {
                        Console.WriteLine("SSE Shuffle failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // XXYY
                    vf3 = Sse.Shuffle(vf1, vf2, 5);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[1]) && (z[1] == x[1]) &&
                                                             (z[2] == y[0]) && (z[3] == y[0])))
                    {
                        Console.WriteLine("SSE Shuffle failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // WWZZ
                    vf3 = Sse.Shuffle(vf1, vf2, 250);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[2]) && (z[1] == x[2]) &&
                                                             (z[2] == y[3]) && (z[3] == y[3])))
                    {
                        Console.WriteLine("SSE Shuffle failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    // WZYX
                    vf3 = Sse.Shuffle(vf1, vf2, 228);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[0]) && (z[1] == x[1]) &&
                                                             (z[2] == y[2]) && (z[3] == y[3])))
                    {
                        Console.WriteLine("SSE Shuffle failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                    
                    // XYZW
                    vf3 = (Vector128<float>)typeof(Sse).GetMethod(nameof(Sse.Shuffle), new Type[] { vf1.GetType(), vf2.GetType(), typeof(byte) }).Invoke(null, new object[] { vf1, vf2, (byte)(27) });
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == x[3]) && (z[1] == x[2]) &&
                                                             (z[2] == y[1]) && (z[3] == y[0])))
                    {
                        Console.WriteLine("SSE Shuffle failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }


            Assert.Equal(Pass, testResult);
        }
    }
}

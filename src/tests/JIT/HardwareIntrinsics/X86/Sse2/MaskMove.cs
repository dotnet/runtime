// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void MaskMove()
        {
            int testResult = Pass;

            if (Sse2.IsSupported)
            {
                using (TestTable_2Input<byte> byteTable = new TestTable_2Input<byte>(new byte[16] { 255, 2, 0, 80, 0, 7, 0, 1, 2, 7, 80, 0, 123, 127, 5, 255 }, new byte[16] { 255, 0, 255, 0, 255, 0, 255, 0, 0, 255, 0, 255, 0, 255, 0, 255 }, new byte[16]))
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

                using (TestTable_2Input<sbyte> sbyteTable = new TestTable_2Input<sbyte>(new sbyte[16] { -1, 2, 0, 6, 0, 7, 111, 1, 2, 55, 80, 0, 11, 127, 5, -9 }, new sbyte[16] { -1, 0, -1, 0, -1, 0, -1, 0, 0, -1, 0, -1, 0, -1, 0, -1 }, new sbyte[16]))
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

            Assert.Equal(Pass, testResult);
        }
    }
}

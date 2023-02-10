// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void SubtractSaturate()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                using (TestTable<byte, byte, byte> byteTable = new TestTable<byte, byte, byte>(new byte[32] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[32] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new byte[32]))
                using (TestTable<sbyte, sbyte, sbyte> sbyteTable = new TestTable<sbyte, sbyte, sbyte>(new sbyte[32] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[32] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new sbyte[32]))
                using (TestTable<short, short, short> shortTable = new TestTable<short, short, short>(new short[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new short[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0}, new short[16]))
                using (TestTable<ushort, ushort, ushort> ushortTable = new TestTable<ushort, ushort, ushort>(new ushort[16] { 1, 5, 100, 0, 1, 5, 100, 0,  1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[16] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new ushort[16]))
                {

                    var vb1 = Unsafe.Read<Vector256<byte>>(byteTable.inArray1Ptr);
                    var vb2 = Unsafe.Read<Vector256<byte>>(byteTable.inArray2Ptr);
                    var vb3 = Avx2.SubtractSaturate(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    var vsb1 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Avx2.SubtractSaturate(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    var vs1 = Unsafe.Read<Vector256<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector256<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx2.SubtractSaturate(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vus1 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Avx2.SubtractSaturate(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);
                    
                    for (int i = 0; i < byteTable.outArray.Length; i++)
                    {
                        int value = byteTable.inArray1[i] - byteTable.inArray2[i];
                        value = Math.Max(value, 0);
                        value = Math.Min(value, byte.MaxValue);
                        if ((byte)value != byteTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 SubtractSaturate failed on byte:");
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }

                    for (int i = 0; i < sbyteTable.outArray.Length; i++)
                    {
                        int value = sbyteTable.inArray1[i] - sbyteTable.inArray2[i];
                        value = Math.Max(value, sbyte.MinValue);
                        value = Math.Min(value, sbyte.MaxValue);
                        if ((sbyte)value != sbyteTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 SubtractSaturate failed on sbyte:");
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                    
                    
                    for (int i = 0; i < shortTable.outArray.Length; i++)
                    {
                        int value = shortTable.inArray1[i] - shortTable.inArray2[i];
                        value = Math.Max(value, short.MinValue);
                        value = Math.Min(value, short.MaxValue);
                        if ((short)value != shortTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 SubtractSaturate failed on short:");
                            Console.WriteLine();
                            
                            testResult = Fail;
                            break;
                        }
                    }
                    
                    for (int i = 0; i < ushortTable.outArray.Length; i++)
                    {
                        int value = ushortTable.inArray1[i] - ushortTable.inArray2[i];
                        value = Math.Max(value, 0);
                        value = Math.Min(value, ushort.MaxValue);
                        if ((ushort)value != ushortTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 SubtractSaturate failed on ushort:");
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}

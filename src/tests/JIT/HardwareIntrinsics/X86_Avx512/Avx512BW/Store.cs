// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512BW
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Store()
        {
            int testResult = Pass;

            if (Avx512BW.IsSupported)
            {
                using (TestTable<short> intTable = new TestTable<short>(new short[32] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new short[32]))
                {
                    var vf = Unsafe.Read<Vector512<short>>(intTable.inArrayPtr);
                    Avx512BW.Store((short*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((short x, short y) => x == y))
                    {
                        Console.WriteLine("AVX512BW Store failed on short:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> intTable = new TestTable<ushort>(new ushort[32] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new ushort[32]))
                {
                    var vf = Unsafe.Read<Vector512<ushort>>(intTable.inArrayPtr);
                    Avx512BW.Store((ushort*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((ushort x, ushort y) => x == y))
                    {
                        Console.WriteLine("AVX512BW Store failed on ushort:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> intTable = new TestTable<sbyte>(new sbyte[64] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new sbyte[64]))
                {
                    var vf = Unsafe.Read<Vector512<sbyte>>(intTable.inArrayPtr);
                    Avx512BW.Store((sbyte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((sbyte x, sbyte y) => x == y))
                    {
                        Console.WriteLine("AVX512BW Store failed on sbyte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> intTable = new TestTable<byte>(new byte[64] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new byte[64]))
                {
                    var vf = Unsafe.Read<Vector512<byte>>(intTable.inArrayPtr);
                    Avx512BW.Store((byte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((byte x, byte y) => x == y))
                    {
                        Console.WriteLine("AVX512BW Store failed on byte:");
                        foreach (var item in intTable.outArray)
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

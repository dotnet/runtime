// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    internal static partial class Program
    {
        private const int Pass = 100;
        private const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.SetAllVector128);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double>.Create(testsCount))
                using (var longTable = TestTableSse2<long>.Create(testsCount))
                using (var ulongTable = TestTableSse2<ulong>.Create(testsCount))
                using (var intTable = TestTableSse2<int>.Create(testsCount))
                using (var uintTable = TestTableSse2<uint>.Create(testsCount))
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                using (var ushortTable = TestTableSse2<ushort>.Create(testsCount))
                using (var sbyteTable = TestTableSse2<sbyte>.Create(testsCount))
                using (var byteTable = TestTableSse2<byte>.Create(testsCount))
                {
                    for (double i = 0; i < testsCount; i += 1)
                    {
                       Vector128<double> result = Sse2.SetAllVector128(i);
                       doubleTable.SetOutArray(result, (int)i);
                    }

                    if (Environment.Is64BitProcess)
                    {
                        for (long i = 0; i < testsCount; i++)
                        {
                            Vector128<long> result = Sse2.SetAllVector128(i);
                            longTable.SetOutArray(result, (int)i);
                        }

                        for (ulong i = 0; i < (ulong)testsCount; i++)
                        {
                            Vector128<ulong> result = Sse2.SetAllVector128(i);
                            ulongTable.SetOutArray(result, (int)i);
                        }
                    }
                    else
                    {
                        try
                        {
                            var vd = Sse2.SetAllVector128((long)0xffffl);
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetAllVector128)} failed for long: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {

                        }

                        try
                        {
                            var vd = Sse2.SetAllVector128((ulong)0xfffful);
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetAllVector128)} failed for ulong: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {

                        }    
                    }



                    for (int i = 0; i < testsCount; i++)
                    {
                       Vector128<int> result = Sse2.SetAllVector128((int)i);
                       intTable.SetOutArray(result, i);
                    }

                    for (uint i = 0; i < testsCount; i++)
                    {
                       Vector128<uint> result = Sse2.SetAllVector128(i);
                       uintTable.SetOutArray(result, (int)i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                       Vector128<short> result = Sse2.SetAllVector128((short)i);
                       shortTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                       Vector128<ushort> result = Sse2.SetAllVector128((ushort)i);
                       ushortTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                       Vector128<sbyte> result = Sse2.SetAllVector128((sbyte)i);
                       sbyteTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                       Vector128<byte> result = Sse2.SetAllVector128((byte)i);
                       byteTable.SetOutArray(result, i);
                    }

                    double doubleCounter = 0.0;
                    CheckMethodSpan<double> checkDouble = (Span<double> x, Span<double> y, Span<double> z, Span<double> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != doubleCounter)
                               result = false;
                       }
                       doubleCounter += 1;
                       return result;
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                       PrintError(doubleTable, methodUnderTestName, "(double x, double y, double z, ref double a) => (a = BitwiseXor(x, y)) == z", checkDouble);
                       testResult = Fail;
                    }

                    if (Environment.Is64BitProcess)
                    {
                        long longCounter = 0;
                        CheckMethodSpan<long> checkInt64 = (Span<long> x, Span<long> y, Span<long> z, Span<long> a) =>
                        {
                            bool result = true;
                            for (int i = 0; i < x.Length; i++)
                            {
                                if (z[i] != longCounter)
                                    result = false;
                            }
                            longCounter++;
                            return result;
                        };

                        if (!longTable.CheckResult(checkInt64))
                        {
                            PrintError(longTable, methodUnderTestName, "(long x, long y, long z, ref long a) => (a = x ^ y) == z", checkInt64);
                            testResult = Fail;
                        }

                        ulong ulongCounter = 0;
                        CheckMethodSpan<ulong> checkUInt64 = (Span<ulong> x, Span<ulong> y, Span<ulong> z, Span<ulong> a) =>
                        {
                            bool result = true;
                            for (int i = 0; i < x.Length; i++)
                            {
                                if (z[i] != ulongCounter)
                                    result = false;
                            }
                            ulongCounter++;
                            return result;
                        };

                        if (!ulongTable.CheckResult(checkUInt64))
                        {
                            PrintError(ulongTable, methodUnderTestName, "(Span<ulong> x, Span<ulong> y, Span<ulong> z, Span<ulong> a) => SetAllVector128", checkUInt64);
                            testResult = Fail;
                        }
                    }

                    int intCounter = 0;
                    CheckMethodSpan<int> checkInt32 = (Span<int> x, Span<int> y, Span<int> z, Span<int> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != intCounter)
                               result = false;
                       }
                       intCounter++;
                       return result;
                    };

                    if (!intTable.CheckResult(checkInt32))
                    {
                       PrintError(intTable, methodUnderTestName, "(int x, int y, int z, ref int a) => (a = x ^ y) == z", checkInt32);
                       testResult = Fail;
                    }

                    uint uintCounter = 0;
                    CheckMethodSpan<uint> checkUInt32 = (Span<uint> x, Span<uint> y, Span<uint> z, Span<uint> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != uintCounter)
                               result = false;
                       }
                       uintCounter++;
                       return result;
                    };

                    if (!uintTable.CheckResult(checkUInt32))
                    {
                       PrintError(uintTable, methodUnderTestName, "(uint x, uint y, uint z, ref uint a) => (a = x ^ y) == z", checkUInt32);
                       testResult = Fail;
                    }

                    int shortCounter = 0;
                    CheckMethodSpan<short> checkInt16 = (Span<short> x, Span<short> y, Span<short> z, Span<short> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != shortCounter)
                               result = false;
                       }
                       shortCounter++;
                       return result;
                    };

                    if (!shortTable.CheckResult(checkInt16))
                    {
                       PrintError(shortTable, methodUnderTestName, "(short x, short y, short z, ref short a) => (a = (short)(x ^ y)) == z", checkInt16);
                       testResult = Fail;
                    }

                    int ushortCounter = 0;
                    CheckMethodSpan<ushort> checkUInt16 = (Span<ushort> x, Span<ushort> y, Span<ushort> z, Span<ushort> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != ushortCounter)
                               result = false;
                       }
                       ushortCounter++;
                       return result;
                    };

                    if (!ushortTable.CheckResult(checkUInt16))
                    {
                       PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x ^ y)) == z", checkUInt16);
                       testResult = Fail;
                    }

                    int sbyteCounter = 0;
                    CheckMethodSpan<sbyte> checkSByte = (Span<sbyte> x, Span<sbyte> y, Span<sbyte> z, Span<sbyte> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < z.Length; i++)
                       {
                           if (z[i] != sbyteCounter)
                               result = false;
                       }
                       sbyteCounter++;
                       return result;
                    };

                    if (!sbyteTable.CheckResult(checkSByte))
                    {
                       PrintError(sbyteTable, methodUnderTestName, "(sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x ^ y)) == z", checkSByte);
                       testResult = Fail;
                    }

                    int byteCounter = 0;
                    CheckMethodSpan<byte> checkByte = (Span<byte> x, Span<byte> y, Span<byte> z, Span<byte> a) =>
                    {
                       bool result = true;
                       for (int i = 0; i < x.Length; i++)
                       {
                           if (z[i] != byteCounter)
                               result = false;
                       }
                       byteCounter++;
                       return result;
                    };

                    if (!byteTable.CheckResult(checkByte))
                    {
                       PrintError(byteTable, methodUnderTestName, "(byte x, byte y, byte z, ref byte a) => (a = (byte)(x ^ y)) == z", checkByte);
                       testResult = Fail;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }

            return testResult;
        }
    }
}

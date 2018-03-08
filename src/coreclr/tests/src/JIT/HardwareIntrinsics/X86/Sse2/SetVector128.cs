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
            string methodUnderTestName = nameof(Sse2.SetVector128);

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
                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<double> value = doubleTable.GetAssignmentData(i).Span;
                        Vector128<double> result = Sse2.SetVector128(value[1], value[0]);
                        doubleTable.SetOutArray(result, i);
                    }

                    if (Environment.Is64BitProcess)
                    {
                        for (int i = 0; i < testsCount; i++)
                        {
                            Span<long> value = longTable.GetAssignmentData(i).Span;
                            Vector128<long> result = Sse2.SetVector128(value[1], value[0]);
                            longTable.SetOutArray(result, i);
                        }

                        for (int i = 0; i < testsCount; i++)
                        {
                            Span<ulong> value = ulongTable.GetAssignmentData(i).Span;
                            Vector128<ulong> result = Sse2.SetVector128(value[1], value[0]);
                            ulongTable.SetOutArray(result, i);
                        }
                    }
                    else
                    {
                        try
                        {
                            for (int i = 0; i < testsCount; i++)
                            {
                                Span<long> value = longTable.GetAssignmentData(i).Span;
                                Vector128<long> result = Sse2.SetVector128(value[1], value[0]);
                                longTable.SetOutArray(result, i);
                            }
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetVector128)} failed on long: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {
                            // We expect PlatformNotSupportedException
                        }
                        catch (Exception ex)
                        {
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetVector128)}-{ex} failed on long: expected PlatformNotSupportedException exception.");                            
                        }

                        try
                        {
                            for (int i = 0; i < testsCount; i++)
                            {
                                Span<ulong> value = ulongTable.GetAssignmentData(i).Span;
                                Vector128<ulong> result = Sse2.SetVector128(value[1], value[0]);
                                ulongTable.SetOutArray(result, i);
                            }
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetVector128)} failed on ulong: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {
                            // We expect PlatformNotSupportedException
                        }
                        catch (Exception ex)
                        {
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetVector128)}-{ex} failed on ulong: expected PlatformNotSupportedException exception.");                            
                        }
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<int> value = intTable.GetAssignmentData(i).Span;
                        Vector128<int> result = Sse2.SetVector128(value[3], value[2], value[1], value[0]);
                        intTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<uint> value = uintTable.GetAssignmentData(i).Span;
                        Vector128<uint> result = Sse2.SetVector128(value[3], value[2], value[1], value[0]);
                        uintTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<short> value = shortTable.GetAssignmentData(i).Span;
                        Vector128<short> result = Sse2.SetVector128(value[7], value[6], value[5], value[4], value[3], value[2], value[1], value[0]);
                        shortTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<ushort> value = ushortTable.GetAssignmentData(i).Span;
                        Vector128<ushort> result = Sse2.SetVector128(value[7], value[6], value[5], value[4], value[3], value[2], value[1], value[0]);
                        ushortTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<sbyte> value = sbyteTable.GetAssignmentData(i).Span;
                        Vector128<sbyte> result = Sse2.SetVector128(value[15], value[14], value[13], value[12], value[11], value[10], value[9],
                            value[8], value[7], value[6], value[5], value[4], value[3], value[2], value[1], value[0]);
                        sbyteTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        Span<byte> value = byteTable.GetAssignmentData(i).Span;
                        Vector128<byte> result = Sse2.SetVector128(value[15], value[14], value[13], value[12], value[11], value[10], value[9],
                            value[8], value[7], value[6], value[5], value[4], value[3], value[2], value[1], value[0]);
                        byteTable.SetOutArray(result, i);
                    }

                    CheckMethodSpan<double> checkDouble = (Span<double> x, Span<double> y, Span<double> z, Span<double> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (BitConverter.DoubleToInt64Bits(z[i]) != BitConverter.DoubleToInt64Bits(x[i]))
                                result = false;
                        }
                        return result;
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x, double y, double z, ref double a) => (a = BitwiseXor(x, y)) == z", checkDouble);
                        testResult = Fail;
                    }

                     if (Environment.Is64BitProcess)
                     {
                        CheckMethodSpan<long> checkLong = (Span<long> x, Span<long> y, Span<long> z, Span<long> a) =>
                        {
                            bool result = true;
                            for (int i = 0; i < x.Length; i++)
                            {
                                if (x[i] != z[i])
                                    result = false;
                            }
                            return result;
                        };

                        if (!longTable.CheckResult(checkLong))
                        {
                            PrintError(longTable, methodUnderTestName, "(long x, long y, long z, ref long a) => (a = x ^ y) == z", checkLong);
                            testResult = Fail;
                        }

                        CheckMethodSpan<ulong> checkUlong = (Span<ulong> x, Span<ulong> y, Span<ulong> z, Span<ulong> a) =>
                        {
                            bool result = true;
                            for (int i = 0; i < x.Length; i++)
                            {
                                if (x[i] != z[i])
                                    result = false;
                            }
                            return result;
                        };

                        if (!longTable.CheckResult(checkLong))
                        {
                            PrintError(ulongTable, methodUnderTestName, "(ulong x, ulong y, ulong z, ref ulong a) => (a = x ^ y) == z", checkUlong);
                            testResult = Fail;
                        }
                     }

                    CheckMethodSpan<int> checkInt32 = (Span<int> x, Span<int> y, Span<int> z, Span<int> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!intTable.CheckResult(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x, int y, int z, ref int a) => (a = x ^ y) == z", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethodSpan<uint> checkUInt32 = (Span<uint> x, Span<uint> y, Span<uint> z, Span<uint> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!uintTable.CheckResult(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(uint x, uint y, uint z, ref uint a) => (a = x ^ y) == z", checkUInt32);
                        testResult = Fail;
                    }

                    CheckMethodSpan<short> checkInt16 = (Span<short> x, Span<short> y, Span<short> z, Span<short> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x, short y, short z, ref short a) => (a = (short)(x ^ y)) == z", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethodSpan<ushort> checkUInt16 = (Span<ushort> x, Span<ushort> y, Span<ushort> z, Span<ushort> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!ushortTable.CheckResult(checkUInt16))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x ^ y)) == z", checkUInt16);
                        testResult = Fail;
                    }

                    CheckMethodSpan<sbyte> checkSByte = (Span<sbyte> x, Span<sbyte> y, Span<sbyte> z, Span<sbyte> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!sbyteTable.CheckResult(checkSByte))
                    {
                        PrintError(sbyteTable, methodUnderTestName, "(sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x ^ y)) == z", checkSByte);
                        testResult = Fail;
                    }

                    CheckMethodSpan<byte> checkByte = (Span<byte> x, Span<byte> y, Span<byte> z, Span<byte> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if (x[i] != z[i])
                                result = false;
                        }
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

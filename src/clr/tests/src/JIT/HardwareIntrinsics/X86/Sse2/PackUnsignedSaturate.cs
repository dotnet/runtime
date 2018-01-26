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
        const int Pass = 100;
        const int Fail = 0;

        internal static unsafe int Main(string[] args)
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.PackUnsignedSaturate);

            if (Sse2.IsSupported)
            {
                using (var shortTable = TestTableSse2<short, byte>.Create(testsCount, 0.5))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>) value = shortTable[i];
                        Vector128<byte> result = Sse2.PackUnsignedSaturate(value.Item1, value.Item2);
                        shortTable.SetOutArrayU(result);
                    }

                    CheckMethodSixteen<short, byte> checkInt16 =
                        (ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> x,
                        ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> y,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> z1,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> z2,
                        ref byte a1, ref byte a2, ref byte a3, ref byte a4, ref byte a5, ref byte a6, ref byte a7, ref byte a8,
                        ref byte a9, ref byte a10, ref byte a11, ref byte a12, ref byte a13, ref byte a14, ref byte a15, ref byte a16) =>
                    {
                        a1 = ToByteSaturate(x.Item1);
                        a2 = ToByteSaturate(x.Item2);
                        a3 = ToByteSaturate(x.Item3);
                        a4 = ToByteSaturate(x.Item4);
                        a5 = ToByteSaturate(x.Item5);
                        a6 = ToByteSaturate(x.Item6);
                        a7 = ToByteSaturate(x.Item7);
                        a8 = ToByteSaturate(x.Item8);
                        a9 = ToByteSaturate(y.Item1);
                        a10 = ToByteSaturate(y.Item2);
                        a11 = ToByteSaturate(y.Item3);
                        a12 = ToByteSaturate(y.Item4);
                        a13 = ToByteSaturate(y.Item5);
                        a14 = ToByteSaturate(y.Item6);
                        a15 = ToByteSaturate(y.Item7);
                        a16 = ToByteSaturate(y.Item8);

                        return a1 == z1.Item1 && a2 == z1.Item2 && a3 == z1.Item3 && a4 == z1.Item4 &&
                            a5 == z1.Item5 && a6 == z1.Item6 && a7 == z1.Item7 && a8 == z1.Item8 &&
                            a9 == z2.Item1 && a10 == z2.Item2 && a11 == z2.Item3 && a12 == z2.Item4 &&
                            a13 == z2.Item5 && a14 == z2.Item6 && a15 == z2.Item7 && a16 == z2.Item8;
                    };

                    if (!shortTable.CheckPackSaturate(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "CheckPackSaturate", checkInt16);
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

        private static byte ToByteSaturate(short value)
        {
            return (byte)(value > byte.MaxValue ? byte.MaxValue : value < 0 ? 0 : value);
        }
    }
}

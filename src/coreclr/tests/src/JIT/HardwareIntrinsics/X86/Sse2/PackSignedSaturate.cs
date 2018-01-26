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
            string methodUnderTestName = nameof(Sse2.PackSignedSaturate);

            if (Sse2.IsSupported)
            {
                using (var intTable = TestTableSse2<int, short>.Create(testsCount, 0.5))
                using (var shortTable = TestTableSse2<short, sbyte>.Create(testsCount, 0.5))
                {
                    intTable.Initialize(InitMode.NumberFirstVectors);
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>) value = intTable[i];
                        Vector128<short> result = Sse2.PackSignedSaturate(value.Item1, value.Item2);
                        intTable.SetOutArrayU(result, i);
                    }

                    shortTable.Initialize(InitMode.NumberFirstVectors);
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>) value = shortTable[i];
                        Vector128<sbyte> result = Sse2.PackSignedSaturate(value.Item1, value.Item2);
                        shortTable.SetOutArrayU(result);
                    }

                    CheckMethodSix<int, short> checkInt32 =
                        (ValueTuple<int, int, int, int> x, ValueTuple<int, int, int, int> y,
                        ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> z,
                        ref short a1, ref short a2, ref short a3, ref short a4, ref short a5, ref short a6, ref short a7, ref short a8) =>
                        {
                            a1 = ToInt16Saturate(x.Item1);
                            a2 = ToInt16Saturate(x.Item2);
                            a3 = ToInt16Saturate(x.Item3);
                            a4 = ToInt16Saturate(x.Item4);
                            a5 = ToInt16Saturate(y.Item1);
                            a6 = ToInt16Saturate(y.Item2);
                            a7 = ToInt16Saturate(y.Item3);
                            a8 = ToInt16Saturate(y.Item4);
                            return a1 == z.Item1 && a2 == z.Item2 && a3 == z.Item3 && a4 == z.Item4 &&
                                a5 == z.Item5 && a6 == z.Item6 && a7 == z.Item7 && a8 == z.Item8;
                        };

                    if (!intTable.CheckPackSaturate(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "CheckPackSaturate", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethodSixteen<short, sbyte> checkInt16 =
                        (ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> x,
                         ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> y,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> z1,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> z2,
                        ref sbyte a1, ref sbyte a2, ref sbyte a3, ref sbyte a4, ref sbyte a5, ref sbyte a6, ref sbyte a7, ref sbyte a8,
                        ref sbyte a9, ref sbyte a10, ref sbyte a11, ref sbyte a12, ref sbyte a13, ref sbyte a14, ref sbyte a15, ref sbyte a16) =>
                        {
                            a1 = ToSByteSaturate(x.Item1);
                            a2 = ToSByteSaturate(x.Item2);
                            a3 = ToSByteSaturate(x.Item3);
                            a4 = ToSByteSaturate(x.Item4);
                            a5 = ToSByteSaturate(x.Item5);
                            a6 = ToSByteSaturate(x.Item6);
                            a7 = ToSByteSaturate(x.Item7);
                            a8 = ToSByteSaturate(x.Item8);
                            a9 = ToSByteSaturate(y.Item1);
                            a10 = ToSByteSaturate(y.Item2);
                            a11 = ToSByteSaturate(y.Item3);
                            a12 = ToSByteSaturate(y.Item4);
                            a13 = ToSByteSaturate(y.Item5);
                            a14 = ToSByteSaturate(y.Item6);
                            a15 = ToSByteSaturate(y.Item7);
                            a16 = ToSByteSaturate(y.Item8);

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

        private static short ToInt16Saturate(int value)
        {
            return (short)(value > short.MaxValue ? short.MaxValue : value < short.MinValue ? short.MinValue : value);
        }

        private static sbyte ToSByteSaturate(short value)
        {
            return (sbyte)(value > sbyte.MaxValue ? sbyte.MaxValue : value < sbyte.MinValue ? sbyte.MinValue : value);
        }
    }
}

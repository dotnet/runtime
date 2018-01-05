// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Reflection;
using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static int Main(string[] args)
        {
            ulong s1l = 0, s2l = 0, resl;
            int testResult = Pass;

            if (!Sse42.IsSupported || !Environment.Is64BitProcess)
            {
                try
                {
                    resl = Sse42.Crc32(s1l, s2l);
                    Console.WriteLine("Intrinsic Sse42.Crc32 is called on non-supported hardware.");
                    Console.WriteLine("Sse42.IsSupported " + Sse42.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (PlatformNotSupportedException)
                {
                }

                try
                {
                    resl = Convert.ToUInt64(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { s1l.GetType(), s2l.GetType() }).Invoke(null, new object[] { s1l, s2l }));
                    Console.WriteLine("Intrinsic Sse42.Crc32 is called via reflection on non-supported hardware.");
                    Console.WriteLine("Sse42.IsSupported " + Sse42.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
                {
                }
            }


            if (Sse42.IsSupported)
            {
                if (Environment.Is64BitProcess)
                {
                    for (int i = 0; i < longCrcTable.Length; i++)
                    {
                        s1l = longCrcTable[i].s1;
                        s2l = longCrcTable[i].s2;

                        resl = Sse42.Crc32(s1l, s2l);
                        if (resl != longCrcTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x}, 0x{2,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                                i, s1l, s2l, longCrcTable[i].res, resl);
                            testResult = Fail;
                        }

                        resl = Convert.ToUInt64(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { s1l.GetType(), s2l.GetType() }).Invoke(null, new object[] { s1l, s2l }));
                        if (resl != longCrcTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x}, 0x{2,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                                i, s1l, s2l, longCrcTable[i].res, resl);
                            testResult = Fail;
                        }
                    }
                }

                uint s1i, s2i, resi;
                for (int i = 0; i < intCrcTable.Length; i++)
                {
                    s1i = intCrcTable[i].s1;
                    s2i = intCrcTable[i].s2;

                    resi = Sse42.Crc32(s1i, s2i);
                    if (resi != intCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x}",
                            i, s1i, s2i, intCrcTable[i].res, resi);
                        testResult = Fail;
                    }

                    resi = Convert.ToUInt32(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { s1i.GetType(), s2i.GetType() }).Invoke(null, new object[] { s1i, s2i }));
                    if (resi != intCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x} - Reflection",
                            i, s1i, s2i, intCrcTable[i].res, resi);
                        testResult = Fail;
                    }
                }

                ushort s2s;
                for (int i = 0; i < shortCrcTable.Length; i++)
                {
                    s1i = shortCrcTable[i].s1;
                    s2s = shortCrcTable[i].s2;

                    resi = Sse42.Crc32(s1i, s2s);
                    if (resi != shortCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x}",
                            i, s1i, s2s, shortCrcTable[i].res, resi);
                        testResult = Fail;
                    }

                    resi = Convert.ToUInt32(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { s1i.GetType(), s2s.GetType() }).Invoke(null, new object[] { s1i, s2s }));
                    if (resi != shortCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x} - Reflection",
                            i, s1i, s2s, shortCrcTable[i].res, resi);
                        testResult = Fail;
                    }
                }

                byte s2b;
                for (int i = 0; i < byteCrcTable.Length; i++)
                {
                    s1i = byteCrcTable[i].s1;
                    s2b = byteCrcTable[i].s2;

                    resi = Sse42.Crc32(s1i, s2b);
                    if (resi != byteCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x}",
                            i, s1i, s2b, byteCrcTable[i].res, resi);
                        testResult = Fail;
                    }

                    resi = Convert.ToUInt32(typeof(Sse42).GetMethod(nameof(Sse42.Crc32), new Type[] { s1i.GetType(), s2b.GetType() }).Invoke(null, new object[] { s1i, s2b }));
                    if (resi != byteCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,8:x}, 0x{2,8:x} Expected: 0x{3,8:x} actual: 0x{4,8:x}",
                            i, s1i, s2b, byteCrcTable[i].res, resi);
                        testResult = Fail;
                    }
                }
            }

            return testResult;
        }

        public struct Crc<T, U> where T : struct where U : struct
        {
            public T s1;
            public U s2;
            public T res;
            public Crc(T a, U b, T c)
            {
                this.s1 = a;
                this.s2 = b;
                this.res = c;
            }
        }

        public static Crc<ulong, ulong>[] longCrcTable = {
            new Crc<ulong, ulong>(0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL),
            new Crc<ulong, ulong>(0x0000000000000000UL, 0x0000000000000001UL, 0x00000000493c7d27UL),
            new Crc<ulong, ulong>(0x0000000000000001UL, 0x0000000000000000UL, 0x00000000493c7d27UL),
            new Crc<ulong, ulong>(0x0000000000000001UL, 0x0000000000000001UL, 0x0000000000000000UL),
            new Crc<ulong, ulong>(0x0000000000000000UL, 0xffffffffffffffffUL, 0x00000000c44ff94dUL),
            new Crc<ulong, ulong>(0xffffffffffffffffUL, 0x0000000000000000UL, 0x0000000073d74d75UL),
            new Crc<ulong, ulong>(0xffffffffffffffffUL, 0xffffffffffffffffUL, 0x00000000b798b438UL),
            new Crc<ulong, ulong>(0x0000000000000001UL, 0xffffffffffffffffUL, 0x000000008d73846aUL),
            new Crc<ulong, ulong>(0xffffffffffffffffUL, 0x0000000000000001UL, 0x000000003aeb3052UL),
            new Crc<ulong, ulong>(0xfffffffffffe1f0dUL, 0x00000000f5c1ddb3UL, 0x000000000504c066UL),
            new Crc<ulong, ulong>(0x0000000000000005UL, 0x000000bce1263cffUL, 0x000000004ab954daUL),
            new Crc<ulong, ulong>(0x0000000000000463UL, 0xffffffffff840d0dUL, 0x00000000797d59f3UL),
            new Crc<ulong, ulong>(0x00000000000f423fUL, 0x000000000001e0f3UL, 0x000000005c6b8093UL)
        };

        public static Crc<uint, uint>[] intCrcTable = {
            new Crc<uint, uint>(0x00000000U, 0x00000000U, 0x00000000U),
            new Crc<uint, uint>(0x00000000U, 0x00000001U, 0xdd45aab8U),
            new Crc<uint, uint>(0x00000001U, 0x00000000U, 0xdd45aab8U),
            new Crc<uint, uint>(0x00000001U, 0x00000001U, 0x00000000U),
            new Crc<uint, uint>(0x00000000U, 0xffffffffU, 0xb798b438U),
            new Crc<uint, uint>(0xffffffffU, 0x00000000U, 0xb798b438U),
            new Crc<uint, uint>(0xffffffffU, 0xffffffffU, 0x00000000U),
            new Crc<uint, uint>(0x00000001U, 0xffffffffU, 0x6add1e80U),
            new Crc<uint, uint>(0xffffffffU, 0x00000001U, 0x6add1e80U),
            new Crc<uint, uint>(0xfffe1f0dU, 0xf5c1ddb3U, 0x911888ccU),
            new Crc<uint, uint>(0x00000005U, 0xe1263cffU, 0xbe12f661U),
            new Crc<uint, uint>(0x00000463U, 0xff840d0dU, 0xcba65e37U),
            new Crc<uint, uint>(0x000f423fU, 0x0001e0f3U, 0xa5b7881dU)
        };

        public static Crc<uint, ushort>[] shortCrcTable = {
            new Crc<uint, ushort>(0x00000000U, 0x0000, 0x00000000U),
            new Crc<uint, ushort>(0x00000000U, 0x0001, 0x13a29877U),
            new Crc<uint, ushort>(0x00000001U, 0x0000, 0x13a29877U),
            new Crc<uint, ushort>(0x00000001U, 0x0001, 0x00000000U),
            new Crc<uint, ushort>(0x00000000U, 0xffff, 0xe9e77d2U),
            new Crc<uint, ushort>(0xffffffffU, 0x0000, 0xe9e882dU),
            new Crc<uint, ushort>(0xffffffffU, 0xffff, 0x0000ffffU),
            new Crc<uint, ushort>(0x00000001U, 0xffff, 0x1d3cefa5U),
            new Crc<uint, ushort>(0xffffffffU, 0x0001, 0x1d3c105aU),
            new Crc<uint, ushort>(0xfffe1f0dU, 0xddb3, 0x6de0d33dU),
            new Crc<uint, ushort>(0x00000005U, 0x3cff, 0x836b5b49U),
            new Crc<uint, ushort>(0x00000463U, 0x0d0d, 0x0cf56c40U),
            new Crc<uint, ushort>(0x000f423fU, 0xe0f3, 0x943a5bc7U)
        };

        public static Crc<uint, byte>[] byteCrcTable = {
            new Crc<uint, byte>(0x00000000U, 0x00, 0x00000000U),
            new Crc<uint, byte>(0x00000000U, 0x01, 0xf26b8303U),
            new Crc<uint, byte>(0x00000001U, 0x00, 0xf26b8303U),
            new Crc<uint, byte>(0x00000001U, 0x01, 0x00000000U),
            new Crc<uint, byte>(0x00000000U, 0xff, 0xad7d5351U),
            new Crc<uint, byte>(0xffffffffU, 0x00, 0xad82acaeU),
            new Crc<uint, byte>(0xffffffffU, 0xff, 0x00ffffffU),
            new Crc<uint, byte>(0x00000001U, 0xff, 0x5f16d052U),
            new Crc<uint, byte>(0xffffffffU, 0x01, 0x5fe92fadU),
            new Crc<uint, byte>(0xfffe1f0dU, 0xb3, 0x1e9233f1U),
            new Crc<uint, byte>(0x00000005U, 0xff, 0x988c474dU),
            new Crc<uint, byte>(0x00000463U, 0x0d, 0xcdbe2c41U),
            new Crc<uint, byte>(0x000f423fU, 0xf3, 0x8ecee656U)
        };

    }
}

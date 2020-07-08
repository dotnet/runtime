// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

            if (Sse42.X64.IsSupported)
            {
                for (int i = 0; i < longCrcTable.Length; i++)
                {
                    s1l = longCrcTable[i].s1;
                    s2l = longCrcTable[i].s2;

                    resl = Sse42.X64.Crc32(s1l, s2l);
                    if (resl != longCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x}, 0x{2,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                            i, s1l, s2l, longCrcTable[i].res, resl);
                        testResult = Fail;
                    }

                    resl = Convert.ToUInt64(typeof(Sse42.X64).GetMethod(nameof(Sse42.X64.Crc32), new Type[] { s1l.GetType(), s2l.GetType() }).Invoke(null, new object[] { s1l, s2l }));
                    if (resl != longCrcTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x}, 0x{2,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                            i, s1l, s2l, longCrcTable[i].res, resl);
                        testResult = Fail;
                    }
                }
            }
            else
            {
                try
                {
                    resl = Sse42.X64.Crc32(s1l, s2l);
                    Console.WriteLine("Intrinsic Sse42.X64.Crc32 is called on non-supported hardware.");
                    Console.WriteLine("Sse42.IsSupported " + Sse42.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (PlatformNotSupportedException)
                {
                }

                try
                {
                    resl = Convert.ToUInt64(typeof(Sse42.X64).GetMethod(nameof(Sse42.X64.Crc32), new Type[] { s1l.GetType(), s2l.GetType() }).Invoke(null, new object[] { s1l, s2l }));
                    Console.WriteLine("Intrinsic Sse42.X64.Crc32 is called via reflection on non-supported hardware.");
                    Console.WriteLine("Sse42.IsSupported " + Sse42.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
                {
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

    }
}

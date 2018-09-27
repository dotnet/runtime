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
            ulong sl = 0;
            ulong resl;
            int testResult = Pass;

            if (!Popcnt.IsSupported || !Environment.Is64BitProcess)
            {
                try
                {
                    resl = Popcnt.PopCount(sl);
                    Console.WriteLine("Intrinsic Popcnt.PopCount is called on non-supported hardware");
                    Console.WriteLine("Popcnt.IsSupported " + Popcnt.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (PlatformNotSupportedException)
                {
                }

                try
                {
                    resl = Convert.ToUInt64(typeof(Popcnt).GetMethod(nameof(Popcnt.PopCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                    Console.WriteLine("Intrinsic Popcnt.PopCount is called via reflection on non-supported hardware");
                    Console.WriteLine("Popcnt.IsSupported " + Popcnt.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
                {
                }
            }


            if (Popcnt.IsSupported)
            {
                if (Environment.Is64BitProcess)
                {
                    for (int i = 0; i < longPopcntTable.Length; i++)
                    {
                        sl = longPopcntTable[i].s;

                        resl = Popcnt.PopCount(sl);
                        if (resl != longPopcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                                i, sl, longPopcntTable[i].res, resl);
                            testResult = Fail;
                        }

                        resl = Convert.ToUInt64(typeof(Popcnt).GetMethod(nameof(Popcnt.PopCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                        if (resl != longPopcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                                i, sl, longPopcntTable[i].res, resl);
                            testResult = Fail;
                        }
                    }
                }

                uint si;
                uint resi;
                for (int i = 0; i < intPopcntTable.Length; i++)
                {
                    si = intPopcntTable[i].s;

                    resi = Popcnt.PopCount(si);
                    if (resi != intPopcntTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                            i, si, intPopcntTable[i].res, resi);
                        testResult = Fail;
                    }

                    resi = Convert.ToUInt32(typeof(Popcnt).GetMethod(nameof(Popcnt.PopCount), new Type[] { si.GetType() }).Invoke(null, new object[] { si }));
                    if (resi != intPopcntTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                            i, si, intPopcntTable[i].res, resi);
                        testResult = Fail;
                    }
                }
            }

            return testResult;
        }

        public struct POPCNT<T, U> where T : struct where U : struct
        {
            public T s;
            public U res;
            public POPCNT(T a, U r)
            {
                this.s = a;
                this.res = r;
            }
        }

        public static POPCNT<ulong,ulong>[] longPopcntTable = {
            new POPCNT<ulong,ulong>(0x0000000000000000UL, 0UL),
            new POPCNT<ulong,ulong>(0x0000000000000001UL, 1UL),
            new POPCNT<ulong,ulong>(0xffffffffffffffffUL, 64UL),
            new POPCNT<ulong,ulong>(0x8000000000000000UL, 1UL),
            new POPCNT<ulong,ulong>(0x00050000000f423fUL, 14UL)
        };

        public static POPCNT<uint,uint>[] intPopcntTable = {
            new POPCNT<uint,uint>(0x00000000U, 0U),
            new POPCNT<uint,uint>(0x00000001U, 1U),
            new POPCNT<uint,uint>(0xffffffffU, 32U),
            new POPCNT<uint,uint>(0x80000000U, 1U),
            new POPCNT<uint,uint>(0x0005423fU, 10U)
        };
    }
}

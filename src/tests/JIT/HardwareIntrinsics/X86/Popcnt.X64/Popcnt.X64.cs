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

            if (Popcnt.X64.IsSupported)
            {
                    for (int i = 0; i < longPopcntTable.Length; i++)
                    {
                        sl = longPopcntTable[i].s;

                        resl = Popcnt.X64.PopCount(sl);
                        if (resl != longPopcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                                i, sl, longPopcntTable[i].res, resl);
                            testResult = Fail;
                        }

                        resl = Convert.ToUInt64(typeof(Popcnt.X64).GetMethod(nameof(Popcnt.X64.PopCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                        if (resl != longPopcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                                i, sl, longPopcntTable[i].res, resl);
                            testResult = Fail;
                        }
                    }

            }
            else
            {
                try
                {
                    resl = Popcnt.X64.PopCount(sl);
                    Console.WriteLine("Intrinsic Popcnt.X64.PopCount is called on non-supported hardware");
                    Console.WriteLine("Popcnt.X64.IsSupported " + Popcnt.X64.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (PlatformNotSupportedException)
                {
                }

                try
                {
                    resl = Convert.ToUInt64(typeof(Popcnt.X64).GetMethod(nameof(Popcnt.X64.PopCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                    Console.WriteLine("Intrinsic Popcnt.X64.PopCount is called via reflection on non-supported hardware");
                    Console.WriteLine("Popcnt.X64.IsSupported " + Popcnt.X64.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
                {
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
    }
}

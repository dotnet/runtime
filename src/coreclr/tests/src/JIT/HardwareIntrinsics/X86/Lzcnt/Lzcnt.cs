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
            ulong sl = 0, resl;
            int testResult = Pass;

            if (!Lzcnt.IsSupported || !Environment.Is64BitProcess)
            {
                try
                {
                    resl = Lzcnt.LeadingZeroCount(sl);
                    Console.WriteLine("Intrinsic Lzcnt.LeadingZeroCount is called on non-supported hardware.");
                    Console.WriteLine("Lzcnt.IsSupported " + Lzcnt.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (PlatformNotSupportedException)
                {
                }

                try
                {
                    resl = Convert.ToUInt64(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                    Console.WriteLine("Intrinsic Lzcnt.LeadingZeroCount is called via reflection on non-supported hardware.");
                    Console.WriteLine("Lzcnt.IsSupported " + Lzcnt.IsSupported);
                    Console.WriteLine("Environment.Is64BitProcess " + Environment.Is64BitProcess);
                    testResult = Fail;
                }
                catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
                {
                }
            }


            if (Lzcnt.IsSupported)
            {
                if (Environment.Is64BitProcess)
                {
                    for (int i = 0; i < longLzcntTable.Length; i++)
                    {
                        sl = longLzcntTable[i].s;

                        resl = Lzcnt.LeadingZeroCount(sl);
                        if (resl != longLzcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                                i, sl, longLzcntTable[i].res, resl);
                            testResult = Fail;
                        }
                        
                        resl = Convert.ToUInt64(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { sl.GetType() }).Invoke(null, new object[] { sl }));
                        if (resl != longLzcntTable[i].res)
                        {
                            Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                                i, sl, longLzcntTable[i].res, resl);
                            testResult = Fail;
                        }
                    }
                }

                uint si, resi;
                for (int i = 0; i < intLzcntTable.Length; i++)
                {
                    si = intLzcntTable[i].s;

                    resi = Lzcnt.LeadingZeroCount(si);
                    if (resi != intLzcntTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x}",
                            i, si, intLzcntTable[i].res, resi);
                        testResult = Fail;
                    }

                    resl = Convert.ToUInt64(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { si.GetType() }).Invoke(null, new object[] { si }));
                    if (resi != intLzcntTable[i].res)
                    {
                        Console.WriteLine("{0}: Inputs: 0x{1,16:x} Expected: 0x{3,16:x} actual: 0x{4,16:x} - Reflection",
                            i, si, intLzcntTable[i].res, resi);
                        testResult = Fail;
                    }
                }
            }

            return testResult;
        }

        public struct LZCNT<T> where T : struct
        {
            public T s;
            public T res;
            public LZCNT(T a, T r)
            {
                this.s = a;
                this.res = r;
            }
        }

        public static LZCNT<ulong>[] longLzcntTable = {
            new LZCNT<ulong>(0x0000000000000000UL, 64),
            new LZCNT<ulong>(0x0000000000000001UL, 63),
            new LZCNT<ulong>(0xffffffffffffffffUL, 0),
            new LZCNT<ulong>(0xf000000000000000UL, 0),
            new LZCNT<ulong>(0x00050000000f423fUL, 13)
        };

        public static LZCNT<uint>[] intLzcntTable = {
            new LZCNT<uint>(0x00000000U, 32),
            new LZCNT<uint>(0x00000001U, 31),
            new LZCNT<uint>(0xffffffffU, 0),
            new LZCNT<uint>(0xf0000000U, 0),
            new LZCNT<uint>(0x0005423fU, 13)
        };
    }
}

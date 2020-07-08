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
            int testResult = Pass;

            if (Popcnt.IsSupported)
            {

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

        public static POPCNT<uint,uint>[] intPopcntTable = {
            new POPCNT<uint,uint>(0x00000000U, 0U),
            new POPCNT<uint,uint>(0x00000001U, 1U),
            new POPCNT<uint,uint>(0xffffffffU, 32U),
            new POPCNT<uint,uint>(0x80000000U, 1U),
            new POPCNT<uint,uint>(0x0005423fU, 10U)
        };
    }
}

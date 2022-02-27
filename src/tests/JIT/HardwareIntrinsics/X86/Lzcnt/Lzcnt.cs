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

            if (Lzcnt.IsSupported)
            {

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

                    resi = Convert.ToUInt32(typeof(Lzcnt).GetMethod(nameof(Lzcnt.LeadingZeroCount), new Type[] { si.GetType() }).Invoke(null, new object[] { si }));
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

        public static LZCNT<uint>[] intLzcntTable = {
            new LZCNT<uint>(0x00000000U, 32),
            new LZCNT<uint>(0x00000001U, 31),
            new LZCNT<uint>(0xffffffffU, 0),
            new LZCNT<uint>(0xf0000000U, 0),
            new LZCNT<uint>(0x0005423fU, 13)
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
/*
   IndexOutOfRange Exception When Using UShort or Short as an Input Array Type
*/

using System;
using System.Runtime.CompilerServices;

class small_repro
{
    void bug(int num)
    {
        short[] src = GetArray();
        // The induction variable is i4, but the array indexes are i8
        // on x64.  OSR gets confused by the different sym keys for the
        // equivsyms and creates different symbols for the rewritten
        // IVs and ends up with a def with no use and a use with no def!
        for (int i = 0; i < num; i += src.Length)
        {
            this.dst[i] = src[0];
            this.dst[i + 1] = src[1];
            this.dst[i + 2] = src[2];
        }
    }

    short[] dst = new short[12];

    [MethodImpl(MethodImplOptions.NoInlining)]
    short[] GetArray()
    {
        return new short[] { 0x100, 0x101, 0x102 };
    }

    static int Main()
    {
        small_repro s = new small_repro();
        try
        {
            s.bug(12);
            Console.WriteLine("Pass");
            return 100;
        }
        catch
        {
            Console.WriteLine("Fail");
            return 110;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal class Test
{
    [DllImport("kernel32.dll")]
    private extern static IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    private extern static IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, int flAllocationType, int flProtect);


    private static void EatAddressSpace()
    {
        IntPtr clrDllHandle = GetModuleHandle("clr.dll");
        long clrDll = (long)clrDllHandle;

        for (long i = clrDll - 0x300000000; i < clrDll + 0x300000000; i += 0x10000)
        {
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A1()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A2()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A3()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A4()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A5()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A6()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A7()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A8()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A9()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void A10()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B1()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B2()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B3()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B4()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B5()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B6()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B7()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B8()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B9()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void B10()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C1()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C2()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C3()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C4()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C5()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C6()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C7()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C8()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C9()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void C10()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D1()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D2()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D3()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D4()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D5()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D6()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D7()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D8()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D9()
    {
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void D10()
    {
    }



    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void Dummy()
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void GenericRecursion<T, U>(int level)
    {
        if (level == 0) return;
        level--;

        GenericRecursion<KeyValuePair<T, U>, U>(level);
        GenericRecursion<KeyValuePair<U, T>, U>(level);
        GenericRecursion<T, KeyValuePair<T, U>>(level);
        GenericRecursion<T, KeyValuePair<U, T>>(level);

        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
        Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy(); Dummy();
    }

    private static int Main()
    {
        try
        {
            Console.WriteLine("Eating address space");
            EatAddressSpace();

            Console.WriteLine("Eating code heap");
            GenericRecursion<int, uint>(5);

            A1(); A2(); A3(); A4(); A5(); A6(); A7(); A8(); A9(); A10();
            B1(); B2(); B3(); B4(); B5(); B6(); B7(); B8(); B9(); B10();
            C1(); C2(); C3(); C4(); C5(); C6(); C7(); C8(); C9(); C10();
            D1(); D2(); D3(); D4(); D5(); D6(); D7(); D8(); D9(); D10();

            A1(); A2(); A3(); A4(); A5(); A6(); A7(); A8(); A9(); A10();
            B1(); B2(); B3(); B4(); B5(); B6(); B7(); B8(); B9(); B10();
            C1(); C2(); C3(); C4(); C5(); C6(); C7(); C8(); C9(); C10();
            D1(); D2(); D3(); D4(); D5(); D6(); D7(); D8(); D9(); D10();

            Console.WriteLine("Done");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
    }
}

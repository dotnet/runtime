// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe void Localloc()
    {
        byte* a = stackalloc byte[5];
        byte i;
        for (i=1; i < 5; ++i)
        {
           a[i] = i;
        }

        for (i=1; i < 5; ++i)
        {
           Console.WriteLine(a[i]);
        }        
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static unsafe void Localloc(byte n)
    {
        byte* a = stackalloc byte[n];
        *a = 0;

        byte i;
        for (i=1; i < n; ++i)
        {
           a[i] = i;
        }

        for (i=1; i < n; ++i)
        {
           Console.WriteLine(a[i]);
        }
    }


    public static int Main()
    {
        int ret = Pass;
        Localloc();
        Localloc(25);

        bool flag = false;
        try { Localloc(0); } catch (Exception) { flag = true; } finally { if(!flag) ret = Fail; }
        return ret;        
    }
}

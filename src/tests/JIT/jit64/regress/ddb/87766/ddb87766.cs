// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class VInline
{
    private int _fi1;
    private int _fi2;

    public VInline(int ival)
    {
        _fi1 = ival;
        _fi2 = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GetI1(ref int i)
    {
        i = _fi1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Accumulate(int a)
    {
        int i = 0;
        GetI1(ref i); //here's the ldloca, passing the address of i as the arg
        i = i / _fi2;    //fi2 == 0 so this should always cause an exception
        return i;
    }
}
public class VIMain
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;
        VInline vi = new VInline(1);
        int ival = 2;
        try
        {
            ival = vi.Accumulate(ival);  //this call should throw a divide by zero exception
        }
        catch (DivideByZeroException e)
        {
            Console.WriteLine("exception stack trace: " + e.StackTrace.ToString());  //display the stack trace
            if (e.StackTrace.ToString().Contains("Accumulate"))
            {
                Console.WriteLine("Fail, method Accumulate NOT inlined.");
                ret = 666;
            }
            else
            {
                Console.WriteLine("Pass, method Accumulate inlined.");
            }
        }

        return ret;
    }
}


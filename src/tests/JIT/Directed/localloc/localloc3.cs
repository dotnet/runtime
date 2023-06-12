// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public unsafe class test1
{
    static public int f(int i)
    {
        byte* p = stackalloc byte[i];
        p[0] = 4;
        return p[0];
    }

    internal void f0()
    {
        while (true)
        {
            char* p = stackalloc char[10];
        }
    }

    internal void f1()
    {
        char* p = stackalloc char[1000000];
    }

    [Fact]
    unsafe public static int TestEntryPoint()
    {
        bool pass = true;

        //testcase 1:
        try
        {
            char* p = stackalloc char[0];
        }
        catch (Exception e)
        {
            Console.WriteLine("testcase 1: should not be here");
            Console.WriteLine(e.Message);
            pass = false;
        }

        if (!pass)
            goto output;

        //testcase 2:
        try
        {
            char* p = stackalloc char[100];
        }
        catch (Exception e)
        {
            Console.WriteLine("testcase 1: should not be here");
            Console.WriteLine(e.Message);
            pass = false;
        }

        if (!pass)
            goto output;

        //testcase 3:
        try
        {
            Console.Write("stackalloc(10)...");
            f(10);
            Console.WriteLine("done");
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception occurred: {0}", e.Message);
            pass = false;
        }

    output:
        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}

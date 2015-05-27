// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


internal class baseclass
{
    public virtual int virtualmethod(int a, int b)
    {
        throw new System.Exception("test failed");
    }
}

internal class Test : baseclass
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int testmethod1(int a, int b)
    {
        return a / b;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int testmethod2(int a, int b)
    {
        return a / b;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public override int virtualmethod(int a, int b)
    {
        return a / b;
    }
}



internal class Program
{
    private volatile static int s_a = 5;
    private volatile static int s_b = 0;

    private static int Main(string[] args)
    {
        try
        {
            Test.testmethod1(s_a, s_b);
        }
        catch (DivideByZeroException ex)
        {
            if (!ex.StackTrace.ToString().Contains("testmethod1"))
            {
                Console.WriteLine("testmethod1 failed");
                return 1;
            }
            Console.WriteLine("passed");
        }
        Test mytest = new Test();
        try
        {
            mytest.testmethod2(s_a, s_b);
        }
        catch (DivideByZeroException ex)
        {
            if (!ex.StackTrace.ToString().Contains("testmethod2"))
            {
                Console.WriteLine("testmethod2 failed");
                return 1;
            }
            Console.WriteLine("passed");
        }
        try
        {
            mytest.virtualmethod(s_a, s_b);
        }
        catch (DivideByZeroException ex)
        {
            if (!ex.StackTrace.ToString().Contains("virtualmethod"))
            {
                Console.WriteLine("virtualmethod failed");
                return 1;
            }
            Console.WriteLine("passed");
        }


        return 100;
    }
}

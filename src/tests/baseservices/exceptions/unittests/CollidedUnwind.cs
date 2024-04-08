// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class TestSet
{
    static void CountResults(int testReturnValue, ref int nSuccesses, ref int nFailures)
    {
        if (100 == testReturnValue)
        {
            nSuccesses++;
        }
        else
        {
            nFailures++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int nSuccesses = 0;
        int nFailures = 0;

        CountResults(new CollidedUnwindTest().Run(),            ref nSuccesses, ref nFailures);
        
        if (0 == nFailures)
        {
            Console.WriteLine("OVERALL PASS: " + nSuccesses + " tests");
            return 100;
        }
        else
        {
            Console.WriteLine("OVERALL FAIL: " + nFailures + " tests failed");
            return 999;
        }
    }
}

public class CollidedUnwindTest
{
    class ExType1 : Exception
    {
    }
    
    class ExType2 : Exception
    {
    }

    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("CollidedUnwindTest", "0123456789ABCDE");
        
        try
        {
            _trace.Write("0");
            Foo();
        }
        catch (ExType2 e)
        {
            Console.WriteLine(e);
            _trace.Write("E");
        }

        return _trace.Match();
    }

    void Foo()
    {
        try
        {
            _trace.Write("1");
            FnAAA();
        }
        catch (ExType1 e)
        {
            Console.WriteLine(e);
            _trace.Write(" BAD ");
        }
    }

    void FnAAA()
    {
        try
        {
            _trace.Write("2");
            FnBBB();   
        }
        finally
        {
            _trace.Write("D");
        }
    }

    void FnBBB()
    {
        try
        {
            _trace.Write("3");
            Bar();   
        }
        finally
        {
            _trace.Write("C");
        }
    }

    void Bar()
    {
        try
        {
            _trace.Write("4");
            FnCCC();
        }
        finally
        {
            _trace.Write("B");
            throw new ExType2();
        }
    }

    void FnCCC()
    {
        try
        {
            _trace.Write("5");
            FnDDD();   
        }
        finally
        {
            _trace.Write("A");
        }
    }

    void FnDDD()
    {
        try
        {
            _trace.Write("6");
            Fubar();   
        }
        finally
        {
            _trace.Write("9");
        }
    }

    void Fubar()
    {
        try
        {
            _trace.Write("7");
            throw new ExType1();
        }
        finally
        {
            _trace.Write("8");
        }
    }
}


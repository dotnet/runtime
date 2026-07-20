// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using TestLibrary;

public class Dummy
{
    ~Dummy()
    {
        Console.WriteLine("In Finalize() of Dummy");
    }
}


public class CreateObj
{
    private Dummy _obj;
    private WeakReference _weak;

    public CreateObj()
    {
        _obj = new Dummy();
        _weak = new WeakReference(_obj);
    }


    public bool RunTest()
    {
        _obj = null;
        GC.Collect();

        try
        {
            GC.GetGeneration(_weak.Target);
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("Expected exception");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception: " + e);
        }

        Console.WriteLine("Expected exception not thrown!");
        return false;
    }


    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        CreateObj temp = new CreateObj();

        if (temp.RunTest())
        {
            Console.WriteLine("Test passed!");
            return 100;
        }

        Console.WriteLine("Test failed!");
        return 1;
    }
}




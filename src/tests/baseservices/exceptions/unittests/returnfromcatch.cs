// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class TestSet
{

    [Fact]
    public static int TestEntryPoint()
    {
        int retCode = (new ReturnFromCatchTest()).Run();
        if (100 == retCode)
        {
            Console.WriteLine("Test Passed");
        }
        else
        {
            Console.WriteLine("Test Failed");
        }
        return retCode;
    }
}

class ReturnFromCatchTest
{    
    public int Run() 
    {
        int henry = 0;

        DoNotInlineMethod(ref henry);

        return henry;
    }

    private void DoNotInlineMethod(ref int bob)
    {
        try
        {
            Console.WriteLine("Inside Try. Setting return code to 50");
            bob = 50;

        }
        catch(Exception)
        {
            Console.WriteLine("Inside Catch. Setting return code to 25");
            Console.WriteLine("Next line returns so Finally in this method should be next and it will set return code to 100;");
            bob = 25;
            return;
        }
        finally
        {
            Console.WriteLine("Finally running. This is expected.");
            bob = 100;
        }
    }
}


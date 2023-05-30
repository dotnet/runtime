// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//



using System;
using System.Runtime.CompilerServices;
using Xunit;

public class child
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;
        int testResult = 0;
        int divResult = 0;
        try
        {
            divResult = div2(0, -1);                  // Should not throw an exception
            if (divResult != 0)
            {
                testResult += 0x1000;                 // Wrong result
            }
            divResult = div2(Int32.MinValue, -1);     // Should throw ArithmeticException
            Console.WriteLine(divResult);
            testResult += 0x10;
        }
        catch (System.ArithmeticException)
        {
            testResult += 1;
        }
        catch (System.Exception)
        {
            Console.WriteLine("Caught other exception for MinInt/-1");
            testResult += 0x100;
        }

        try
        {
            divResult = div2(80442, 654);             // Should not throw an exception
            if (divResult != 123)
            {
                testResult += 0x2000;                 // Wrong result
            }
            divResult = div2(1, 0);                   // Should throw DivideByZeroException
            Console.WriteLine(divResult);
            testResult += 0x20;
        }
        catch (System.DivideByZeroException)
        {
            testResult += 2;
        }
        catch (System.Exception)
        {
            Console.WriteLine("Caught other exception for x/0");
            testResult += 0x200;
        }

        if ((testResult & 1) != 1)
        {
            Console.WriteLine("Did not see Arithmetic exception");
        }
        if ((testResult & 2) != 2)
        {
            Console.WriteLine("Did not see Divide-by-zero exception");
        }

        Console.Write("testResult is 0x");
        Console.WriteLine(testResult.ToString("x"));

        if (testResult == 3)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int div2(int a, int b)
    {

        return a / b;
    }

}


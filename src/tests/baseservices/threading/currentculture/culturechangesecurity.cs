// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Globalization;

public class CultureChangeSecurity
{
    public static int Main(string[] args)
    {
        bool status = true;

        status &= Scenario1();
        status &= Scenario2();

        if (!status)
        {
            Console.WriteLine("Test Failed");
            return 101;
        }
        else
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }
    }

    public static bool Scenario1()
    {
        Console.WriteLine("Scenario1: Ensure user code can access Globalization.CultureInfo.CurrentCulture");

        try
        {
            CultureInfo ci = System.Globalization.CultureInfo.CurrentCulture;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Fail - Got unexpected Exception: " + e);
            return false;
        }
    }

    public static bool Scenario2()
    {
        Console.WriteLine("Scenario2: Ensure user code can access Globalization.CultureInfo.CurrentUICulture");

        try
        {
            CultureInfo ci = System.Globalization.CultureInfo.CurrentUICulture;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Fail - Got unexpected Exception: " + e);
            return false;
        }
    }
}



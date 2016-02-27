// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Regression case for:
//      CoreCLR => Dev10 #629953 - SL4: User Breaking Change: Enum.IsDefined no longer throws an ArgumentException in invalid cases
//      Desktop => Dev10 #766944 - Breaking change follow up: Enum.IsDefined no longer throws an ArgumentException in invalid cases (This bug should be fixed, if 629953 is fixed for Silverlight)

using System;

public enum EnumInt32 : int
{
    One, Two, Three
}

public class Test
{
    public static bool Negative()
    {
        Console.WriteLine();
        Console.WriteLine("Negative case - Enum type is Int32, value type is byte");

        try
        {
            bool b = Enum.IsDefined(typeof(EnumInt32), (byte)1);
            Console.WriteLine("FAIL> Expect to see an ArgumentException: returned ({0})!", b);
            return false;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("PASS> Expected ArgumentException is thrown.");
            return true;
        }
    }

    public static bool Positive()
    {
        Console.WriteLine();
        Console.WriteLine("Positive case - Enum type is Int32, value type is Int32");

        try
        {
            bool b = Enum.IsDefined(typeof(EnumInt32), (int)1);
            Console.WriteLine("PASS> No exception: returned ({0}).", b);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL> Unexpected exception is thrown.");
            Console.WriteLine(e);
            return false;
        }
    }

    public static int Main()
    {
        bool retVal = true;

        retVal &= Negative();
        retVal &= Positive();

        if (retVal)
        {
            Console.WriteLine();
            Console.WriteLine("TEST PASSED.");
            return 100;
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("TEST FAILED!");
            return 110;
        }
    }
}

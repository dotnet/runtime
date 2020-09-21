// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests ReRegisterForFinalize()

using System;

public class Test
{
    public bool RunTest()
    {
        try
        {
            GC.ReRegisterForFinalize(null); // should call Finalize() for obj1 now.
        }
        catch (ArgumentNullException)
        {
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("Unexpected Exception!");
        }

        return false;
    }


    public static int Main()
    {
        Test t = new Test();
        if (t.RunTest())
        {
            Console.WriteLine("Null Test for ReRegisterForFinalize() passed!");
            return 100;
        }

        Console.WriteLine("Null Test for ReRegisterForFinalize() failed!");
        return 1;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests SuppressFinalize()

using System;

public class Test
{
    public bool RunTest()
    {
        try
        {
            GC.SuppressFinalize(null);  // should not call the Finalizer() for obj1
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
            Console.WriteLine("Null test for SuppressFinalize() passed!");
            return 100;
        }

        Console.WriteLine("Null test for SuppressFinalize() failed!");
        return 1;
    }
}

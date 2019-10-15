// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

/// <summary>
/// Regression test case for Dev10 897464 bug: Assemblies that contain global values will work with .NET 2.0  but fail with a BadImageFormat exception on .NET 4.0
/// </summary>
class Program
{
    static Int32 Main()
    {
        Program p = new Program();

        if (p.Run())
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return -1;
        }
    }

    public Boolean Run()
    {
        try
        {
            var v = Test.MyEnum.Zero;
            Console.WriteLine(v);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Got unexpected error: " + ex);
            return false;
        }

        return true;
    }
}

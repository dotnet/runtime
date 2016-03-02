// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Security;

/* Regression case for Dev10 #630880 - SL4: User Breaking Change: Users are not able to run critical class constructors in platform assembles */

public class Dev10_630880
{
    public static int Main()
    {
        int failures = 0;
 
        Console.WriteLine("Getting type of System.AppDomainManager.");
        Type t = Type.GetType("System.AppDomainManager");

        Console.WriteLine("Getting type handle of System.AppDomainManager type.");
        RuntimeTypeHandle h = t.TypeHandle;

        Console.WriteLine("Calling RuntimeHelpers.RunClassConstructor with type handle of System.AppDomainManager type.");
        try
        {
            // In V2, this throws TypeLoadException.
            // In V4, this shouldn't throw any exception
            RuntimeHelpers.RunClassConstructor(h);

            Console.WriteLine("PASS> No exception is thrown.");
        }
        catch (Exception e)
        {
            failures++;
            Console.WriteLine("FAIL> Unexpected {0}!", e.GetType());
            Console.WriteLine("Please revisit Dev10 #630880.");
            Console.WriteLine();
            Console.WriteLine(e);
        }

        Console.WriteLine();
        Console.WriteLine("TEST {0}", failures == 0 ? "PASSED." : "FAILED!");
        return 100 + failures;
    }
}

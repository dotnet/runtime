// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;

class RuntimeHelpersTests
{
    static int Main(string[] args)
    {
        try
        {
            ExecuteCodeWithGuaranteedCleanupTest.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }

        return 100;
    }
}

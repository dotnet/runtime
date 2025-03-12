// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class InterpreterTest
{
    static int Main(string[] args)
    {
        RunInterpreterTests();
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunInterpreterTests()
    {
//        Console.WriteLine("Run interp tests");
    }

}

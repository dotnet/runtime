// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Main()
    {
        string stackTrace = Environment.StackTrace;

        Console.WriteLine($"Current stack trace: {stackTrace}");

#if STRIPPED
        const bool expected = false;
#else
        const bool expected = true;
#endif
        bool actual = stackTrace.Contains(nameof(Main)) && stackTrace.Contains(nameof(Program));

        int exitCode = expected == actual ? 100 : 1;

        Console.WriteLine($"Exit code: {exitCode}");

        return exitCode;
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    static int TestEntryPoint()
    {
        string stackTrace = Environment.StackTrace;

        Console.WriteLine(stackTrace);

#if STRIPPED
        const bool expected = false;
#else
        const bool expected = true;
#endif
        bool actual = stackTrace.Contains(nameof(Main)) && stackTrace.Contains(nameof(Program));
        return expected == actual ? 100 : 1;
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

public static class BasicTest
{
    private static int Main()
    {
        const int Pass = 100;

        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
        ProfileOptimization.StartProfile("profile.mcj");

        // Record a method
        Foo();

        // Let the multi-core JIT recorder time out. The timeout is set to 1 s in the test project.
        Thread.Sleep(2000);

        // Stop the profile again after timeout (just verifying that it works)
        ProfileOptimization.StartProfile(null);
        return Pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo()
    {
    }
}

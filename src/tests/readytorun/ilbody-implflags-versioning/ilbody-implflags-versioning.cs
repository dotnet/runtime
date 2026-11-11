// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// This assembly is compiled with cross-module inlining against V1 of ImplFlagHelper.dll and runs against V2,
// whose methods have the same IL bodies but different runtime-async or synchronized impl flags. Code
// that inlined the V1 bodies must be rejected, so every result below must reflect the V2 semantics.
public class ILBodyImplFlagsVersioning
{
    static bool s_failed;

    static void Check(string testName, bool passed)
    {
        if (!passed)
        {
            Console.WriteLine($"  FAILED: {testName}");
            s_failed = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<object> AwaitBecomesAsync() => await ImplFlagHelper.BecomesAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Task<object> CallBecomesAsync() => ImplFlagHelper.BecomesAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<object> AwaitStopsBeingAsync() => await ImplFlagHelper.StopsBeingAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CallBecomesSynchronized() => ImplFlagHelper.BecomesSynchronized();

    static async Task RunTests()
    {
        Check("AwaitBecomesAsync", await AwaitBecomesAsync() is Task<object>);
        Check("CallBecomesAsync", await CallBecomesAsync() is Task<object>);
        Check("AwaitStopsBeingAsync", await AwaitStopsBeingAsync() is null);
        Check("CallBecomesSynchronized", CallBecomesSynchronized());
    }

    public static int Main()
    {
        RunTests().GetAwaiter().GetResult();

        Console.WriteLine(s_failed ? "FAILED" : "PASSED");
        return s_failed ? 1 : 100;
    }
}

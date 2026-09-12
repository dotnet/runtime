// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

public static class UnresolvedPInvokeTests
{
    private static bool s_enteredMissingPInvokeMethod;

    [Fact]
    public static void MissingPInvokeThrowsWhenCalled()
    {
        s_enteredMissingPInvokeMethod = false;

        Assert.Throws<DllNotFoundException>(CallMissingPInvoke);
        Assert.True(s_enteredMissingPInvokeMethod);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallMissingPInvoke()
    {
        s_enteredMissingPInvokeMethod = true;
        MissingPInvoke();
    }

    [DllImport("UnresolvedPInvokeTests_MissingNativeLibrary")]
    private static extern void MissingPInvoke();
}

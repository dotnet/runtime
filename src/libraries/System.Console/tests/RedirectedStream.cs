// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class RedirectedStream : RemoteExecutorTestBase
{
    [Fact] // the CI system redirects stdout, so we can only really test the redirected behavior.
    public static void InputRedirect()
    {
        RunRemote(() => { Assert.True(Console.IsInputRedirected); return 42; }, new ProcessStartInfo() { RedirectStandardInput = true });
    }

    [Fact]
    public static void OutputRedirect() // the CI system redirects stdout, so we can only really test the redirected behavior.
    {
        RunRemote(() => { Assert.True(Console.IsOutputRedirected); return 42; }, new ProcessStartInfo() { RedirectStandardOutput = true });
    }

    [Fact]
    public static void ErrorRedirect() // the CI system redirects stdout, so we can only really test the redirected behavior.
    {
        RunRemote(() => { Assert.True(Console.IsErrorRedirected); return 42; }, new ProcessStartInfo() { RedirectStandardError = true });
    }

    [Fact]
    public static void InvokeRedirected()
    {
        // We can't be sure of the state of stdin/stdout/stderr redirects, so we can't validate
        // the results of the Redirected properties one way or the other, but we can at least
        // invoke them to ensure that no exceptions are thrown.
        bool result;
        result = Console.IsInputRedirected;
        result = Console.IsOutputRedirected;
        result = Console.IsErrorRedirected;
    }

    //[Fact] // the CI system redirects stdout, so we can only really test the redirected behavior.
    public static void CheckNonRedirectedBehavior()
    {
        Assert.False(Console.IsInputRedirected);
        Assert.False(Console.IsOutputRedirected);
        Assert.False(Console.IsErrorRedirected);
    }

    private static void RunRemote(Func<int> func, ProcessStartInfo psi = null)
    {
        using (var remote = RemoteInvoke(func, true, psi)) { }
    }
}

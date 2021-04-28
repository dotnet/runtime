// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
public class RedirectedStream
{
    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))] // the CI system redirects stdout, so we can only really test the redirected behavior.
    public static void InputRedirect()
    {
        RunRemote(() => { Assert.True(Console.IsInputRedirected); return 42; }, new ProcessStartInfo() { RedirectStandardInput = true });
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    public static void OutputRedirect() // the CI system redirects stdout, so we can only really test the redirected behavior.
    {
        RunRemote(() => { Assert.True(Console.IsOutputRedirected); return 42; }, new ProcessStartInfo() { RedirectStandardOutput = true });
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

    private static void RunRemote(Func<int> func, ProcessStartInfo psi = null)
    {
        var options = new RemoteInvokeOptions();
        if (psi != null)
        {
            options.StartInfo = psi;
        }

        RemoteExecutor.Invoke(func, options).Dispose();
    }
}

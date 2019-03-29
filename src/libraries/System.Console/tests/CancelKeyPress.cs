// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class CancelKeyPressTests
{
    private const int WaitFailTestTimeoutSeconds = 30;

    [Fact]
    public static void CanAddAndRemoveHandler()
    {
        ConsoleCancelEventHandler handler = (sender, e) =>
        {
            // We don't actually want to do anything here.  This will only get called on the off chance
            // that someone CTRL+C's the test run while the handler is hooked up.  This is just used to 
            // validate that we can add and remove a handler, we don't care about exercising it.
        };
        Console.CancelKeyPress += handler;
        Console.CancelKeyPress -= handler;
    }

    [Fact]
    public void CanAddAndRemoveHandler_Remote()
    {
        // xunit registers a CancelKeyPress handler at the beginning of the test run and never 
        // unregisters it, thus we can't execute all of the removal code in the same process.
        RemoteExecutor.Invoke(() =>
        {
            CanAddAndRemoveHandler();
            CanAddAndRemoveHandler(); // add and remove again
            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }
}

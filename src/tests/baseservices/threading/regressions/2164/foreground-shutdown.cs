// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using Xunit;

/*
 * Issue description:
  Running foreground threads do not prevent runtime shutdown
  on return from main
*/

public class Test_foreground_shutdown
{
    [Fact]
    public static int TestEntryPoint()
    {
        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Environment.Exit(100);
        }).Start();

        // foreground thread created above prevents
        // runtime shutdown and non-100 exit code propagation
        return 101;
    }
}

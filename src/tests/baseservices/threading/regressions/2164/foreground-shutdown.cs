// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

/*
 * Issue description:
  Running foreground threads do not prevent runtime shutdown
  on return from main

Change description:
  For CoreCLR: introduce BOOL waitForOtherThreads parameter
  to Assembly::ExecuteMainMethod and exit conditionally;
  For CoreRT aka NativeAOT: implement missing logic
*/

public class Test
{
    public static int Main()
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

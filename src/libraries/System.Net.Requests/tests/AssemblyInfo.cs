// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Net.Requests is not supported on Browser.")]

class AbortBeforeTimeout
{
    [ModuleInitializer]
    public static void Initialize()
    {
        if (OperatingSystem.IsBrowser())
            return;

        Thread t = new Thread(() => { Thread.Sleep(10 * 60 * 1000); Environment.FailFast("Early Timeout"); });
        t.IsBackground = true;
        t.Start();
    }
}

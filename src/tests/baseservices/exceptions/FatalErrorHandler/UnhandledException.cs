// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

unsafe public class UnhandledException
{
    [UnmanagedCallersOnly]
    static int Handler(int i, void* ptr)
    {
        Environment.Exit(100);

        // unreachable
        return 42;
    }

    public static int Main()
    {
        // set handler
        ExceptionHandling.SetFatalErrorHandler((delegate* unmanaged<int, void*, int>)&Handler);

        // throw
        Environment.FailFast("hello");

        return 42;
    }
}

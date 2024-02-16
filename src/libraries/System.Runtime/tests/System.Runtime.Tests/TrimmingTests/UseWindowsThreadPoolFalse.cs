// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Ensures setting UseWindowsThreadPool = false still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // SetMinThreads should work when using PortableThreadPool, this call should return true
        if (!ThreadPool.SetMinThreads(1, 1))
        {
            return -1;
        }

        // SetMaxThreads should work when using PortableThreadPool, this call should return true
        if (!ThreadPool.SetMaxThreads(10, 10))
        {
            return -1;
        }

        return 100;
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Ensures setting UseWindowsThreadPool = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // SetMinThreads is not supported in WindowsThreadPool class, this call should return false
        if (ThreadPool.SetMinThreads(1, 1))
        {
            return -1;
        }

        // SetMaxThreads is not supported in WindowsThreadPool class, this call should return false
        if (ThreadPool.SetMaxThreads(10, 10))
        {
            return -1;
        }

        return 100;
    }
}
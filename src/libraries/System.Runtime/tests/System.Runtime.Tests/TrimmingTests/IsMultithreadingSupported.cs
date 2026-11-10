// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

class Program
{
    static int Main()
    {
        bool threadStarted;
        try
        {
            var thread = new Thread(static () => { });
            thread.Start();
            // Do not join: blocking is not supported on the browser's main thread.
            threadStarted = true;
        }
        catch (PlatformNotSupportedException)
        {
            threadStarted = false;
        }

        if (RuntimeFeature.IsMultithreadingSupported != threadStarted)
        {
            Console.WriteLine($"IsMultithreadingSupported is {RuntimeFeature.IsMultithreadingSupported}, but starting a thread succeeded: {threadStarted}.");
            return -1;
        }

        return 100;
    }
}

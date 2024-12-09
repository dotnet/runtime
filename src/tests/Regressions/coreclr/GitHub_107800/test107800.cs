// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static int Main()
    {
        Task.Run(() =>
        {
            for (; ; )
            {
                GC.Collect();
            }
        });

        Thread.Sleep(10);
        Environment.Exit(100);

        throw new Exception("UNREACHABLE");
    }
}

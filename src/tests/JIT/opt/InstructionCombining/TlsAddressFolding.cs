// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class TlsAddressFolding
{
    [ThreadStatic]
    private static int s_marker;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadThreadId() => Environment.CurrentManagedThreadId;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckThread(int marker)
    {
        int expectedId = Thread.CurrentThread.ManagedThreadId;
        s_marker = marker;
        for (int i = 0; i < 100; i++)
        {
            if ((ReadThreadId() != expectedId) || (s_marker != marker))
            {
                return false;
            }
        }
        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (!CheckThread(123))
        {
            return 101;
        }

        int mainId = ReadThreadId();
        bool workerPassed = false;
        Thread worker = new Thread(() =>
        {
            workerPassed = (s_marker == 0) && (ReadThreadId() != mainId) && CheckThread(456);
        });
        worker.Start();
        worker.Join();

        return workerPassed && (s_marker == 123) && (ReadThreadId() == mainId) ? 100 : 102;
    }
}

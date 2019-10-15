// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Threading;

public class ThreadData
{
    public AutoResetEvent autoEvent;
    public int initialAllocation;
    public int result;

    public unsafe void Run()
    {
        autoEvent.WaitOne();
        int* buffer = stackalloc int[initialAllocation];
        int last = initialAllocation - 1;
        buffer[0] = 100;
        buffer[last] = 200;

        for (int i = 2; i < 1026; i++)
        {
            result += AllocateMore(i);
        }

        result += buffer[last] - buffer[0];
    }

    public unsafe int AllocateMore(int n)
    {
        int* buffer = stackalloc int[n];
        int last = n - 1;
        buffer[0] = n + 1;
        buffer[last] = n - 1;
        return buffer[last] - buffer[0] + 2;
    }
}

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    public static bool RunTest(int n)
    {
        ThreadData data = new ThreadData();
        data.autoEvent = new AutoResetEvent(false);
        data.initialAllocation = n;

        Thread t = new Thread(data.Run);
        t.Start();
        if (!t.IsAlive)
        {
            return false;
        }
        data.autoEvent.Set();
        t.Join();
        bool ok = data.result == 100;
        return ok;
    }

    public static int Main()
    {
        for (int j = 2; j < 1024 * 100; j += 331)
        {
            bool b = RunTest(j);
            if (!b) return Fail;
        }
        return Pass;
    }
}

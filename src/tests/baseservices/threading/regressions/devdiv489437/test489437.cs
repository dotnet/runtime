// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

/*
 * Issue description:
  If a SemaphoreSlim.WaitAsync call is cancelled just after Release has
  caused it to start to complete, we end up decrementing the semaphore's
  count *and* cancelling the task.  The caller does not know the count has
  been decremented, because the call appears to fail.  This leads to
  deadlock later, because the caller has no reason to believe it should
  release the erroneously-acquired count.

Change description:
  If the operation has already begun completing successfully, do not
  cancel the associated Task.
*/

public class Test
{
    [Fact]
    public static int TestEntryPoint()
    {
        SemaphoreSlim s = new SemaphoreSlim(initialCount: 1);

        var cts = new CancellationTokenSource();
        s.Wait();
        var t = s.WaitAsync(cts.Token);
        s.Release();
        cts.Cancel();


        if (t.Status != TaskStatus.Canceled && s.CurrentCount == 0)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            Console.WriteLine("Expected task status to not be Canceled and s.CurrentCount == 0");
            Console.WriteLine("Actual: Task: " + t.Status + "; CurrentCount: " + s.CurrentCount);
            return 101;
        }


    }
}


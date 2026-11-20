// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading;

internal sealed partial class LowLevelLifoSemaphore
{
    private bool WaitSlow(int timeoutMs)
    {
        RuntimeFeature.ThrowIfBlockingIsNotSupported(timeoutMs);

        int spinsRemaining = Environment.IsSingleProcessor ? 0 : _maxSpinCount;

        uint iteration = 0;
        while (spinsRemaining > 0)
        {
            spinsRemaining -= Backoff.Exponential(iteration++);

            Counts counts = _separated._counts;
            if (counts.SignalCount != 0)
            {
                Counts newCounts = counts;
                newCounts.DecrementSignalCount();
                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    // we've consumed a signal
                    return true;
                }
            }
        }

        return WaitNoSpin(timeoutMs);
    }
}

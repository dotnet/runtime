// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// A class that limits operations to a maximum count per second.
    /// </summary>
    internal sealed class RateLimiter
    {
        private readonly int _maxOperationsPerSecond;
        private readonly Stopwatch _stopwatch;
        private readonly long _ticksPerSecond;
        private int _currentOperationCount;
        private long _intervalStartTicks;

        /// <summary>
        /// Initializes a new instance of the RateLimiter class.
        /// </summary>
        /// <param name="maxOperationsPerSecond">Maximum number of operations allowed per second.</param>
        internal RateLimiter(int maxOperationsPerSecond)
        {
            Debug.Assert(maxOperationsPerSecond > 0, "maxOperationsPerSecond must be greater than 0");

            _maxOperationsPerSecond = maxOperationsPerSecond;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _intervalStartTicks = _stopwatch.ElapsedTicks;
            _currentOperationCount = 0;
            _ticksPerSecond = Stopwatch.Frequency;
        }

        /// <summary>
        /// Tries to perform an operation if the rate limit allows it.
        /// </summary>
        /// <returns>True if the operation is allowed within the rate limit, otherwise false.</returns>
        internal bool TryAcquire()
        {
            do
            {
                long currentTicks = _stopwatch.ElapsedTicks;
                long intervalStartTicks = Interlocked.Read(ref _intervalStartTicks);
                int currentOperationCount = Volatile.Read(ref _currentOperationCount);
                long elapsedTicks = currentTicks - intervalStartTicks;

                // If a second has elapsed, reset the counter
                if (elapsedTicks >= _ticksPerSecond)
                {
                    if (Interlocked.CompareExchange(ref _currentOperationCount, 1, currentOperationCount) != currentOperationCount)
                    {
                        // Another thread has already reset the counter, so we need to check again
                        continue;
                    }

                    // Update the _intervalStartTicks if no-one else updated it in the meantime.
                    Interlocked.CompareExchange(ref _intervalStartTicks, currentTicks, intervalStartTicks);

                    return true; // Allow the operation
                }

                return Interlocked.Increment(ref _currentOperationCount) <= _maxOperationsPerSecond;
            } while (true);
        }
    }
}

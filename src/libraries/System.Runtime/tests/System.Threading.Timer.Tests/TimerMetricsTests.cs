// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class TimerMetricsTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CountTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int TimersPerThread = 64;
                int processorCount = Environment.ProcessorCount;
                int totalTimerCount = processorCount * TimersPerThread;

                var timers = new List<Timer>(totalTimerCount);
                TimerCallback timerCallback = _ => { };
                var startCreateTimerThreads = new ManualResetEvent(false);
                Action createTimerThreadStart = () =>
                {
                    startCreateTimerThreads.WaitOne();
                    for (int i = 0; i < TimersPerThread; ++i)
                    {
                        lock (timers)
                        {
                            timers.Add(
                                new Timer(
                                    timerCallback,
                                    null,
                                    ThreadTestHelpers.UnexpectedTimeoutMilliseconds,
                                    ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                            Assert.True(Timer.ActiveCount >= timers.Count);
                        }
                    }
                };
                var waitsForThread = new Action[processorCount];
                for (int i = 0; i < processorCount; ++i)
                {
                    Thread t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], createTimerThreadStart);
                    t.IsBackground = true;
                    t.Start();
                }

                startCreateTimerThreads.Set();
                foreach (Action waitForThread in waitsForThread)
                {
                    waitForThread();
                }

                // To leave some room for unknown timers to be scheduled and removed, remove a large number of timers at a time and
                // verify that the timer count has decreased
                while (timers.Count > 0)
                {
                    long timerCountBeforeRemove = Timer.ActiveCount;
                    int endIndex = timers.Count - processorCount * 8;
                    for (int i = timers.Count - 1; i >= Math.Max(0, endIndex); --i)
                    {
                        timers[i].Dispose();
                        timers.RemoveAt(i);
                    }

                    if (endIndex >= 0)
                    {
                        Assert.True(Timer.ActiveCount < timerCountBeforeRemove);
                    }
                }
            }).Dispose();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class EtwTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestEtw()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("System.Threading.SynchronizationEventSource", EventLevel.Verbose))
                {
                    const int BarrierPhaseFinishedId = 3;
                    const int ExpectedInvocations = 5;
                    int eventsRaised = 0;
                    int delegateInvocations = 0;
                    listener.RunWithCallback(ev => {
                            Assert.Equal(expected: BarrierPhaseFinishedId, actual: ev.EventId);
                            eventsRaised++;
                        },
                        () => {
                            Barrier b = new Barrier(1, _ => delegateInvocations++);
                            for (int i = 0; i < ExpectedInvocations; i++)
                                b.SignalAndWait();
                        });
                    Assert.Equal(ExpectedInvocations, delegateInvocations);
                    Assert.Equal(ExpectedInvocations, eventsRaised);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnMono("WaitHandleWait event is not (yet?) implemented in mono")]
        public void WaitHandleWaitEventTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                const string providerName = "Microsoft-Windows-DotNETRuntime";
                const EventKeywords waitHandleKeyword = (EventKeywords)0x40000000000;
                const int waitHandleWaitStartEventId = 301;
                const int waitHandleWaitStopEventId = 302;

                List<object[]> startPayloads = new();
                List<object[]> stopPayloads = new();
                const int waitCount = 100;

                using TestEventListener listener = new();
                listener.AddSource(providerName, EventLevel.Verbose, waitHandleKeyword);

                ManualResetEventSlim mres = new(false);

                listener.RunWithCallback(eventData =>
                {
                    var payload = new object[eventData.Payload!.Count];
                    eventData.Payload.CopyTo(payload, 0);

                    if (eventData.EventId == waitHandleWaitStartEventId)
                    {
                        startPayloads.Add(payload);
                    }
                    else if (eventData.EventId == waitHandleWaitStopEventId)
                    {
                        stopPayloads.Add(payload);
                    }

                    if (startPayloads.Count >= waitCount && stopPayloads.Count >= waitCount)
                    {
                        mres.Set();
                    }
                }, () =>
                {
                    object l = new();
                    Monitor.Enter(l);
                    for (int i = 0; i < waitCount; i += 1)
                    {
                        bool reacquired = Monitor.Wait(l, millisecondsTimeout: 5);
                        Assert.False(reacquired);
                    }

                    bool set = mres.Wait(TimeSpan.FromSeconds(30));
                    Assert.True(set, $"Not enough WaitHandleWait events were collected ({startPayloads.Count} + {stopPayloads.Count})");
                });

                // Avoid exact equality because some random thread could be waiting and so sending events.
                Assert.True(startPayloads.Count >= waitCount, $"Unexpected number of start events ({startPayloads.Count})");
                foreach (object[] payload in startPayloads)
                {
                    Assert.Equal(3, payload.Length);
                    Assert.IsType<byte>(payload[0]);
                    Assert.IsType<nint>(payload[1]);
                    Assert.NotEqual(0, payload[1]);
                    Assert.IsType<ushort>(payload[2]);
                }

                Assert.True(stopPayloads.Count >= waitCount, $"Unexpected number of stop events ({stopPayloads.Count})");
                foreach (object[] payload in stopPayloads)
                {
                    Assert.Equal(1, payload.Length);
                    Assert.IsType<ushort>(payload[0]);
                }
            }).Dispose();
        }
    }
}

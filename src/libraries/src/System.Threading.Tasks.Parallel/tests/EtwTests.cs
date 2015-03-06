// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using Xunit;

namespace System.Threading.Tasks.Test.Unit
{
    public class EtwTests
    {
        [Fact]
        public void TestEtw()
        {
            using (var listener = new TestEventListener("System.Threading.Tasks.Parallel.EventSource", EventLevel.Verbose))
            {
                var events = new ConcurrentQueue<int>();
                listener.RunWithCallback(ev => events.Enqueue(ev.EventId), () => {
                    Parallel.For(0, 10000, i => { });

                    var barrier = new Barrier(2);
                    Parallel.Invoke(
                        () => barrier.SignalAndWait(),
                        () => barrier.SignalAndWait());
                });

                const int BeginLoopEventId = 1;
                const int BeginInvokeEventId = 3;
                Assert.Equal(expected: 1, actual: events.Count(i => i == BeginLoopEventId));
                Assert.Equal(expected: 1, actual: events.Count(i => i == BeginInvokeEventId));

                const int EndLoopEventId = 2;
                const int EndInvokeEventId = 4;
                Assert.Equal(expected: 1, actual: events.Count(i => i == EndLoopEventId));
                Assert.Equal(expected: 1, actual: events.Count(i => i == EndInvokeEventId));

                const int ForkEventId = 5;
                const int JoinEventId = 6;
                Assert.True(events.Count(i => i == ForkEventId) >= 1);
                Assert.Equal(events.Count(i => i == ForkEventId), events.Count(i => i == JoinEventId));
            }
        }
    }
}

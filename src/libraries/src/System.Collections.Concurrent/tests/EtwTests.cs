// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class EtwTests
    {
        [Fact]
        public void TestEtw()
        {
            using (var listener = new TestEventListener("System.Collections.Concurrent.ConcurrentCollectionsEventSource", EventLevel.Verbose))
            {
                var events = new ConcurrentQueue<int>();

                const int AcquiringAllLocksEventId = 3;
                Clear(events);
                listener.RunWithCallback(ev => events.Enqueue(ev.EventId), () =>
                {
                    var cd = new ConcurrentDictionary<int, int>();
                    cd.TryAdd(1, 1);
                    cd.Clear();
                });
                Assert.True(events.Count(i => i == AcquiringAllLocksEventId) > 0);

                const int TryTakeStealsEventId = 4;
                const int TryPeekStealsEventId = 5;
                Clear(events);
                listener.RunWithCallback(ev => events.Enqueue(ev.EventId), () =>
                {
                    var cb = new ConcurrentBag<int>();
                    int item;
                    cb.TryPeek(out item);
                    cb.TryTake(out item);
                });
                Assert.True(events.Count(i => i == TryPeekStealsEventId) > 0);
                Assert.True(events.Count(i => i == TryTakeStealsEventId) > 0);

                // No tests for:
                //      CONCURRENTSTACK_FASTPUSHFAILED_ID
                //      CONCURRENTSTACK_FASTPOPFAILED_ID
                // These require certain race condition interleavings in order to fire.
            }
        }

        private static void Clear<T>(ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item)) ;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class EtwTests
    {
        [Fact]
        public static void TestEtw()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener(new Guid("159eeeec-4a14-4418-a8fe-faabcd987887"), EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<int>();
                    listener.RunWithCallback(ev => events.Enqueue(ev.EventId), () =>
                    {
                        Enumerable.Range(0, 10000).AsParallel().Select(i => i).ToArray();
                    });

                    const int BeginEventId = 1;
                    Assert.Equal(expected: 1, actual: events.Count(i => i == BeginEventId));

                    const int EndEventId = 2;
                    Assert.Equal(expected: 1, actual: events.Count(i => i == EndEventId));

                    const int ForkEventId = 3;
                    const int JoinEventId = 4;
                    Assert.True(events.Count(i => i == ForkEventId) > 0);
                    Assert.Equal(events.Count(i => i == ForkEventId), events.Count(i => i == JoinEventId));
                }
            }).Dispose();
        }
    }
}

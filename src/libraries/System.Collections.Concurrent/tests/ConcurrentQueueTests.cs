// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class ConcurrentQueueTests
    {
        [Fact]
        public static void TestConcurrentQueueBasic()
        {
            ConcurrentQueue<int> cq = new ConcurrentQueue<int>();
            cq.Enqueue(1);

            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                cq.Enqueue(2);
                cq.Enqueue(3);
                cq.Enqueue(4);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            tks[1] = Task.Factory.StartNew(() =>
            {
                int item1;
                var ret1 = cq.TryDequeue(out item1);
                int item2;
                var ret2 = cq.TryDequeue(out item2);
                // at least one item
                Assert.True(ret1);
                // two item
                if (ret2)
                {
                    Assert.True(item1 < item2, String.Format("{0} should less than {1}", item1, item2));
                }
                else // one item
                {
                    Assert.Equal(1, item1);
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }
    }
}

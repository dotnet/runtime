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
    /// <summary>The class that contains the unit tests of the LazyInit.</summary>
    public class ConcurrentBagTests
    {
        [Fact]
        public static void TestConcurrentBagBasic()
        {
            ConcurrentBag<int> cb = new ConcurrentBag<int>();
            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                cb.Add(4);
                cb.Add(5);
                cb.Add(6);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            // Consume the items in the bag 
            tks[1] = Task.Factory.StartNew(() =>
            {
                int item;
                while (!cb.IsEmpty)
                {
                    bool ret = cb.TryTake(out item);
                    Assert.True(ret);
                    // loose check
                    if (item != 4 && item != 5 && item != 6)
                    {
                        Assert.False(true, "Expected: 4|5|6; actual: " + item.ToString());
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }
    }
}

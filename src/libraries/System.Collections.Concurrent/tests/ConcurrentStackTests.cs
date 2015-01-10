// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class ConcurrentStackTests
    {
        [Fact]
        public static void TestConcurrentStackBasic()
        {
            ConcurrentStack<int> cs = new ConcurrentStack<int>();
            cs.Push(1);

            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                cs.Push(2);
                cs.Push(3);
                cs.Push(4);
            });

            tks[1] = Task.Factory.StartNew(() =>
            {
                int item1;
                var ret1 = cs.TryPop(out item1);
                int item2;
                var ret2 = cs.TryPop(out item2);
                // at least one item
                Assert.True(ret1);
                // two item
                if (ret2)
                {
                    Assert.Equal(item1 - 1, item2);
                }
                else // one item
                {
                    Assert.Equal(1, item1);
                }
            });

            Task.WaitAll(tks);
        }
    }
}

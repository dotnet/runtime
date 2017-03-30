// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public partial class InterlockedTests
    {
        [Fact]
        public void IncrementDecrement_int()
        {
            List<Task> threads = new List<Task>();
            int count = 0;
            for (int i = 0; i < 10000; i++)
            {
                threads.Add(Task.Run(() => Interlocked.Increment(ref count)));
                threads.Add(Task.Run(() => Interlocked.Decrement(ref count)));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Equal(0, count);
        }

        [Fact]
        public void IncrementDecrement_long()
        {
            List<Task> threads = new List<Task>();
            long count = 0;
            for (int i = 0; i < 10000; i++)
            {
                threads.Add(Task.Run(() => Interlocked.Increment(ref count)));
                threads.Add(Task.Run(() => Interlocked.Decrement(ref count)));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Equal(0, count);
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class ParallelTests
    {
        // The behavior of APIs like Invoke depends on how many items they are asked to invoke
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(32)]
        [InlineData(250)]
        public static void ParallelInvokeActionArray(int count)
        {
            var actions = new List<Action>();
            int sum = 0, expected = 0;
            for (int i = 0; i < count; i++) {
                int j = i;
                actions.Add(() => {
                    sum += j;
                });
                expected += j;
            }

            Parallel.Invoke(actions.ToArray());
            Assert.Equal(expected, sum);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(250)]
        public static void ParallelFor(int count)
        {
            int sum = 0, expected = 0;
            for (int i = 0; i < count; i++)
                expected += i;
            Parallel.For(0, count, (i) => { sum += i; });
            Assert.Equal(expected, sum);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(250)]
        public static void ParallelForEach(int count)
        {
            int sum = 0, expected = 0;
            var items = new List<int>();
            for (int i = 0; i < count; i++) {
                items.Add(i);
                expected += i;
            }
            Parallel.ForEach(items, (i) => { sum += i; });
            Assert.Equal(expected, sum);
        }
    }
}
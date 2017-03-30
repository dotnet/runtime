// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class SelectManyTests : EnumerableTests
    {
        [Theory]
        [InlineData(10)]
        public void EvaluateSelectorOncePerItem(int count)
        {
            int[] timesCalledMap = new int[count];

            IEnumerable<int> source = Enumerable.Range(0, 10);
            IEnumerable<int> iterator = source.SelectMany(index =>
            {
                timesCalledMap[index]++;
                return new[] { index };
            });
            
            // Iteration
            foreach (int index in iterator)
            {
                Assert.Equal(Enumerable.Repeat(1, index + 1), timesCalledMap.Take(index + 1));
                Assert.Equal(Enumerable.Repeat(0, timesCalledMap.Length - index - 1), timesCalledMap.Skip(index + 1));
            }

            Array.Clear(timesCalledMap, 0, timesCalledMap.Length);

            // ToArray
            iterator.ToArray();
            Assert.Equal(Enumerable.Repeat(1, timesCalledMap.Length), timesCalledMap);

            Array.Clear(timesCalledMap, 0, timesCalledMap.Length);

            // ToList
            iterator.ToList();
            Assert.Equal(Enumerable.Repeat(1, timesCalledMap.Length), timesCalledMap);

            Array.Clear(timesCalledMap, 0, timesCalledMap.Length);

            // ToHashSet
            iterator.ToHashSet();
            Assert.Equal(Enumerable.Repeat(1, timesCalledMap.Length), timesCalledMap);
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Linq.Expressions.Tests
{
    /// <summary>Forces tests to be carried out according to the order of their <see cref="TestOrderAttribute.Order"/>, with
    /// those tests with no attribute happening in the same batch as those with an Order of zero.</summary>
    internal class TestOrderer : ITestCaseOrderer
    {
        IEnumerable<TTestCase> ITestCaseOrderer.OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        {
            Dictionary<int, List<TTestCase>> queue = new Dictionary<int, List<TTestCase>>();
            foreach (TTestCase testCase in testCases)
            {
                var orderAttribute = testCase.TestMethod.Method.GetCustomAttributes(typeof(TestOrderAttribute)).FirstOrDefault();
                int order;
                if (orderAttribute == null || (order = orderAttribute.GetConstructorArguments().Cast<int>().First()) == 0)
                {
                    yield return testCase;
                }
                else
                {
                    List<TTestCase> batch;
                    if (!queue.TryGetValue(order, out batch))
                        queue.Add(order, batch = new List<TTestCase>());
                    batch.Add(testCase);
                }
            }
            foreach (var order in queue.Keys.OrderBy(i => i))
                foreach (var testCase in queue[order])
                    yield return testCase;
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
#if !SINGLE_FILE_TEST_RUNNER
using System.Reflection;
#endif
using Xunit.Sdk;
using Xunit.v3;

namespace System.Linq.Expressions.Tests
{
    /// <summary>Forces tests to be carried out according to the order of their <see cref="TestOrderAttribute.Order"/>, with
    /// those tests with no attribute happening in the same batch as those with an Order of zero.</summary>
    internal class TestOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
        {
#if SINGLE_FILE_TEST_RUNNER
            // In Native AOT, ITestMethod does not expose MethodInfo, so
            // attribute-based ordering is not available.  Return the
            // original collection unmodified.
            return testCases;
#else
            Dictionary<int, List<TTestCase>> queue = new Dictionary<int, List<TTestCase>>();
            List<TTestCase> result = new List<TTestCase>();

            foreach (TTestCase testCase in testCases)
            {
                int order = 0;
                string? className = testCase.TestMethod?.TestClass?.TestClassName;
                string? methodName = testCase.TestMethod?.MethodName;
                if (className is not null && methodName is not null)
                {
                    Type? type = Type.GetType(className);
                    MethodInfo? method = type?.GetMethod(methodName);
                    TestOrderAttribute? orderAttribute = method?.GetCustomAttribute<TestOrderAttribute>();
                    if (orderAttribute is not null)
                    {
                        order = orderAttribute.Order;
                    }
                }

                if (order == 0)
                {
                    result.Add(testCase);
                }
                else
                {
                    if (!queue.TryGetValue(order, out List<TTestCase>? batch))
                        queue.Add(order, batch = new List<TTestCase>());
                    batch.Add(testCase);
                }
            }

            foreach (var orderKey in queue.Keys.OrderBy(i => i))
                foreach (var testCase in queue[orderKey])
                    result.Add(testCase);

            return result;
#endif
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: OptimizeForBenchmarks]

// Constrain tests to execute in a predetermined order
[assembly: TestCaseOrderer("PerfLabTests.DisplayNameTestCaseOrderer", "PerfLab")]

namespace PerfLabTests
{

    public class DisplayNameTestCaseOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
        => testCases.OrderBy(test => test.DisplayName);
    }

}

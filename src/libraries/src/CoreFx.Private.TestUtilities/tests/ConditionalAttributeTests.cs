// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CoreFx.Private.TestUtilities.Tests
{
    [TestCaseOrderer("CoreFx.Private.TestUtilities.Tests.AlphabeticalOrderer", "CoreFx.Private.TestUtilities.Tests")]
    public class ConditionalAttributeTests
    {
        // The tests under this class are to validate that ConditionalFact and ConditionalAttribute tests are being executed correctly
        // In the past we have had cases where infrastructure changes broke this feature and tests where not running in certain framework.
        // This test class is test order dependent so do not rename the tests.
        // If new tests need to be added, follow the same naming pattern ConditionalAttribute{LetterToOrderTest} and then add a Validate{TestName}.

        private static bool s_conditionalFactExecuted;
        private static int s_conditionalTheoryCount;

        public static bool AlwaysTrue => true;

        [ConditionalFact(nameof(AlwaysTrue))]
        public void ConditionalAttributeA()
        {
            s_conditionalFactExecuted = true;
        }

        [ConditionalTheory(nameof(AlwaysTrue))]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ConditionalAttributeB(int _)
        {
            s_conditionalTheoryCount++;
        }

        [Fact]
        public void ValidateConditionalFact()
        {
            Assert.True(s_conditionalFactExecuted);
        }

        [Fact]
        public void ValidateConditionalTheory()
        {
            Assert.Equal(3, s_conditionalTheoryCount);
        }
    }

    // We need this TestCaseOrderer in order to guarantee that the ConditionalAttributeTests are always executed in the same order.
    public class AlphabeticalOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
                where TTestCase : ITestCase
        {
            List<TTestCase> result = testCases.ToList();
            result.Sort((x, y) => StringComparer.Ordinal.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
            return result;
        }
    }
}

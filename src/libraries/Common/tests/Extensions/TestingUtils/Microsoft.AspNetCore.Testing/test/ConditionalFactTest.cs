// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class ConditionalFactTest : IClassFixture<ConditionalFactTest.ConditionalFactAsserter>
    {
        public ConditionalFactTest(ConditionalFactAsserter collector)
        {
            Asserter = collector;
        }

        private ConditionalFactAsserter Asserter { get; }

        [Fact]
        public void TestAlwaysRun()
        {
            // This is required to ensure that the type at least gets initialized.
            Assert.True(true);
        }

        [ConditionalFact(Skip = "Test is always skipped.")]
        public void ConditionalFactSkip()
        {
            Assert.True(false, "This test should always be skipped.");
        }

#if NETCOREAPP2_2
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        public void ThisTestMustRunOnCoreCLR()
        {
            Asserter.TestRan = true;
        }
#elif NET461 || NET46
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public void ThisTestMustRunOnCLR()
        {
            Asserter.TestRan = true;
        }
#else
#error Target frameworks need to be updated.
#endif

        public class ConditionalFactAsserter : IDisposable
        {
            public bool TestRan { get; set; }

            public void Dispose()
            {
                Assert.True(TestRan, "If this assertion fails, a conditional fact wasn't discovered.");
            }
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class AssumeTests
    {
        // At run time, Contract.Assume is the same as Contract.Assert (other than the failure kind).
        // As such, these tests all mirror those in AssertTests.

        [Fact]
        public static void AssertTrueDoesNotRaiseEvent()
        {
            bool eventRaised = false;
            EventHandler<ContractFailedEventArgs> handler = (s, e) =>
            {
                eventRaised = true;
                e.SetHandled();
            };
            using (Utilities.WithContractFailed(handler))
            {
                Contract.Assume(true);
                Assert.False(eventRaised, "ContractFailed event was raised");
            }
        }

        [Fact]
        public static void AssertFalseRaisesEvent()
        {
            bool eventRaised = false;
            EventHandler<ContractFailedEventArgs> handler = (s, e) =>
            {
                eventRaised = true;
                e.SetHandled();
            };
            using (Utilities.WithContractFailed(handler))
            {
                Contract.Assume(false);
#if DEBUG
                Assert.True(eventRaised, "ContractFailed event not raised");
#else
                Assert.False(eventRaised, "ContractFailed event was raised");
#endif
            }
        }

        [Fact]
        public static void AssertFalseThrows()
        {
            bool eventRaised = false;
            EventHandler<ContractFailedEventArgs> handler = (s, e) =>
            {
                eventRaised = true;
                e.SetUnwind();
            };
            using (Utilities.WithContractFailed(handler))
            {
#if DEBUG
                Utilities.AssertThrowsContractException(() => Contract.Assume(false, "Some kind of user message"));
                Assert.True(eventRaised, "ContractFailed was not raised");
#else
                Contract.Assume(false, "Some kind of user message");
                Assert.False(eventRaised, "ContractFailed event was raised");
#endif
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class AssertTests
    {
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
                Contract.Assert(true);
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
                Contract.Assert(false);
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
                Utilities.AssertThrowsContractException(() => Contract.Assert(false, "Some kind of user message"));
                Assert.True(eventRaised, "ContractFailed was not raised");
#else
                Contract.Assert(false, "Some kind of user message");
                Assert.False(eventRaised, "ContractFailed event was raised");
#endif
            }
        }
    }
}

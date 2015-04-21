// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class BasicContractsTest
    {
        [Fact]
        public static void AssertTrueDoesNotRaiseEvent()
        {
            bool eventRaised = false;
            Contract.ContractFailed += (s, e) =>
            {
                eventRaised = true;
                e.SetHandled();
            };

            Contract.Assert(true);

            Assert.False(eventRaised, "ContractFailed event was raised");
        }

        [Fact]
        public static void AssertFalseRaisesEvent()
        {
            bool eventRaised = false;
            Contract.ContractFailed += (s, e) =>
            {
                eventRaised = true;
                e.SetHandled();
            };

            Contract.Assert(false);
#if DEBUG
            Assert.True(eventRaised, "ContractFailed event not raised");            
#else
            Assert.False(eventRaised, "ContractFailed event was raised");
#endif            
        }

        [Fact]
        public static void AssertFalseThrows()
        {
            bool eventRaised = false;
            Contract.ContractFailed += (s, e) =>
            {
                eventRaised = true;
                e.SetUnwind();
            };


#if DEBUG
            Assert.ThrowsAny<Exception>(() =>
            {
                Contract.Assert(false, "Some kind of user message");
            });

            Assert.True(eventRaised, "Event was not raised");
#else
            Contract.Assert(false, "Some kind of user message");
            
            Assert.False(eventRaised, "ContractFailed event was raised");
#endif            
        }
    }
}

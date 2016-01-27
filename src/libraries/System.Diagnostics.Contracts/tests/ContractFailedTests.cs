// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
#if DEBUG
    public class ContractFailedTests
    {
        [Fact]
        public static void ValidArgs()
        {
            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Null(s);
                Assert.NotNull(e);
                e.SetHandled();
            }))
            {
                Contract.Assert(false);
            }

            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Null(s);
                Assert.NotNull(e);
                e.SetHandled();
            }))
            {
                Contract.Assume(false);
            }
        }

        [Fact]
        public static void DefaultEventArgValues()
        {
            string message = "This is the failure message";

            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Null(e.Condition);
                Assert.False(e.Handled);
                Assert.True(e.Message.Contains(message));
                Assert.False(e.Unwind);
                e.SetHandled();
            }))
            {
                Contract.Assert(false, message);
            }

            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Null(e.Condition);
                Assert.False(e.Handled);
                Assert.True(e.Message.Contains(message));
                Assert.False(e.Unwind);
                e.SetHandled();
            }))
            {
                Contract.Assume(false, message);
            }
        }

        [Fact]
        public static void FailureKind()
        {
            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Equal(ContractFailureKind.Assert, e.FailureKind);
                e.SetHandled();
            }))
            {
                Contract.Assert(false);
            }

            using (Utilities.WithContractFailed((s, e) =>
            {
                Assert.Equal(ContractFailureKind.Assume, e.FailureKind);
                e.SetHandled();
            }))
            {
                Contract.Assume(false);
            }
        }

    }
#endif
}

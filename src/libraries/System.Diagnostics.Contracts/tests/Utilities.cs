// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Contract.ContractFailed is non-local; changes in one test will affect any others running concurrently
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace System.Diagnostics.Contracts.Tests
{
    internal static class Utilities
    {
        internal static void AssertThrowsContractException(Action action)
        {
            Exception exc = Assert.ThrowsAny<Exception>(action);
            Assert.Equal("ContractException", exc.GetType().Name);
        }

        internal static IDisposable WithContractFailed(EventHandler<ContractFailedEventArgs> handler)
        {
            Contract.ContractFailed += handler;
            return new UnregisterContractFailed { _handler = handler };
        }

        private class UnregisterContractFailed : IDisposable
        {
            internal EventHandler<ContractFailedEventArgs> _handler;

            public void Dispose() { Contract.ContractFailed -= _handler; }
        }
    }
}
